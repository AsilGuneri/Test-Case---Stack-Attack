using StackAttack.Gameplay;
using StackAttack.Input;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace StackAttack.Game
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameView view;
        [SerializeField] private GameUIController gameUIController;
        [SerializeField] private ObstacleCountUIController obstacleCountUIController;
        [SerializeField] private UpgradeUIController upgradeUIController;
        [SerializeField] private PopupManager popupManager;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private LevelBuilder levelBuilder;
        [SerializeField] private UpgradeSettings upgradeSettings;

        protected override void Configure(IContainerBuilder builder)
        {
            if (view == null || gameUIController == null ||
                obstacleCountUIController == null || upgradeUIController == null ||
                popupManager == null ||
                playerController == null ||
                levelBuilder == null || upgradeSettings == null)
            {
                throw new MissingReferenceException(
                    "GameLifetimeScope requires its game UI, player, and level references.");
            }

            var playerHealth = playerController.GetComponent<PlayerHealth>();
            var playerShooter = playerController.GetComponent<PlayerShooter>();
            if (playerHealth == null || playerShooter == null)
            {
                throw new MissingReferenceException(
                    "GameLifetimeScope requires PlayerHealth and PlayerShooter on its player.");
            }

            builder.RegisterComponent(view);
            builder.RegisterComponent(gameUIController);
            builder.RegisterComponent(obstacleCountUIController);
            builder.RegisterComponent(upgradeUIController);
            builder.RegisterComponent(popupManager).As<IPopupManager>();
            builder.RegisterComponent(playerController);
            builder.RegisterComponent(playerShooter);
            builder.RegisterComponent<IPlayerHealth>(playerHealth);
            builder.RegisterInstance(upgradeSettings);
            builder.RegisterComponent(levelBuilder);
            builder.Register<GameFlowManager>(Lifetime.Scoped).As<IGameFlowManager>();
            builder.Register<PlayerAmmoService>(Lifetime.Scoped)
                .As<IPlayerAmmoService>();
            builder.Register<UpgradeService>(Lifetime.Scoped)
                .As<IUpgradeService>();
            builder.RegisterEntryPoint<LevelProgressService>(Lifetime.Scoped)
                .As<ILevelProgressService>();
            builder.RegisterEntryPoint<CameraController>(Lifetime.Scoped);
            builder.RegisterEntryPoint<TouchInputManager>(Lifetime.Scoped)
                .As<ITouchInputManager>();
            builder.RegisterEntryPoint<PlayerInputController>(Lifetime.Scoped);
            builder.RegisterEntryPoint<GamePresenter>();
        }
    }
}
