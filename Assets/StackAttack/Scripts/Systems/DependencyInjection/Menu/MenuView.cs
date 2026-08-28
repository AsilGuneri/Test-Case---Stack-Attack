using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackAttack.Menu
{
    public sealed class MenuView : MonoBehaviour
    {
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private Button startButton;

        public TMP_Text LevelLabel => levelLabel;
        public Button StartButton => startButton;

        public event Action StartRequested;

        private void Awake()
        {
            startButton.onClick.AddListener(NotifyStartRequested);
        }

        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(NotifyStartRequested);
        }

        public void SetLevel(int levelNumber)
        {
            levelLabel.text = $"Level {levelNumber}";
        }

        public void SetInteractable(bool interactable)
        {
            startButton.interactable = interactable;
        }

        private void NotifyStartRequested()
        {
            StartRequested?.Invoke();
        }
    }
}
