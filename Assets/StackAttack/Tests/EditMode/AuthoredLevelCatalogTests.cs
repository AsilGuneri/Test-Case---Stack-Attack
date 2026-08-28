using NUnit.Framework;
using StackAttack.Gameplay;
using UnityEditor;

namespace StackAttack.Tests
{
    public sealed class AuthoredLevelCatalogTests
    {
        private const string CatalogPath =
            "Assets/StackAttack/Config/Levels/LevelCatalog.asset";

        [Test]
        public void Catalog_ContainsFiveIncreasingValidLevels()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(
                CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var previousHeight = 0;
            for (var levelNumber = 1; levelNumber <= 5; levelNumber++)
            {
                var level = catalog.GetRequiredLevel(levelNumber);
                Assert.That(level.GridSize.x, Is.EqualTo(6));
                Assert.That(level.GridSize.y, Is.GreaterThan(previousHeight));
                previousHeight = level.GridSize.y;

                var occupiedCount = 0;
                for (var y = 0; y < level.GridSize.y; y++)
                {
                    for (var x = 0; x < level.GridSize.x; x++)
                    {
                        var cell = level.GetCell(x, y);
                        if (!cell.IsEmpty)
                        {
                            occupiedCount++;
                        }

                        if (cell.HasGroup)
                        {
                            Assert.That(
                                level.TryGetGroup(cell.GroupId, out _),
                                Is.True,
                                $"Level {levelNumber} cell ({x}, {y}) " +
                                "references a missing group.");
                        }
                    }
                }

                Assert.That(occupiedCount, Is.GreaterThan(0));
                foreach (var group in level.Groups)
                {
                    Assert.That(group, Is.Not.Null);
                    Assert.That(group.Behaviours, Is.Not.Empty);
                    Assert.That(group.Behaviours, Has.None.Null);
                }
            }
        }
    }
}
