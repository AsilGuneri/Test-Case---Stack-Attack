using System;
using System.Collections.Generic;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [CreateAssetMenu(
        fileName = "Level Catalog",
        menuName = "Stack Attack/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private List<LevelDefinition> levels = new();

        public IReadOnlyList<LevelDefinition> Levels => levels;

        public bool TryGetLevel(int levelNumber, out LevelDefinition level)
        {
            if (levelNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(levelNumber),
                    levelNumber,
                    "A level number must be at least 1.");
            }

            ValidateEntries();
            for (var index = 0; index < levels.Count; index++)
            {
                if (levels[index].LevelNumber == levelNumber)
                {
                    level = levels[index];
                    return true;
                }
            }

            level = null;
            return false;
        }

        public LevelDefinition GetRequiredLevel(int levelNumber)
        {
            if (TryGetLevel(levelNumber, out var level))
            {
                return level;
            }

            throw new KeyNotFoundException(
                $"LevelCatalog does not contain level {levelNumber}.");
        }

        private void ValidateEntries()
        {
            if (levels == null)
            {
                throw new InvalidOperationException(
                    "LevelCatalog has no serialized level collection.");
            }

            var levelNumbers = new HashSet<int>();
            for (var index = 0; index < levels.Count; index++)
            {
                var level = levels[index];
                if (level == null)
                {
                    throw new InvalidOperationException(
                        $"LevelCatalog entry {index} is missing a LevelDefinition.");
                }

                if (level.LevelNumber < 1)
                {
                    throw new InvalidOperationException(
                        $"'{level.name}' has invalid level number {level.LevelNumber}.");
                }

                if (!levelNumbers.Add(level.LevelNumber))
                {
                    throw new InvalidOperationException(
                        $"LevelCatalog contains duplicate level number {level.LevelNumber}.");
                }
            }
        }
    }
}
