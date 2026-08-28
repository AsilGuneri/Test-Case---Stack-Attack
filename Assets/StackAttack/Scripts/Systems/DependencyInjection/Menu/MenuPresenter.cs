using System;
using StackAttack.Systems;
using VContainer.Unity;

namespace StackAttack.Menu
{
    public sealed class MenuPresenter : IStartable, IDisposable
    {
        private readonly MenuView _view;
        private readonly ISceneFlowService _sceneFlow;
        private readonly ILevelSelectionService _levelSelection;

        public MenuPresenter(
            MenuView view,
            ISceneFlowService sceneFlow,
            ILevelSelectionService levelSelection)
        {
            _view = view;
            _sceneFlow = sceneFlow;
            _levelSelection = levelSelection;
        }

        public void Start()
        {
            _view.SetLevel(_levelSelection.CurrentLevelNumber);
            _view.SetInteractable(true);
            _view.StartRequested += HandleStartRequested;
            _levelSelection.LevelChanged += _view.SetLevel;
        }

        public void Dispose()
        {
            _view.StartRequested -= HandleStartRequested;
            _levelSelection.LevelChanged -= _view.SetLevel;
        }

        private void HandleStartRequested()
        {
            var request = new GameStartRequest(
                _levelSelection.CurrentLevelNumber);
            if (_sceneFlow.TryStartGame(request))
            {
                _view.SetInteractable(false);
            }
        }
    }
}
