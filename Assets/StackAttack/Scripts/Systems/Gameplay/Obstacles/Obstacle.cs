using System;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Obstacle : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maximumHealth = 1;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Color _restingColor;
        private bool _hasRestingColor;

        public int CurrentHealth { get; private set; }
        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public event Action<Obstacle> Died;

        private void Awake()
        {
            CacheRestingColor();
            CurrentHealth = maximumHealth;
        }

        public void SetColor(Color color)
        {
            CacheSpriteRenderer();
            if (spriteRenderer == null)
            {
                throw new MissingReferenceException(
                    $"Obstacle '{name}' requires a SpriteRenderer.");
            }

            _restingColor = color;
            _hasRestingColor = true;
            spriteRenderer.color = color;
        }

        internal void ApplyHitFlash(float intensity)
        {
            CacheSpriteRenderer();
            if (spriteRenderer == null)
            {
                return;
            }

            CacheRestingColor();
            spriteRenderer.color = Color.Lerp(
                _restingColor,
                Color.white,
                Mathf.Clamp01(intensity));
        }

        internal void RestoreVisualColor()
        {
            if (!_hasRestingColor || spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.color = _restingColor;
        }

        public int TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0)
            {
                return 0;
            }

            var acceptedDamage = Mathf.Min(CurrentHealth, amount);
            CurrentHealth -= acceptedDamage;
            if (CurrentHealth == 0)
            {
                Died?.Invoke(this);
                Destroy(gameObject);
            }

            return acceptedDamage;
        }

        private void OnValidate()
        {
            CacheSpriteRenderer();
        }

        private void CacheSpriteRenderer()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void CacheRestingColor()
        {
            CacheSpriteRenderer();
            if (!_hasRestingColor && spriteRenderer != null)
            {
                _restingColor = spriteRenderer.color;
                _hasRestingColor = true;
            }
        }
    }
}
