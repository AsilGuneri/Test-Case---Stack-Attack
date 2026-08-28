using System.Collections;
using NUnit.Framework;
using StackAttack.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace StackAttack.Tests
{
    public sealed class PlayerHealthTests
    {
        [UnityTest]
        public IEnumerator Damage_TracksHeartsAndInvulnerability()
        {
            var player = new GameObject("Player");
            var health = player.AddComponent<PlayerHealth>();
            health.InvulnerabilityDuration = 0.05f;

            var healthChanges = 0;
            var invulnerabilityChanges = 0;
            health.HealthChanged += _ => healthChanges++;
            health.InvulnerabilityChanged += _ => invulnerabilityChanges++;

            Assert.That(health.MaximumHealth, Is.EqualTo(5));
            Assert.That(health.CurrentHealth, Is.EqualTo(5));
            Assert.That(health.IsDead, Is.False);
            Assert.That(health.IsInvulnerable, Is.False);

            health.TakeDamage(1);

            Assert.That(health.CurrentHealth, Is.EqualTo(4));
            Assert.That(health.IsInvulnerable, Is.True);
            Assert.That(healthChanges, Is.EqualTo(1));
            Assert.That(invulnerabilityChanges, Is.EqualTo(1));

            health.TakeDamage(1);
            Assert.That(health.CurrentHealth, Is.EqualTo(4));
            Assert.That(healthChanges, Is.EqualTo(1));

            yield return new WaitForSeconds(0.06f);

            Assert.That(health.IsInvulnerable, Is.False);
            Assert.That(invulnerabilityChanges, Is.EqualTo(2));

            Object.Destroy(player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ObstacleContact_DamagesOnlyOnFreshContactEntry()
        {
            var player = CreatePhysicsPlayer();
            var contactPosition = new Vector3(1000f, 1000f);
            player.transform.position = contactPosition;
            var health = player.GetComponent<PlayerHealth>();
            health.InvulnerabilityDuration = 0.05f;
            var obstacle = CreateObstacle("Obstacle", contactPosition);

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(health.CurrentHealth, Is.EqualTo(4));

            var simultaneousObstacle = CreateObstacle(
                "Simultaneous Obstacle",
                contactPosition);
            yield return new WaitForFixedUpdate();
            Assert.That(
                health.CurrentHealth,
                Is.EqualTo(4),
                "A second contact during invulnerability must be ignored.");
            Object.Destroy(simultaneousObstacle);

            yield return new WaitForSeconds(0.08f);
            yield return new WaitForFixedUpdate();

            Assert.That(health.IsInvulnerable, Is.False);
            Assert.That(
                health.CurrentHealth,
                Is.EqualTo(4),
                "Continuous overlap must not repeatedly damage the player.");

            obstacle.transform.position = contactPosition + Vector3.right * 5f;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            obstacle.transform.position = contactPosition;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(health.CurrentHealth, Is.EqualTo(3));

            Object.Destroy(player);
            Object.Destroy(obstacle);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NonObstacleCollision_IsIgnored()
        {
            var player = CreatePhysicsPlayer();
            var isolatedPosition = new Vector3(1100f, 1100f);
            player.transform.position = isolatedPosition;
            var health = player.GetComponent<PlayerHealth>();
            var unrelatedDamageable = new GameObject(
                "Unrelated Damageable",
                typeof(BoxCollider2D));
            unrelatedDamageable.layer = LayerMask.NameToLayer("Damageable");
            unrelatedDamageable.transform.position = isolatedPosition;

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(health.CurrentHealth, Is.EqualTo(5));
            Assert.That(health.IsInvulnerable, Is.False);

            Object.Destroy(player);
            Object.Destroy(unrelatedDamageable);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StackedObstacle_OnlyBottomBlockDamagesPlayer()
        {
            var player = CreatePhysicsPlayer();
            var isolatedPosition = new Vector3(1200f, 1200f);
            var playerCollider = player.GetComponentInChildren<BoxCollider2D>();
            playerCollider.size = Vector2.one * 0.1f;

            var columnObject = new GameObject("Column");
            columnObject.transform.position = isolatedPosition;
            columnObject.SetActive(false);
            var bottom = CreateObstacle("Obstacle 1", Vector3.zero);
            bottom.transform.SetParent(columnObject.transform, false);
            bottom.GetComponent<BoxCollider2D>().size = Vector2.one * 0.1f;
            var top = CreateObstacle("Obstacle 2", new Vector3(0f, 0.3f));
            top.transform.SetParent(columnObject.transform, false);
            top.GetComponent<BoxCollider2D>().size = Vector2.one * 0.1f;
            var column = columnObject.AddComponent<ObstacleColumn>();
            column.Amount = 2;
            columnObject.SetActive(true);
            column.RefreshObstacles();

            player.transform.position = top.transform.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(player.GetComponent<PlayerHealth>().CurrentHealth,
                Is.EqualTo(5), "The upper stack blocks must not hurt the player.");

            player.transform.position = Vector3.right * 5f;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            player.transform.position = bottom.transform.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(player.GetComponent<PlayerHealth>().CurrentHealth,
                Is.EqualTo(4), "The bottom stack block should hurt the player.");

            Object.Destroy(player);
            Object.Destroy(columnObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Death_FiresOnceAndStopsPlayerGameplay()
        {
            var player = new GameObject("Player");
            var movement = player.AddComponent<PlayerMovement>();
            var shooter = player.AddComponent<PlayerShooter>();
            var health = player.AddComponent<PlayerHealth>();
            var controller = player.AddComponent<PlayerController>();
            health.InvulnerabilityDuration = 0f;

            var deathCount = 0;
            var healthChanges = 0;
            health.Died += () => deathCount++;
            health.HealthChanged += _ => healthChanges++;

            health.TakeDamage(1);
            health.TakeDamage(1);
            health.TakeDamage(10);
            health.TakeDamage(1);

            Assert.That(controller.Health, Is.SameAs(health));
            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(health.IsDead, Is.True);
            Assert.That(health.IsInvulnerable, Is.False);
            Assert.That(healthChanges, Is.EqualTo(3));
            Assert.That(deathCount, Is.EqualTo(1));
            Assert.That(movement.enabled, Is.False);
            Assert.That(shooter.enabled, Is.False);

            controller.Move(100f);
            controller.SetFireHeld(true);
            Assert.That(shooter.IsFireHeld, Is.False);

            Object.Destroy(player);
            yield return null;
        }

        private static GameObject CreatePhysicsPlayer()
        {
            var player = new GameObject("Player");
            var body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.useFullKinematicContacts = true;
            player.AddComponent<PlayerHealth>();
            player.AddComponent<PlayerMovement>();
            player.AddComponent<PlayerShooter>();
            player.AddComponent<PlayerController>();

            var model = new GameObject("PlayerModel", typeof(BoxCollider2D));
            model.transform.SetParent(player.transform, false);
            return player;
        }

        private static GameObject CreateObstacle(string name, Vector3 position)
        {
            var obstacle = new GameObject(
                name,
                typeof(BoxCollider2D),
                typeof(Obstacle));
            obstacle.layer = LayerMask.NameToLayer("Damageable");
            obstacle.transform.position = position;
            return obstacle;
        }
    }
}
