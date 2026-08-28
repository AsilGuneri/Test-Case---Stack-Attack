using UnityEngine;

namespace StackAttack.Gameplay
{
    [CreateAssetMenu(
        fileName = "Ammo Definition",
        menuName = "Stack Attack/Ammo Definition")]
    public sealed class AmmoDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Ball";
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField, Min(1)] private int baseDamage = 1;
        [SerializeField, Min(0)] private int basePiercing;
        [SerializeField, Min(1)] private int baseProjectileCount = 1;
        [SerializeField, Min(0.01f)] private float baseFireRate = 5f;
        [SerializeField, Min(0f)] private float projectileSpacing = 0.35f;

        public string DisplayName => displayName;
        public Projectile ProjectilePrefab => projectilePrefab;
        public int BaseDamage => baseDamage;
        public int BasePiercing => basePiercing;
        public int BaseProjectileCount => baseProjectileCount;
        public float BaseFireRate => baseFireRate;
        public float ProjectileSpacing => projectileSpacing;

        private void OnValidate()
        {
            baseDamage = Mathf.Max(1, baseDamage);
            basePiercing = Mathf.Max(0, basePiercing);
            baseProjectileCount = Mathf.Max(1, baseProjectileCount);
            baseFireRate = Mathf.Max(0.01f, baseFireRate);
            projectileSpacing = Mathf.Max(0f, projectileSpacing);
        }
    }
}
