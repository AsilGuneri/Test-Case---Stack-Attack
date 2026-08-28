using System;
using UnityEngine;

namespace StackAttack.Gameplay
{
    public enum UpgradeTarget
    {
        AmmoStat,
        MaximumHealth
    }

    public enum AmmoStatType
    {
        Damage,
        Piercing,
        ProjectileCount,
        FireRate
    }

    [Serializable]
    public sealed class UpgradeDefinition
    {
        [SerializeField] private UpgradeTarget target;
        [SerializeField] private AmmoDefinition ammo;
        [SerializeField] private AmmoStatType stat;
        [SerializeField, Min(0.01f)] private float amount = 1f;
        [SerializeField, Min(0f)] private float maximumValue = 1f;

        public UpgradeTarget Target => target;
        public AmmoDefinition Ammo => ammo;
        public AmmoStatType Stat => stat;
        public float Amount => amount;
        public float MaximumValue => maximumValue;
        public string DisplayName => target == UpgradeTarget.MaximumHealth
            ? $"+{amount:0.##} Heart{(Mathf.Approximately(amount, 1f) ? string.Empty : "s")}" 
            : $"{ammo.DisplayName} - {GetStatName(stat)} +{amount:0.##}";

        private static string GetStatName(AmmoStatType value)
        {
            return value switch
            {
                AmmoStatType.ProjectileCount => "Projectile Count",
                AmmoStatType.FireRate => "Fire Rate",
                _ => value.ToString()
            };
        }
    }
}
