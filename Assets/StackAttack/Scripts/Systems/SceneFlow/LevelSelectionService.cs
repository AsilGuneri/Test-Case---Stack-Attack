using System;
using StackAttack.Gameplay;

namespace StackAttack.Systems
{
    public interface ILevelSelectionService
    {
        int CurrentLevelNumber { get; }
        event Action<int> LevelChanged;

        void SelectLevel(int levelNumber);
    }

    public sealed class LevelSelectionService : ILevelSelectionService
    {
        private readonly LevelCatalog _catalog;

        public int CurrentLevelNumber { get; private set; }
        public event Action<int> LevelChanged;

        public LevelSelectionService(
            ISaveManager saveManager,
            LevelCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (saveManager == null)
            {
                throw new ArgumentNullException(nameof(saveManager));
            }

            CurrentLevelNumber = FindPlayableLevel(
                saveManager.Data.LastCompletedLevel + 1);
        }

        public void SelectLevel(int levelNumber)
        {
            if (levelNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(levelNumber),
                    "A selected level must be at least 1.");
            }

            if (!_catalog.TryGetLevel(levelNumber, out _))
            {
                throw new ArgumentException(
                    $"Level {levelNumber} is not registered in the catalog.",
                    nameof(levelNumber));
            }

            if (CurrentLevelNumber == levelNumber)
            {
                return;
            }

            CurrentLevelNumber = levelNumber;
            LevelChanged?.Invoke(CurrentLevelNumber);
        }

        private int FindPlayableLevel(int desiredLevelNumber)
        {
            if (_catalog.TryGetLevel(desiredLevelNumber, out var exact))
            {
                return exact.LevelNumber;
            }

            LevelDefinition closestEarlier = null;
            LevelDefinition first = null;
            for (var index = 0; index < _catalog.Levels.Count; index++)
            {
                var level = _catalog.Levels[index];
                if (first == null || level.LevelNumber < first.LevelNumber)
                {
                    first = level;
                }

                if (level.LevelNumber <= desiredLevelNumber &&
                    (closestEarlier == null ||
                     level.LevelNumber > closestEarlier.LevelNumber))
                {
                    closestEarlier = level;
                }
            }

            var playable = closestEarlier ?? first;
            if (playable == null)
            {
                throw new InvalidOperationException(
                    "LevelCatalog must contain at least one level.");
            }

            return playable.LevelNumber;
        }
    }
}
