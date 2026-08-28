using System.Collections;
using System.Reflection;
using NUnit.Framework;
using StackAttack.Game;
using StackAttack.Gameplay;
using StackAttack.Menu;
using StackAttack.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace StackAttack.Tests
{
    public sealed class AddressableSceneFlowTests
    {
        private const float TimeoutSeconds = 20f;
        private static int _expectedLevelNumber;

        [UnityTest]
        public IEnumerator BaseMenuGameMenu_CompletesAndKeepsOnlyOneChildScene()
        {
            yield return SceneManager.LoadSceneAsync("BaseScene", LoadSceneMode.Single);
            yield return WaitForScene("MenuScene");

            Assert.That(SceneManager.sceneCount, Is.EqualTo(2));
            Assert.That(ActiveCameraCount(), Is.EqualTo(1));
            _expectedLevelNumber = ReadDisplayedLevelNumber();
            AssertMenu(_expectedLevelNumber);

            yield return StartGameAndAssertTransitionGuard(true);
            yield return ReturnToMenu();
            yield return StartGameAndAssertTransitionGuard(true);
            yield return RetryAfterLoss();
            yield return ReturnToMenu();
        }

        private static IEnumerator StartGameAndAssertTransitionGuard(
            bool simulateLoss)
        {
            var startButton = FindButton("Start Button");
            Assert.That(startButton, Is.Not.Null);

            var sceneFlow = Object.FindFirstObjectByType<AddressableSceneFlowService>();
            Assert.That(sceneFlow, Is.Not.Null);

            startButton.onClick.Invoke();
            Assert.That(sceneFlow.IsTransitioning, Is.True);
            Assert.That(sceneFlow.TryStartGame(new GameStartRequest(99)), Is.False);

            yield return WaitForScene("GameScene");

            Assert.That(SceneManager.GetSceneByName("MenuScene").isLoaded, Is.False);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(2));
            Assert.That(ActiveCameraCount(), Is.EqualTo(1));

            var requestLabel = GameObject.Find("Request Label")?.GetComponent<Text>();
            if (requestLabel != null)
            {
                Assert.That(
                    requestLabel.text,
                    Is.EqualTo(
                        $"Injected start request: Level {_expectedLevelNumber}"));
            }

            var camera = Camera.main;
            var playerController = GameObject.Find("Player")?.GetComponent<PlayerController>();
            Assert.That(camera, Is.Not.Null);
            Assert.That(playerController, Is.Not.Null);
            Assert.That(playerController.Movement, Is.Not.Null);
            Assert.That(playerController.Movement.IsInitialized, Is.True);
            Assert.That(playerController.Shooter, Is.Not.Null);
            Assert.That(playerController.Shooter.IsInitialized, Is.True);
            Assert.That(playerController.Health, Is.Not.Null);
            Assert.That(playerController.Health.MaximumHealth, Is.EqualTo(5));
            Assert.That(playerController.Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(
                playerController.Health.InvulnerabilityDuration,
                Is.EqualTo(3.5f));
            var hitFeedback = playerController.GetComponent<PlayerHitFeedback>();
            Assert.That(hitFeedback, Is.Not.Null);
            Assert.That(hitFeedback.PlayerVisual, Is.Not.Null);
            Assert.That(hitFeedback.BlinkCount, Is.EqualTo(7));
            var shield = playerController.GetComponent<PlayerInvulnerabilityShield>();
            Assert.That(shield, Is.Not.Null);
            Assert.That(shield.ShieldCollider, Is.Not.Null);
            Assert.That(shield.ShieldCollider.isTrigger, Is.True);
            Assert.That(shield.ShieldVisualParent, Is.Not.Null);
            Assert.That(shield.DamageInterval, Is.EqualTo(0.1f));
            Assert.That(shield.IsShieldActive, Is.False);
            Assert.That(shield.ShieldVisualParent.activeSelf, Is.False);
            Assert.That(
                playerController.ObstacleLayers.value &
                (1 << LayerMask.NameToLayer("Damageable")),
                Is.Not.Zero);

            var gameUiController = Object.FindFirstObjectByType<GameUIController>();
            Assert.That(gameUiController, Is.Not.Null);
            Assert.That(gameUiController.HeartVisuals.Count, Is.EqualTo(5));
            Assert.That(gameUiController.ActiveHeartCount, Is.EqualTo(5));
            Assert.That(gameUiController.LevelProgressSlider, Is.Not.Null);
            Assert.That(
                gameUiController.LevelProgressSlider.direction,
                Is.EqualTo(Slider.Direction.BottomToTop));
            Assert.That(gameUiController.LevelProgressSlider.interactable, Is.False);
            Assert.That(gameUiController.TotalLevelDistance, Is.GreaterThan(0f));
            Assert.That(gameUiController.CurrentLevelDistance, Is.GreaterThanOrEqualTo(0f));
            Assert.That(
                gameUiController.NormalizedLevelProgress,
                Is.InRange(0f, 1f));

            var upgradeUiController =
                Object.FindFirstObjectByType<UpgradeUIController>();
            Assert.That(upgradeUiController, Is.Not.Null);
            Assert.That(upgradeUiController.ProgressSlider, Is.Not.Null);
            Assert.That(upgradeUiController.ProgressSlider.value, Is.Zero);
            Assert.That(upgradeUiController.ProgressSlider.interactable, Is.False);
            var popupManager = Object.FindFirstObjectByType<PopupManager>();
            Assert.That(popupManager, Is.Not.Null);
            Assert.That(popupManager.CurrentPopup, Is.Null);
            var progressBeforeForwardMovement =
                gameUiController.CurrentLevelDistance;

            playerController.Health.TakeDamage(1);
            yield return null;

            Assert.That(gameUiController.ActiveHeartCount, Is.EqualTo(4));
            Assert.That(shield.IsShieldActive, Is.True);
            Assert.That(shield.ShieldVisualParent.activeSelf, Is.True);
            playerController.Health.InvulnerabilityDuration = 0f;
            playerController.Health.InvulnerabilityDuration = 3.5f;
            Assert.That(shield.IsShieldActive, Is.False);
            Assert.That(shield.ShieldVisualParent.activeSelf, Is.False);

            var levelBuilder = Object.FindFirstObjectByType<LevelBuilder>();
            Assert.That(levelBuilder, Is.Not.Null);
            var obstacleParent = levelBuilder.GetComponent<ObstacleParent>();
            Assert.That(obstacleParent, Is.Not.Null);
            Assert.That(levelBuilder.CurrentLevel, Is.Not.Null);
            Assert.That(
                levelBuilder.CurrentLevel.LevelNumber,
                Is.EqualTo(_expectedLevelNumber));
            Assert.That(levelBuilder.CurrentLevel.GridSize.x, Is.GreaterThan(0));
            Assert.That(levelBuilder.CurrentLevel.GridSize.y, Is.GreaterThan(0));

            var expectedColumnCount = 0;
            var expectedObstacleCount = 0;
            var levelGridSize = levelBuilder.CurrentLevel.GridSize;
            for (var y = 0; y < levelGridSize.y; y++)
            {
                for (var x = 0; x < levelGridSize.x; x++)
                {
                    var cell = levelBuilder.CurrentLevel.GetCell(x, y);
                    if (cell.IsEmpty)
                    {
                        continue;
                    }

                    expectedColumnCount++;
                    expectedObstacleCount += cell.ObstacleAmount;
                }
            }

            Assert.That(expectedColumnCount, Is.GreaterThan(0));
            Assert.That(levelBuilder.Columns.Count, Is.EqualTo(expectedColumnCount));
            Assert.That(
                CountObstacles(levelBuilder.Columns),
                Is.EqualTo(expectedObstacleCount));
            var obstacleCountUi =
                Object.FindFirstObjectByType<ObstacleCountUIController>();
            Assert.That(obstacleCountUi, Is.Not.Null);
            Assert.That(obstacleCountUi.VisualCount, Is.EqualTo(expectedColumnCount));
            for (var columnIndex = 0;
                 columnIndex < levelBuilder.Columns.Count;
                 columnIndex++)
            {
                var column = levelBuilder.Columns[columnIndex];
                Assert.That(
                    obstacleCountUi.TryGetVisual(column, out var countVisual),
                    Is.True);
                Assert.That(
                    countVisual.text,
                    Is.EqualTo(column.RemainingCount.ToString()));
            }
            Assert.That(
                obstacleParent.GetComponent<ObstacleOrbitBehaviour>(),
                Is.Null);

            var obstacle = levelBuilder.Columns[0].Obstacles[0];
            Assert.That(obstacle.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("Damageable")));
            Assert.That(obstacle.CurrentHealth, Is.EqualTo(1));
            var initialObstacleCount = CountObstacles(levelBuilder.Columns);

            var player = playerController.transform;

            yield return new WaitForFixedUpdate();

            var forwardMovementStart = player.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();

            Assert.That(
                Vector3.Dot(player.position - forwardMovementStart, Vector3.up),
                Is.GreaterThan(0f));
            Assert.That(
                gameUiController.CurrentLevelDistance,
                Is.GreaterThan(progressBeforeForwardMovement));
            Assert.That(
                gameUiController.LevelProgressSlider.value,
                Is.EqualTo(gameUiController.NormalizedLevelProgress)
                    .Within(0.001f));

            var playerBody = player.GetComponent<Rigidbody2D>();
            Assert.That(playerBody, Is.Not.Null);

            var swerveStartX = playerBody.position.x;
            playerController.Move(100000f);

            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();

            Assert.That(playerBody.position.x, Is.GreaterThan(swerveStartX));
            Assert.That(
                playerController.Movement.CurrentSwerveSpeed,
                Is.GreaterThan(0f));
            Assert.That(
                Mathf.DeltaAngle(0f, playerBody.rotation),
                Is.LessThan(0f));

            var projectileTargetColumn = FindLowestColumn(levelBuilder.Columns);
            Assert.That(projectileTargetColumn, Is.Not.Null);
            Assert.That(
                obstacleCountUi.TryGetVisual(
                    projectileTargetColumn,
                    out var projectileTargetCountVisual),
                Is.True);
            var targetCountBeforeHit = projectileTargetColumn.RemainingCount;
            playerBody.position = new Vector2(
                projectileTargetColumn.Obstacles[0].transform.position.x,
                playerBody.position.y);
            yield return new WaitForFixedUpdate();

            playerController.SetFireHeld(true);
            yield return null;
            yield return new WaitForEndOfFrame();

            var projectiles = Object.FindObjectsByType<Projectile>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Assert.That(projectiles, Is.Not.Empty);

            var projectile = projectiles[0];
            Assert.That(projectile.IsLaunched, Is.True);
            Assert.That(Vector2.Dot(projectile.Direction, Vector2.up), Is.EqualTo(1f));
            Assert.That(
                projectile.gameObject.layer,
                Is.EqualTo(LayerMask.NameToLayer("Ammo")));

            var projectileStartY = projectile.transform.position.y;
            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();

            Assert.That(
                projectile.transform.position.y,
                Is.GreaterThan(projectileStartY));

            playerController.SetFireHeld(false);
            Assert.That(playerController.Shooter.IsFireHeld, Is.False);

            var hitDeadline = Time.realtimeSinceStartup + 2f;
            while (CountObstacles(levelBuilder.Columns) == initialObstacleCount &&
                   Time.realtimeSinceStartup < hitDeadline)
            {
                yield return null;
            }

            Assert.That(
                CountObstacles(levelBuilder.Columns),
                Is.LessThan(initialObstacleCount),
                "Projectile did not destroy the obstacle.");
            Assert.That(
                projectileTargetColumn.RemainingCount,
                Is.EqualTo(targetCountBeforeHit - 1));
            Assert.That(
                projectileTargetCountVisual.text,
                Is.EqualTo(projectileTargetColumn.RemainingCount.ToString()));
            Assert.That(
                projectileTargetCountVisual.gameObject.activeSelf,
                Is.EqualTo(projectileTargetColumn.RemainingCount > 0));
            Assert.That(
                upgradeUiController.ProgressSlider.value,
                Is.Zero);
            Assert.That(
                projectile == null,
                Is.True,
                "Projectile was not consumed on hit.");

            var upgradeService = (IUpgradeService)typeof(UpgradeUIController)
                .GetField(
                    "_upgrades",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(upgradeUiController);
            upgradeService.ReportClearedColumn(5);

            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(upgradeService.CurrentProgress, Is.EqualTo(5));
            Assert.That(
                popupManager.CurrentPopup,
                Is.TypeOf<UpgradePopup>());
            var upgradePopup = (UpgradePopup)popupManager.CurrentPopup;
            Assert.That(upgradePopup.VisibleOptionCount, Is.EqualTo(3));

            var upgradeButton = FindButton("Upgrade Option 1");
            Assert.That(upgradeButton, Is.Not.Null);
            upgradeButton.onClick.Invoke();

            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(upgradeService.CurrentProgress, Is.Zero);
            Assert.That(popupManager.CurrentPopup, Is.Null);
            Assert.That(upgradeUiController.ProgressSlider.value, Is.Zero);

            var initialCameraPosition = camera.transform.position;
            var initialRotation = camera.transform.rotation;
            playerBody.position += new Vector2(2f, 3f);

            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();

            Assert.That(
                camera.transform.position.y,
                Is.GreaterThan(initialCameraPosition.y));
            Assert.That(
                Mathf.Abs(camera.transform.position.x - initialCameraPosition.x),
                Is.LessThan(0.0001f));
            Assert.That(
                Mathf.Abs(camera.transform.position.z - initialCameraPosition.z),
                Is.LessThan(0.0001f));
            Assert.That(
                Quaternion.Angle(camera.transform.rotation, initialRotation),
                Is.LessThan(0.0001f));

            var viewportCollider = player.GetComponent<Collider2D>();
            Assert.That(viewportCollider, Is.Not.Null);
            var viewportMargin =
                playerController.Movement.HorizontalViewportMargin;
            var cameraDistance = Vector3.Dot(
                player.position - camera.transform.position,
                camera.transform.forward);
            var viewportLeft = camera.ViewportToWorldPoint(
                new Vector3(viewportMargin, 0.5f, cameraDistance)).x;
            var viewportRight = camera.ViewportToWorldPoint(
                new Vector3(1f - viewportMargin, 0.5f, cameraDistance)).x;
            Assert.That(
                viewportCollider.bounds.min.x,
                Is.GreaterThanOrEqualTo(viewportLeft - 0.01f));
            Assert.That(
                viewportCollider.bounds.max.x,
                Is.LessThanOrEqualTo(viewportRight + 0.01f));

            if (simulateLoss)
            {
                playerController.Health.InvulnerabilityDuration = 0f;
                playerController.Health.TakeDamage(
                    playerController.Health.CurrentHealth);
            }
            else
            {
                playerBody.position += Vector2.up *
                    (gameUiController.TotalLevelDistance + 1f);
            }

            var completionDeadline = Time.realtimeSinceStartup + 1f;
            while (popupManager.CurrentPopup == null &&
                   Time.realtimeSinceStartup < completionDeadline)
            {
                yield return null;
            }

            Assert.That(Time.timeScale, Is.Zero);
            var popupRect = popupManager.CurrentPopup.GetComponent<RectTransform>();
            Assert.That(popupRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(popupRect.anchorMax, Is.EqualTo(Vector2.one));

            if (simulateLoss)
            {
                var levelLostPopup = popupManager.CurrentPopup as LevelLostPopup;
                Assert.That(levelLostPopup, Is.Not.Null);
                Assert.That(levelLostPopup.RetryButton, Is.Not.Null);
                Assert.That(levelLostPopup.MainMenuButton, Is.Not.Null);
            }
            else
            {
                var levelWonPopup = popupManager.CurrentPopup as LevelWonPopup;
                Assert.That(levelWonPopup, Is.Not.Null);
                Assert.That(levelWonPopup.NextLevelButton, Is.Not.Null);
                Assert.That(levelWonPopup.MainMenuButton, Is.Not.Null);
                Assert.That(
                    levelWonPopup.NextLevelButton.interactable,
                    Is.EqualTo(levelWonPopup.HasNextLevel));
            }
        }

        private static IEnumerator RetryAfterLoss()
        {
            var popup = Object.FindFirstObjectByType<LevelLostPopup>();
            Assert.That(popup, Is.Not.Null);
            var sceneFlow = Object.FindFirstObjectByType<AddressableSceneFlowService>();

            popup.RetryButton.onClick.Invoke();
            Assert.That(sceneFlow.IsTransitioning, Is.True);

            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (sceneFlow.IsTransitioning)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail("Timed out retrying the level.");
                }

                yield return null;
            }

            var player = GameObject.Find("Player")?.GetComponent<PlayerController>();
            Assert.That(player, Is.Not.Null);
            Assert.That(player.Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(Object.FindFirstObjectByType<LevelLostPopup>(), Is.Null);
        }

        private static IEnumerator ReturnToMenu()
        {
            var levelWonPopup = Object.FindFirstObjectByType<LevelWonPopup>();
            if (levelWonPopup != null)
            {
                levelWonPopup.MainMenuButton.onClick.Invoke();
            }
            else if (Object.FindFirstObjectByType<LevelLostPopup>() is { } lostPopup)
            {
                lostPopup.MainMenuButton.onClick.Invoke();
            }
            else
            {
                var backButton = FindButton("Back Button");
                Assert.That(backButton, Is.Not.Null);
                backButton.onClick.Invoke();
            }

            yield return WaitForScene("MenuScene");

            Assert.That(SceneManager.GetSceneByName("GameScene").isLoaded, Is.False);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(2));
            Assert.That(ActiveCameraCount(), Is.EqualTo(1));
            AssertMenu(_expectedLevelNumber);
        }

        private static void AssertMenu(int expectedLevelNumber)
        {
            var view = Object.FindFirstObjectByType<MenuView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.LevelLabel, Is.TypeOf<TextMeshProUGUI>());
            Assert.That(
                view.LevelLabel.text,
                Is.EqualTo($"Level {expectedLevelNumber}"));
            Assert.That(view.StartButton, Is.Not.Null);
            Assert.That(
                view.StartButton.GetComponentInChildren<TextMeshProUGUI>(true),
                Is.Not.Null);
        }

        private static int ReadDisplayedLevelNumber()
        {
            var view = Object.FindFirstObjectByType<MenuView>();
            Assert.That(view, Is.Not.Null);
            var parts = view.LevelLabel.text.Split(' ');
            Assert.That(int.TryParse(parts[^1], out var levelNumber), Is.True);
            return levelNumber;
        }

        private static Button FindButton(string buttonName)
        {
            var buttons = Object.FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (var index = 0; index < buttons.Length; index++)
            {
                if (buttons[index].name == buttonName)
                {
                    return buttons[index];
                }
            }

            return null;
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail($"Timed out waiting for {sceneName}.");
                }

                yield return null;
            }

            // Let VContainer dispatch IStartable entry points for the new scope.
            yield return null;
        }

        private static int ActiveCameraCount()
        {
            return Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None).Length;
        }

        private static ObstacleColumn FindLowestColumn(
            System.Collections.Generic.IReadOnlyList<ObstacleColumn> columns)
        {
            ObstacleColumn result = null;
            for (var index = 0; index < columns.Count; index++)
            {
                var candidate = columns[index];
                if (result == null ||
                    candidate.transform.position.y < result.transform.position.y)
                {
                    result = candidate;
                }
            }

            return result;
        }

        private static int CountObstacles(
            System.Collections.Generic.IReadOnlyList<ObstacleColumn> columns)
        {
            var count = 0;
            for (var index = 0; index < columns.Count; index++)
            {
                count += columns[index].RemainingCount;
            }

            return count;
        }
    }
}
