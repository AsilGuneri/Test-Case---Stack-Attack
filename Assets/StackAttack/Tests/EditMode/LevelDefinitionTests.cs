using System;
using NUnit.Framework;
using StackAttack.Gameplay;
using UnityEditor;
using UnityEngine;

namespace StackAttack.Tests
{
    public sealed class LevelDefinitionTests
    {
        [Test]
        public void ResizeGrid_PreservesBottomLeftOverlapAndCreatesEmptyCells()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            try
            {
                level.ResizeGrid(new Vector2Int(2, 2));
                level.SetObstacleAmount(0, 0, 1);
                level.SetCellColor(0, 0, Color.red);
                level.SetObstacleAmount(1, 0, 2);
                level.SetObstacleAmount(0, 1, 3);
                level.SetObstacleAmount(1, 1, 4);

                level.ResizeGrid(new Vector2Int(3, 3));

                Assert.That(level.GridSize, Is.EqualTo(new Vector2Int(3, 3)));
                Assert.That(level.GetCell(0, 0).ObstacleAmount, Is.EqualTo(1));
                Assert.That(level.GetCell(0, 0).Color, Is.EqualTo(Color.red));
                Assert.That(level.GetCell(1, 0).ObstacleAmount, Is.EqualTo(2));
                Assert.That(level.GetCell(0, 1).ObstacleAmount, Is.EqualTo(3));
                Assert.That(level.GetCell(1, 1).ObstacleAmount, Is.EqualTo(4));
                Assert.That(level.GetCell(2, 0).IsEmpty, Is.True);
                Assert.That(level.GetCell(0, 2).IsEmpty, Is.True);

                level.ResizeGrid(new Vector2Int(1, 2));

                Assert.That(level.GetCell(0, 0).ObstacleAmount, Is.EqualTo(1));
                Assert.That(level.GetCell(0, 1).ObstacleAmount, Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void GetCell_WithInvalidCoordinates_Throws()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            try
            {
                level.ResizeGrid(new Vector2Int(2, 2));

                Assert.Throws<ArgumentOutOfRangeException>(
                    () => level.GetCell(-1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => level.GetCell(0, 2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void Catalog_ResolvesUniqueLevelsAndRejectsDuplicates()
        {
            var first = CreateLevel(1);
            var second = CreateLevel(2);
            var catalog = ScriptableObject.CreateInstance<LevelCatalog>();

            try
            {
                SetCatalogLevels(catalog, first, second);

                Assert.That(catalog.TryGetLevel(2, out var found), Is.True);
                Assert.That(found, Is.SameAs(second));
                Assert.That(catalog.TryGetLevel(3, out _), Is.False);
                Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
                    () => catalog.GetRequiredLevel(3));

                SetLevelNumber(second, 1);
                Assert.Throws<InvalidOperationException>(
                    () => catalog.TryGetLevel(1, out _));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void Groups_UseStableAssignmentsAndRemovalClearsCells()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            try
            {
                level.ResizeGrid(new Vector2Int(2, 2));
                var group = level.AddGroup("Orbiters");
                level.SetCellGroup(1, 0, group.Id);

                level.ResizeGrid(new Vector2Int(3, 3));

                Assert.That(level.GetCell(1, 0).GroupId, Is.EqualTo(group.Id));
                Assert.That(level.TryGetGroup(group.Id, out var found), Is.True);
                Assert.That(found.DisplayName, Is.EqualTo("Orbiters"));

                Assert.That(level.RemoveGroup(group.Id), Is.True);
                Assert.That(level.GetCell(1, 0).HasGroup, Is.False);
                Assert.That(level.TryGetGroup(group.Id, out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(level);
            }
        }

        private static LevelDefinition CreateLevel(int levelNumber)
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            SetLevelNumber(level, levelNumber);
            return level;
        }

        private static void SetLevelNumber(
            LevelDefinition level,
            int levelNumber)
        {
            var serializedLevel = new SerializedObject(level);
            serializedLevel.FindProperty("levelNumber").intValue = levelNumber;
            serializedLevel.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetCatalogLevels(
            LevelCatalog catalog,
            params LevelDefinition[] levels)
        {
            var serializedCatalog = new SerializedObject(catalog);
            var levelsProperty = serializedCatalog.FindProperty("levels");
            levelsProperty.arraySize = levels.Length;
            for (var index = 0; index < levels.Length; index++)
            {
                levelsProperty.GetArrayElementAtIndex(index).objectReferenceValue =
                    levels[index];
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
