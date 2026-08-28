using StackAttack.Game;
using UnityEngine;
using VContainer;

namespace StackAttack.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;

        private IPlayerAmmoService _ammo;
        private IUpgradeService _upgrades;
        private IGameFlowManager _gameFlow;
        private bool _isInitialized;
        private bool _isFireHeld;
        private float _nextFireTime;

        public bool IsInitialized => _isInitialized;
        public bool IsFireHeld => _isFireHeld;

        [Inject]
        public void Construct(
            IPlayerAmmoService ammo,
            IUpgradeService upgrades,
            IGameFlowManager gameFlow)
        {
            _ammo = ammo;
            _upgrades = upgrades;
            _gameFlow = gameFlow;
            _gameFlow.StateChanged += HandleGameFlowChanged;
        }

        public void Initialize()
        {
            if (_ammo == null || _upgrades == null || _gameFlow == null)
            {
                throw new System.InvalidOperationException(
                    "PlayerShooter must be constructed by GameLifetimeScope.");
            }

            if (firePoint == null)
            {
                throw new MissingReferenceException(
                    "PlayerShooter requires a Fire Point.");
            }

            _isInitialized = true;
        }

        public void SetFireHeld(bool isHeld)
        {
            if (isHeld && !_gameFlow.IsGameplayActive)
            {
                return;
            }

            if (_isFireHeld == isHeld)
            {
                return;
            }

            _isFireHeld = isHeld;
            if (isHeld)
            {
                _nextFireTime = Time.time;
            }
        }

        private void Update()
        {
            if (!_isInitialized || !_isFireHeld ||
                !_gameFlow.IsGameplayActive || Time.time < _nextFireTime)
            {
                return;
            }

            ShootVolley();
            _nextFireTime = Time.time + GetFireInterval();
        }

        private void ShootVolley()
        {
            var stats = _ammo.Stats;
            var prefab = _ammo.CurrentAmmo.ProjectilePrefab;
            var centerIndex = (stats.ProjectileCount - 1) * 0.5f;

            for (var index = 0; index < stats.ProjectileCount; index++)
            {
                var horizontalOffset =
                    (index - centerIndex) * stats.ProjectileSpacing;
                var projectile = Instantiate(
                    prefab,
                    firePoint.position + Vector3.right * horizontalOffset,
                    Quaternion.identity);

                // Player tilt is visual feedback; every ball remains parallel.
                projectile.Launch(
                    Vector2.up,
                    stats.Damage,
                    stats.Piercing,
                    _upgrades.ReportClearedColumn);
            }
        }

        private float GetFireInterval()
        {
            return 1f / Mathf.Max(0.01f, _ammo.Stats.FireRate);
        }

        private void HandleGameFlowChanged(GameFlowState state)
        {
            if (state != GameFlowState.Playing)
            {
                _isFireHeld = false;
            }
        }

        private void OnDisable()
        {
            _isFireHeld = false;
        }

        private void OnDestroy()
        {
            if (_gameFlow != null)
            {
                _gameFlow.StateChanged -= HandleGameFlowChanged;
            }
        }
    }
}
