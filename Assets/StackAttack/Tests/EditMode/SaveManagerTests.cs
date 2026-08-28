using System;
using System.IO;
using NUnit.Framework;
using StackAttack.Systems;

namespace StackAttack.Tests
{
    public sealed class SaveManagerTests
    {
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _savePath = Path.Combine(
                Path.GetTempPath(),
                $"StackAttack-{Guid.NewGuid():N}.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }
        }

        [Test]
        public void CompletedLevelPersistsAndNeverMovesBackward()
        {
            var save = new SaveManager(_savePath);
            save.MarkLevelCompleted(2);
            save.MarkLevelCompleted(1);

            var loaded = new SaveManager(_savePath);

            Assert.That(File.Exists(_savePath), Is.True);
            Assert.That(loaded.Data.LastCompletedLevel, Is.EqualTo(2));
            Assert.That(
                File.ReadAllText(_savePath),
                Does.Contain("lastCompletedLevel"));
        }

        [Test]
        public void MissingFileStartsWithNoCompletedLevels()
        {
            var save = new SaveManager(_savePath);

            Assert.That(save.Data.LastCompletedLevel, Is.Zero);
        }
    }
}
