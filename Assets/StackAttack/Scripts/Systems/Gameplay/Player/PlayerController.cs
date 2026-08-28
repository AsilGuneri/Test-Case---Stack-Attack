using UnityEngine;

namespace StackAttack.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerShooter))]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerController : MonoBehaviour
    {
        private const int ObstacleContactDamage = 1;

        [SerializeField] private LayerMask obstacleLayers = ~0;

        public PlayerMovement Movement { get; private set; }
        public PlayerShooter Shooter { get; private set; }
        public PlayerHealth Health { get; private set; }
        public LayerMask ObstacleLayers => obstacleLayers;

        private void Awake()
        {
            CacheComponents();
            Health.Died += HandleDied;
        }

        private void OnDestroy()
        {
            if (Health != null)
            {
                Health.Died -= HandleDied;
            }
        }

        public void Initialize()
        {
            CacheComponents();
            Movement.Initialize();
            Shooter.Initialize();
        }

        public void Move(float horizontalTouchDelta)
        {
            if (Health.IsDead)
            {
                return;
            }

            Movement.Move(horizontalTouchDelta);
        }

        public void SetFireHeld(bool isHeld)
        {
            if (Health.IsDead)
            {
                return;
            }

            Shooter.SetFireHeld(isHeld);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var otherCollider = collision.collider;
            var otherLayerMask = 1 << otherCollider.gameObject.layer;
            var obstacle = otherCollider.GetComponentInParent<Obstacle>();
            if ((obstacleLayers.value & otherLayerMask) == 0 || obstacle == null)
            {
                return;
            }

            var column = obstacle.GetComponentInParent<ObstacleColumn>();
            if (column != null && !column.IsBottomObstacle(obstacle))
            {
                return;
            }

            Health.TakeDamage(ObstacleContactDamage);
        }

        private void HandleDied()
        {
            Shooter.SetFireHeld(false);
            Movement.enabled = false;
            Shooter.enabled = false;
        }

        private void CacheComponents()
        {
            if (Movement == null)
            {
                Movement = GetComponent<PlayerMovement>();
            }

            if (Shooter == null)
            {
                Shooter = GetComponent<PlayerShooter>();
            }

            if (Health == null)
            {
                Health = GetComponent<PlayerHealth>();
            }
        }
    }
}
