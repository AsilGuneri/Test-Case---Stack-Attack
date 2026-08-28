using System;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [Serializable]
    public sealed class ObstacleOrbitGroupBehaviourConfig
        : ObstacleGroupBehaviourConfig
    {
        [SerializeField] private float degreesPerSecond = 20f;

        public float DegreesPerSecond => degreesPerSecond;

        public override ObstacleGroupBehaviour CreateRuntimeBehaviour(
            ObstacleGroupRuntimeContext context)
        {
            var orbit =
                context.GroupObject.GetComponent<ObstacleOrbitBehaviour>() ??
                context.GroupObject.AddComponent<ObstacleOrbitBehaviour>();
            orbit.DegreesPerSecond = degreesPerSecond;
            return orbit;
        }
    }
}
