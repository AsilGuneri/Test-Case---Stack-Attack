using System.Collections.Generic;
using StackAttack.Gameplay;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace StackAttack.Editor
{
    [CustomEditor(typeof(LevelCatalog))]
    public sealed class LevelCatalogEditor : UnityEditor.Editor
    {
        private SerializedProperty _levels;
        private ReorderableList _list;

        private void OnEnable()
        {
            _levels = serializedObject.FindProperty("levels");
            _list = new ReorderableList(
                serializedObject,
                _levels,
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect =>
                    EditorGUI.LabelField(rect, "Level Definitions"),
                drawElementCallback = DrawLevel,
                drawNoneElementCallback = rect =>
                    EditorGUI.LabelField(rect, "No levels registered."),
                onAddCallback = AddEmptyLevel,
                onRemoveCallback = RemoveLevel
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _list.DoLayoutList();
            DrawValidation();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLevel(Rect rect, int index, bool _, bool __)
        {
            var property = _levels.GetArrayElementAtIndex(index);
            var level = property.objectReferenceValue as LevelDefinition;
            var label = level == null
                ? $"Entry {index + 1}"
                : $"Level {level.LevelNumber} — {level.name}";
            EditorGUI.PropertyField(
                rect,
                property,
                new GUIContent(label));
        }

        private void AddEmptyLevel(ReorderableList list)
        {
            var index = _levels.arraySize;
            _levels.InsertArrayElementAtIndex(index);
            _levels.GetArrayElementAtIndex(index).objectReferenceValue = null;
            list.index = index;
            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveLevel(ReorderableList list)
        {
            if (list.index < 0 || list.index >= _levels.arraySize)
            {
                return;
            }

            var previousSize = _levels.arraySize;
            _levels.DeleteArrayElementAtIndex(list.index);
            if (_levels.arraySize == previousSize)
            {
                _levels.DeleteArrayElementAtIndex(list.index);
            }

            list.index = Mathf.Min(list.index, _levels.arraySize - 1);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawValidation()
        {
            var numbers = new HashSet<int>();
            for (var index = 0; index < _levels.arraySize; index++)
            {
                var level = _levels.GetArrayElementAtIndex(index)
                    .objectReferenceValue as LevelDefinition;
                if (level == null)
                {
                    EditorGUILayout.HelpBox(
                        $"Entry {index + 1} needs a Level Definition.",
                        MessageType.Warning);
                    continue;
                }

                if (!numbers.Add(level.LevelNumber))
                {
                    EditorGUILayout.HelpBox(
                        $"Level number {level.LevelNumber} is registered more than once.",
                        MessageType.Error);
                }
            }
        }
    }
}
