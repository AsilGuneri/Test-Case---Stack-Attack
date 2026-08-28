using System.Collections.Generic;
using StackAttack.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace StackAttack.Game
{
    [DisallowMultipleComponent]
    public sealed class GameUIController : MonoBehaviour
    {
        [SerializeField] private GameObject heartVisualPrefab;
        [SerializeField] private Transform heartParent;
        [SerializeField] private Slider levelProgressSlider;

        private readonly List<GameObject> _heartVisuals = new();
        private IPlayerHealth _playerHealth;
        private ILevelProgressService _levelProgressService;
        private LevelProgressState _levelProgress;

        public IReadOnlyList<GameObject> HeartVisuals => _heartVisuals;
        public LevelProgressState LevelProgress => _levelProgress;
        public float TotalLevelDistance => _levelProgress.TotalDistance;
        public float CurrentLevelDistance => _levelProgress.CurrentDistance;
        public float NormalizedLevelProgress => _levelProgress.NormalizedProgress;
        public Slider LevelProgressSlider => levelProgressSlider;

        public int ActiveHeartCount
        {
            get
            {
                var activeCount = 0;
                for (var index = 0; index < _heartVisuals.Count; index++)
                {
                    if (_heartVisuals[index] != null &&
                        _heartVisuals[index].activeSelf)
                    {
                        activeCount++;
                    }
                }

                return activeCount;
            }
        }

        [Inject]
        public void Construct(
            IPlayerHealth playerHealth,
            ILevelProgressService levelProgressService)
        {
            if (playerHealth == null)
            {
                throw new MissingReferenceException(
                    "GameUIController requires an IPlayerHealth.");
            }

            if (levelProgressService == null)
            {
                throw new MissingReferenceException(
                    "GameUIController requires an ILevelProgressService.");
            }

            if (heartVisualPrefab == null || heartParent == null ||
                levelProgressSlider == null)
            {
                throw new MissingReferenceException(
                    "GameUIController requires its heart and level progress UI references.");
            }

            UnsubscribeFromHealth();
            UnsubscribeFromLevelProgress();
            _playerHealth = playerHealth;
            _levelProgressService = levelProgressService;
            RebuildHeartVisuals();
            RefreshHearts(_playerHealth.CurrentHealth);
            RefreshLevelProgress(_levelProgressService.Progress);
            _playerHealth.HealthChanged += RefreshHearts;
            _playerHealth.MaximumHealthChanged += HandleMaximumHealthChanged;
            _levelProgressService.ProgressChanged += RefreshLevelProgress;
        }

        public void Refresh()
        {
            if (_playerHealth != null)
            {
                RefreshHearts(_playerHealth.CurrentHealth);
            }

            if (_levelProgressService != null)
            {
                RefreshLevelProgress(_levelProgressService.Progress);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromHealth();
            UnsubscribeFromLevelProgress();
        }

        private void RebuildHeartVisuals()
        {
            for (var index = 0; index < _heartVisuals.Count; index++)
            {
                if (_heartVisuals[index] != null)
                {
                    Destroy(_heartVisuals[index]);
                }
            }

            _heartVisuals.Clear();
            for (var index = 0; index < _playerHealth.MaximumHealth; index++)
            {
                var heartVisual = Instantiate(heartVisualPrefab, heartParent);
                heartVisual.name = $"Heart {index + 1}";
                _heartVisuals.Add(heartVisual);
            }
        }

        private void HandleMaximumHealthChanged(int maximumHealth)
        {
            while (_heartVisuals.Count < maximumHealth)
            {
                var heartVisual = Instantiate(heartVisualPrefab, heartParent);
                heartVisual.name = $"Heart {_heartVisuals.Count + 1}";
                _heartVisuals.Add(heartVisual);
            }
        }

        private void RefreshHearts(int currentHealth)
        {
            for (var index = 0; index < _heartVisuals.Count; index++)
            {
                var heartVisual = _heartVisuals[index];
                if (heartVisual != null)
                {
                    heartVisual.SetActive(index < currentHealth);
                }
            }
        }

        private void RefreshLevelProgress(LevelProgressState progress)
        {
            _levelProgress = progress;
            levelProgressSlider.SetValueWithoutNotify(
                progress.NormalizedProgress);
        }

        private void UnsubscribeFromHealth()
        {
            if (_playerHealth != null)
            {
                _playerHealth.HealthChanged -= RefreshHearts;
                _playerHealth.MaximumHealthChanged -= HandleMaximumHealthChanged;
            }
        }

        private void UnsubscribeFromLevelProgress()
        {
            if (_levelProgressService != null)
            {
                _levelProgressService.ProgressChanged -= RefreshLevelProgress;
            }
        }
    }
}
