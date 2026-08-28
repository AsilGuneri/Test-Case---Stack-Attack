using System.Collections.Generic;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ObstacleWaveBehaviour : ObstacleGroupBehaviour
    {
        [SerializeField]
        private ObstacleWaveDirection direction =
            ObstacleWaveDirection.PositiveHorizontal;

        [SerializeField, Min(0f)]
        private float speedInCellsPerSecond = 1f;

        [SerializeField, Min(0f)]
        private float maximumCellDistance = 1f;

        [SerializeField, HideInInspector, Min(0f)]
        private float cellSize = 1f;

        private float _elapsedTime;
        private Vector3 _lastAppliedOffset;

        public ObstacleWaveDirection Direction => direction;
        public float SpeedInCellsPerSecond => speedInCellsPerSecond;
        public float MaximumCellDistance => maximumCellDistance;
        public float CellSize => cellSize;

        public void Configure(
            ObstacleWaveDirection waveDirection,
            float cellsPerSecond,
            float maximumDistanceInCells,
            float runtimeCellSize)
        {
            direction = waveDirection;
            speedInCellsPerSecond = Mathf.Max(0f, cellsPerSecond);
            maximumCellDistance = Mathf.Max(0f, maximumDistanceInCells);
            cellSize = Mathf.Max(0f, runtimeCellSize);
            _elapsedTime = 0f;
            _lastAppliedOffset = Vector3.zero;
        }

        protected override void ApplyToGroup(
            ObstacleParent parent,
            IReadOnlyList<ObstacleColumn> columns,
            float deltaTime)
        {
            var worldSpeed = speedInCellsPerSecond * cellSize;
            var maximumWorldDistance = maximumCellDistance * cellSize;
            if (worldSpeed <= 0f || maximumWorldDistance <= 0f)
            {
                return;
            }

            _elapsedTime += deltaTime;
            var distance = Mathf.PingPong(
                _elapsedTime * worldSpeed,
                maximumWorldDistance);
            var nextOffset = GetWorldDirection(direction) * distance;
            parent.transform.position += nextOffset - _lastAppliedOffset;
            _lastAppliedOffset = nextOffset;
        }

        private void OnValidate()
        {
            speedInCellsPerSecond = Mathf.Max(0f, speedInCellsPerSecond);
            maximumCellDistance = Mathf.Max(0f, maximumCellDistance);
            cellSize = Mathf.Max(0f, cellSize);
        }

        private static Vector3 GetWorldDirection(
            ObstacleWaveDirection waveDirection)
        {
            return waveDirection switch
            {
                ObstacleWaveDirection.PositiveHorizontal => Vector3.right,
                ObstacleWaveDirection.NegativeHorizontal => Vector3.left,
                ObstacleWaveDirection.PositiveVertical => Vector3.up,
                ObstacleWaveDirection.NegativeVertical => Vector3.down,
                _ => Vector3.zero
            };
        }
    }
}
