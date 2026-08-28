using System;

namespace StackAttack.Gameplay
{
    public interface IPlayerHealth : IDamageable
    {
        int MaximumHealth { get; }
        int CurrentHealth { get; }
        float InvulnerabilityDuration { get; set; }
        bool IsInvulnerable { get; }
        bool IsDead { get; }

        event Action<int> HealthChanged;
        event Action<int> MaximumHealthChanged;
        event Action<bool> InvulnerabilityChanged;
        event Action Died;

        bool CanIncreaseMaximumHealth(int maximumValue);
        int IncreaseMaximumHealth(int amount, int maximumValue);
    }
}
