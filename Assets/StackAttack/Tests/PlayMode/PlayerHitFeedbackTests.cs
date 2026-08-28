using System.Collections;
using System.Reflection;
using NUnit.Framework;
using StackAttack.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace StackAttack.Tests
{
    public sealed class PlayerHitFeedbackTests
    {
        [UnityTest]
        public IEnumerator BlinkSequence_MatchesSafetyTimeAndRestoresVisual()
        {
            var player = new GameObject("Player");
            player.SetActive(false);
            var health = player.AddComponent<PlayerHealth>();
            health.InvulnerabilityDuration = 0.12f;

            var visualObject = new GameObject(
                "Player Visual",
                typeof(SpriteRenderer));
            visualObject.transform.SetParent(player.transform, false);
            var visual = visualObject.GetComponent<SpriteRenderer>();
            visual.color = new Color(0.7f, 0.8f, 0.9f, 0.8f);

            var feedback = player.AddComponent<PlayerHitFeedback>();
            SetField(feedback, "playerVisual", visual);
            SetField(feedback, "blinkCount", 1);
            player.SetActive(true);

            health.TakeDamage(1);

            Assert.That(feedback.IsBlinking, Is.True);
            Assert.That(
                feedback.CurrentBlinkDuration,
                Is.EqualTo(health.InvulnerabilityDuration).Within(0.001f));

            yield return new WaitForSeconds(0.04f);
            Assert.That(visual.color.a, Is.LessThan(0.8f));

            yield return new WaitForSeconds(0.1f);
            Assert.That(health.IsInvulnerable, Is.False);
            Assert.That(feedback.IsBlinking, Is.False);
            Assert.That(visual.color.a, Is.EqualTo(0.8f).Within(0.001f));

            Object.Destroy(player);
            yield return null;
        }

        [Test]
        public void LethalDamage_DoesNotStartBlinking()
        {
            var player = new GameObject("Player");
            player.SetActive(false);
            var health = player.AddComponent<PlayerHealth>();
            var visualObject = new GameObject(
                "Player Visual",
                typeof(SpriteRenderer));
            visualObject.transform.SetParent(player.transform, false);
            var feedback = player.AddComponent<PlayerHitFeedback>();
            SetField(
                feedback,
                "playerVisual",
                visualObject.GetComponent<SpriteRenderer>());
            player.SetActive(true);

            health.TakeDamage(health.MaximumHealth);

            Assert.That(health.IsDead, Is.True);
            Assert.That(feedback.IsBlinking, Is.False);
            Object.DestroyImmediate(player);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
