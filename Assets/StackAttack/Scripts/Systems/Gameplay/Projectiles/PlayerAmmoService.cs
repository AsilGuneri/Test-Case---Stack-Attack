using System;
using UnityEngine;

namespace StackAttack.Gameplay
{
    public interface IPlayerAmmoService
    {
        AmmoDefinition CurrentAmmo { get; }
        AmmoStats Stats { get; }
        event Action<AmmoStats> StatsChanged;

        bool CanApply(UpgradeDefinition upgrade);
        void ApplyUpgrade(UpgradeDefinition upgrade);
    }

    public sealed class PlayerAmmoService : IPlayerAmmoService
    {
        public PlayerAmmoService(UpgradeSettings settings)
        {
            CurrentAmmo = settings != null ? settings.StartingAmmo : null;
            if (CurrentAmmo == null || CurrentAmmo.ProjectilePrefab == null)
            {
                throw new InvalidOperationException(
                    "UpgradeSettings requires a starting ammo with a projectile prefab.");
            }

            Stats = new AmmoStats(
                CurrentAmmo.BaseDamage,
                CurrentAmmo.BasePiercing,
                CurrentAmmo.BaseProjectileCount,
                CurrentAmmo.BaseFireRate,
                CurrentAmmo.ProjectileSpacing);
        }

        public AmmoDefinition CurrentAmmo { get; }
        public AmmoStats Stats { get; private set; }
        public event Action<AmmoStats> StatsChanged;

        public bool CanApply(UpgradeDefinition upgrade)
        {
            if (upgrade == null || upgrade.Target != UpgradeTarget.AmmoStat ||
                upgrade.Ammo != CurrentAmmo ||
                upgrade.Amount <= 0f)
            {
                return false;
            }

            return GetValue(upgrade.Stat) < upgrade.MaximumValue;
        }

        public void ApplyUpgrade(UpgradeDefinition upgrade)
        {
            if (!CanApply(upgrade))
            {
                return;
            }

            var cappedValue = Mathf.Min(
                GetValue(upgrade.Stat) + upgrade.Amount,
                upgrade.MaximumValue);

            Stats = upgrade.Stat switch
            {
                AmmoStatType.Damage => new AmmoStats(
                    Mathf.RoundToInt(cappedValue), Stats.Piercing,
                    Stats.ProjectileCount, Stats.FireRate,
                    Stats.ProjectileSpacing),
                AmmoStatType.Piercing => new AmmoStats(
                    Stats.Damage, Mathf.RoundToInt(cappedValue),
                    Stats.ProjectileCount, Stats.FireRate,
                    Stats.ProjectileSpacing),
                AmmoStatType.ProjectileCount => new AmmoStats(
                    Stats.Damage, Stats.Piercing,
                    Mathf.RoundToInt(cappedValue), Stats.FireRate,
                    Stats.ProjectileSpacing),
                AmmoStatType.FireRate => new AmmoStats(
                    Stats.Damage, Stats.Piercing, Stats.ProjectileCount,
                    cappedValue, Stats.ProjectileSpacing),
                _ => Stats
            };

            StatsChanged?.Invoke(Stats);
        }

        private float GetValue(AmmoStatType stat)
        {
            return stat switch
            {
                AmmoStatType.Damage => Stats.Damage,
                AmmoStatType.Piercing => Stats.Piercing,
                AmmoStatType.ProjectileCount => Stats.ProjectileCount,
                AmmoStatType.FireRate => Stats.FireRate,
                _ => 0f
            };
        }
    }
}
