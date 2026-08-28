using NUnit.Framework;
using StackAttack.Gameplay;
using UnityEditor;
using UnityEngine;

namespace StackAttack.Tests
{
    public sealed class ObstacleColumnEditorTests
    {
        private const string ColumnPrefabPath =
            "Assets/StackAttack/Prefabs/Obstacles/ObstacleColumn.prefab";

        [Test]
        public void Synchronize_UsesPrefabBaseAndMaintainsExactSpacing()
        {
            var columnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ColumnPrefabPath);
            Assert.That(columnPrefab, Is.Not.Null);

            var columnObject = PrefabUtility.InstantiatePrefab(columnPrefab)
                as GameObject;
            try
            {
                Assert.That(columnObject, Is.Not.Null);
                var column = columnObject.GetComponent<ObstacleColumn>();
                Assert.That(column, Is.Not.Null);
                column.VerticalSpacing = 0.2f;
                column.DepthSpacing = 0.01f;
                column.Amount = 4;
                column.Synchronize();

                Assert.That(column.RemainingCount, Is.EqualTo(4));
                for (var index = 0; index < column.Obstacles.Count; index++)
                {
                    var obstacle = column.Obstacles[index];
                    Assert.That(obstacle.name, Is.EqualTo($"Obstacle {index + 1}"));
                    Assert.That(
                        obstacle.transform.localPosition,
                        Is.EqualTo(new Vector3(
                            0f,
                            0.2f * index,
                            -0.01f * index)));
                    Assert.That(
                        PrefabUtility.IsPartOfPrefabInstance(obstacle.gameObject),
                        Is.True);
                }

                column.Amount = 2;
                column.Synchronize();

                Assert.That(column.RemainingCount, Is.EqualTo(2));
                Assert.That(column.transform.childCount, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(columnObject);
            }
        }
    }
}
