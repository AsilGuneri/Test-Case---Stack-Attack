using System;
using System.Collections.Generic;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [CreateAssetMenu(
        fileName = "Level Definition",
        menuName = "Stack Attack/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField, Min(1)] private int levelNumber = 1;
        [SerializeField] private Vector2Int gridSize = Vector2Int.one;
        [SerializeField, Min(0f)] private float startOffset = 10f;
        [SerializeField, Min(0f)] private float completionOffset = 2.5f;
        [SerializeField, Range(0f, 0.49f)]
        private float horizontalViewportMargin = 0.02f;
        [SerializeField, Range(0f, 0.95f)] private float cellSpacing = 0.05f;
        [SerializeField] private List<LevelObstacleGroup> groups = new();
        [SerializeField] private List<LevelCell> cells = new() { new LevelCell() };

        public int LevelNumber => levelNumber;
        public Vector2Int GridSize => gridSize;
        public float StartOffset => startOffset;
        public float CompletionOffset => completionOffset;
        public float HorizontalViewportMargin => horizontalViewportMargin;
        public float CellSpacing => cellSpacing;
        public IReadOnlyList<LevelObstacleGroup> Groups => groups;

        public LevelCell GetCell(int x, int y)
        {
            ValidateCoordinates(x, y);
            EnsureGridIntegrity();
            return cells[(y * gridSize.x) + x];
        }

        public void ResizeGrid(Vector2Int requestedSize)
        {
            var newSize = ClampGridSize(requestedSize);
            EnsureGridIntegrity();

            if (newSize == gridSize)
            {
                return;
            }

            var oldSize = gridSize;
            var oldCells = cells;
            var resizedCells = CreateEmptyCells(newSize.x * newSize.y);
            var preservedWidth = Mathf.Min(oldSize.x, newSize.x);
            var preservedHeight = Mathf.Min(oldSize.y, newSize.y);

            for (var y = 0; y < preservedHeight; y++)
            {
                for (var x = 0; x < preservedWidth; x++)
                {
                    var oldIndex = (y * oldSize.x) + x;
                    var newIndex = (y * newSize.x) + x;
                    resizedCells[newIndex] = oldCells[oldIndex]?.Copy() ??
                                             new LevelCell();
                }
            }

            gridSize = newSize;
            cells = resizedCells;
        }

        public void SetObstacleAmount(int x, int y, int obstacleAmount)
        {
            ValidateCoordinates(x, y);
            EnsureGridIntegrity();
            cells[(y * gridSize.x) + x].SetObstacleAmount(obstacleAmount);
        }

        public void SetCellColor(int x, int y, Color color)
        {
            ValidateCoordinates(x, y);
            EnsureGridIntegrity();
            cells[(y * gridSize.x) + x].SetColor(color);
        }

        public void SetCellGroup(int x, int y, string groupId)
        {
            ValidateCoordinates(x, y);
            EnsureGridIntegrity();

            if (!string.IsNullOrEmpty(groupId) &&
                !TryGetGroup(groupId, out _))
            {
                throw new ArgumentException(
                    $"Level '{name}' does not contain obstacle group '{groupId}'.",
                    nameof(groupId));
            }

            cells[(y * gridSize.x) + x].SetGroup(groupId);
        }

        public LevelObstacleGroup AddGroup(string displayName)
        {
            groups ??= new List<LevelObstacleGroup>();
            var group = new LevelObstacleGroup(displayName);
            groups.Add(group);
            return group;
        }

        public bool RemoveGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId) || groups == null)
            {
                return false;
            }

            var removed = false;
            for (var index = groups.Count - 1; index >= 0; index--)
            {
                if (groups[index] != null && groups[index].Id == groupId)
                {
                    groups.RemoveAt(index);
                    removed = true;
                }
            }

            if (!removed)
            {
                return false;
            }

            EnsureGridIntegrity();
            for (var index = 0; index < cells.Count; index++)
            {
                if (cells[index].GroupId == groupId)
                {
                    cells[index].SetGroup(null);
                }
            }

            return true;
        }

        public bool TryGetGroup(
            string groupId,
            out LevelObstacleGroup group)
        {
            if (!string.IsNullOrEmpty(groupId) && groups != null)
            {
                for (var index = 0; index < groups.Count; index++)
                {
                    var candidate = groups[index];
                    if (candidate != null && candidate.Id == groupId)
                    {
                        group = candidate;
                        return true;
                    }
                }
            }

            group = null;
            return false;
        }

        private void OnValidate()
        {
            levelNumber = Mathf.Max(1, levelNumber);
            gridSize = ClampGridSize(gridSize);
            startOffset = Mathf.Max(0f, startOffset);
            completionOffset = Mathf.Max(0f, completionOffset);
            horizontalViewportMargin = Mathf.Clamp(
                horizontalViewportMargin,
                0f,
                0.49f);
            cellSpacing = Mathf.Clamp(cellSpacing, 0f, 0.95f);
            EnsureGroupsIntegrity();
            EnsureGridIntegrity();
        }

        private void EnsureGroupsIntegrity()
        {
            groups ??= new List<LevelObstacleGroup>();
            for (var index = 0; index < groups.Count; index++)
            {
                groups[index]?.EnsureIntegrity();
            }
        }

        private void EnsureGridIntegrity()
        {
            gridSize = ClampGridSize(gridSize);
            var requiredCount = gridSize.x * gridSize.y;
            cells ??= new List<LevelCell>(requiredCount);

            while (cells.Count < requiredCount)
            {
                cells.Add(new LevelCell());
            }

            if (cells.Count > requiredCount)
            {
                cells.RemoveRange(requiredCount, cells.Count - requiredCount);
            }

            for (var index = 0; index < cells.Count; index++)
            {
                cells[index] ??= new LevelCell();
            }
        }

        private void ValidateCoordinates(int x, int y)
        {
            if (x < 0 || x >= gridSize.x || y < 0 || y >= gridSize.y)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    $"Cell ({x}, {y}) is outside the {gridSize.x}x{gridSize.y} grid.");
            }
        }

        private static Vector2Int ClampGridSize(Vector2Int size)
        {
            return new Vector2Int(
                Mathf.Max(1, size.x),
                Mathf.Max(1, size.y));
        }

        private static List<LevelCell> CreateEmptyCells(int count)
        {
            var result = new List<LevelCell>(count);
            for (var index = 0; index < count; index++)
            {
                result.Add(new LevelCell());
            }

            return result;
        }
    }
}
