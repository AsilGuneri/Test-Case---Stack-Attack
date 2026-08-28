using System;
using UnityEngine;
using UnityEngine.UI;

namespace StackAttack.Game
{
    public sealed class GameView : MonoBehaviour
    {
        [SerializeField] private Text requestLabel;
        [SerializeField] private Button backButton;

        public event Action BackRequested;

        private void Awake()
        {
            backButton.onClick.AddListener(NotifyBackRequested);
        }

        private void OnDestroy()
        {
            backButton.onClick.RemoveListener(NotifyBackRequested);
        }

        public void ShowRequest(int levelNumber)
        {
            if (requestLabel != null)
            {
                requestLabel.text = $"Injected start request: Level {levelNumber}";
            }
        }

        public void SetInteractable(bool interactable)
        {
            backButton.interactable = interactable;
        }

        private void NotifyBackRequested()
        {
            BackRequested?.Invoke();
        }
    }
}
