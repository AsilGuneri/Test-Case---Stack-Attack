using System;
using System.Collections.Generic;
using StackAttack.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;

namespace StackAttack.Game
{
    [DisallowMultipleComponent]
    public sealed class UpgradeUIController : MonoBehaviour
    {
        [FormerlySerializedAs("damageProgressSlider")]
        [SerializeField] private Slider upgradeProgressSlider;

        private IUpgradeService _upgrades;
        private IPopupManager _popups;

        public Slider ProgressSlider => upgradeProgressSlider;

        [Inject]
        public void Construct(
            IUpgradeService upgrades,
            IPopupManager popups)
        {
            if (upgrades == null || popups == null ||
                upgradeProgressSlider == null)
            {
                throw new MissingReferenceException(
                    "UpgradeUIController requires its services and progress slider.");
            }

            _upgrades = upgrades;
            _popups = popups;
            upgradeProgressSlider.minValue = 0f;
            upgradeProgressSlider.maxValue = 1f;
            upgradeProgressSlider.interactable = false;

            _upgrades.ProgressChanged += HandleProgressChanged;
            _upgrades.OffersChanged += HandleOffersChanged;
            HandleProgressChanged(
                _upgrades.CurrentProgress,
                _upgrades.NormalizedProgress);
            HandleOffersChanged(_upgrades.CurrentOffers);
        }

        private void OnDestroy()
        {
            if (_upgrades == null)
            {
                return;
            }

            _upgrades.ProgressChanged -= HandleProgressChanged;
            _upgrades.OffersChanged -= HandleOffersChanged;
        }

        private void HandleProgressChanged(int _, float normalizedProgress)
        {
            upgradeProgressSlider.SetValueWithoutNotify(normalizedProgress);
        }

        private void HandleOffersChanged(
            IReadOnlyList<UpgradeDefinition> offers)
        {
            if (offers.Count > 0 && !_popups.IsOpen(PopupType.Upgrade))
            {
                _popups.Open(PopupType.Upgrade, _upgrades);
            }
        }
    }
}
