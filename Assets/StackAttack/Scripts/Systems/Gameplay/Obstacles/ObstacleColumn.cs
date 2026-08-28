using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace StackAttack.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ObstacleColumn : MonoBehaviour, IDamageable
    {
        [Header("Stack")]
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField, Min(0f)] private float verticalSpacing = 0.18f;
        [SerializeField, Min(0f)] private float depthSpacing = 0.01f;
        [SerializeField] private Obstacle baseObstacle;

        [Header("Hit Feedback")]
        [SerializeField, Min(0f)] private float hitFadeDuration = 0.25f;

        private readonly List<Obstacle> _obstacles = new();
        private bool _isSynchronizing;
        private int _lastReportedRemainingCount = -1;
        private Obstacle _feedbackObstacle;
        private Tween _hitTween;
        private float _hitFlash;

#if UNITY_EDITOR
        private bool _isEditorSyncQueued;
#endif

        public IReadOnlyList<Obstacle> Obstacles => _obstacles;
        public int RemainingCount => _obstacles.Count;
        public float HitFadeDuration => hitFadeDuration;
        public event Action<ObstacleColumn, int> RemainingCountChanged;

        public bool IsBottomObstacle(Obstacle obstacle)
        {
            return obstacle != null && _obstacles.Count > 0 &&
                   _obstacles[0] == obstacle;
        }

        public int Amount
        {
            get => amount;
            set
            {
                amount = Mathf.Max(1, value);
                Synchronize();
            }
        }

        public float VerticalSpacing
        {
            get => verticalSpacing;
            set
            {
                verticalSpacing = Mathf.Max(0f, value);
                Synchronize();
            }
        }

        public float DepthSpacing
        {
            get => depthSpacing;
            set
            {
                depthSpacing = Mathf.Max(0f, value);
                Synchronize();
            }
        }

        public void Configure(int initialAmount, Color color)
        {
            amount = Mathf.Max(1, initialAmount);
            Synchronize();

            for (var index = 0; index < _obstacles.Count; index++)
            {
                _obstacles[index]?.SetColor(color);
            }
        }

        private void Awake()
        {
            Synchronize();
        }

        private void OnEnable()
        {
            RefreshObstacles();
        }

        private void OnDisable()
        {
            StopHitFeedback();
            UnsubscribeFromObstacles();
        }

        private void OnTransformChildrenChanged()
        {
            if (_isSynchronizing)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                QueueEditorSynchronize();
                return;
            }
#endif

            RefreshObstacles();
        }

        private void OnValidate()
        {
            amount = Mathf.Max(1, amount);
            verticalSpacing = Mathf.Max(0f, verticalSpacing);
            depthSpacing = Mathf.Max(0f, depthSpacing);
            hitFadeDuration = Mathf.Max(0f, hitFadeDuration);

#if UNITY_EDITOR
            QueueEditorSynchronize();
#endif
        }

        public void Synchronize()
        {
            if (_isSynchronizing)
            {
                return;
            }

            _isSynchronizing = true;
            try
            {
                amount = Mathf.Max(1, amount);
                verticalSpacing = Mathf.Max(0f, verticalSpacing);
                depthSpacing = Mathf.Max(0f, depthSpacing);
                RefreshObstacles();

                if (_obstacles.Count == 0)
                {
                    SetBaseObstacle(null);
                    if (Application.isPlaying)
                    {
                        Debug.LogError(
                            $"'{name}' requires one direct-child Obstacle as its base.",
                            this);
                    }

                    return;
                }

                EnsureBaseObstacle();

                while (_obstacles.Count < amount)
                {
                    var createdObstacle = CreateObstacleFromBase();
                    if (createdObstacle == null)
                    {
                        break;
                    }

                    _obstacles.Add(createdObstacle);
                }

                while (_obstacles.Count > amount)
                {
                    var lastIndex = _obstacles.Count - 1;
                    var extraObstacle = _obstacles[lastIndex];
                    _obstacles.RemoveAt(lastIndex);
                    DestroyObstacle(extraObstacle);
                }

                NormalizeObstacles();
                SubscribeToObstacles();
            }
            finally
            {
                _isSynchronizing = false;
                NotifyRemainingCountChanged();
            }
        }

        public void RefreshObstacles()
        {
            UnsubscribeFromObstacles();
            _obstacles.Clear();

            for (var childIndex = 0; childIndex < transform.childCount; childIndex++)
            {
                var child = transform.GetChild(childIndex);
                if (child.TryGetComponent<Obstacle>(out var obstacle))
                {
                    _obstacles.Add(obstacle);
                }
            }

            _obstacles.Sort(CompareObstacles);
            EnsureBaseObstacle();
            SubscribeToObstacles();

            if (!_isSynchronizing)
            {
                NotifyRemainingCountChanged();
            }
        }

        public int TakeDamage(int amount)
        {
            if (amount <= 0 || _obstacles.Count == 0)
            {
                return 0;
            }

            StartHitFeedback();
            var acceptedDamage = 0;
            while (amount > 0 && _obstacles.Count > 0)
            {
                var topIndex = _obstacles.Count - 1;
                var obstacle = _obstacles[topIndex];
                if (obstacle == null)
                {
                    _obstacles.RemoveAt(topIndex);
                    continue;
                }

                var applied = obstacle.TakeDamage(amount);
                if (applied <= 0)
                {
                    break;
                }

                acceptedDamage += applied;
                amount -= applied;
            }

            return acceptedDamage;
        }

        private void HandleObstacleDied(Obstacle obstacle)
        {
            obstacle.Died -= HandleObstacleDied;
            _obstacles.Remove(obstacle);

            if (baseObstacle == obstacle)
            {
                SetBaseObstacle(_obstacles.Count > 0 ? _obstacles[0] : null);
            }

            NormalizeObstacles();
            ApplyHitFeedback();
            NotifyRemainingCountChanged();
        }

        private void StartHitFeedback()
        {
            _hitTween?.Kill(false);
            _hitTween = null;
            _hitFlash = 1f;
            ApplyHitFeedback();

            if (hitFadeDuration <= 0f)
            {
                StopHitFeedback();
                return;
            }

            _hitTween = DOTween
                .To(
                    () => _hitFlash,
                    value =>
                    {
                        _hitFlash = value;
                        ApplyHitFeedback();
                    },
                    0f,
                    hitFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    _hitTween = null;
                    _hitFlash = 0f;
                    ApplyHitFeedback();
                });
        }

        private void ApplyHitFeedback()
        {
            var nextObstacle = _obstacles.Count > 0
                ? _obstacles[_obstacles.Count - 1]
                : null;
            if (_feedbackObstacle != null && _feedbackObstacle != nextObstacle)
            {
                _feedbackObstacle.RestoreVisualColor();
            }

            _feedbackObstacle = nextObstacle;
            if (_feedbackObstacle != null)
            {
                _feedbackObstacle.ApplyHitFlash(_hitFlash);
            }
        }

        private void StopHitFeedback()
        {
            _hitTween?.Kill(false);
            _hitTween = null;
            _hitFlash = 0f;

            if (_feedbackObstacle != null)
            {
                _feedbackObstacle.RestoreVisualColor();
                _feedbackObstacle = null;
            }
        }

        private void NotifyRemainingCountChanged()
        {
            if (_lastReportedRemainingCount == RemainingCount)
            {
                return;
            }

            _lastReportedRemainingCount = RemainingCount;
            RemainingCountChanged?.Invoke(this, RemainingCount);
        }

        private void NormalizeObstacles()
        {
            _obstacles.Sort(CompareObstacles);

            for (var index = 0; index < _obstacles.Count; index++)
            {
                var obstacle = _obstacles[index];
                if (obstacle == null)
                {
                    continue;
                }

                var targetName = $"Obstacle {index + 1}";
                var targetPosition = new Vector3(
                    0f,
                    verticalSpacing * index,
                    -depthSpacing * index);

#if UNITY_EDITOR
                if (!Application.isPlaying &&
                    (obstacle.name != targetName ||
                     obstacle.transform.localPosition != targetPosition))
                {
                    Undo.RecordObject(obstacle.gameObject, "Synchronize Obstacle Column");
                    Undo.RecordObject(obstacle.transform, "Synchronize Obstacle Column");
                }
#endif

                obstacle.name = targetName;
                obstacle.transform.localPosition = targetPosition;
            }
        }

        private Obstacle CreateObstacleFromBase()
        {
            if (baseObstacle == null)
            {
                return null;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var prefabSource =
                    PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                        baseObstacle.gameObject);
                GameObject createdObject;

                if (prefabSource != null)
                {
                    createdObject = PrefabUtility.InstantiatePrefab(
                        prefabSource,
                        transform) as GameObject;
                }
                else
                {
                    createdObject = Instantiate(baseObstacle.gameObject, transform);
                }

                if (createdObject != null)
                {
                    Undo.RegisterCreatedObjectUndo(
                        createdObject,
                        "Add Obstacle To Column");
                    return createdObject.GetComponent<Obstacle>();
                }

                return null;
            }
#endif

            return Instantiate(baseObstacle, transform);
        }

        private void DestroyObstacle(Obstacle obstacle)
        {
            if (obstacle == null)
            {
                return;
            }

            obstacle.Died -= HandleObstacleDied;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(obstacle.gameObject);
                return;
            }
#endif

            Destroy(obstacle.gameObject);
        }

        private void EnsureBaseObstacle()
        {
            if (baseObstacle != null &&
                baseObstacle.transform.parent == transform &&
                _obstacles.Contains(baseObstacle))
            {
                return;
            }

            SetBaseObstacle(_obstacles.Count > 0 ? _obstacles[0] : null);
        }

        private void SetBaseObstacle(Obstacle obstacle)
        {
            if (baseObstacle == obstacle)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(this, "Assign Obstacle Column Base");
            }
#endif

            baseObstacle = obstacle;
        }

        private void SubscribeToObstacles()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            for (var index = 0; index < _obstacles.Count; index++)
            {
                var obstacle = _obstacles[index];
                if (obstacle == null)
                {
                    continue;
                }

                obstacle.Died -= HandleObstacleDied;
                obstacle.Died += HandleObstacleDied;
            }
        }

        private void UnsubscribeFromObstacles()
        {
            for (var index = 0; index < _obstacles.Count; index++)
            {
                var obstacle = _obstacles[index];
                if (obstacle != null)
                {
                    obstacle.Died -= HandleObstacleDied;
                }
            }
        }

        private static int CompareObstacles(Obstacle left, Obstacle right)
        {
            if (left == null)
            {
                return right == null ? 0 : 1;
            }

            if (right == null)
            {
                return -1;
            }

            var heightComparison = left.transform.localPosition.y.CompareTo(
                right.transform.localPosition.y);
            return heightComparison != 0
                ? heightComparison
                : left.transform.GetSiblingIndex().CompareTo(
                    right.transform.GetSiblingIndex());
        }

#if UNITY_EDITOR
        private void QueueEditorSynchronize()
        {
            if (Application.isPlaying ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                _isEditorSyncQueued)
            {
                return;
            }

            _isEditorSyncQueued = true;
            EditorApplication.delayCall += SynchronizeAfterValidation;
        }

        private void SynchronizeAfterValidation()
        {
            _isEditorSyncQueued = false;
            if (this == null || Application.isPlaying)
            {
                return;
            }

            Synchronize();
            EditorUtility.SetDirty(this);

            if (gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
#endif
    }
}
