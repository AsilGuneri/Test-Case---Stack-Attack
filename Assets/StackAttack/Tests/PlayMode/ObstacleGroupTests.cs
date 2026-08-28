using System.Collections;
using NUnit.Framework;
using StackAttack.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace StackAttack.Tests
{
    public sealed class ObstacleGroupTests
    {
        [UnityTest]
        public IEnumerator RefreshColumns_DiscoversColumnsAndHonorsNestedBoundary()
        {
            var root = new GameObject("Root Group");
            var parent = root.AddComponent<ObstacleParent>();

            var directColumn = CreateColumn("Direct", root.transform, 2, 0.18f);
            var branch = new GameObject("Branch");
            branch.transform.SetParent(root.transform, false);
            var inactiveColumn = CreateColumn("Inactive", branch.transform, 1, 0.18f);
            inactiveColumn.gameObject.SetActive(false);

            var nestedRoot = new GameObject("Nested Group");
            nestedRoot.transform.SetParent(root.transform, false);
            var nestedParent = nestedRoot.AddComponent<ObstacleParent>();
            var nestedColumn = CreateColumn("Nested", nestedRoot.transform, 1, 0.18f);

            parent.RefreshColumns();
            nestedParent.RefreshColumns();

            Assert.That(Contains(parent, directColumn), Is.True);
            Assert.That(Contains(parent, inactiveColumn), Is.True);
            Assert.That(Contains(parent, nestedColumn), Is.False);
            Assert.That(parent.Columns.Count, Is.EqualTo(2));
            Assert.That(parent.Obstacles.Count, Is.EqualTo(3));
            Assert.That(nestedParent.Columns.Count, Is.EqualTo(1));
            Assert.That(nestedParent.Columns[0], Is.SameAs(nestedColumn));

            Object.Destroy(directColumn.gameObject);
            yield return null;

            parent.RefreshColumns();
            Assert.That(parent.Columns.Count, Is.EqualTo(1));
            Assert.That(parent.Columns[0], Is.SameAs(inactiveColumn));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OrbitBehaviour_MovesColumnsAsRigidUprightFormations()
        {
            var root = new GameObject("Orbit Group");
            var parent = root.AddComponent<ObstacleParent>();
            var orbit = root.AddComponent<ObstacleOrbitBehaviour>();
            orbit.DegreesPerSecond = 180f;

            var column = CreateColumn("Orbiting Column", root.transform, 2, 0.18f);
            column.transform.localPosition = new Vector3(2f, 0f, 0f);
            column.transform.rotation = Quaternion.Euler(0f, 0f, 15f);
            parent.RefreshColumns();

            var initialColumnPosition = column.transform.position;
            var initialColumnRotation = column.transform.rotation;
            var firstInitialPosition = column.Obstacles[0].transform.position;
            var secondInitialPosition = column.Obstacles[1].transform.position;
            var firstInitialRotation = column.Obstacles[0].transform.rotation;
            var secondInitialRotation = column.Obstacles[1].transform.rotation;
            var initialGap = secondInitialPosition - firstInitialPosition;

            yield return new WaitForSeconds(0.1f);

            var firstDisplacement =
                column.Obstacles[0].transform.position - firstInitialPosition;
            var secondDisplacement =
                column.Obstacles[1].transform.position - secondInitialPosition;

            Assert.That(column.transform.position.y, Is.GreaterThan(initialColumnPosition.y));
            Assert.That(
                Quaternion.Angle(column.transform.rotation, initialColumnRotation),
                Is.LessThan(0.001f));
            Assert.That(
                Vector3.Distance(firstDisplacement, secondDisplacement),
                Is.LessThan(0.001f));
            Assert.That(
                Vector3.Distance(
                    column.Obstacles[1].transform.position -
                    column.Obstacles[0].transform.position,
                    initialGap),
                Is.LessThan(0.001f));
            Assert.That(
                Quaternion.Angle(
                    column.Obstacles[0].transform.rotation,
                    firstInitialRotation),
                Is.LessThan(0.001f));
            Assert.That(
                Quaternion.Angle(
                    column.Obstacles[1].transform.rotation,
                    secondInitialRotation),
                Is.LessThan(0.001f));

            orbit.enabled = false;
            var disabledPosition = column.transform.position;
            yield return new WaitForSeconds(0.1f);
            Assert.That(
                Vector3.Distance(column.transform.position, disabledPosition),
                Is.LessThan(0.001f));

            Object.Destroy(orbit);
            yield return null;
            var replacementOrbit = root.AddComponent<ObstacleOrbitBehaviour>();
            replacementOrbit.DegreesPerSecond = 180f;
            yield return new WaitForSeconds(0.1f);
            Assert.That(
                Vector3.Distance(column.transform.position, disabledPosition),
                Is.GreaterThan(0.01f));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Damage_CompactsSurvivorsWithoutRefillingConfiguredAmount()
        {
            var columnObject = new GameObject("Damage Column");
            var column = CreateColumn(
                "Damage Column",
                null,
                4,
                0.18f,
                columnObject);

            column.Obstacles[0].TakeDamage(1);
            yield return null;

            Assert.That(column.Amount, Is.EqualTo(4));
            Assert.That(column.RemainingCount, Is.EqualTo(3));
            AssertCompact(column, 0.18f);

            column.Obstacles[1].TakeDamage(1);
            yield return null;

            Assert.That(column.Amount, Is.EqualTo(4));
            Assert.That(column.RemainingCount, Is.EqualTo(2));
            AssertCompact(column, 0.18f);

            Object.Destroy(columnObject);
            yield return null;
        }

        private static ObstacleColumn CreateColumn(
            string name,
            Transform parent,
            int amount,
            float spacing,
            GameObject existingObject = null)
        {
            var columnObject = existingObject ?? new GameObject(name);
            columnObject.name = name;
            if (parent != null)
            {
                columnObject.transform.SetParent(parent, false);
            }

            CreateObstacle("Obstacle 1", columnObject.transform);
            var column = columnObject.AddComponent<ObstacleColumn>();
            column.VerticalSpacing = spacing;
            column.DepthSpacing = 0.01f;
            column.Amount = amount;
            column.Synchronize();
            return column;
        }

        private static Obstacle CreateObstacle(string name, Transform parent)
        {
            var obstacleObject = new GameObject(name, typeof(BoxCollider2D));
            obstacleObject.transform.SetParent(parent, false);
            return obstacleObject.AddComponent<Obstacle>();
        }

        private static bool Contains(ObstacleParent parent, ObstacleColumn target)
        {
            for (var index = 0; index < parent.Columns.Count; index++)
            {
                if (parent.Columns[index] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertCompact(ObstacleColumn column, float spacing)
        {
            for (var index = 0; index < column.Obstacles.Count; index++)
            {
                Assert.That(
                    column.Obstacles[index].transform.localPosition,
                    Is.EqualTo(new Vector3(
                        0f,
                        spacing * index,
                        -0.01f * index)));
            }
        }
    }
}
