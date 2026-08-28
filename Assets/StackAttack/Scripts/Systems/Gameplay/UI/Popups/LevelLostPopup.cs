using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackAttack.Game
{
    public sealed class LevelLostPopupArgs
    {
        public int LevelNumber { get; }
        public Func<bool> Retry { get; }
        public Func<bool> ReturnToMenu { get; }

        public LevelLostPopupArgs(
            int levelNumber,
            Func<bool> retry,
            Func<bool> returnToMenu)
        {
            LevelNumber = levelNumber;
            Retry = retry ?? throw new ArgumentNullException(nameof(retry));
            ReturnToMenu = returnToMenu ??
                throw new ArgumentNullException(nameof(returnToMenu));
        }
    }

    public sealed class LevelLostPopup : PopupBase
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        private LevelLostPopupArgs _args;

        public Button RetryButton => retryButton;
        public Button MainMenuButton => mainMenuButton;

        protected override void OnOpened(object[] args)
        {
            _args = RequireArgument<LevelLostPopupArgs>(args);
            if (titleLabel == null || retryButton == null ||
                mainMenuButton == null)
            {
                throw new MissingReferenceException(
                    "LevelLostPopup requires its label and buttons.");
            }

            titleLabel.text = $"LEVEL {_args.LevelNumber} FAILED";
            retryButton.onClick.AddListener(HandleRetry);
            mainMenuButton.onClick.AddListener(HandleMainMenu);
        }

        protected override void OnClosed()
        {
            retryButton?.onClick.RemoveListener(HandleRetry);
            mainMenuButton?.onClick.RemoveListener(HandleMainMenu);
            _args = null;
        }

        private void HandleRetry()
        {
            TryTransition(_args.Retry);
        }

        private void HandleMainMenu()
        {
            TryTransition(_args.ReturnToMenu);
        }

        private void TryTransition(Func<bool> transition)
        {
            retryButton.interactable = false;
            mainMenuButton.interactable = false;
            if (transition())
            {
                Close();
                return;
            }

            retryButton.interactable = true;
            mainMenuButton.interactable = true;
        }
    }
}
