using System;
using System.Collections.Generic;
using StackAttack.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackAttack.Game
{
    public sealed class UpgradePopup : PopupBase
    {
        [SerializeField] private Button[] optionButtons = Array.Empty<Button>();
        [SerializeField] private TMP_Text[] optionLabels = Array.Empty<TMP_Text>();

        private IUpgradeService _upgrades;

        public int VisibleOptionCount { get; private set; }

        protected override void OnOpened(object[] args)
        {
            _upgrades = RequireArgument<IUpgradeService>(args);
            if (optionButtons.Length == 0 ||
                optionButtons.Length != optionLabels.Length)
            {
                throw new MissingReferenceException(
                    "UpgradePopup requires matching option buttons and labels.");
            }

            for (var index = 0; index < optionButtons.Length; index++)
            {
                var optionIndex = index;
                optionButtons[index].onClick.AddListener(
                    () => _upgrades.SelectUpgrade(optionIndex));
            }

            _upgrades.OffersChanged += HandleOffersChanged;
            HandleOffersChanged(_upgrades.CurrentOffers);
        }

        protected override void OnClosed()
        {
            if (_upgrades != null)
            {
                _upgrades.OffersChanged -= HandleOffersChanged;
            }
        }

        private void HandleOffersChanged(
            IReadOnlyList<UpgradeDefinition> offers)
        {
            if (offers.Count == 0)
            {
                Close();
                return;
            }

            VisibleOptionCount = Mathf.Min(offers.Count, optionButtons.Length);
            for (var index = 0; index < optionButtons.Length; index++)
            {
                var visible = index < VisibleOptionCount;
                optionButtons[index].gameObject.SetActive(visible);
                if (visible)
                {
                    optionLabels[index].text = offers[index].DisplayName;
                }
            }
        }
    }
}
