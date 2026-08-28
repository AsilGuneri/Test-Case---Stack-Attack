using System;
using StackAttack.Game;
using StackAttack.Input;
using UnityEngine;
using VContainer.Unity;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace StackAttack.Gameplay
{
    public sealed class PlayerInputController :
        IInitializable,
        ITickable,
        IDisposable
    {
        private const int NoActiveTouch = -1;

        private readonly ITouchInputManager _touchInput;
        private readonly PlayerController _player;
        private readonly IGameFlowManager _gameFlow;

        private int _activeTouchId = NoActiveTouch;
        private Vector2 _lastTouchPosition;
        private float _pendingHorizontalDelta;
        private bool _waitForTouchRelease;

        public PlayerInputController(
            ITouchInputManager touchInput,
            PlayerController player,
            IGameFlowManager gameFlow)
        {
            _touchInput = touchInput;
            _player = player;
            _gameFlow = gameFlow;
        }

        public void Initialize()
        {
            _touchInput.TouchSampled += HandleTouchSampled;
            _gameFlow.StateChanged += HandleGameFlowChanged;
        }

        public void Tick()
        {
            if (_gameFlow.IsGameplayActive)
            {
                _player.Move(_pendingHorizontalDelta);
            }

            _pendingHorizontalDelta = 0f;
        }

        public void Dispose()
        {
            _touchInput.TouchSampled -= HandleTouchSampled;
            _gameFlow.StateChanged -= HandleGameFlowChanged;
            ResetActiveTouch();
            _pendingHorizontalDelta = 0f;
        }

        private void HandleTouchSampled(TouchData touch)
        {
            if (!_gameFlow.IsGameplayActive)
            {
                if (_waitForTouchRelease && IsFinished(touch.Phase))
                {
                    _waitForTouchRelease = false;
                }

                return;
            }

            if (_waitForTouchRelease)
            {
                if (IsFinished(touch.Phase))
                {
                    _waitForTouchRelease = false;
                }

                return;
            }

            if (_activeTouchId == NoActiveTouch)
            {
                if (IsFinished(touch.Phase))
                {
                    return;
                }

                _activeTouchId = touch.TouchId;
                _lastTouchPosition = touch.ScreenPosition;
                _player.SetFireHeld(true);
                return;
            }

            if (touch.TouchId != _activeTouchId)
            {
                return;
            }

            if (touch.Phase != InputTouchPhase.Canceled)
            {
                _pendingHorizontalDelta +=
                    touch.ScreenPosition.x - _lastTouchPosition.x;
                _lastTouchPosition = touch.ScreenPosition;
            }

            if (IsFinished(touch.Phase))
            {
                ResetActiveTouch();
            }
        }

        private static bool IsFinished(InputTouchPhase phase)
        {
            return phase == InputTouchPhase.Ended ||
                   phase == InputTouchPhase.Canceled;
        }

        private void ResetActiveTouch()
        {
            _player.SetFireHeld(false);
            _activeTouchId = NoActiveTouch;
            _lastTouchPosition = default;
        }

        private void HandleGameFlowChanged(GameFlowState state)
        {
            if (state == GameFlowState.Playing)
            {
                return;
            }

            _waitForTouchRelease = _activeTouchId != NoActiveTouch;
            ResetActiveTouch();
            _pendingHorizontalDelta = 0f;
        }
    }
}
