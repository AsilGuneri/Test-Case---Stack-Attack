using System.Collections;
using NUnit.Framework;
using StackAttack.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace StackAttack.Tests
{
    public sealed class ObstacleHitFeedbackTests
    {
        private GameObject _columnObject;

        [UnityTest]
        public IEnumerator DestroyedTopObstacle_TransfersFadeToNextTopObstacle()
        {
            _columnObject = new GameObject("Column");
            _columnObject.SetActive(false);

            CreateObstacle("Obstacle 1", Color.red, 0f);
            CreateObstacle("Obstacle 2", Color.green, 0.18f);

            var column = _columnObject.AddComponent<ObstacleColumn>();
            column.Amount = 2;
            _columnObject.SetActive(true);

            var nextObstacle = column.Obstacles[0];
            var topObstacle = column.Obstacles[1];
            nextObstacle.SetColor(Color.red);

            Assert.That(column.TakeDamage(1), Is.EqualTo(1));
            Assert.That(column.RemainingCount, Is.EqualTo(1));
            Assert.That(column.Obstacles[0], Is.Not.SameAs(topObstacle));
            Assert.That(column.Obstacles[0], Is.SameAs(nextObstacle));
            AssertColor(nextObstacle.SpriteRenderer.color, Color.white);

            yield return new WaitForSeconds(column.HitFadeDuration + 0.05f);

            AssertColor(nextObstacle.SpriteRenderer.color, Color.red);
        }

        [TearDown]
        public void TearDown()
        {
            if (_columnObject != null)
            {
                Object.DestroyImmediate(_columnObject);
            }
        }

        private void CreateObstacle(string name, Color color, float localY)
        {
            var obstacleObject = new GameObject(
                name,
                typeof(SpriteRenderer),
                typeof(BoxCollider2D),
                typeof(Obstacle));
            obstacleObject.transform.SetParent(_columnObject.transform, false);
            obstacleObject.transform.localPosition = new Vector3(0f, localY, 0f);
            obstacleObject.GetComponent<SpriteRenderer>().color = color;
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.01f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.01f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.01f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.01f));
        }
    }
}
