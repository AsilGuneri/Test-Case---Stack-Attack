using System;
using UnityEngine;

namespace StackAttack.Gameplay
{
    public readonly struct ObstacleGroupRuntimeContext
    {
        public GameObject GroupObject { get; }
        public float CellSize { get; }

        public ObstacleGroupRuntimeContext(
            GameObject groupObject,
            float cellSize)
        {
            GroupObject = groupObject;
            CellSize = cellSize;
        }
    }

    [Serializable]
    public abstract class ObstacleGroupBehaviourConfig
    {
        public abstract ObstacleGroupBehaviour CreateRuntimeBehaviour(
            ObstacleGroupRuntimeContext context);
    }
}
