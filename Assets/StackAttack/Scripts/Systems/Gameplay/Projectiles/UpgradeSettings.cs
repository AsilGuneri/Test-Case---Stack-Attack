using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace StackAttack.Gameplay
{
    [CreateAssetMenu(
        fileName = "Upgrade Settings",
        menuName = "Stack Attack/Upgrade Settings")]
    public sealed class UpgradeSettings : ScriptableObject
    {
        [SerializeField] private AmmoDefinition startingAmmo;
        [SerializeField, Min(1)]
        private int initialRequiredClearedObstacleCount = 5;
        [SerializeField, Min(1f)] private float requirementMultiplier = 2f;
        [SerializeField, Min(1)] private int offerCount = 3;
        [FormerlySerializedAs("ammoUpgrades")]
        [SerializeField] private List<UpgradeDefinition> upgrades = new();

        public AmmoDefinition StartingAmmo => startingAmmo;
        public int InitialRequiredClearedObstacleCount =>
            initialRequiredClearedObstacleCount;
        public float RequirementMultiplier => requirementMultiplier;
        public int OfferCount => offerCount;
        public IReadOnlyList<UpgradeDefinition> Upgrades => upgrades;

        public int GetRequiredClearedObstacleCount(int upgradeIndex)
        {
            var scaledRequirement = initialRequiredClearedObstacleCount *
                Math.Pow(requirementMultiplier, Mathf.Max(0, upgradeIndex));
            return scaledRequirement >= int.MaxValue
                ? int.MaxValue
                : Mathf.Max(1, Mathf.CeilToInt((float)scaledRequirement));
        }

        private void OnValidate()
        {
            initialRequiredClearedObstacleCount = Mathf.Max(
                1,
                initialRequiredClearedObstacleCount);
            requirementMultiplier = Mathf.Max(1f, requirementMultiplier);
            offerCount = Mathf.Max(1, offerCount);
        }
    }
}
