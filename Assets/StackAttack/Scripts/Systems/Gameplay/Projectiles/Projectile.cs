using System;
using System.Collections.Generic;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Projectile : MonoBehaviour
    {
        private const string DamageableLayerName = "Damageable";

        [SerializeField, Min(0f)] private float movementSpeed = 15f;
        [SerializeField, Min(0f)] private float lifetime = 5f;
        private Rigidbody2D _rigidbody;
        private Vector2 _direction;
        private int _damageableLayer;
        private int _damage = 1;
        private int _remainingPiercing;
        private Action<int> _columnCleared;
        private readonly HashSet<int> _damagedTargets = new();
        private bool _isLaunched;

        public bool IsLaunched => _isLaunched;
        public Vector2 Direction => _direction;
        public int Damage => _damage;
        public int RemainingPiercing => _remainingPiercing;

        public void Launch(Vector2 direction)
        {
            Launch(direction, 1, 0);
        }

        public void Launch(
            Vector2 direction,
            int damage,
            int piercing,
            Action<int> columnCleared = null)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                throw new ArgumentException(
                    "A projectile requires a non-zero launch direction.",
                    nameof(direction));
            }

            CacheRigidbody();
            _direction = direction.normalized;
            _damage = Mathf.Max(1, damage);
            _remainingPiercing = Mathf.Max(0, piercing);
            _columnCleared = columnCleared;
            _damagedTargets.Clear();
            _isLaunched = true;

            if (lifetime > 0f)
            {
                Destroy(gameObject, lifetime);
            }
        }

        private void Awake()
        {
            CacheRigidbody();
            _damageableLayer = LayerMask.NameToLayer(DamageableLayerName);
            if (_damageableLayer < 0)
            {
                throw new InvalidOperationException(
                    $"Projectile requires a '{DamageableLayerName}' layer.");
            }
        }

        private void FixedUpdate()
        {
            if (!_isLaunched)
            {
                return;
            }

            var movement = _direction * (movementSpeed * Time.fixedDeltaTime);
            _rigidbody.MovePosition(_rigidbody.position + movement);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isLaunched || other.gameObject.layer != _damageableLayer)
            {
                return;
            }

            var column = other.GetComponentInParent<ObstacleColumn>();
            var damageable = column != null
                ? column
                : other.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                Debug.LogWarning(
                    $"'{other.name}' is on the {DamageableLayerName} layer " +
                    $"but does not implement {nameof(IDamageable)}.",
                    other);
                return;
            }

            var target = damageable as Component;
            if (target != null && !_damagedTargets.Add(target.GetInstanceID()))
            {
                return;
            }

            var acceptedDamage = damageable.TakeDamage(_damage);
            if (acceptedDamage > 0 && column != null &&
                column.RemainingCount == 0)
            {
                _columnCleared?.Invoke(column.Amount);
            }

            if (_remainingPiercing > 0)
            {
                _remainingPiercing--;
                return;
            }

            _isLaunched = false;
            Destroy(gameObject);
        }

        private void CacheRigidbody()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }
        }
    }
}
