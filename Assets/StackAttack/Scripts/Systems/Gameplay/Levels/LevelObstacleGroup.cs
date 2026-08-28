using System;
using System.Collections.Generic;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [Serializable]
    public sealed class LevelObstacleGroup
    {
        [SerializeField, HideInInspector] private string id;
        [SerializeField] private string displayName = "Obstacle Group";
        [SerializeField] private Color editorColor = new(1f, 0.65f, 0.1f, 1f);
        [SerializeReference]
        private List<ObstacleGroupBehaviourConfig> behaviours = new();

        public string Id => id;
        public string DisplayName => displayName;
        public Color EditorColor => editorColor;
        public IReadOnlyList<ObstacleGroupBehaviourConfig> Behaviours => behaviours;

        internal LevelObstacleGroup(string displayName)
        {
            id = Guid.NewGuid().ToString("N");
            this.displayName = string.IsNullOrWhiteSpace(displayName)
                ? "Obstacle Group"
                : displayName;
        }

        internal void EnsureIntegrity()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Guid.NewGuid().ToString("N");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = "Obstacle Group";
            }

            behaviours ??= new List<ObstacleGroupBehaviourConfig>();
        }
    }
}
