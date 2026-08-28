using System.Collections.Generic;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ObstacleOrbitBehaviour : ObstacleGroupBehaviour
    {
        [SerializeField] private float degreesPerSecond = 20f;

        public float DegreesPerSecond
        {
            get => degreesPerSecond;
            set => degreesPerSecond = value;
        }

        protected override void ApplyToGroup(
            ObstacleParent parent,
            IReadOnlyList<ObstacleColumn> columns,
            float deltaTime)
        {
            var angle = degreesPerSecond * deltaTime;
            if (Mathf.Approximately(angle, 0f))
            {
                return;
            }

            var pivot = parent.transform.position;
            for (var index = 0; index < columns.Count; index++)
            {
                var column = columns[index];
                if (column == null)
                {
                    continue;
                }

                var columnTransform = column.transform;
                var worldRotation = columnTransform.rotation;
                columnTransform.RotateAround(pivot, Vector3.forward, angle);
                columnTransform.rotation = worldRotation;
            }
        }
    }
}
