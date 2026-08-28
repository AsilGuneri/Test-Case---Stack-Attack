using System;
using System.Collections.Generic;
using StackAttack.Game;
using UnityEngine;

namespace StackAttack.Gameplay
{
    public interface IUpgradeService
    {
        int CurrentProgress { get; }
        int RequiredProgress { get; }
        float NormalizedProgress { get; }
        IReadOnlyList<UpgradeDefinition> CurrentOffers { get; }
        bool IsSelectionOpen { get; }

        event Action<int, float> ProgressChanged;
        event Action<IReadOnlyList<UpgradeDefinition>> OffersChanged;
        event Action<UpgradeDefinition> UpgradeSelected;

        void ReportClearedColumn(int obstacleCount);
        bool SelectUpgrade(int optionIndex);
    }

    public sealed class UpgradeService : IUpgradeService
    {
        private readonly UpgradeSettings _settings;
        private readonly IPlayerAmmoService _ammo;
        private readonly IPlayerHealth _health;
        private readonly IGameFlowManager _gameFlow;
        private readonly List<UpgradeDefinition> _offers = new();

        public UpgradeService(
            UpgradeSettings settings,
            IPlayerAmmoService ammo,
            IPlayerHealth health,
            IGameFlowManager gameFlow)
        {
            _settings = settings != null
                ? settings
                : throw new ArgumentNullException(nameof(settings));
            _ammo = ammo ?? throw new ArgumentNullException(nameof(ammo));
            _health = health ?? throw new ArgumentNullException(nameof(health));
            _gameFlow = gameFlow ?? throw new ArgumentNullException(nameof(gameFlow));
        }

        public int CurrentProgress { get; private set; }
        public int CompletedUpgradeCount { get; private set; }
        public int RequiredProgress =>
            _settings.GetRequiredClearedObstacleCount(CompletedUpgradeCount);
        public float NormalizedProgress =>
            Mathf.Clamp01((float)CurrentProgress / RequiredProgress);
        public IReadOnlyList<UpgradeDefinition> CurrentOffers => _offers;
        public bool IsSelectionOpen => _offers.Count > 0;

        public event Action<int, float> ProgressChanged;
        public event Action<IReadOnlyList<UpgradeDefinition>> OffersChanged;
        public event Action<UpgradeDefinition> UpgradeSelected;

        public void ReportClearedColumn(int obstacleCount)
        {
            if (obstacleCount <= 0 || IsSelectionOpen ||
                !_gameFlow.IsGameplayActive || !HasEligibleUpgrade())
            {
                return;
            }

            CurrentProgress = Mathf.Min(
                RequiredProgress,
                CurrentProgress + obstacleCount);
            ProgressChanged?.Invoke(CurrentProgress, NormalizedProgress);

            if (CurrentProgress >= RequiredProgress)
            {
                CreateOffer();
            }
        }

        public bool SelectUpgrade(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= _offers.Count)
            {
                return false;
            }

            var selected = _offers[optionIndex];
            if (!CanApply(selected))
            {
                return false;
            }

            Apply(selected);
            CompletedUpgradeCount++;
            CurrentProgress = 0;
            _offers.Clear();
            ProgressChanged?.Invoke(CurrentProgress, NormalizedProgress);
            OffersChanged?.Invoke(_offers);
            UpgradeSelected?.Invoke(selected);
            return true;
        }

        private bool HasEligibleUpgrade()
        {
            var upgrades = _settings.Upgrades;
            for (var index = 0; index < upgrades.Count; index++)
            {
                if (CanApply(upgrades[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateOffer()
        {
            var eligible = new List<UpgradeDefinition>();
            var upgrades = _settings.Upgrades;
            for (var index = 0; index < upgrades.Count; index++)
            {
                if (CanApply(upgrades[index]))
                {
                    eligible.Add(upgrades[index]);
                }
            }

            for (var index = eligible.Count - 1; index > 0; index--)
            {
                var swapIndex = UnityEngine.Random.Range(0, index + 1);
                (eligible[index], eligible[swapIndex]) =
                    (eligible[swapIndex], eligible[index]);
            }

            _offers.Clear();
            var count = Mathf.Min(_settings.OfferCount, eligible.Count);
            for (var index = 0; index < count; index++)
            {
                _offers.Add(eligible[index]);
            }

            if (_offers.Count == 0)
            {
                return;
            }

            OffersChanged?.Invoke(_offers);
        }

        private bool CanApply(UpgradeDefinition upgrade)
        {
            if (upgrade == null || upgrade.Amount <= 0f)
            {
                return false;
            }

            return upgrade.Target == UpgradeTarget.MaximumHealth
                ? _health.CanIncreaseMaximumHealth(
                    Mathf.RoundToInt(upgrade.MaximumValue))
                : _ammo.CanApply(upgrade);
        }

        private void Apply(UpgradeDefinition upgrade)
        {
            if (upgrade.Target == UpgradeTarget.MaximumHealth)
            {
                _health.IncreaseMaximumHealth(
                    Mathf.RoundToInt(upgrade.Amount),
                    Mathf.RoundToInt(upgrade.MaximumValue));
                return;
            }

            _ammo.ApplyUpgrade(upgrade);
        }
    }
}
