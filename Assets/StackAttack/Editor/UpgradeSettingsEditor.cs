using System;
using StackAttack.Gameplay;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace StackAttack.Editor
{
    [CustomEditor(typeof(UpgradeSettings))]
    public sealed class UpgradeSettingsEditor : UnityEditor.Editor
    {
        private const float LabelWidth = 190f;

        private SerializedProperty _startingAmmo;
        private SerializedProperty _initialRequirement;
        private SerializedProperty _requirementMultiplier;
        private SerializedProperty _offerCount;
        private ReorderableList _upgradeList;
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;

        private void OnEnable()
        {
            _startingAmmo = serializedObject.FindProperty("startingAmmo");
            _initialRequirement = serializedObject.FindProperty(
                "initialRequiredClearedObstacleCount");
            _requirementMultiplier = serializedObject.FindProperty(
                "requirementMultiplier");
            _offerCount = serializedObject.FindProperty("offerCount");

            var upgrades = serializedObject.FindProperty("upgrades");
            _upgradeList = new ReorderableList(
                serializedObject,
                upgrades,
                true,
                true,
                true,
                true)
            {
                elementHeightCallback = GetUpgradeHeight,
                drawHeaderCallback = DrawUpgradeHeader,
                drawElementCallback = DrawUpgrade
            };
        }

        public override void OnInspectorGUI()
        {
            EnsureStyles();
            serializedObject.Update();

            EditorGUILayout.LabelField("Ammo", _headerStyle);
            DrawProperty(_startingAmmo, "Starting Ammo");

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Progression", _headerStyle);
            DrawProperty(
                _initialRequirement,
                "Initial Cleared Obstacle Count");
            DrawProperty(_requirementMultiplier, "Requirement Multiplier");
            DrawProperty(_offerCount, "Offer Count");
            DrawRequirementPreview();

            EditorGUILayout.Space(8f);
            _upgradeList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawUpgradeHeader(Rect rect)
        {
            EnsureStyles();
            EditorGUI.LabelField(rect, "Upgrades", _headerStyle);
        }

        private void DrawUpgrade(Rect rect, int index, bool _, bool __)
        {
            EnsureStyles();
            var element = _upgradeList.serializedProperty
                .GetArrayElementAtIndex(index);
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var y = rect.y + 3f;
            var target = element.FindPropertyRelative("target");

            DrawProperty(
                new Rect(rect.x, y, rect.width, lineHeight),
                target,
                "Target");
            y += lineHeight + 2f;

            if (target.enumValueIndex == (int)UpgradeTarget.AmmoStat)
            {
                DrawProperty(
                    new Rect(rect.x, y, rect.width, lineHeight),
                    element.FindPropertyRelative("ammo"),
                    "Ammo");
                y += lineHeight + 2f;
                DrawProperty(
                    new Rect(rect.x, y, rect.width, lineHeight),
                    element.FindPropertyRelative("stat"),
                    "Stat");
                y += lineHeight + 2f;
            }

            DrawProperty(
                new Rect(rect.x, y, rect.width, lineHeight),
                element.FindPropertyRelative("amount"),
                "Increase Amount");
            y += lineHeight + 2f;
            DrawProperty(
                new Rect(rect.x, y, rect.width, lineHeight),
                element.FindPropertyRelative("maximumValue"),
                target.enumValueIndex == (int)UpgradeTarget.MaximumHealth
                    ? "Maximum Hearts"
                    : "Maximum Value");
        }

        private float GetUpgradeHeight(int index)
        {
            var element = _upgradeList.serializedProperty
                .GetArrayElementAtIndex(index);
            var isAmmo = element.FindPropertyRelative("target").enumValueIndex ==
                         (int)UpgradeTarget.AmmoStat;
            var lineCount = isAmmo ? 5 : 3;
            return EditorGUIUtility.singleLineHeight * lineCount +
                   (lineCount - 1) * 2f + 6f;
        }

        private void DrawProperty(SerializedProperty property, string label)
        {
            var rect = EditorGUILayout.GetControlRect();
            DrawProperty(rect, property, label);
        }

        private void DrawProperty(
            Rect rect,
            SerializedProperty property,
            string label)
        {
            var labelRect = new Rect(
                rect.x,
                rect.y,
                Mathf.Min(LabelWidth, rect.width * 0.48f),
                rect.height);
            var fieldRect = new Rect(
                labelRect.xMax + 4f,
                rect.y,
                rect.xMax - labelRect.xMax - 4f,
                rect.height);

            EditorGUI.LabelField(labelRect, label, _labelStyle);
            EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
        }

        private void DrawRequirementPreview()
        {
            var initial = Mathf.Max(1, _initialRequirement.intValue);
            var multiplier = Mathf.Max(1f, _requirementMultiplier.floatValue);
            var values = new string[5];
            for (var index = 0; index < values.Length; index++)
            {
                var value = initial * Math.Pow(multiplier, index);
                values[index] = value >= int.MaxValue
                    ? int.MaxValue.ToString()
                    : Mathf.CeilToInt((float)value).ToString();
            }

            EditorGUILayout.HelpBox(
                $"Requirement preview: {string.Join("  →  ", values)} …",
                MessageType.Info);
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null)
            {
                return;
            }

            var textColor = EditorGUIUtility.isProSkin
                ? new Color(0.88f, 0.88f, 0.88f)
                : new Color(0.12f, 0.12f, 0.12f);
            _labelStyle = CreateStyle(EditorStyles.label, textColor);
            _headerStyle = CreateStyle(EditorStyles.boldLabel, textColor);
        }

        private static GUIStyle CreateStyle(GUIStyle source, Color textColor)
        {
            var style = new GUIStyle(source);
            style.normal.textColor = textColor;
            style.hover.textColor = textColor;
            style.active.textColor = textColor;
            style.focused.textColor = textColor;
            return style;
        }
    }
}
