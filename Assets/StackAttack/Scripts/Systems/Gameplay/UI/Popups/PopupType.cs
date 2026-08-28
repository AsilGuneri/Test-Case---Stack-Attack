using System;
using UnityEngine;

namespace StackAttack.Game
{
    public enum PopupType
    {
        Upgrade,
        LevelWon,
        LevelLost
    }

    [Serializable]
    public struct PopupPrefabEntry
    {
        [SerializeField] private PopupType type;
        [SerializeField] private PopupBase prefab;

        public PopupType Type => type;
        public PopupBase Prefab => prefab;

        public PopupPrefabEntry(PopupType type, PopupBase prefab)
        {
            this.type = type;
            this.prefab = prefab;
        }
    }
}
