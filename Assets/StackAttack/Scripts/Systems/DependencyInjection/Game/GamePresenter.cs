using System;
using StackAttack.Gameplay;
using StackAttack.Systems;
using VContainer.Unity;

namespace StackAttack.Game
{
    public sealed class GamePresenter : IStartable, IDisposable
    {
        private readonly GameView _view;
        private readonly GameUIController _gameUiController;
        private readonly ObstacleCountUIController _obstacleCountUiController;
        private readonly ISceneFlowService _sceneFlow;
        private readonly IGameFlowManager _gameFlow;
        private readonly PlayerController _player;
        private readonly GameStartRequest _request;
        private readonly LevelCatalog _levelCatalog;
        private readonly LevelBuilder _levelBuilder;
        private readonly ILevelProgressService _levelProgress;
        private readonly ILevelSelectionService _levelSelection;
        private readonly ISaveManager _saveManager;
        private readonly IPopupManager _popups;
        private bool _outcomeReported;

        public GamePresenter(
            GameView view,
            GameUIController gameUiController,
            ObstacleCountUIController obstacleCountUiController,
            ISceneFlowService sceneFlow,
            IGameFlowManager gameFlow,
            PlayerController player,
            GameStartRequest request,
            LevelCatalog levelCatalog,
            LevelBuilder levelBuilder,
            ILevelProgressService levelProgress,
            ILevelSelectionService levelSelection,
            ISaveManager saveManager,
            IPopupManager popups)
        {
            _view = view;
            _gameUiController = gameUiController;
            _obstacleCountUiController = obstacleCountUiController;
            _sceneFlow = sceneFlow;
            _gameFlow = gameFlow;
            _player = player;
            _request = request;
            _levelCatalog = levelCatalog;
            _levelBuilder = levelBuilder;
            _levelProgress = levelProgress;
            _levelSelection = levelSelection;
            _saveManager = saveManager;
            _popups = popups;
        }

        public void Start()
        {
            var level = _levelCatalog.GetRequiredLevel(_request.LevelNumber);
            var playerStartPosition = _player.transform.position;
            _levelBuilder.Build(level, playerStartPosition);
            _obstacleCountUiController.Initialize(_levelBuilder.Columns);
            _levelProgress.ProgressChanged += HandleLevelProgressChanged;
            _levelProgress.Initialize(
                playerStartPosition.y,
                _levelBuilder.Columns,
                level.CompletionOffset);
            _player.Initialize();
            _gameUiController.Refresh();
            _gameFlow.BeginGameplay();
            _view.ShowRequest(_request.LevelNumber);
            _view.SetInteractable(true);
            _view.BackRequested += HandleBackRequested;
            _player.Health.Died += HandlePlayerDied;
        }

        public void Dispose()
        {
            _view.BackRequested -= HandleBackRequested;
            _levelProgress.ProgressChanged -= HandleLevelProgressChanged;
            _player.Health.Died -= HandlePlayerDied;
        }

        private void HandleBackRequested()
        {
            if (TryReturnToMenu())
            {
                _view.SetInteractable(false);
            }
        }

        private void HandleLevelProgressChanged(LevelProgressState progress)
        {
            if (_outcomeReported || !progress.IsComplete)
            {
                return;
            }

            _outcomeReported = true;
            _saveManager.MarkLevelCompleted(_request.LevelNumber);
            var nextLevelNumber = _request.LevelNumber + 1;
            int? availableNextLevel = null;
            if (_levelCatalog.TryGetLevel(nextLevelNumber, out _))
            {
                _levelSelection.SelectLevel(nextLevelNumber);
                availableNextLevel = nextLevelNumber;
            }

            _popups.Open(
                PopupType.LevelWon,
                new LevelWonPopupArgs(
                    _request.LevelNumber,
                    availableNextLevel,
                    availableNextLevel.HasValue
                        ? () => TryStartLevel(availableNextLevel.Value)
                        : null,
                    TryReturnToMenu));
        }

        private void HandlePlayerDied()
        {
            if (_outcomeReported)
            {
                return;
            }

            _outcomeReported = true;
            _popups.Open(
                PopupType.LevelLost,
                new LevelLostPopupArgs(
                    _request.LevelNumber,
                    () => TryStartLevel(_request.LevelNumber),
                    TryReturnToMenu));
        }

        private bool TryStartLevel(int levelNumber)
        {
            if (!_levelCatalog.TryGetLevel(levelNumber, out _))
            {
                return false;
            }

            var started = _sceneFlow.TryStartGame(
                new GameStartRequest(levelNumber));
            if (started)
            {
                _view.SetInteractable(false);
            }

            return started;
        }

        private bool TryReturnToMenu()
        {
            return _sceneFlow.TryShowMenu();
        }
    }
}
