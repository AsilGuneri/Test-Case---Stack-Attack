using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackAttack.Game
{
    public sealed class LevelWonPopupArgs
    {
        public int CompletedLevelNumber { get; }
        public int? NextLevelNumber { get; }
        public bool HasNextLevel => NextLevelNumber.HasValue;
        public Func<bool> StartNextLevel { get; }
        public Func<bool> ReturnToMenu { get; }

        public LevelWonPopupArgs(
            int completedLevelNumber,
            int? nextLevelNumber,
            Func<bool> startNextLevel,
            Func<bool> returnToMenu)
        {
            CompletedLevelNumber = completedLevelNumber;
            NextLevelNumber = nextLevelNumber;
            StartNextLevel = startNextLevel;
            ReturnToMenu = returnToMenu ??
                throw new ArgumentNullException(nameof(returnToMenu));
        }
    }

    public sealed class LevelWonPopup : PopupBase
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button mainMenuButton;

        private LevelWonPopupArgs _args;

        public Button NextLevelButton => nextLevelButton;
        public Button MainMenuButton => mainMenuButton;
        public bool HasNextLevel => _args?.HasNextLevel == true;

        protected override void OnOpened(object[] args)
        {
            _args = RequireArgument<LevelWonPopupArgs>(args);
            if (titleLabel == null || nextLevelButton == null ||
                mainMenuButton == null)
            {
                throw new MissingReferenceException(
                    "LevelWonPopup requires its label and buttons.");
            }

            titleLabel.text = $"{_args.CompletedLevelNumber}";
            nextLevelButton.interactable = _args.HasNextLevel;
            nextLevelButton.onClick.AddListener(HandleNextLevel);
            mainMenuButton.onClick.AddListener(HandleMainMenu);
        }

        protected override void OnClosed()
        {
            nextLevelButton?.onClick.RemoveListener(HandleNextLevel);
            mainMenuButton?.onClick.RemoveListener(HandleMainMenu);
            _args = null;
        }

        private void HandleNextLevel()
        {
            TryTransition(_args.StartNextLevel);
        }

        private void HandleMainMenu()
        {
            TryTransition(_args.ReturnToMenu);
        }

        private void TryTransition(Func<bool> transition)
        {
            if (transition == null)
            {
                return;
            }

            SetInteractable(false);
            if (transition())
            {
                Close();
            }
            else
            {
                SetInteractable(true);
            }
        }

        private void SetInteractable(bool interactable)
        {
            nextLevelButton.interactable = interactable && HasNextLevel;
            mainMenuButton.interactable = interactable;
        }
    }
}
