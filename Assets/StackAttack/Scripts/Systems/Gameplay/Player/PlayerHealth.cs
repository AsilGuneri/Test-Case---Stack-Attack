using System;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour, IPlayerHealth
    {
        [SerializeField, Min(1)] private int maximumHealth = 5;
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 3.5f;

        private float _invulnerableUntil;

        public int MaximumHealth => maximumHealth;
        public int CurrentHealth { get; private set; }
        public bool IsInvulnerable { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        public float InvulnerabilityDuration
        {
            get => invulnerabilityDuration;
            set
            {
                invulnerabilityDuration = Mathf.Max(0f, value);
                if (Mathf.Approximately(invulnerabilityDuration, 0f))
                {
                    SetInvulnerable(false);
                }
            }
        }

        public event Action<int> HealthChanged;
        public event Action<int> MaximumHealthChanged;
        public event Action<bool> InvulnerabilityChanged;
        public event Action Died;

        private void Awake()
        {
            maximumHealth = Mathf.Max(1, maximumHealth);
            invulnerabilityDuration = Mathf.Max(0f, invulnerabilityDuration);
            CurrentHealth = maximumHealth;
        }

        private void Update()
        {
            if (IsInvulnerable && Time.time >= _invulnerableUntil)
            {
                SetInvulnerable(false);
            }
        }

        private void OnValidate()
        {
            maximumHealth = Mathf.Max(1, maximumHealth);
            invulnerabilityDuration = Mathf.Max(0f, invulnerabilityDuration);
        }

        public int TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead || IsInvulnerable)
            {
                return 0;
            }

            var acceptedDamage = Mathf.Min(CurrentHealth, amount);
            CurrentHealth -= acceptedDamage;
            HealthChanged?.Invoke(CurrentHealth);

            if (IsDead)
            {
                SetInvulnerable(false);
                Died?.Invoke();
                return acceptedDamage;
            }

            if (invulnerabilityDuration > 0f)
            {
                _invulnerableUntil = Time.time + invulnerabilityDuration;
                SetInvulnerable(true);
            }

            return acceptedDamage;
        }

        public bool CanIncreaseMaximumHealth(int maximumValue)
        {
            return !IsDead && maximumHealth < Mathf.Max(1, maximumValue);
        }

        public int IncreaseMaximumHealth(int amount, int maximumValue)
        {
            if (amount <= 0 || !CanIncreaseMaximumHealth(maximumValue))
            {
                return 0;
            }

            var increase = Mathf.Min(amount, maximumValue - maximumHealth);
            maximumHealth += increase;
            CurrentHealth += increase;
            MaximumHealthChanged?.Invoke(maximumHealth);
            HealthChanged?.Invoke(CurrentHealth);
            return increase;
        }

        private void SetInvulnerable(bool isInvulnerable)
        {
            if (IsInvulnerable == isInvulnerable)
            {
                return;
            }

            IsInvulnerable = isInvulnerable;
            InvulnerabilityChanged?.Invoke(IsInvulnerable);
        }
    }
}
