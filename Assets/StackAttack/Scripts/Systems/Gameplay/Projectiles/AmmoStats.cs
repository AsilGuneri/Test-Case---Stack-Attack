namespace StackAttack.Gameplay
{
    public readonly struct AmmoStats
    {
        public AmmoStats(
            int damage,
            int piercing,
            int projectileCount,
            float fireRate,
            float projectileSpacing)
        {
            Damage = damage;
            Piercing = piercing;
            ProjectileCount = projectileCount;
            FireRate = fireRate;
            ProjectileSpacing = projectileSpacing;
        }

        public int Damage { get; }
        public int Piercing { get; }
        public int ProjectileCount { get; }
        public float FireRate { get; }
        public float ProjectileSpacing { get; }
    }
}
