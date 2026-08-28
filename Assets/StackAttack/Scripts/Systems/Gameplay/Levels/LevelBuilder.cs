using System;
using System.Collections.Generic;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ObstacleParent))]
    public sealed class LevelBuilder : MonoBehaviour
    {
        private const float MinimumColliderWidth = 0.0001f;

        [SerializeField] private ObstacleColumn columnPrefab;
        [SerializeField] private ObstacleParent generationRoot;

        private readonly List<ObstacleColumn> _columns = new();
        private readonly List<ObstacleParent> _groups = new();

        public LevelDefinition CurrentLevel { get; private set; }
        public IReadOnlyList<ObstacleColumn> Columns => _columns;
        public IReadOnlyList<ObstacleParent> Groups => _groups;

        public void Build(LevelDefinition level, Vector3 playerStartPosition)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            ValidateReferences();
            ClearGeneratedChildren();
            CurrentLevel = level;

            var camera = Camera.main;
            if (camera == null)
            {
                throw new MissingReferenceException(
                    "LevelBuilder requires a camera tagged MainCamera.");
            }

            if (!camera.orthographic)
            {
                throw new InvalidOperationException(
                    "LevelBuilder requires an orthographic Main Camera.");
            }

            var gridSize = level.GridSize;
            var margin = Mathf.Clamp(
                level.HorizontalViewportMargin,
                0f,
                0.49f);
            var cameraDistance = Vector3.Dot(
                generationRoot.transform.position - camera.transform.position,
                camera.transform.forward);
            var leftPoint = camera.ViewportToWorldPoint(
                new Vector3(margin, 0.5f, cameraDistance));
            var rightPoint = camera.ViewportToWorldPoint(
                new Vector3(1f - margin, 0.5f, cameraDistance));
            var viewportLeft = Mathf.Min(leftPoint.x, rightPoint.x);
            var viewportRight = Mathf.Max(leftPoint.x, rightPoint.x);
            var usableWidth = viewportRight - viewportLeft;
            var cellWidth = usableWidth / gridSize.x;
            var obstacleWidth = cellWidth *
                                (1f - Mathf.Clamp01(level.CellSpacing));

            if (obstacleWidth <= MinimumColliderWidth)
            {
                throw new InvalidOperationException(
                    "The current viewport and cell spacing leave no room " +
                    "to build this level.");
            }

            var generatedCells = new List<GeneratedCell>();
            for (var y = 0; y < gridSize.y; y++)
            {
                for (var x = 0; x < gridSize.x; x++)
                {
                    var cell = level.GetCell(x, y);
                    if (cell.IsEmpty)
                    {
                        continue;
                    }

                    if (cell.HasGroup &&
                        !level.TryGetGroup(cell.GroupId, out _))
                    {
                        throw new InvalidOperationException(
                            $"Cell ({x}, {y}) references missing obstacle group " +
                            $"'{cell.GroupId}'.");
                    }

                    generatedCells.Add(CreateColumn(
                        x,
                        y,
                        cell.ObstacleAmount,
                        cell.Color,
                        cell.GroupId));
                }
            }

            if (generatedCells.Count == 0)
            {
                generationRoot.RefreshColumns();
                return;
            }

            Physics2D.SyncTransforms();
            var referenceCell = generatedCells[0];
            var referenceCollider = GetRequiredCollider(referenceCell.BaseObstacle);
            var authoredColliderWidth = referenceCollider.bounds.size.x;
            if (authoredColliderWidth <= MinimumColliderWidth)
            {
                throw new InvalidOperationException(
                    $"Obstacle column prefab '{columnPrefab.name}' has no measurable collider width.");
            }

            var uniformScaleMultiplier = obstacleWidth / authoredColliderWidth;
            for (var index = 0; index < generatedCells.Count; index++)
            {
                generatedCells[index].Column.transform.localScale *=
                    uniformScaleMultiplier;
            }

            Physics2D.SyncTransforms();
            var scaledReferenceBounds = referenceCollider.bounds;
            var referencePosition = referenceCell.Column.transform.position;
            var colliderCenterOffsetX =
                scaledReferenceBounds.center.x - referencePosition.x;
            var colliderMinimumOffsetY =
                scaledReferenceBounds.min.y - referencePosition.y;
            var rowZeroNearestEdgeY = playerStartPosition.y + level.StartOffset;
            var rowZeroColumnY = rowZeroNearestEdgeY - colliderMinimumOffsetY;
            var generatedZ = generationRoot.transform.position.z;

            for (var index = 0; index < generatedCells.Count; index++)
            {
                var generatedCell = generatedCells[index];
                var cellCenterX = viewportLeft +
                                  ((generatedCell.X + 0.5f) * cellWidth);
                generatedCell.Column.transform.position = new Vector3(
                    cellCenterX - colliderCenterOffsetX,
                    rowZeroColumnY + (generatedCell.Y * cellWidth),
                    generatedZ);
            }

            Physics2D.SyncTransforms();
            CreateRuntimeGroups(level, generatedCells, cellWidth);
            generationRoot.RefreshColumns();
        }

        private GeneratedCell CreateColumn(
            int x,
            int y,
            int obstacleAmount,
            Color color,
            string groupId)
        {
            var column = Instantiate(columnPrefab, generationRoot.transform);
            column.name = $"Obstacle Column [{x}, {y}]";
            column.transform.SetLocalPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            column.Configure(obstacleAmount, color);

            if (column.Obstacles.Count == 0)
            {
                throw new MissingReferenceException(
                    $"Obstacle column prefab '{columnPrefab.name}' requires " +
                    "one direct-child Obstacle.");
            }

            var baseObstacle = column.Obstacles[0];

            _columns.Add(column);
            return new GeneratedCell(x, y, column, baseObstacle, groupId);
        }

        private void CreateRuntimeGroups(
            LevelDefinition level,
            IReadOnlyList<GeneratedCell> generatedCells,
            float cellSize)
        {
            var levelGroups = level.Groups;
            for (var groupIndex = 0;
                 groupIndex < levelGroups.Count;
                 groupIndex++)
            {
                var groupDefinition = levelGroups[groupIndex];
                if (groupDefinition == null)
                {
                    throw new InvalidOperationException(
                        $"Level '{level.name}' contains a missing obstacle group definition.");
                }

                var memberCount = 0;
                var pivot = Vector3.zero;
                for (var cellIndex = 0;
                     cellIndex < generatedCells.Count;
                     cellIndex++)
                {
                    var generatedCell = generatedCells[cellIndex];
                    if (generatedCell.GroupId != groupDefinition.Id)
                    {
                        continue;
                    }

                    pivot += generatedCell.Column.transform.position;
                    memberCount++;
                }

                if (memberCount == 0)
                {
                    continue;
                }

                pivot /= memberCount;
                var groupObject = new GameObject(
                    $"Obstacle Group - {groupDefinition.DisplayName}");
                groupObject.transform.SetParent(generationRoot.transform, false);
                groupObject.transform.SetPositionAndRotation(
                    pivot,
                    Quaternion.identity);
                groupObject.transform.localScale = Vector3.one;
                var groupParent = groupObject.AddComponent<ObstacleParent>();

                for (var cellIndex = 0;
                     cellIndex < generatedCells.Count;
                     cellIndex++)
                {
                    var generatedCell = generatedCells[cellIndex];
                    if (generatedCell.GroupId == groupDefinition.Id)
                    {
                        generatedCell.Column.transform.SetParent(
                            groupObject.transform,
                            true);
                    }
                }

                groupParent.RefreshColumns();
                var behaviourConfigs = groupDefinition.Behaviours;
                for (var behaviourIndex = 0;
                     behaviourIndex < behaviourConfigs.Count;
                     behaviourIndex++)
                {
                    var behaviourConfig = behaviourConfigs[behaviourIndex];
                    if (behaviourConfig == null)
                    {
                        throw new InvalidOperationException(
                            $"Obstacle group '{groupDefinition.DisplayName}' " +
                            "contains a missing behaviour configuration.");
                    }

                    behaviourConfig.CreateRuntimeBehaviour(
                        new ObstacleGroupRuntimeContext(
                            groupObject,
                            cellSize));
                }

                _groups.Add(groupParent);
            }
        }

        private void ClearGeneratedChildren()
        {
            _columns.Clear();
            _groups.Clear();
            var rootTransform = generationRoot != null
                ? generationRoot.transform
                : transform;

            for (var childIndex = rootTransform.childCount - 1;
                 childIndex >= 0;
                 childIndex--)
            {
                var child = rootTransform.GetChild(childIndex);
                child.SetParent(null, true);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            generationRoot?.RefreshColumns();
        }

        private void ValidateReferences()
        {
            generationRoot ??= GetComponent<ObstacleParent>();
            if (generationRoot == null)
            {
                throw new MissingReferenceException(
                    "LevelBuilder requires an ObstacleParent generation root.");
            }

            if (columnPrefab == null)
            {
                throw new MissingReferenceException(
                    "LevelBuilder requires an ObstacleColumn prefab.");
            }

            var rootScale = generationRoot.transform.lossyScale;
            if (!Mathf.Approximately(Mathf.Abs(rootScale.x), Mathf.Abs(rootScale.y)))
            {
                throw new InvalidOperationException(
                    "LevelBuilder requires a uniformly scaled generation root.");
            }
        }

        private static Collider2D GetRequiredCollider(Obstacle obstacle)
        {
            var obstacleCollider = obstacle.GetComponent<Collider2D>();
            if (obstacleCollider == null || !obstacleCollider.enabled)
            {
                throw new MissingReferenceException(
                    $"Obstacle '{obstacle.name}' requires an enabled Collider2D.");
            }

            return obstacleCollider;
        }

        private void OnValidate()
        {
            generationRoot ??= GetComponent<ObstacleParent>();
        }

        private readonly struct GeneratedCell
        {
            public int X { get; }
            public int Y { get; }
            public ObstacleColumn Column { get; }
            public Obstacle BaseObstacle { get; }
            public string GroupId { get; }

            public GeneratedCell(
                int x,
                int y,
                ObstacleColumn column,
                Obstacle baseObstacle,
                string groupId)
            {
                X = x;
                Y = y;
                Column = column;
                BaseObstacle = baseObstacle;
                GroupId = groupId;
            }
        }
    }
}
