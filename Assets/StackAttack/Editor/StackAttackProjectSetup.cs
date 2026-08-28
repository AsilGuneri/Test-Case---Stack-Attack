using System;
using System.Reflection;
using StackAttack.Gameplay;
using StackAttack.Systems;
using StackAttack.Game;
using StackAttack.Menu;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace StackAttack.Editor
{
    public static class StackAttackProjectSetup
    {
        private const string BaseScenePath = "Assets/Scenes/BaseScene.unity";
        private const string MenuScenePath = "Assets/Scenes/MenuScene.unity";
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private const string ConfigFolder = "Assets/StackAttack/Config";
        private const string CatalogPath = ConfigFolder + "/SceneCatalog.asset";
        private const string LevelConfigFolder = ConfigFolder + "/Levels";
        private const string LevelOnePath = LevelConfigFolder + "/Level 1.asset";
        private const string LevelCatalogPath = LevelConfigFolder + "/LevelCatalog.asset";
        private const string AmmoConfigFolder = ConfigFolder + "/Ammo";
        private const string BallDefinitionPath = AmmoConfigFolder + "/Ball.asset";
        private const string UpgradeSettingsPath =
            AmmoConfigFolder + "/Upgrade Settings.asset";
        private const string PlayerPrefabPath = "Assets/StackAttack/Prefabs/Player.prefab";
        private const string ProjectilePrefabFolder = "Assets/StackAttack/Prefabs/Projectiles";
        private const string DefaultAmmoPrefabPath = ProjectilePrefabFolder + "/DefaultAmmo.prefab";
        private const string ObstaclePrefabFolder = "Assets/StackAttack/Prefabs/Obstacles";
        private const string ObstaclePrefabPath =
            ObstaclePrefabFolder + "/Obstacle.prefab";
        private const string ObstacleColumnPrefabPath =
            ObstaclePrefabFolder + "/ObstacleColumn.prefab";
        private const string UiPrefabFolder = "Assets/StackAttack/Prefabs/UI";
        private const string HeartVisualPrefabPath = UiPrefabFolder + "/HeartVisual.prefab";
        private const string ObstacleCountVisualPrefabPath =
            UiPrefabFolder + "/ObstacleCountVisual.prefab";
        private const string UpgradePopupPrefabPath =
            UiPrefabFolder + "/UpgradePopup.prefab";
        private const string LevelWonPopupPrefabPath =
            UiPrefabFolder + "/LevelWonPopup.prefab";
        private const string LevelLostPopupPrefabPath =
            UiPrefabFolder + "/LevelLostPopup.prefab";
        private const string ScenesGroupName = "Scenes-Local";

        [MenuItem("Tools/Stack Attack/Configure Foundation")]
        public static void ConfigureFoundation()
        {
            try
            {
                EnsureFolder("Assets/StackAttack", "Config");
                EnsureFolder(ConfigFolder, "Levels");
                EnsureFolder(ConfigFolder, "Ammo");
                EnsureFolder("Assets/StackAttack", "Prefabs");
                EnsureFolder("Assets/StackAttack/Prefabs", "Projectiles");
                EnsureFolder("Assets/StackAttack/Prefabs", "Obstacles");
                EnsureFolder("Assets/StackAttack/Prefabs", "UI");
                var defaultAmmoPrefab = CreateOrUpdateDefaultAmmoPrefab();
                var ballDefinition = CreateOrUpdateBallDefinition(defaultAmmoPrefab);
                var upgradeSettings = CreateOrUpdateUpgradeSettings(ballDefinition);
                var obstaclePrefab = CreateOrUpdateObstaclePrefab();
                var obstacleColumnPrefab =
                    CreateObstacleColumnPrefabIfMissing(obstaclePrefab);
                var playerPrefab = CreateOrUpdatePlayerPrefab();
                var heartVisualPrefab = CreateOrUpdateHeartVisualPrefab();
                var obstacleCountVisualPrefab =
                    CreateOrUpdateObstacleCountVisualPrefab();
                var upgradePopupPrefab = CreateOrUpdateUpgradePopupPrefab();
                var levelWonPopupPrefab = CreateOrUpdateLevelWonPopupPrefab();
                var levelLostPopupPrefab = CreateOrUpdateLevelLostPopupPrefab();
                var levelCatalog = CreateOrUpdateLevelCatalog();
                CreateMenuScene();
                upgradePopupPrefab = LoadPopupPrefab<UpgradePopup>(
                    UpgradePopupPrefabPath);
                levelWonPopupPrefab = LoadPopupPrefab<LevelWonPopup>(
                    LevelWonPopupPrefabPath);
                levelLostPopupPrefab = LoadPopupPrefab<LevelLostPopup>(
                    LevelLostPopupPrefabPath);
                CreateGameScene(
                    playerPrefab,
                    obstacleColumnPrefab,
                    heartVisualPrefab,
                    obstacleCountVisualPrefab,
                    upgradePopupPrefab,
                    levelWonPopupPrefab,
                    levelLostPopupPrefab,
                    upgradeSettings);

                var addressableSettings = ConfigureAddressables();
                var sceneCatalog = CreateOrUpdateSceneCatalog();
                CreateBaseScene(sceneCatalog, levelCatalog);
                ConfigureBuildSettings();

                EditorUtility.SetDirty(addressableSettings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);

                Debug.Log(
                    "Stack Attack foundation configured: BaseScene is built-in; " +
                    "MenuScene and GameScene are local Addressables with VContainer scopes.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem("Tools/Stack Attack/Build Addressables Content")]
        public static void BuildAddressablesContent()
        {
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new InvalidOperationException(
                    $"Addressables content build failed: {result.Error}");
            }

            Debug.Log($"Addressables content build completed: {result.OutputPath}");
        }

        private static AddressableAssetSettings ConfigureAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                throw new InvalidOperationException("Could not create Addressables settings.");
            }

            var group = settings.FindGroup(ScenesGroupName);
            if (group == null)
            {
                group = settings.CreateGroup(
                    ScenesGroupName,
                    false,
                    false,
                    true,
                    null,
                    typeof(BundledAssetGroupSchema),
                    typeof(ContentUpdateGroupSchema));
            }

            var bundledSchema = group.GetSchema<BundledAssetGroupSchema>()
                                ?? group.AddSchema<BundledAssetGroupSchema>();
            bundledSchema.BuildPath.SetVariableByName(
                settings,
                AddressableAssetSettings.kLocalBuildPath);
            bundledSchema.LoadPath.SetVariableByName(
                settings,
                AddressableAssetSettings.kLocalLoadPath);
            bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            bundledSchema.IncludeInBuild = true;

            AddSceneEntry(settings, group, MenuScenePath, "Scenes/MenuScene");
            AddSceneEntry(settings, group, GameScenePath, "Scenes/GameScene");

            var baseGuid = AssetDatabase.AssetPathToGUID(BaseScenePath);
            if (!string.IsNullOrEmpty(baseGuid) && settings.FindAssetEntry(baseGuid) != null)
            {
                settings.RemoveAssetEntry(baseGuid);
            }

            settings.BuildAddressablesWithPlayerBuild =
                AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;

            return settings;
        }

        private static void AddSceneEntry(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string scenePath,
            string address)
        {
            var guid = AssetDatabase.AssetPathToGUID(scenePath);
            if (string.IsNullOrEmpty(guid))
            {
                throw new InvalidOperationException($"Scene has no asset GUID: {scenePath}");
            }

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
        }

        private static SceneCatalog CreateOrUpdateSceneCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SceneCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SceneCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var serializedCatalog = new SerializedObject(catalog);
            SetAssetReferenceGuid(
                serializedCatalog.FindProperty("menuScene"),
                AssetDatabase.AssetPathToGUID(MenuScenePath));
            SetAssetReferenceGuid(
                serializedCatalog.FindProperty("gameScene"),
                AssetDatabase.AssetPathToGUID(GameScenePath));
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static LevelCatalog CreateOrUpdateLevelCatalog()
        {
            var levelOne = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelOnePath);
            if (levelOne == null)
            {
                levelOne = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(levelOne, LevelOnePath);
            }

            var serializedLevel = new SerializedObject(levelOne);
            serializedLevel.FindProperty("levelNumber").intValue = 1;
            serializedLevel.FindProperty("startOffset").floatValue = 10f;
            serializedLevel.FindProperty("completionOffset").floatValue = 2.5f;
            serializedLevel.FindProperty("horizontalViewportMargin").floatValue = 0.02f;
            serializedLevel.FindProperty("cellSpacing").floatValue = 0.05f;
            serializedLevel.ApplyModifiedPropertiesWithoutUndo();

            levelOne.ResizeGrid(new Vector2Int(5, 4));
            for (var y = 0; y < levelOne.GridSize.y; y++)
            {
                for (var x = 0; x < levelOne.GridSize.x; x++)
                {
                    levelOne.SetObstacleAmount(x, y, 0);
                    levelOne.SetCellColor(x, y, Color.white);
                    levelOne.SetCellGroup(x, y, null);
                }
            }

            SetLevelRow(levelOne, 0, 0, 0, 0, 3, 0);
            SetLevelRow(levelOne, 1, 0, 3, 3, 0, 0);
            SetLevelRow(levelOne, 2, 0, 3, 3, 0, 3);
            SetLevelRow(levelOne, 3, 6, 6, 0, 6, 6);
            EditorUtility.SetDirty(levelOne);

            var levelCatalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(
                LevelCatalogPath);
            if (levelCatalog == null)
            {
                levelCatalog = ScriptableObject.CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(levelCatalog, LevelCatalogPath);
            }

            var serializedCatalog = new SerializedObject(levelCatalog);
            var levelsProperty = serializedCatalog.FindProperty("levels");
            levelsProperty.arraySize = 1;
            levelsProperty.GetArrayElementAtIndex(0).objectReferenceValue = levelOne;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(levelCatalog);
            return levelCatalog;
        }

        private static void SetLevelRow(
            LevelDefinition level,
            int y,
            params int[] obstacleAmounts)
        {
            if (obstacleAmounts.Length != level.GridSize.x)
            {
                throw new ArgumentException(
                    "A configured level row must match the grid width.",
                    nameof(obstacleAmounts));
            }

            for (var x = 0; x < obstacleAmounts.Length; x++)
            {
                level.SetObstacleAmount(x, y, obstacleAmounts[x]);
            }
        }

        private static void SetAssetReferenceGuid(SerializedProperty property, string guid)
        {
            if (property == null)
            {
                throw new InvalidOperationException("SceneCatalog serialization does not match setup code.");
            }

            var guidProperty = property.FindPropertyRelative("m_AssetGUID");
            if (guidProperty == null)
            {
                throw new InvalidOperationException("Could not serialize an AssetReference GUID.");
            }

            guidProperty.stringValue = guid;
        }

        private static void CreateBaseScene(
            SceneCatalog catalog,
            LevelCatalog levelCatalog)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var application = new GameObject("Application");
            var scope = application.AddComponent<BaseLifetimeScope>();
            var sceneFlow = application.AddComponent<AddressableSceneFlowService>();

            var eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));

            var transitionCanvas = CreateCanvas("Scene Transition Canvas", 1000);
            var overlay = CreatePanel(
                "Transition Overlay",
                transitionCanvas.transform,
                new Color(0.025f, 0.035f, 0.07f, 1f));
            var loadingText = CreateText(
                "Transition Message",
                overlay.transform,
                "Loading...",
                36,
                TextAnchor.MiddleCenter,
                Color.white);
            Stretch(loadingText.rectTransform, 120f, 120f, 120f, 120f);

            var transitionView = transitionCanvas.gameObject.AddComponent<SceneTransitionView>();
            SetObjectReference(transitionView, "overlay", overlay);
            SetObjectReference(transitionView, "messageLabel", loadingText);

            SetObjectReference(scope, "sceneCatalog", catalog);
            SetObjectReference(scope, "levelCatalog", levelCatalog);
            SetObjectReference(scope, "sceneFlowService", sceneFlow);
            SetObjectReference(scope, "transitionView", transitionView);

            eventSystemObject.transform.SetSiblingIndex(1);
            EditorSceneManager.SaveScene(scene, BaseScenePath);
        }

        private static void CreateMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreatePresentationCamera(new Color(0.035f, 0.075f, 0.14f, 1f));
            CreateDirectionalLight();

            var scopeObject = new GameObject("Menu Scope");
            var scope = scopeObject.AddComponent<MenuLifetimeScope>();

            var canvas = CreateCanvas("Menu Canvas", 0);
            var background = CreatePanel(
                "Background",
                canvas.transform,
                new Color(0.035f, 0.075f, 0.14f, 1f));

            var title = CreateTMPText(
                "Title",
                background.transform,
                "STACK ATTACK",
                54,
                new Color(1f, 0.55f, 0.12f));
            SetRect(title.rectTransform, 0.5f, 0.72f, 780f, 100f);

            var level = CreateTMPText(
                "Level Label",
                background.transform,
                "Level 1",
                38,
                Color.white);
            SetRect(level.rectTransform, 0.5f, 0.56f, 520f, 80f);

            var startButton = CreateTMPButton(
                "Start Button",
                background.transform,
                "PLAY",
                new Color(1f, 0.45f, 0.08f));
            SetRect(startButton.GetComponent<RectTransform>(), 0.5f, 0.29f, 360f, 100f);

            var view = canvas.gameObject.AddComponent<MenuView>();
            SetObjectReference(view, "levelLabel", level);
            SetObjectReference(view, "startButton", startButton);
            SetObjectReference(scope, "view", view);

            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        private static Projectile CreateOrUpdateDefaultAmmoPrefab()
        {
            var temporaryAmmo = new GameObject(
                "DefaultAmmo",
                typeof(SpriteRenderer),
                typeof(CircleCollider2D),
                typeof(Rigidbody2D),
                typeof(Projectile));
            temporaryAmmo.layer = GetRequiredLayer("Ammo");

            var renderer = temporaryAmmo.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 20;

            var collider = temporaryAmmo.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.15f;

            var rigidbody = temporaryAmmo.GetComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.gravityScale = 0f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporaryAmmo,
                    DefaultAmmoPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "Could not create the DefaultAmmo prefab.");
                }

                return prefab.GetComponent<Projectile>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryAmmo);
            }
        }

        private static AmmoDefinition CreateOrUpdateBallDefinition(
            Projectile projectilePrefab)
        {
            var definition = AssetDatabase.LoadAssetAtPath<AmmoDefinition>(
                BallDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<AmmoDefinition>();
                AssetDatabase.CreateAsset(definition, BallDefinitionPath);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("displayName").stringValue = "Ball";
            serialized.FindProperty("projectilePrefab").objectReferenceValue =
                projectilePrefab;
            serialized.FindProperty("baseDamage").intValue = 1;
            serialized.FindProperty("basePiercing").intValue = 0;
            serialized.FindProperty("baseProjectileCount").intValue = 1;
            serialized.FindProperty("baseFireRate").floatValue = 5f;
            serialized.FindProperty("projectileSpacing").floatValue = 0.35f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static UpgradeSettings CreateOrUpdateUpgradeSettings(
            AmmoDefinition ball)
        {
            var settings = AssetDatabase.LoadAssetAtPath<UpgradeSettings>(
                UpgradeSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<UpgradeSettings>();
                AssetDatabase.CreateAsset(settings, UpgradeSettingsPath);
            }

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("startingAmmo").objectReferenceValue = ball;
            serialized.FindProperty("initialRequiredClearedObstacleCount")
                .intValue = 5;
            serialized.FindProperty("requirementMultiplier").floatValue = 2f;
            serialized.FindProperty("offerCount").intValue = 3;

            var upgrades = serialized.FindProperty("upgrades");
            upgrades.arraySize = 5;
            ConfigureUpgrade(upgrades.GetArrayElementAtIndex(0), ball,
                AmmoStatType.Damage, 5f);
            ConfigureUpgrade(upgrades.GetArrayElementAtIndex(1), ball,
                AmmoStatType.Piercing, 3f);
            ConfigureUpgrade(upgrades.GetArrayElementAtIndex(2), ball,
                AmmoStatType.ProjectileCount, 5f);
            ConfigureUpgrade(upgrades.GetArrayElementAtIndex(3), ball,
                AmmoStatType.FireRate, 10f);
            ConfigureHealthUpgrade(upgrades.GetArrayElementAtIndex(4), 8f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private static void ConfigureUpgrade(
            SerializedProperty property,
            AmmoDefinition ammo,
            AmmoStatType stat,
            float maximumValue)
        {
            property.FindPropertyRelative("target").enumValueIndex =
                (int)UpgradeTarget.AmmoStat;
            property.FindPropertyRelative("ammo").objectReferenceValue = ammo;
            property.FindPropertyRelative("stat").enumValueIndex = (int)stat;
            property.FindPropertyRelative("amount").floatValue = 1f;
            property.FindPropertyRelative("maximumValue").floatValue = maximumValue;
        }

        private static void ConfigureHealthUpgrade(
            SerializedProperty property,
            float maximumHealth)
        {
            property.FindPropertyRelative("target").enumValueIndex =
                (int)UpgradeTarget.MaximumHealth;
            property.FindPropertyRelative("ammo").objectReferenceValue = null;
            property.FindPropertyRelative("amount").floatValue = 1f;
            property.FindPropertyRelative("maximumValue").floatValue =
                maximumHealth;
        }

        private static GameObject CreateOrUpdateObstaclePrefab()
        {
            var temporaryObstacle = new GameObject(
                "Obstacle",
                typeof(SpriteRenderer),
                typeof(BoxCollider2D),
                typeof(Obstacle));
            temporaryObstacle.layer = GetRequiredLayer("Damageable");

            var renderer = temporaryObstacle.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 5;
            SetObjectReference(
                temporaryObstacle.GetComponent<Obstacle>(),
                "spriteRenderer",
                renderer);

            var collider = temporaryObstacle.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(2f, 1f);

            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporaryObstacle,
                    ObstaclePrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "Could not create the Obstacle prefab.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryObstacle);
            }
        }

        [MenuItem("Tools/Stack Attack/Create Missing Obstacle Column Prefab")]
        public static void CreateMissingObstacleColumnPrefab()
        {
            var obstaclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ObstaclePrefabPath);
            if (obstaclePrefab == null)
            {
                throw new MissingReferenceException(
                    $"Missing obstacle prefab at '{ObstaclePrefabPath}'.");
            }

            CreateObstacleColumnPrefabIfMissing(obstaclePrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CreateObstacleColumnPrefabIfMissing(
            GameObject obstaclePrefab)
        {
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ObstacleColumnPrefabPath);
            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            var temporaryColumn = new GameObject("ObstacleColumn");
            var baseObject = PrefabUtility.InstantiatePrefab(
                obstaclePrefab,
                temporaryColumn.transform) as GameObject;
            if (baseObject == null)
            {
                UnityEngine.Object.DestroyImmediate(temporaryColumn);
                throw new InvalidOperationException(
                    "Could not instantiate the Obstacle column base.");
            }

            baseObject.name = "Obstacle 1";
            baseObject.transform.SetLocalPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);

            var column = temporaryColumn.AddComponent<ObstacleColumn>();
            var serialized = new SerializedObject(column);
            serialized.FindProperty("amount").intValue = 1;
            serialized.FindProperty("verticalSpacing").floatValue = 0.18f;
            serialized.FindProperty("depthSpacing").floatValue = 0.01f;
            serialized.FindProperty("hitFadeDuration").floatValue = 0.25f;
            serialized.FindProperty("baseObstacle").objectReferenceValue =
                baseObject.GetComponent<Obstacle>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporaryColumn,
                    ObstacleColumnPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "Could not create the ObstacleColumn prefab.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryColumn);
            }
        }

        private static GameObject CreateOrUpdatePlayerPrefab()
        {
            var temporaryPlayer = new GameObject("Player");
            var rigidbody = temporaryPlayer.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = false;
            rigidbody.useFullKinematicContacts = true;
            var movement = temporaryPlayer.AddComponent<PlayerMovement>();
            var shooter = temporaryPlayer.AddComponent<PlayerShooter>();
            var health = temporaryPlayer.AddComponent<PlayerHealth>();
            var hitFeedback = temporaryPlayer.AddComponent<PlayerHitFeedback>();
            var shield = temporaryPlayer.AddComponent<PlayerInvulnerabilityShield>();
            var controller = temporaryPlayer.AddComponent<PlayerController>();
            SetLayerMask(controller, "obstacleLayers", "Damageable");

            var visual = new GameObject(
                "PlayerModel",
                typeof(SpriteRenderer),
                typeof(BoxCollider2D));
            visual.transform.SetParent(temporaryPlayer.transform, false);
            visual.GetComponent<SpriteRenderer>().sortingOrder = 10;
            SetObjectReference(
                hitFeedback,
                "playerVisual",
                visual.GetComponent<SpriteRenderer>());
            SetFloat(health, "invulnerabilityDuration", 3.5f);
            var serializedHealth = new SerializedObject(health);
            serializedHealth.FindProperty("maximumHealth").intValue = 5;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();
            SetObjectReference(
                movement,
                "horizontalBoundsCollider",
                visual.GetComponent<BoxCollider2D>());

            var firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(temporaryPlayer.transform, false);
            firePoint.localPosition = new Vector3(0f, 0.75f, 0f);

            var shieldObject = new GameObject(
                "Shield",
                typeof(CircleCollider2D));
            shieldObject.transform.SetParent(temporaryPlayer.transform, false);
            var shieldCollider = shieldObject.GetComponent<CircleCollider2D>();
            shieldCollider.isTrigger = true;
            shieldCollider.radius = 1.15f;
            shieldCollider.enabled = false;

            var shieldVisualParent = new GameObject("Shield Visual Parent");
            shieldVisualParent.transform.SetParent(shieldObject.transform, false);
            shieldVisualParent.SetActive(false);

            SetObjectReference(shield, "shieldCollider", shieldCollider);
            SetObjectReference(
                shield,
                "shieldVisualParent",
                shieldVisualParent);

            SetObjectReference(shooter, "firePoint", firePoint);

            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporaryPlayer,
                    PlayerPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Could not create the Player prefab.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryPlayer);
            }
        }

        private static GameObject CreateOrUpdateHeartVisualPrefab()
        {
            var temporaryHeart = new GameObject(
                "Heart Visual",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            var rect = temporaryHeart.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64f, 64f);
            temporaryHeart.GetComponent<Image>().color =
                new Color(0.92f, 0.16f, 0.24f, 1f);

            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporaryHeart,
                    HeartVisualPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "Could not create the Heart Visual prefab.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryHeart);
            }
        }

        private static Text CreateOrUpdateObstacleCountVisualPrefab()
        {
            var temporaryVisual = new GameObject(
                "Obstacle Count Visual",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            var rect = temporaryVisual.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80f, 60f);

            var text = temporaryVisual.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "1";
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            text.raycastTarget = false;

            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporaryVisual,
                    ObstacleCountVisualPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "Could not create the Obstacle Count Visual prefab.");
                }

                return prefab.GetComponent<Text>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryVisual);
            }
        }

        private static UpgradePopup CreateOrUpdateUpgradePopupPrefab()
        {
            var temporaryPopup = new GameObject(
                "Upgrade Popup",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(UpgradePopup));
            var rect = temporaryPopup.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(820f, 560f);
            temporaryPopup.GetComponent<Image>().color =
                new Color(0.04f, 0.06f, 0.11f, 0.96f);

            var title = CreateTMPText(
                "Title",
                temporaryPopup.transform,
                "CHOOSE AN UPGRADE",
                42f,
                Color.white);
            SetRect(title.rectTransform, 0.5f, 0.82f, 700f, 80f);

            var buttons = new Button[3];
            var labels = new TMP_Text[3];
            for (var index = 0; index < buttons.Length; index++)
            {
                var button = CreateTMPButton(
                    $"Upgrade Option {index + 1}",
                    temporaryPopup.transform,
                    "Ball Upgrade",
                    new Color(0.12f, 0.55f, 0.78f, 1f));
                SetRect(
                    button.GetComponent<RectTransform>(),
                    0.5f,
                    0.61f - index * 0.2f,
                    680f,
                    90f);
                buttons[index] = button;
                labels[index] = button.GetComponentInChildren<TMP_Text>();
            }

            var popup = temporaryPopup.GetComponent<UpgradePopup>();
            var serialized = new SerializedObject(popup);
            serialized.FindProperty("pauseGame").boolValue = true;
            SetObjectArray(serialized.FindProperty("optionButtons"), buttons);
            SetObjectArray(serialized.FindProperty("optionLabels"), labels);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporaryPopup,
                    UpgradePopupPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "Could not create the Upgrade Popup prefab.");
                }

                return prefab.GetComponent<UpgradePopup>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryPopup);
            }
        }

        private static LevelWonPopup CreateOrUpdateLevelWonPopupPrefab()
        {
            var temporaryPopup = new GameObject(
                "Level Won Popup",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LevelWonPopup));
            Stretch(
                temporaryPopup.GetComponent<RectTransform>(),
                0f,
                0f,
                0f,
                0f);
            temporaryPopup.GetComponent<Image>().color =
                new Color(0.04f, 0.06f, 0.11f, 0.96f);

            var title = CreateTMPText(
                "Title",
                temporaryPopup.transform,
                "LEVEL COMPLETE",
                46f,
                Color.white);
            SetRect(title.rectTransform, 0.5f, 0.75f, 700f, 100f);

            var nextButton = CreateTMPButton(
                "Next Level Button",
                temporaryPopup.transform,
                "NEXT LEVEL",
                new Color(0.12f, 0.65f, 0.38f, 1f));
            SetRect(
                nextButton.GetComponent<RectTransform>(),
                0.5f,
                0.48f,
                620f,
                90f);

            var menuButton = CreateTMPButton(
                "Main Menu Button",
                temporaryPopup.transform,
                "MAIN MENU",
                new Color(0.12f, 0.55f, 0.78f, 1f));
            SetRect(
                menuButton.GetComponent<RectTransform>(),
                0.5f,
                0.25f,
                620f,
                90f);

            var popup = temporaryPopup.GetComponent<LevelWonPopup>();
            var serialized = new SerializedObject(popup);
            serialized.FindProperty("pauseGame").boolValue = true;
            serialized.FindProperty("titleLabel").objectReferenceValue = title;
            serialized.FindProperty("nextLevelButton").objectReferenceValue =
                nextButton;
            serialized.FindProperty("mainMenuButton").objectReferenceValue =
                menuButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporaryPopup,
                    LevelWonPopupPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "Could not create the Level Won Popup prefab.");
                }

                return prefab.GetComponent<LevelWonPopup>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryPopup);
            }
        }

        private static LevelLostPopup CreateOrUpdateLevelLostPopupPrefab()
        {
            var temporaryPopup = new GameObject(
                "Level Lost Popup",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LevelLostPopup));
            Stretch(
                temporaryPopup.GetComponent<RectTransform>(),
                0f,
                0f,
                0f,
                0f);
            temporaryPopup.GetComponent<Image>().color =
                new Color(0.12f, 0.025f, 0.04f, 0.98f);

            var title = CreateTMPText(
                "Title",
                temporaryPopup.transform,
                "LEVEL FAILED",
                46f,
                Color.white);
            SetRect(title.rectTransform, 0.5f, 0.68f, 700f, 100f);

            var retryButton = CreateTMPButton(
                "Retry Button",
                temporaryPopup.transform,
                "RETRY",
                new Color(0.78f, 0.22f, 0.2f, 1f));
            SetRect(
                retryButton.GetComponent<RectTransform>(),
                0.5f,
                0.48f,
                620f,
                90f);

            var menuButton = CreateTMPButton(
                "Main Menu Button",
                temporaryPopup.transform,
                "MAIN MENU",
                new Color(0.12f, 0.55f, 0.78f, 1f));
            SetRect(
                menuButton.GetComponent<RectTransform>(),
                0.5f,
                0.34f,
                620f,
                90f);

            var popup = temporaryPopup.GetComponent<LevelLostPopup>();
            var serialized = new SerializedObject(popup);
            serialized.FindProperty("pauseGame").boolValue = true;
            serialized.FindProperty("titleLabel").objectReferenceValue = title;
            serialized.FindProperty("retryButton").objectReferenceValue =
                retryButton;
            serialized.FindProperty("mainMenuButton").objectReferenceValue =
                menuButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporaryPopup,
                    LevelLostPopupPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "Could not create the Level Lost Popup prefab.");
                }

                return prefab.GetComponent<LevelLostPopup>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryPopup);
            }
        }

        private static void CreateGameScene(
            GameObject playerPrefab,
            GameObject obstacleColumnPrefab,
            GameObject heartVisualPrefab,
            Text obstacleCountVisualPrefab,
            UpgradePopup upgradePopupPrefab,
            LevelWonPopup levelWonPopupPrefab,
            LevelLostPopup levelLostPopupPrefab,
            UpgradeSettings upgradeSettings)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = CreatePresentationCamera(new Color(0.055f, 0.055f, 0.085f, 1f));
            camera.transform.position = new Vector3(0f, 4f, -10f);

            var playerObject = PrefabUtility.InstantiatePrefab(playerPrefab, scene) as GameObject;
            if (playerObject == null)
            {
                throw new InvalidOperationException("Could not instantiate the Player prefab.");
            }

            playerObject.name = "Player";
            playerObject.transform.position = Vector3.zero;
            var playerController = playerObject.GetComponent<PlayerController>();

            var environmentObject = new GameObject("Environment");
            var levelRootObject = new GameObject(
                "Level Obstacles",
                typeof(ObstacleParent),
                typeof(LevelBuilder));
            levelRootObject.transform.SetParent(environmentObject.transform, false);
            var levelRoot = levelRootObject.GetComponent<ObstacleParent>();
            var levelBuilder = levelRootObject.GetComponent<LevelBuilder>();
            SetObjectReference(
                levelBuilder,
                "columnPrefab",
                obstacleColumnPrefab.GetComponent<ObstacleColumn>());
            SetObjectReference(levelBuilder, "generationRoot", levelRoot);

            var scopeObject = new GameObject("Game Scope");
            var scope = scopeObject.AddComponent<GameLifetimeScope>();

            var canvas = CreateCanvas("Game Canvas", 0);
            var background = CreatePanel(
                "Background",
                canvas.transform,
                new Color(0.055f, 0.055f, 0.085f, 1f));

            var title = CreateText(
                "Title",
                background.transform,
                "GAME SCENE",
                52,
                TextAnchor.MiddleCenter,
                new Color(0.45f, 0.9f, 1f));
            SetRect(title.rectTransform, 0.5f, 0.7f, 780f, 100f);

            var requestLabel = CreateText(
                "Request Label",
                background.transform,
                "Waiting for injected request...",
                32,
                TextAnchor.MiddleCenter,
                Color.white);
            SetRect(requestLabel.rectTransform, 0.5f, 0.51f, 880f, 110f);

            var explanation = CreateText(
                "Explanation",
                background.transform,
                "This value exists only in the GameScene child scope.",
                24,
                TextAnchor.MiddleCenter,
                new Color(0.72f, 0.78f, 0.9f));
            SetRect(explanation.rectTransform, 0.5f, 0.4f, 880f, 80f);

            var backButton = CreateButton(
                "Back Button",
                background.transform,
                "BACK TO MENU",
                new Color(0.1f, 0.55f, 0.7f));
            SetRect(backButton.GetComponent<RectTransform>(), 0.5f, 0.25f, 420f, 100f);

            var heartParentObject = new GameObject(
                "Heart Container",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));
            heartParentObject.transform.SetParent(background.transform, false);
            SetRect(
                heartParentObject.GetComponent<RectTransform>(),
                0.16f,
                0.9f,
                260f,
                72f);
            var heartLayout = heartParentObject.GetComponent<HorizontalLayoutGroup>();
            heartLayout.spacing = 12f;
            heartLayout.childAlignment = TextAnchor.MiddleLeft;
            heartLayout.childControlWidth = false;
            heartLayout.childControlHeight = false;

            var levelProgressSlider = CreateVerticalProgressSlider(
                background.transform);
            var upgradeProgressSlider = CreateUpgradeProgressSlider(
                background.transform);

            var popupRootObject = new GameObject(
                "Popup Root",
                typeof(RectTransform));
            popupRootObject.transform.SetParent(canvas.transform, false);
            Stretch(
                popupRootObject.GetComponent<RectTransform>(),
                0f,
                0f,
                0f,
                0f);
            var popupManager = canvas.gameObject.AddComponent<PopupManager>();
            SetObjectReference(
                popupManager,
                "popupRoot",
                popupRootObject.transform);
            SetObjectReference(
                popupManager,
                "popupPrefabs",
                new System.Collections.Generic.List<PopupPrefabEntry>
                {
                    new(PopupType.Upgrade, upgradePopupPrefab),
                    new(PopupType.LevelWon, levelWonPopupPrefab),
                    new(PopupType.LevelLost, levelLostPopupPrefab)
                });

            var upgradeUiController =
                canvas.gameObject.AddComponent<UpgradeUIController>();
            SetObjectReference(
                upgradeUiController,
                "upgradeProgressSlider",
                upgradeProgressSlider);

            var countOverlayObject = new GameObject(
                "Obstacle Count Overlay",
                typeof(RectTransform));
            countOverlayObject.transform.SetParent(canvas.transform, false);
            Stretch(
                countOverlayObject.GetComponent<RectTransform>(),
                0f,
                0f,
                0f,
                0f);

            var gameUiController = canvas.gameObject.AddComponent<GameUIController>();
            SetObjectReference(
                gameUiController,
                "heartVisualPrefab",
                heartVisualPrefab);
            SetObjectReference(
                gameUiController,
                "heartParent",
                heartParentObject.transform);
            SetObjectReference(
                gameUiController,
                "levelProgressSlider",
                levelProgressSlider);

            var obstacleCountUiController =
                canvas.gameObject.AddComponent<ObstacleCountUIController>();
            SetObjectReference(
                obstacleCountUiController,
                "countVisualPrefab",
                obstacleCountVisualPrefab);
            SetObjectReference(
                obstacleCountUiController,
                "visualParent",
                countOverlayObject.GetComponent<RectTransform>());

            var view = canvas.gameObject.AddComponent<GameView>();
            SetObjectReference(view, "requestLabel", requestLabel);
            SetObjectReference(view, "backButton", backButton);
            SetObjectReference(scope, "view", view);
            SetObjectReference(scope, "gameUIController", gameUiController);
            SetObjectReference(
                scope,
                "obstacleCountUIController",
                obstacleCountUiController);
            SetObjectReference(scope, "upgradeUIController", upgradeUiController);
            SetObjectReference(scope, "popupManager", popupManager);
            SetObjectReference(scope, "playerController", playerController);
            SetObjectReference(scope, "levelBuilder", levelBuilder);
            SetObjectReference(scope, "upgradeSettings", upgradeSettings);

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        private static int GetRequiredLayer(string layerName)
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                throw new InvalidOperationException(
                    $"Required layer '{layerName}' does not exist.");
            }

            return layer;
        }

        private static Camera CreatePresentationCamera(Color backgroundColor)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            return camera;
        }

        private static Light CreateDirectionalLight()
        {
            var lightObject = new GameObject("Directional Light", typeof(Light));
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            return light;
        }

        private static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var canvasObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            Stretch(panel.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            return panel;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            var textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static TextMeshProUGUI CreateTMPText(
            string name,
            Transform parent,
            string value,
            float fontSize,
            Color color)
        {
            var textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Color color)
        {
            var buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = color;
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var labelText = CreateText(
                "Label",
                buttonObject.transform,
                label,
                30,
                TextAnchor.MiddleCenter,
                Color.white);
            labelText.fontStyle = FontStyle.Bold;
            Stretch(labelText.rectTransform, 18f, 18f, 12f, 12f);
            return button;
        }

        private static Button CreateTMPButton(
            string name,
            Transform parent,
            string label,
            Color color)
        {
            var buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = color;
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var labelText = CreateTMPText(
                "Text (TMP)",
                buttonObject.transform,
                label,
                30f,
                Color.white);
            labelText.fontStyle = FontStyles.Bold;
            Stretch(labelText.rectTransform, 18f, 18f, 12f, 12f);
            return button;
        }

        private static Slider CreateVerticalProgressSlider(Transform parent)
        {
            var sliderObject = DefaultControls.CreateSlider(
                new DefaultControls.Resources());
            sliderObject.name = "Level Progress Slider";
            sliderObject.transform.SetParent(parent, false);

            var rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-40f, 0f);
            rect.sizeDelta = new Vector2(48f, 900f);

            var slider = sliderObject.GetComponent<Slider>();
            slider.direction = Slider.Direction.BottomToTop;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(0f);
            slider.interactable = false;
            return slider;
        }

        private static Slider CreateUpgradeProgressSlider(Transform parent)
        {
            var sliderObject = DefaultControls.CreateSlider(
                new DefaultControls.Resources());
            sliderObject.name = "Upgrade Progress Slider";
            sliderObject.transform.SetParent(parent, false);
            SetRect(
                sliderObject.GetComponent<RectTransform>(),
                0.5f,
                0.94f,
                560f,
                38f);

            var slider = sliderObject.GetComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(0f);
            slider.interactable = false;
            return slider;
        }

        private static void SetRect(
            RectTransform rect,
            float normalizedX,
            float normalizedY,
            float width,
            float height)
        {
            rect.anchorMin = new Vector2(normalizedX, normalizedY);
            rect.anchorMax = new Vector2(normalizedX, normalizedY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(
            RectTransform rect,
            float left,
            float right,
            float bottom,
            float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            object value)
        {
            var field = target.GetType().GetField(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                var availableFields = string.Join(
                    ", ",
                    Array.ConvertAll(
                        target.GetType().GetFields(
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                        candidate => candidate.Name));
                throw new InvalidOperationException(
                    $"{target.GetType().AssemblyQualifiedName} has no serialized field named " +
                    $"'{propertyName}'. Available fields: {availableFields}");
            }

            field.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectArray<T>(
            SerializedProperty property,
            T[] values)
            where T : UnityEngine.Object
        {
            property.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }
        }

        private static T LoadPopupPrefab<T>(string path)
            where T : PopupBase
        {
            AssetDatabase.ImportAsset(
                path,
                ImportAssetOptions.ForceSynchronousImport);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var popup = prefab != null ? prefab.GetComponent<T>() : null;
            if (popup == null)
            {
                throw new InvalidOperationException(
                    $"Could not load popup prefab at '{path}'.");
            }

            return popup;
        }

        private static void SetLayerMask(
            UnityEngine.Object target,
            string propertyName,
            string layerName)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name} is missing serialized property '{propertyName}'.");
            }

            property.intValue = 1 << GetRequiredLayer(layerName);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(
            UnityEngine.Object target,
            string propertyName,
            float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name} is missing serialized property '{propertyName}'.");
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BaseScenePath, true)
            };
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
