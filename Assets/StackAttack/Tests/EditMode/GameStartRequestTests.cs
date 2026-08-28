using System;
using NUnit.Framework;
using StackAttack.Systems;

namespace StackAttack.Tests
{
    public sealed class GameStartRequestTests
    {
        [Test]
        public void Constructor_WithPositiveLevel_StoresLevel()
        {
            var request = new GameStartRequest(3);

            Assert.That(request.LevelNumber, Is.EqualTo(3));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidLevel_Throws(int levelNumber)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GameStartRequest(levelNumber));
        }
    }
}
