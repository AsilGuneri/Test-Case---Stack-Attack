using System.Collections.Generic;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [RequireComponent(typeof(ObstacleParent))]
    public abstract class ObstacleGroupBehaviour : MonoBehaviour
    {
        private ObstacleParent _parent;

        protected virtual void OnEnable()
        {
            CacheParent();
            _parent.RegisterBehaviour(this);
        }

        protected virtual void OnDisable()
        {
            if (_parent != null)
            {
                _parent.UnregisterBehaviour(this);
            }
        }

        internal void Apply(
            ObstacleParent parent,
            IReadOnlyList<ObstacleColumn> columns,
            float deltaTime)
        {
            ApplyToGroup(parent, columns, deltaTime);
        }

        protected abstract void ApplyToGroup(
            ObstacleParent parent,
            IReadOnlyList<ObstacleColumn> columns,
            float deltaTime);

        private void CacheParent()
        {
            if (_parent == null)
            {
                _parent = GetComponent<ObstacleParent>();
            }
        }
    }
}
