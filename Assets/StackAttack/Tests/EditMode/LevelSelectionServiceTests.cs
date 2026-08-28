using System;
using System.IO;
using NUnit.Framework;
using StackAttack.Gameplay;
using StackAttack.Systems;
using UnityEditor;
using UnityEngine;

namespace StackAttack.Tests
{
    public sealed class LevelSelectionServiceTests
    {
        private string _savePath;
        private LevelCatalog _catalog;
        private LevelDefinition _levelOne;
        private LevelDefinition _levelTwo;

        [SetUp]
        public void SetUp()
        {
            _savePath = Path.Combine(
                Path.GetTempPath(),
                $"StackAttack-{Guid.NewGuid():N}.json");
            _levelOne = CreateLevel(1);
            _levelTwo = CreateLevel(2);
            _catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            SetCatalogLevels(_catalog, _levelOne, _levelTwo);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }

            UnityEngine.Object.DestroyImmediate(_catalog);
            UnityEngine.Object.DestroyImmediate(_levelOne);
            UnityEngine.Object.DestroyImmediate(_levelTwo);
        }

        [Test]
        public void DefaultsToLevelOneAndPublishesChanges()
        {
            var service = CreateService();
            var publishedLevel = 0;
            service.LevelChanged += level => publishedLevel = level;

            service.SelectLevel(2);

            Assert.That(service.CurrentLevelNumber, Is.EqualTo(2));
            Assert.That(publishedLevel, Is.EqualTo(2));
        }

        [Test]
        public void StartsAtLevelAfterLastCompletedLevel()
        {
            var save = new SaveManager(_savePath);
            save.MarkLevelCompleted(1);

            var service = new LevelSelectionService(save, _catalog);

            Assert.That(service.CurrentLevelNumber, Is.EqualTo(2));
        }

        [Test]
        public void ReplaysLatestLevelWhenNoLaterLevelExists()
        {
            var save = new SaveManager(_savePath);
            save.MarkLevelCompleted(2);

            var service = new LevelSelectionService(save, _catalog);

            Assert.That(service.CurrentLevelNumber, Is.EqualTo(2));
        }

        [Test]
        public void SelectingCurrentLevelDoesNotPublishDuplicateChange()
        {
            var service = CreateService();
            var changeCount = 0;
            service.LevelChanged += _ => changeCount++;

            service.SelectLevel(1);

            Assert.That(changeCount, Is.Zero);
        }

        [Test]
        public void RejectsInvalidOrUnregisteredLevelNumbers()
        {
            var service = CreateService();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => service.SelectLevel(0));
            Assert.Throws<ArgumentException>(
                () => service.SelectLevel(3));
        }

        private LevelSelectionService CreateService()
        {
            return new LevelSelectionService(
                new SaveManager(_savePath),
                _catalog);
        }

        private static LevelDefinition CreateLevel(int levelNumber)
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            var serialized = new SerializedObject(level);
            serialized.FindProperty("levelNumber").intValue = levelNumber;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return level;
        }

        private static void SetCatalogLevels(
            LevelCatalog catalog,
            params LevelDefinition[] levels)
        {
            var serialized = new SerializedObject(catalog);
            var property = serialized.FindProperty("levels");
            property.arraySize = levels.Length;
            for (var index = 0; index < levels.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    levels[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
