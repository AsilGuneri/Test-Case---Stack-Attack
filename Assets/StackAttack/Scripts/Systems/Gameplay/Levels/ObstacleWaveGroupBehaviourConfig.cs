using System;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [Serializable]
    public sealed class ObstacleWaveGroupBehaviourConfig
        : ObstacleGroupBehaviourConfig
    {
        [SerializeField]
        private ObstacleWaveDirection direction =
            ObstacleWaveDirection.PositiveHorizontal;

        [SerializeField, Min(0f)]
        private float speedInCellsPerSecond = 1f;

        [SerializeField, Min(0f)]
        private float maximumCellDistance = 1f;

        public ObstacleWaveDirection Direction => direction;
        public float SpeedInCellsPerSecond => speedInCellsPerSecond;
        public float MaximumCellDistance => maximumCellDistance;

        public override ObstacleGroupBehaviour CreateRuntimeBehaviour(
            ObstacleGroupRuntimeContext context)
        {
            var wave =
                context.GroupObject.GetComponent<ObstacleWaveBehaviour>() ??
                context.GroupObject.AddComponent<ObstacleWaveBehaviour>();
            wave.Configure(
                direction,
                speedInCellsPerSecond,
                maximumCellDistance,
                context.CellSize);
            return wave;
        }
    }
}
