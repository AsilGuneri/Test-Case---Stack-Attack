using System;
using StackAttack.Gameplay;
using UnityEditor;
using UnityEngine;

namespace StackAttack.Editor
{
    [CustomEditor(typeof(LevelDefinition))]
    public sealed class LevelDefinitionEditor : UnityEditor.Editor
    {
        private const float CellWidth = 64f;
        private const float CellHeight = 58f;
        private const float CellGap = 3f;
        private const float CellButtonWidth = 25f;
        private const float CellButtonHeight = 18f;
        private const string ClipboardPrefix = "StackAttack.LevelCell:";

        private Vector2 _gridScrollPosition;
        private int _selectedCellIndex = -1;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("levelNumber"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startOffset"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("completionOffset"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("horizontalViewportMargin"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("cellSpacing"),
                new GUIContent(
                    "Cell Spacing",
                    "Fraction of every square grid cell reserved as spacing. " +
                    "For example, 0.05 leaves a 5% gap."));

            var definition = (LevelDefinition)target;
            var requestedSize = EditorGUILayout.Vector2IntField(
                "Grid Size",
                definition.GridSize);
            requestedSize.x = Mathf.Max(1, requestedSize.x);
            requestedSize.y = Mathf.Max(1, requestedSize.y);

            if (requestedSize != definition.GridSize)
            {
                Undo.RecordObject(definition, "Resize Level Grid");
                definition.ResizeGrid(requestedSize);
                EditorUtility.SetDirty(definition);
                serializedObject.Update();
            }

            DrawGroups(definition);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Level Grid", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Left-click a cell to edit it. Right-click a cell to empty it. " +
                "C copies and P pastes amount and color. Runtime traversal " +
                "starts at row Y 0.",
                MessageType.Info);

            DrawGrid(definition);
            DrawSelectedCell(definition);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGrid(LevelDefinition definition)
        {
            var gridSize = definition.GridSize;
            var cellsProperty = serializedObject.FindProperty("cells");
            _gridScrollPosition = EditorGUILayout.BeginScrollView(
                _gridScrollPosition,
                GUILayout.Height(Mathf.Min(
                    430f,
                    52f + (gridSize.y * (CellHeight + CellGap)))));

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(string.Empty, GUILayout.Width(42f));
                for (var x = 0; x < gridSize.x; x++)
                {
                    GUILayout.Label(
                        $"X {x}",
                        EditorStyles.miniLabel,
                        GUILayout.Width(CellWidth + CellGap));
                }
            }

            for (var y = gridSize.y - 1; y >= 0; y--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"Y {y}", EditorStyles.miniLabel, GUILayout.Width(42f));
                    for (var x = 0; x < gridSize.x; x++)
                    {
                        var cellProperty = cellsProperty.GetArrayElementAtIndex(
                            (y * gridSize.x) + x);
                        var amountProperty = cellProperty.FindPropertyRelative(
                            "obstacleAmount");
                        var colorProperty = cellProperty.FindPropertyRelative("color");
                        var groupProperty = cellProperty.FindPropertyRelative("groupId");
                        DrawCell(
                            (y * gridSize.x) + x,
                            amountProperty,
                            colorProperty,
                            groupProperty,
                            definition);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCell(
            int cellIndex,
            SerializedProperty amountProperty,
            SerializedProperty colorProperty,
            SerializedProperty groupProperty,
            LevelDefinition definition)
        {
            var outerRect = GUILayoutUtility.GetRect(
                CellWidth + CellGap,
                CellHeight + CellGap,
                GUILayout.Width(CellWidth + CellGap),
                GUILayout.Height(CellHeight + CellGap));
            var cellRect = new Rect(
                outerRect.x,
                outerRect.y,
                CellWidth,
                CellHeight);

            var hasGroup = definition.TryGetGroup(
                groupProperty.stringValue,
                out var obstacleGroup);
            if (_selectedCellIndex == cellIndex)
            {
                EditorGUI.DrawRect(cellRect, new Color(0.2f, 0.65f, 1f, 1f));
                cellRect = new Rect(
                    cellRect.x + 2f,
                    cellRect.y + 2f,
                    cellRect.width - 4f,
                    cellRect.height - 4f);
            }


            if (hasGroup)
            {
                EditorGUI.DrawRect(cellRect, obstacleGroup.EditorColor);
                cellRect = new Rect(
                    cellRect.x + 3f,
                    cellRect.y + 3f,
                    cellRect.width - 6f,
                    cellRect.height - 6f);
            }

            var isEmpty = amountProperty.intValue <= 0;
            var displayColor = isEmpty
                ? new Color(0.18f, 0.18f, 0.18f, 1f)
                : colorProperty.colorValue;
            EditorGUI.DrawRect(cellRect, displayColor);
            GUI.Box(cellRect, GUIContent.none, EditorStyles.helpBox);

            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    textColor = GetContrastingTextColor(displayColor)
                }
            };
            GUI.Label(
                new Rect(
                    cellRect.x,
                    cellRect.y,
                    cellRect.width,
                    cellRect.height - CellButtonHeight - 2f),
                isEmpty ? "Empty" : amountProperty.intValue.ToString(),
                labelStyle);

            var copyButtonRect = new Rect(
                cellRect.x + 4f,
                cellRect.yMax - CellButtonHeight - 3f,
                CellButtonWidth,
                CellButtonHeight);
            var pasteButtonRect = new Rect(
                cellRect.xMax - CellButtonWidth - 4f,
                copyButtonRect.y,
                CellButtonWidth,
                CellButtonHeight);

            if (GUI.Button(
                    copyButtonRect,
                    new GUIContent("C", "Copy this cell's amount and color"),
                    EditorStyles.miniButton))
            {
                CopyCell(
                    amountProperty,
                    colorProperty,
                    groupProperty,
                    definition);
                _selectedCellIndex = cellIndex;
                Repaint();
            }

            var hasCellClipboard = TryReadClipboard(out var clipboardData);
            using (new EditorGUI.DisabledScope(!hasCellClipboard))
            {
                if (GUI.Button(
                        pasteButtonRect,
                        new GUIContent("P", "Paste amount and color into this cell"),
                        EditorStyles.miniButton))
                {
                    PasteCell(
                        cellIndex,
                        amountProperty,
                        colorProperty,
                        groupProperty,
                        clipboardData,
                        definition);
                }
            }

            var currentEvent = Event.current;
            if (currentEvent.type != EventType.MouseDown ||
                !cellRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            if (currentEvent.button == 0)
            {
                _selectedCellIndex = cellIndex;
                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.button == 1)
            {
                Undo.RecordObject(target, "Empty Level Cell");
                amountProperty.intValue = 0;
                _selectedCellIndex = cellIndex;
                EditorUtility.SetDirty(target);
                currentEvent.Use();
                Repaint();
            }
        }

        private void DrawSelectedCell(LevelDefinition definition)
        {
            var gridSize = definition.GridSize;
            var cellCount = gridSize.x * gridSize.y;
            if (_selectedCellIndex < 0 || _selectedCellIndex >= cellCount)
            {
                EditorGUILayout.HelpBox(
                    "Select a grid cell to edit its stack amount and color.",
                    MessageType.None);
                return;
            }

            var x = _selectedCellIndex % gridSize.x;
            var y = _selectedCellIndex / gridSize.x;
            var cellProperty = serializedObject.FindProperty("cells")
                .GetArrayElementAtIndex(_selectedCellIndex);
            var amountProperty = cellProperty.FindPropertyRelative("obstacleAmount");
            var colorProperty = cellProperty.FindPropertyRelative("color");
            var groupProperty = cellProperty.FindPropertyRelative("groupId");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                $"Selected Cell (X {x}, Y {y})",
                EditorStyles.boldLabel);
            amountProperty.intValue = Mathf.Max(
                0,
                EditorGUILayout.IntField(
                    "Stack Amount",
                    amountProperty.intValue));
            EditorGUILayout.PropertyField(colorProperty, new GUIContent("Obstacle Color"));
            DrawGroupPopup(groupProperty, definition);
        }

        private void DrawGroups(LevelDefinition definition)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Obstacle Groups", EditorStyles.boldLabel);

            var groupsProperty = serializedObject.FindProperty("groups");
            string groupToRemove = null;
            for (var groupIndex = 0;
                 groupIndex < groupsProperty.arraySize;
                 groupIndex++)
            {
                var groupProperty = groupsProperty.GetArrayElementAtIndex(groupIndex);
                var idProperty = groupProperty.FindPropertyRelative("id");
                var nameProperty = groupProperty.FindPropertyRelative("displayName");
                var colorProperty = groupProperty.FindPropertyRelative("editorColor");
                var behavioursProperty = groupProperty.FindPropertyRelative("behaviours");

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(
                            nameProperty,
                            GUIContent.none);
                        EditorGUILayout.PropertyField(
                            colorProperty,
                            GUIContent.none,
                            GUILayout.Width(55f));
                        if (GUILayout.Button(
                                new GUIContent("−", "Delete this obstacle group"),
                                EditorStyles.miniButton,
                                GUILayout.Width(24f)))
                        {
                            groupToRemove = idProperty.stringValue;
                        }
                    }

                    EditorGUILayout.LabelField(
                        "Behaviours",
                        EditorStyles.miniBoldLabel);
                    for (var behaviourIndex = 0;
                         behaviourIndex < behavioursProperty.arraySize;
                         behaviourIndex++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var behaviourProperty =
                                behavioursProperty.GetArrayElementAtIndex(
                                    behaviourIndex);
                            EditorGUILayout.PropertyField(
                                behaviourProperty,
                                GUIContent.none,
                                true);
                            if (GUILayout.Button(
                                    new GUIContent("−", "Remove this behaviour"),
                                    EditorStyles.miniButton,
                                    GUILayout.Width(24f)))
                            {
                                RemoveBehaviour(groupIndex, behaviourIndex);
                                GUIUtility.ExitGUI();
                            }
                        }
                    }

                    if (GUILayout.Button(
                            "Add Behaviour",
                            EditorStyles.miniButton))
                    {
                        ShowAddBehaviourMenu(groupIndex);
                    }
                }
            }

            if (!string.IsNullOrEmpty(groupToRemove))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(definition, "Remove Obstacle Group");
                definition.RemoveGroup(groupToRemove);
                EditorUtility.SetDirty(definition);
                serializedObject.Update();
            }

            if (GUILayout.Button("Add Obstacle Group"))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(definition, "Add Obstacle Group");
                definition.AddGroup($"Group {definition.Groups.Count + 1}");
                EditorUtility.SetDirty(definition);
                serializedObject.Update();
            }
        }

        private static void DrawGroupPopup(
            SerializedProperty groupProperty,
            LevelDefinition definition)
        {
            var groups = definition.Groups;
            var options = new string[groups.Count + 1];
            options[0] = "None";
            var selectedIndex = 0;

            for (var index = 0; index < groups.Count; index++)
            {
                var group = groups[index];
                options[index + 1] = group?.DisplayName ?? "Missing Group";
                if (group != null && group.Id == groupProperty.stringValue)
                {
                    selectedIndex = index + 1;
                }
            }

            var requestedIndex = EditorGUILayout.Popup(
                "Obstacle Group",
                selectedIndex,
                options);
            groupProperty.stringValue = requestedIndex == 0
                ? string.Empty
                : groups[requestedIndex - 1].Id;
        }

        private void ShowAddBehaviourMenu(int groupIndex)
        {
            var menu = new GenericMenu();
            foreach (var type in TypeCache.GetTypesDerivedFrom<
                         ObstacleGroupBehaviourConfig>())
            {
                if (type.IsAbstract || type.IsGenericType)
                {
                    continue;
                }

                var displayName = ObjectNames.NicifyVariableName(
                    type.Name
                        .Replace("Obstacle", string.Empty)
                        .Replace("GroupBehaviourConfig", string.Empty));
                if (GroupContainsBehaviourType(groupIndex, type))
                {
                    menu.AddDisabledItem(new GUIContent(displayName));
                }
                else
                {
                    var capturedType = type;
                    menu.AddItem(
                        new GUIContent(displayName),
                        false,
                        () => AddBehaviour(groupIndex, capturedType));
                }
            }

            menu.ShowAsContext();
        }

        private bool GroupContainsBehaviourType(int groupIndex, Type type)
        {
            var behavioursProperty = serializedObject.FindProperty("groups")
                .GetArrayElementAtIndex(groupIndex)
                .FindPropertyRelative("behaviours");
            for (var index = 0; index < behavioursProperty.arraySize; index++)
            {
                if (behavioursProperty.GetArrayElementAtIndex(index)
                        .managedReferenceValue?.GetType() == type)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddBehaviour(int groupIndex, Type behaviourType)
        {
            Undo.RecordObject(target, "Add Obstacle Group Behaviour");
            serializedObject.Update();
            var behavioursProperty = serializedObject.FindProperty("groups")
                .GetArrayElementAtIndex(groupIndex)
                .FindPropertyRelative("behaviours");
            var newIndex = behavioursProperty.arraySize;
            behavioursProperty.arraySize++;
            behavioursProperty.GetArrayElementAtIndex(newIndex)
                .managedReferenceValue = Activator.CreateInstance(behaviourType);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Repaint();
        }

        private void RemoveBehaviour(int groupIndex, int behaviourIndex)
        {
            Undo.RecordObject(target, "Remove Obstacle Group Behaviour");
            var behavioursProperty = serializedObject.FindProperty("groups")
                .GetArrayElementAtIndex(groupIndex)
                .FindPropertyRelative("behaviours");
            var previousSize = behavioursProperty.arraySize;
            behavioursProperty.DeleteArrayElementAtIndex(behaviourIndex);
            if (behavioursProperty.arraySize == previousSize)
            {
                behavioursProperty.DeleteArrayElementAtIndex(behaviourIndex);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private static Color GetContrastingTextColor(Color background)
        {
            var luminance = (0.2126f * background.r) +
                            (0.7152f * background.g) +
                            (0.0722f * background.b);
            return luminance > 0.55f ? Color.black : Color.white;
        }

        private static string ResolvePastedGroupId(
            CellClipboardData clipboardData,
            LevelDefinition definition)
        {
            if (string.IsNullOrEmpty(clipboardData.groupId))
            {
                return string.Empty;
            }

            if (definition.TryGetGroup(clipboardData.groupId, out var exactGroup))
            {
                return exactGroup.Id;
            }

            var groups = definition.Groups;
            for (var index = 0; index < groups.Count; index++)
            {
                var group = groups[index];
                if (group != null && group.DisplayName == clipboardData.groupName)
                {
                    return group.Id;
                }
            }

            return string.Empty;
        }

        private static void CopyCell(
            SerializedProperty amountProperty,
            SerializedProperty colorProperty,
            SerializedProperty groupProperty,
            LevelDefinition definition)
        {
            definition.TryGetGroup(
                groupProperty.stringValue,
                out var obstacleGroup);
            var clipboardData = new CellClipboardData
            {
                obstacleAmount = Mathf.Max(0, amountProperty.intValue),
                color = colorProperty.colorValue,
                groupId = groupProperty.stringValue,
                groupName = obstacleGroup?.DisplayName
            };
            EditorGUIUtility.systemCopyBuffer =
                ClipboardPrefix + JsonUtility.ToJson(clipboardData);
        }

        private void PasteCell(
            int cellIndex,
            SerializedProperty amountProperty,
            SerializedProperty colorProperty,
            SerializedProperty groupProperty,
            CellClipboardData clipboardData,
            LevelDefinition definition)
        {
            Undo.RecordObject(target, "Paste Level Cell");
            amountProperty.intValue = Mathf.Max(0, clipboardData.obstacleAmount);
            colorProperty.colorValue = clipboardData.color;
            groupProperty.stringValue = ResolvePastedGroupId(
                clipboardData,
                definition);
            _selectedCellIndex = cellIndex;
            EditorUtility.SetDirty(target);
            Repaint();
        }

        private static bool TryReadClipboard(out CellClipboardData clipboardData)
        {
            var clipboard = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipboard) ||
                !clipboard.StartsWith(ClipboardPrefix))
            {
                clipboardData = default;
                return false;
            }

            try
            {
                clipboardData = JsonUtility.FromJson<CellClipboardData>(
                    clipboard.Substring(ClipboardPrefix.Length));
                return true;
            }
            catch (System.ArgumentException)
            {
                clipboardData = default;
                return false;
            }
        }

        [System.Serializable]
        private struct CellClipboardData
        {
            public int obstacleAmount;
            public Color color;
            public string groupId;
            public string groupName;
        }
    }
}
