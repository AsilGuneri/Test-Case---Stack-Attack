using System.Collections.Generic;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ObstacleParent : MonoBehaviour
    {
        private readonly List<ObstacleColumn> _columns = new();
        private readonly List<Obstacle> _obstacles = new();
        private readonly List<ObstacleGroupBehaviour> _behaviours = new();

        public IReadOnlyList<ObstacleColumn> Columns => _columns;
        public IReadOnlyList<Obstacle> Obstacles => _obstacles;

        private void Awake()
        {
            RefreshColumns();
        }

        private void OnEnable()
        {
            RefreshColumns();
        }

        private void Update()
        {
            RemoveDestroyedReferences();
            RefreshFlattenedObstacles();

            for (var index = 0; index < _behaviours.Count; index++)
            {
                var behaviour = _behaviours[index];
                if (behaviour != null && behaviour.isActiveAndEnabled)
                {
                    behaviour.Apply(this, _columns, Time.deltaTime);
                }
            }
        }

        private void OnTransformChildrenChanged()
        {
            RefreshColumns();
        }

        private void OnValidate()
        {
            RefreshColumns();
        }

        public void RefreshObstacles()
        {
            RefreshColumns();
        }

        public void RefreshColumns()
        {
            _columns.Clear();
            _obstacles.Clear();
            CollectColumns(transform);

            for (var index = 0; index < _columns.Count; index++)
            {
                _columns[index].RefreshObstacles();
            }

            RefreshFlattenedObstacles();
        }

        internal void RegisterBehaviour(ObstacleGroupBehaviour behaviour)
        {
            if (behaviour != null && !_behaviours.Contains(behaviour))
            {
                _behaviours.Add(behaviour);
            }
        }

        internal void UnregisterBehaviour(ObstacleGroupBehaviour behaviour)
        {
            _behaviours.Remove(behaviour);
        }

        private void CollectColumns(Transform root)
        {
            for (var childIndex = 0; childIndex < root.childCount; childIndex++)
            {
                var child = root.GetChild(childIndex);
                if (child.TryGetComponent<ObstacleParent>(out var nestedParent) &&
                    nestedParent != this)
                {
                    continue;
                }

                if (child.TryGetComponent<ObstacleColumn>(out var column))
                {
                    _columns.Add(column);
                    continue;
                }

                CollectColumns(child);
            }
        }

        private void RefreshFlattenedObstacles()
        {
            _obstacles.Clear();

            for (var columnIndex = 0; columnIndex < _columns.Count; columnIndex++)
            {
                var column = _columns[columnIndex];
                if (column == null)
                {
                    continue;
                }

                var columnObstacles = column.Obstacles;
                for (var obstacleIndex = 0;
                     obstacleIndex < columnObstacles.Count;
                     obstacleIndex++)
                {
                    var obstacle = columnObstacles[obstacleIndex];
                    if (obstacle != null)
                    {
                        _obstacles.Add(obstacle);
                    }
                }
            }
        }

        private void RemoveDestroyedReferences()
        {
            for (var index = _obstacles.Count - 1; index >= 0; index--)
            {
                if (_obstacles[index] == null)
                {
                    _obstacles.RemoveAt(index);
                }
            }

            for (var index = _columns.Count - 1; index >= 0; index--)
            {
                if (_columns[index] == null)
                {
                    _columns.RemoveAt(index);
                }
            }

            for (var index = _behaviours.Count - 1; index >= 0; index--)
            {
                if (_behaviours[index] == null)
                {
                    _behaviours.RemoveAt(index);
                }
            }
        }
    }
}
