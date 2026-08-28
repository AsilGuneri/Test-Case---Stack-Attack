using System;
using UnityEngine;

namespace StackAttack.Game
{
    public enum GameFlowState
    {
        PreGame,
        Playing,
        Popup
    }

    public interface IGameFlowManager
    {
        GameFlowState State { get; }
        bool IsGameplayActive { get; }
        event Action<GameFlowState> StateChanged;

        void BeginGameplay();
        void EnterPopup();
        void ExitPopup();
    }

    public sealed class GameFlowManager : IGameFlowManager, IDisposable
    {
        private float _resumeTimeScale = 1f;

        public GameFlowState State { get; private set; } = GameFlowState.PreGame;
        public bool IsGameplayActive => State == GameFlowState.Playing;
        public event Action<GameFlowState> StateChanged;

        public void BeginGameplay()
        {
            SetState(GameFlowState.Playing);
        }

        public void EnterPopup()
        {
            if (!IsGameplayActive)
            {
                return;
            }

            _resumeTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            SetState(GameFlowState.Popup);
        }

        public void ExitPopup()
        {
            if (State != GameFlowState.Popup)
            {
                return;
            }

            Time.timeScale = _resumeTimeScale;
            SetState(GameFlowState.Playing);
        }

        public void Dispose()
        {
            if (State == GameFlowState.Popup)
            {
                Time.timeScale = _resumeTimeScale;
            }
        }

        private void SetState(GameFlowState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(State);
        }
    }
}
