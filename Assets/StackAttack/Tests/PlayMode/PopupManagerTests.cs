using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using StackAttack.Game;
using UnityEngine;

namespace StackAttack.Tests
{
    public sealed class PopupManagerTests
    {
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
            foreach (var popup in Object.FindObjectsByType<PopupTestPopup>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(popup.gameObject);
            }

            foreach (var popup in Object.FindObjectsByType<LevelWonPopup>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(popup.gameObject);
            }

            foreach (var popup in Object.FindObjectsByType<LevelLostPopup>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(popup.gameObject);
            }

            foreach (var manager in Object.FindObjectsByType<PopupManager>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }

        [Test]
        public void Open_PassesArgumentsAndHonorsPopupPauseSetting()
        {
            var flow = new GameFlowManager();
            flow.BeginGameplay();
            var prefabObject = new GameObject("Test Popup Prefab");
            var prefab = prefabObject.AddComponent<PopupTestPopup>();
            SetField(typeof(PopupBase), prefab, "pauseGame", true);

            var managerObject = new GameObject("Popup Manager");
            var manager = managerObject.AddComponent<PopupManager>();
            SetField(typeof(PopupManager), manager, "popupRoot", manager.transform);
            SetField(
                typeof(PopupManager),
                manager,
                "popupPrefabs",
                new List<PopupPrefabEntry>
                {
                    new(PopupType.Upgrade, prefab)
                });
            manager.Construct(flow);

            var opened = (PopupTestPopup)manager.Open(
                PopupType.Upgrade,
                "payload",
                42);

            Assert.That(opened.TextArgument, Is.EqualTo("payload"));
            Assert.That(opened.NumberArgument, Is.EqualTo(42));
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Popup));
            Assert.That(Time.timeScale, Is.Zero);

            opened.Close();

            Assert.That(manager.CurrentPopup, Is.Null);
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void LevelWonPopup_RoutesNextAndMenuActions()
        {
            var flow = new GameFlowManager();
            flow.BeginGameplay();
            var prefabObject = new GameObject("Level Won Popup Prefab");
            var prefab = prefabObject.AddComponent<LevelWonPopup>();
            var title = new GameObject("Title")
                .AddComponent<TMPro.TextMeshProUGUI>();
            title.transform.SetParent(prefabObject.transform);
            var nextButton = new GameObject("Next").AddComponent<UnityEngine.UI.Button>();
            nextButton.transform.SetParent(prefabObject.transform);
            var menuButton = new GameObject("Menu").AddComponent<UnityEngine.UI.Button>();
            menuButton.transform.SetParent(prefabObject.transform);
            SetField(typeof(LevelWonPopup), prefab, "titleLabel", title);
            SetField(typeof(LevelWonPopup), prefab, "nextLevelButton", nextButton);
            SetField(typeof(LevelWonPopup), prefab, "mainMenuButton", menuButton);

            var managerObject = new GameObject("Popup Manager");
            var manager = managerObject.AddComponent<PopupManager>();
            SetField(typeof(PopupManager), manager, "popupRoot", manager.transform);
            SetField(
                typeof(PopupManager),
                manager,
                "popupPrefabs",
                new List<PopupPrefabEntry>
                {
                    new(PopupType.LevelWon, prefab)
                });
            manager.Construct(flow);

            var nextRequested = false;
            var popup = (LevelWonPopup)manager.Open(
                PopupType.LevelWon,
                new LevelWonPopupArgs(
                    1,
                    2,
                    () => nextRequested = true,
                    () => true));

            Assert.That(popup.NextLevelButton.interactable, Is.True);
            popup.NextLevelButton.onClick.Invoke();
            Assert.That(nextRequested, Is.True);
            Assert.That(manager.CurrentPopup, Is.Null);

            var menuRequested = false;
            popup = (LevelWonPopup)manager.Open(
                PopupType.LevelWon,
                new LevelWonPopupArgs(
                    2,
                    null,
                    null,
                    () => menuRequested = true));

            Assert.That(popup.NextLevelButton.interactable, Is.False);
            popup.MainMenuButton.onClick.Invoke();
            Assert.That(menuRequested, Is.True);
            Assert.That(manager.CurrentPopup, Is.Null);
        }

        [Test]
        public void LevelLostPopup_RoutesRetryAndMenuActions()
        {
            var flow = new GameFlowManager();
            flow.BeginGameplay();
            var prefabObject = new GameObject("Level Lost Popup Prefab");
            var prefab = prefabObject.AddComponent<LevelLostPopup>();
            var title = new GameObject("Title")
                .AddComponent<TMPro.TextMeshProUGUI>();
            title.transform.SetParent(prefabObject.transform);
            var retryButton = new GameObject("Retry")
                .AddComponent<UnityEngine.UI.Button>();
            retryButton.transform.SetParent(prefabObject.transform);
            var menuButton = new GameObject("Menu")
                .AddComponent<UnityEngine.UI.Button>();
            menuButton.transform.SetParent(prefabObject.transform);
            SetField(typeof(LevelLostPopup), prefab, "titleLabel", title);
            SetField(typeof(LevelLostPopup), prefab, "retryButton", retryButton);
            SetField(typeof(LevelLostPopup), prefab, "mainMenuButton", menuButton);

            var managerObject = new GameObject("Popup Manager");
            var manager = managerObject.AddComponent<PopupManager>();
            SetField(typeof(PopupManager), manager, "popupRoot", manager.transform);
            SetField(
                typeof(PopupManager),
                manager,
                "popupPrefabs",
                new List<PopupPrefabEntry>
                {
                    new(PopupType.LevelLost, prefab)
                });
            manager.Construct(flow);

            var retryRequested = false;
            var popup = (LevelLostPopup)manager.Open(
                PopupType.LevelLost,
                new LevelLostPopupArgs(
                    1,
                    () => retryRequested = true,
                    () => true));
            popup.RetryButton.onClick.Invoke();
            Assert.That(retryRequested, Is.True);
            Assert.That(manager.CurrentPopup, Is.Null);

            var menuRequested = false;
            popup = (LevelLostPopup)manager.Open(
                PopupType.LevelLost,
                new LevelLostPopupArgs(
                    1,
                    () => true,
                    () => menuRequested = true));
            popup.MainMenuButton.onClick.Invoke();
            Assert.That(menuRequested, Is.True);
            Assert.That(manager.CurrentPopup, Is.Null);
        }

        private static void SetField(
            System.Type owner,
            object target,
            string name,
            object value)
        {
            owner.GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }

    public sealed class PopupTestPopup : PopupBase
    {
        public string TextArgument { get; private set; }
        public int NumberArgument { get; private set; }

        protected override void OnOpened(object[] args)
        {
            TextArgument = RequireArgument<string>(args, 0);
            NumberArgument = RequireArgument<int>(args, 1);
        }
    }
}
