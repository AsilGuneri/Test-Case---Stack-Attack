using System;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [Serializable]
    public sealed class LevelCell
    {
        [SerializeField, Min(0)] private int obstacleAmount;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private string groupId;

        public int ObstacleAmount => obstacleAmount;
        public Color Color => color;
        public string GroupId => groupId;
        public bool HasGroup => !string.IsNullOrEmpty(groupId);
        public bool IsEmpty => obstacleAmount <= 0;

        public LevelCell()
        {
        }

        public LevelCell(int obstacleAmount)
            : this(obstacleAmount, Color.white)
        {
        }

        private LevelCell(int obstacleAmount, Color color, string groupId = null)
        {
            this.obstacleAmount = Mathf.Max(0, obstacleAmount);
            this.color = color;
            this.groupId = groupId;
        }

        internal void SetObstacleAmount(int amount)
        {
            obstacleAmount = Mathf.Max(0, amount);
        }

        internal void SetColor(Color value)
        {
            color = value;
        }

        internal void SetGroup(string value)
        {
            groupId = value;
        }

        internal LevelCell Copy()
        {
            return new LevelCell(obstacleAmount, color, groupId);
        }
    }
}
