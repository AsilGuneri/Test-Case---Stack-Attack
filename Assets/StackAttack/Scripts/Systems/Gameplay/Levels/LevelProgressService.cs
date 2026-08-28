using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace StackAttack.Gameplay
{
    public readonly struct LevelProgressState
    {
        public float TotalDistance { get; }
        public float CurrentDistance { get; }
        public float NormalizedProgress { get; }
        public bool IsComplete => NormalizedProgress >= 1f;

        public LevelProgressState(
            float totalDistance,
            float currentDistance)
        {
            TotalDistance = Mathf.Max(0f, totalDistance);
            CurrentDistance = Mathf.Clamp(
                currentDistance,
                0f,
                TotalDistance);
            NormalizedProgress = TotalDistance > Mathf.Epsilon
                ? CurrentDistance / TotalDistance
                : 1f;
        }
    }

    public interface ILevelProgressService
    {
        bool IsInitialized { get; }
        float StartPositionY { get; }
        float EndPositionY { get; }
        LevelProgressState Progress { get; }
        float TotalDistance { get; }
        float CurrentDistance { get; }
        float NormalizedProgress { get; }

        event Action<LevelProgressState> ProgressChanged;

        void Initialize(
            float playerStartPositionY,
            IReadOnlyList<ObstacleColumn> columns,
            float completionOffset = 0f);
    }

    public sealed class LevelProgressService :
        ILevelProgressService,
        ITickable,
        IDisposable
    {
        private readonly PlayerController _player;

        public bool IsInitialized { get; private set; }
        public float StartPositionY { get; private set; }
        public float EndPositionY { get; private set; }
        public LevelProgressState Progress { get; private set; }
        public float TotalDistance => Progress.TotalDistance;
        public float CurrentDistance => Progress.CurrentDistance;
        public float NormalizedProgress => Progress.NormalizedProgress;

        public event Action<LevelProgressState> ProgressChanged;

        public LevelProgressService(PlayerController player)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public void Initialize(
            float playerStartPositionY,
            IReadOnlyList<ObstacleColumn> columns,
            float completionOffset = 0f)
        {
            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            StartPositionY = playerStartPositionY;
            EndPositionY = FindLevelEndPositionY(
                playerStartPositionY,
                columns) + Mathf.Max(0f, completionOffset);
            IsInitialized = true;
            UpdateProgress(_player.transform.position.y, true);
        }

        public void Tick()
        {
            if (IsInitialized)
            {
                UpdateProgress(_player.transform.position.y, false);
            }
        }

        public void Dispose()
        {
            ProgressChanged = null;
        }

        private void UpdateProgress(float playerPositionY, bool forcePublish)
        {
            var totalDistance = EndPositionY - StartPositionY;
            var currentDistance = playerPositionY - StartPositionY;
            var nextProgress = new LevelProgressState(
                totalDistance,
                currentDistance);

            if (!forcePublish &&
                Mathf.Approximately(
                    Progress.CurrentDistance,
                    nextProgress.CurrentDistance) &&
                Mathf.Approximately(
                    Progress.TotalDistance,
                    nextProgress.TotalDistance))
            {
                return;
            }

            Progress = nextProgress;
            ProgressChanged?.Invoke(Progress);
        }

        private static float FindLevelEndPositionY(
            float playerStartPositionY,
            IReadOnlyList<ObstacleColumn> columns)
        {
            var endPositionY = playerStartPositionY;
            for (var columnIndex = 0;
                 columnIndex < columns.Count;
                 columnIndex++)
            {
                var column = columns[columnIndex];
                if (column == null)
                {
                    continue;
                }

                var obstacles = column.Obstacles;
                for (var obstacleIndex = 0;
                     obstacleIndex < obstacles.Count;
                     obstacleIndex++)
                {
                    var obstacle = obstacles[obstacleIndex];
                    if (obstacle == null)
                    {
                        continue;
                    }

                    var colliders = obstacle.GetComponentsInChildren<Collider2D>(
                        true);
                    for (var colliderIndex = 0;
                         colliderIndex < colliders.Length;
                         colliderIndex++)
                    {
                        var obstacleCollider = colliders[colliderIndex];
                        if (obstacleCollider != null && obstacleCollider.enabled)
                        {
                            endPositionY = Mathf.Max(
                                endPositionY,
                                obstacleCollider.bounds.max.y);
                        }
                    }
                }
            }

            return endPositionY;
        }
    }
}
