using System;

namespace StackAttack.Systems
{
    public sealed class GameStartRequest
    {
        public int LevelNumber { get; }

        public GameStartRequest(int levelNumber)
        {
            if (levelNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(levelNumber),
                    levelNumber,
                    "A level number must be at least 1.");
            }

            LevelNumber = levelNumber;
        }
    }
}
