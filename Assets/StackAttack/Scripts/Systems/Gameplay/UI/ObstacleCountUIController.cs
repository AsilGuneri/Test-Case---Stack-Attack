using System.Collections.Generic;
using StackAttack.Gameplay;
using TMPro;
using UnityEngine;

namespace StackAttack.Game
{
    [DisallowMultipleComponent]
    public sealed class ObstacleCountUIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text countVisualPrefab;
        [SerializeField] private RectTransform visualParent;
        [SerializeField] private Vector2 screenOffset;

        private readonly Dictionary<ObstacleColumn, TMP_Text> _visuals = new();
        private Camera _worldCamera;
        private Camera _uiCamera;

        public int VisualCount => _visuals.Count;

        public void Initialize(IReadOnlyList<ObstacleColumn> columns)
        {
            if (columns == null)
            {
                throw new System.ArgumentNullException(nameof(columns));
            }

            if (countVisualPrefab == null || visualParent == null)
            {
                throw new MissingReferenceException(
                    "ObstacleCountUIController requires its visual prefab and parent.");
            }

            Clear();
            _worldCamera = Camera.main;
            if (_worldCamera == null)
            {
                throw new MissingReferenceException(
                    "ObstacleCountUIController requires a Main Camera.");
            }

            var canvas = visualParent.GetComponentInParent<Canvas>();
            _uiCamera = canvas != null &&
                        canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            for (var index = 0; index < columns.Count; index++)
            {
                AddColumn(columns[index]);
            }
        }

        public bool TryGetVisual(ObstacleColumn column, out TMP_Text visual)
        {
            return _visuals.TryGetValue(column, out visual);
        }

        private void LateUpdate()
        {
            foreach (var entry in _visuals)
            {
                UpdatePosition(entry.Key, entry.Value);
            }
        }

        private void OnDestroy()
        {
            Clear();
        }

        private void AddColumn(ObstacleColumn column)
        {
            if (column == null || _visuals.ContainsKey(column))
            {
                return;
            }

            var visual = Instantiate(countVisualPrefab, visualParent);
            visual.name = $"{column.name} Count";
            _visuals.Add(column, visual);
            column.RemainingCountChanged += HandleRemainingCountChanged;
            RefreshVisual(column, visual, column.RemainingCount);
            UpdatePosition(column, visual);
        }

        private void HandleRemainingCountChanged(
            ObstacleColumn column,
            int remainingCount)
        {
            if (_visuals.TryGetValue(column, out var visual))
            {
                RefreshVisual(column, visual, remainingCount);
            }
        }

        private static void RefreshVisual(
            ObstacleColumn column,
            TMP_Text visual,
            int remainingCount)
        {
            visual.text = remainingCount.ToString();
            visual.gameObject.SetActive(column != null && remainingCount > 0);
        }

        private void UpdatePosition(ObstacleColumn column, TMP_Text visual)
        {
            if (column == null || visual == null ||
                column.RemainingCount == 0)
            {
                visual?.gameObject.SetActive(false);
                return;
            }

            var obstacle = column.Obstacles[column.Obstacles.Count - 1];
            var spriteRenderer = obstacle.SpriteRenderer;
            var worldPosition = spriteRenderer != null
                ? spriteRenderer.bounds.center
                : obstacle.transform.position;
            var viewportPosition = _worldCamera.WorldToViewportPoint(worldPosition);
            var isVisible = viewportPosition.z > 0f &&
                            viewportPosition.x >= 0f && viewportPosition.x <= 1f &&
                            viewportPosition.y >= 0f && viewportPosition.y <= 1f;
            visual.gameObject.SetActive(isVisible);
            if (!isVisible)
            {
                return;
            }

            var screenPosition = _worldCamera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                visualParent,
                screenPosition,
                _uiCamera,
                out var localPosition);
            visual.rectTransform.anchoredPosition = localPosition + screenOffset;
        }

        private void Clear()
        {
            foreach (var entry in _visuals)
            {
                if (entry.Key != null)
                {
                    entry.Key.RemainingCountChanged -=
                        HandleRemainingCountChanged;
                }

                if (entry.Value != null)
                {
                    Destroy(entry.Value.gameObject);
                }
            }

            _visuals.Clear();
        }
    }
}
