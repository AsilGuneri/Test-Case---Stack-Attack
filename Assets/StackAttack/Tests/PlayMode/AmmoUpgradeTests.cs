using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StackAttack.Game;
using StackAttack.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace StackAttack.Tests
{
    public sealed class AmmoUpgradeTests
    {
        private readonly List<Object> _createdObjects = new();
        private float _originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _originalTimeScale;
            for (var index = _createdObjects.Count - 1; index >= 0; index--)
            {
                if (_createdObjects[index] != null)
                {
                    Object.DestroyImmediate(_createdObjects[index]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void AmmoUpgrades_ApplyAndClampEveryBallStat()
        {
            var runtime = CreateRuntime();
            var ammo = runtime.Ammo;

            for (var upgradeIndex = 0;
                 upgradeIndex < runtime.Upgrades.Count;
                 upgradeIndex++)
            {
                var upgrade = runtime.Upgrades[upgradeIndex];
                for (var application = 0; application < 20; application++)
                {
                    ammo.ApplyUpgrade(upgrade);
                }

                Assert.That(ammo.CanApply(upgrade), Is.False);
            }

            Assert.That(ammo.Stats.Damage, Is.EqualTo(5));
            Assert.That(ammo.Stats.Piercing, Is.EqualTo(3));
            Assert.That(ammo.Stats.ProjectileCount, Is.EqualTo(5));
            Assert.That(ammo.Stats.FireRate, Is.EqualTo(10f));
            Assert.That(ammo.Stats.ProjectileSpacing, Is.EqualTo(0.35f));
        }

        [Test]
        public void UpgradeMeter_SaturatesAndOffersDistinctChoices()
        {
            var runtime = CreateRuntime();
            runtime.Flow.BeginGameplay();

            runtime.Service.ReportClearedColumn(7);

            Assert.That(runtime.Service.CurrentProgress, Is.EqualTo(5));
            Assert.That(runtime.Service.NormalizedProgress, Is.EqualTo(1f));
            Assert.That(runtime.Service.CurrentOffers.Count, Is.EqualTo(3));
            Assert.That(
                runtime.Service.CurrentOffers.Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(runtime.Flow.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            var selected = runtime.Service.CurrentOffers[0];
            Assert.That(runtime.Service.SelectUpgrade(0), Is.True);
            Assert.That(runtime.Service.CurrentProgress, Is.Zero);
            Assert.That(runtime.Service.CurrentOffers, Is.Empty);
            Assert.That(selected, Is.Not.Null);
            Assert.That(runtime.Flow.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void UpgradeOffers_HideCappedEntriesAndStopWhenExhausted()
        {
            var runtime = CreateRuntime(2);
            runtime.Flow.BeginGameplay();
            runtime.Ammo.ApplyUpgrade(runtime.Upgrades[0]);
            runtime.Ammo.ApplyUpgrade(runtime.Upgrades[0]);
            runtime.Ammo.ApplyUpgrade(runtime.Upgrades[0]);
            runtime.Ammo.ApplyUpgrade(runtime.Upgrades[0]);

            runtime.Service.ReportClearedColumn(5);

            Assert.That(runtime.Service.CurrentOffers.Count, Is.EqualTo(1));
            Assert.That(runtime.Service.CurrentOffers[0], Is.SameAs(runtime.Upgrades[1]));
            runtime.Service.SelectUpgrade(0);
            runtime.Ammo.ApplyUpgrade(runtime.Upgrades[1]);
            runtime.Ammo.ApplyUpgrade(runtime.Upgrades[1]);
            runtime.Service.ReportClearedColumn(5);

            Assert.That(runtime.Service.CurrentProgress, Is.Zero);
            Assert.That(runtime.Service.CurrentOffers, Is.Empty);
        }

        [Test]
        public void ProgressRequirements_MultiplyAfterEverySelection()
        {
            var runtime = CreateRuntime();
            runtime.Flow.BeginGameplay();

            Assert.That(runtime.Service.RequiredProgress, Is.EqualTo(5));
            runtime.Service.ReportClearedColumn(5);
            runtime.Service.SelectUpgrade(0);

            Assert.That(runtime.Service.RequiredProgress, Is.EqualTo(10));
            runtime.Service.ReportClearedColumn(10);
            runtime.Service.SelectUpgrade(0);

            Assert.That(runtime.Service.RequiredProgress, Is.EqualTo(20));
            runtime.Service.ReportClearedColumn(20);
            runtime.Service.SelectUpgrade(0);

            Assert.That(runtime.Service.CompletedUpgradeCount, Is.EqualTo(3));
            Assert.That(runtime.Service.RequiredProgress, Is.EqualTo(40));
        }

        [Test]
        public void HeartUpgrade_IncreasesCurrentAndMaximumHealthUntilCap()
        {
            var runtime = CreateRuntime(1, true);
            runtime.Flow.BeginGameplay();

            var required = new[] { 5, 10, 20 };
            for (var index = 0; index < required.Length; index++)
            {
                runtime.Service.ReportClearedColumn(required[index]);
                Assert.That(runtime.Service.CurrentOffers.Count, Is.EqualTo(1));
                Assert.That(runtime.Service.CurrentOffers[0].DisplayName,
                    Is.EqualTo("+1 Heart"));
                Assert.That(runtime.Service.SelectUpgrade(0), Is.True);
            }

            Assert.That(runtime.Health.MaximumHealth, Is.EqualTo(8));
            Assert.That(runtime.Health.CurrentHealth, Is.EqualTo(8));
            runtime.Service.ReportClearedColumn(40);
            Assert.That(runtime.Service.CurrentOffers, Is.Empty);
        }

        [Test]
        public void ColumnDamage_RemovesBlocksFromTopDownAndReportsActualDamage()
        {
            var column = CreateColumn(3);
            var lowest = column.Obstacles[0];
            var middle = column.Obstacles[1];
            var highest = column.Obstacles[2];

            var accepted = column.TakeDamage(2);

            Assert.That(accepted, Is.EqualTo(2));
            Assert.That(column.RemainingCount, Is.EqualTo(1));
            Assert.That(column.Obstacles.Contains(middle), Is.False);
            Assert.That(column.Obstacles.Contains(highest), Is.False);
            Assert.That(column.Obstacles[0], Is.SameAs(lowest));
            Assert.That(column.Obstacles[0].transform.localPosition.y, Is.Zero);
        }

        [Test]
        public void ProjectilePiercing_ClearsDistinctColumnsAndReportsTheirTotalCounts()
        {
            var firstColumn = CreateColumn(1);
            var secondColumn = CreateColumn(1);
            var projectile = CreateProjectile();
            var reportedProgress = 0;

            projectile.Launch(
                Vector2.up,
                5,
                1,
                count => reportedProgress += count);
            InvokeTrigger(projectile, firstColumn.Obstacles[0].GetComponent<Collider2D>());
            InvokeTrigger(projectile, firstColumn.GetComponentInChildren<Collider2D>());
            InvokeTrigger(projectile, secondColumn.Obstacles[0].GetComponent<Collider2D>());

            Assert.That(firstColumn.RemainingCount, Is.Zero);
            Assert.That(secondColumn.RemainingCount, Is.Zero);
            Assert.That(reportedProgress, Is.EqualTo(2));
            Assert.That(projectile.IsLaunched, Is.False);
        }

        [Test]
        public void ProjectileProgress_CountsTheWholeColumnOnlyWhenItIsCleared()
        {
            var column = CreateColumn(3);
            var reportedProgress = 0;
            var firstProjectile = CreateProjectile();
            firstProjectile.Launch(
                Vector2.up,
                2,
                0,
                count => reportedProgress += count);

            InvokeTrigger(
                firstProjectile,
                column.Obstacles[0].GetComponent<Collider2D>());

            Assert.That(column.RemainingCount, Is.EqualTo(1));
            Assert.That(reportedProgress, Is.Zero);

            var finishingProjectile = CreateProjectile();
            finishingProjectile.Launch(
                Vector2.up,
                1,
                0,
                count => reportedProgress += count);
            InvokeTrigger(
                finishingProjectile,
                column.Obstacles[0].GetComponent<Collider2D>());

            Assert.That(column.RemainingCount, Is.Zero);
            Assert.That(reportedProgress, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Shooter_UsesSymmetricMultishotAndLiveFireRate()
        {
            var runtime = CreateRuntime();
            runtime.Flow.BeginGameplay();
            var projectileCountUpgrade = runtime.Upgrades.First(
                upgrade => upgrade.Stat == AmmoStatType.ProjectileCount);
            runtime.Ammo.ApplyUpgrade(projectileCountUpgrade);
            runtime.Ammo.ApplyUpgrade(projectileCountUpgrade);

            var shooterObject = CreateObject("Shooter", typeof(PlayerShooter));
            var firePoint = new GameObject("Fire Point").transform;
            _createdObjects.Add(firePoint.gameObject);
            firePoint.SetParent(shooterObject.transform, false);
            SetField(shooterObject.GetComponent<PlayerShooter>(), "firePoint", firePoint);

            var shooter = shooterObject.GetComponent<PlayerShooter>();
            shooter.Construct(runtime.Ammo, runtime.Service, runtime.Flow);
            shooter.Initialize();
            InvokePrivate(shooter, "ShootVolley");

            var projectiles = Object.FindObjectsByType<Projectile>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(candidate => candidate.gameObject != runtime.ProjectileObject)
                .OrderBy(candidate => candidate.transform.position.x)
                .ToArray();
            Assert.That(projectiles.Length, Is.EqualTo(3));
            Assert.That(projectiles[0].transform.position.x, Is.EqualTo(-0.35f).Within(0.001f));
            Assert.That(projectiles[1].transform.position.x, Is.Zero.Within(0.001f));
            Assert.That(projectiles[2].transform.position.x, Is.EqualTo(0.35f).Within(0.001f));

            foreach (var projectile in projectiles)
            {
                _createdObjects.Add(projectile.gameObject);
                Object.DestroyImmediate(projectile.gameObject);
            }

            runtime.Ammo.ApplyUpgrade(projectileCountUpgrade);
            InvokePrivate(shooter, "ShootVolley");
            projectiles = Object.FindObjectsByType<Projectile>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(candidate => candidate.gameObject != runtime.ProjectileObject)
                .OrderBy(candidate => candidate.transform.position.x)
                .ToArray();
            Assert.That(projectiles.Length, Is.EqualTo(4));
            Assert.That(projectiles[0].transform.position.x, Is.EqualTo(-0.525f).Within(0.001f));
            Assert.That(projectiles[1].transform.position.x, Is.EqualTo(-0.175f).Within(0.001f));
            Assert.That(projectiles[2].transform.position.x, Is.EqualTo(0.175f).Within(0.001f));
            Assert.That(projectiles[3].transform.position.x, Is.EqualTo(0.525f).Within(0.001f));
            foreach (var projectile in projectiles)
            {
                _createdObjects.Add(projectile.gameObject);
            }

            var fireRateUpgrade = runtime.Upgrades.First(
                upgrade => upgrade.Stat == AmmoStatType.FireRate);
            runtime.Ammo.ApplyUpgrade(fireRateUpgrade);
            var interval = (float)InvokePrivate(shooter, "GetFireInterval");
            Assert.That(interval, Is.EqualTo(1f / 6f).Within(0.0001f));
            yield return null;
        }

        private Runtime CreateRuntime(
            int upgradeCount = 4,
            bool healthOnly = false)
        {
            var projectileObject = CreateObject(
                "Projectile Template",
                typeof(Rigidbody2D),
                typeof(CircleCollider2D),
                typeof(Projectile));
            projectileObject.layer = LayerMask.NameToLayer("Ammo");
            projectileObject.transform.position = new Vector3(100f, 100f, 0f);

            var ammoDefinition = ScriptableObject.CreateInstance<AmmoDefinition>();
            _createdObjects.Add(ammoDefinition);
            SetField(ammoDefinition, "displayName", "Ball");
            SetField(ammoDefinition, "projectilePrefab", projectileObject.GetComponent<Projectile>());
            SetField(ammoDefinition, "baseDamage", 1);
            SetField(ammoDefinition, "basePiercing", 0);
            SetField(ammoDefinition, "baseProjectileCount", 1);
            SetField(ammoDefinition, "baseFireRate", 5f);
            SetField(ammoDefinition, "projectileSpacing", 0.35f);

            var stats = new[]
            {
                AmmoStatType.Damage,
                AmmoStatType.Piercing,
                AmmoStatType.ProjectileCount,
                AmmoStatType.FireRate
            };
            var caps = new[] { 5f, 3f, 5f, 10f };
            var upgrades = new List<UpgradeDefinition>();
            for (var index = 0; index < upgradeCount; index++)
            {
                var upgrade = new UpgradeDefinition();
                SetField(
                    upgrade,
                    "target",
                    healthOnly
                        ? UpgradeTarget.MaximumHealth
                        : UpgradeTarget.AmmoStat);
                SetField(upgrade, "ammo", healthOnly ? null : ammoDefinition);
                if (!healthOnly)
                {
                    SetField(upgrade, "stat", stats[index]);
                }
                SetField(upgrade, "amount", 1f);
                SetField(
                    upgrade,
                    "maximumValue",
                    healthOnly ? 8f : caps[index]);
                upgrades.Add(upgrade);
            }

            var settings = ScriptableObject.CreateInstance<UpgradeSettings>();
            _createdObjects.Add(settings);
            SetField(settings, "startingAmmo", ammoDefinition);
            SetField(settings, "initialRequiredClearedObstacleCount", 5);
            SetField(settings, "requirementMultiplier", 2f);
            SetField(settings, "offerCount", 3);
            SetField(settings, "upgrades", upgrades);

            var flow = new GameFlowManager();
            var ammo = new PlayerAmmoService(settings);
            var healthObject = CreateObject("Player Health", typeof(PlayerHealth));
            var health = healthObject.GetComponent<PlayerHealth>();
            var service = new UpgradeService(settings, ammo, health, flow);
            return new Runtime(
                projectileObject,
                upgrades,
                flow,
                ammo,
                health,
                service);
        }

        private ObstacleColumn CreateColumn(int amount)
        {
            var columnObject = CreateObject("Column");
            columnObject.SetActive(false);
            for (var index = 0; index < amount; index++)
            {
                var obstacleObject = CreateObject(
                    $"Obstacle {index + 1}",
                    typeof(BoxCollider2D),
                    typeof(Obstacle));
                obstacleObject.layer = LayerMask.NameToLayer("Damageable");
                obstacleObject.transform.SetParent(columnObject.transform, false);
                obstacleObject.transform.localPosition = new Vector3(0f, index * 0.18f, 0f);
            }

            var column = columnObject.AddComponent<ObstacleColumn>();
            SetField(column, "amount", amount);
            columnObject.SetActive(true);
            return column;
        }

        private Projectile CreateProjectile()
        {
            var projectileObject = CreateObject(
                "Projectile",
                typeof(Rigidbody2D),
                typeof(CircleCollider2D),
                typeof(Projectile));
            projectileObject.layer = LayerMask.NameToLayer("Ammo");
            return projectileObject.GetComponent<Projectile>();
        }

        private GameObject CreateObject(string name, params System.Type[] components)
        {
            var gameObject = new GameObject(name, components);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void InvokeTrigger(Projectile projectile, Collider2D collider)
        {
            InvokePrivate(projectile, "OnTriggerEnter2D", collider);
        }

        private static object InvokePrivate(
            object target,
            string methodName,
            params object[] arguments)
        {
            return target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, arguments);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private sealed class Runtime
        {
            public Runtime(
                GameObject projectileObject,
                List<UpgradeDefinition> upgrades,
                GameFlowManager flow,
                PlayerAmmoService ammo,
                PlayerHealth health,
                UpgradeService service)
            {
                ProjectileObject = projectileObject;
                Upgrades = upgrades;
                Flow = flow;
                Ammo = ammo;
                Health = health;
                Service = service;
            }

            public GameObject ProjectileObject { get; }
            public List<UpgradeDefinition> Upgrades { get; }
            public GameFlowManager Flow { get; }
            public PlayerAmmoService Ammo { get; }
            public PlayerHealth Health { get; }
            public UpgradeService Service { get; }
        }
    }
}
