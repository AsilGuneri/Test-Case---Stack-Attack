using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using VContainer.Unity;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace StackAttack.Input
{
    public readonly struct TouchData
    {
        public int TouchId { get; }
        public InputTouchPhase Phase { get; }
        public Vector2 ScreenPosition { get; }
        public Vector2 Delta { get; }

        public TouchData(
            int touchId,
            InputTouchPhase phase,
            Vector2 screenPosition,
            Vector2 delta)
        {
            TouchId = touchId;
            Phase = phase;
            ScreenPosition = screenPosition;
            Delta = delta;
        }
    }

    public interface ITouchInputManager
    {
        IReadOnlyList<TouchData> ActiveTouches { get; }
        event Action<TouchData> TouchSampled;
    }

    public sealed class TouchInputManager :
        ITouchInputManager,
        IInitializable,
        ITickable,
        IDisposable
    {
        private readonly List<TouchData> _activeTouches = new List<TouchData>();

        public IReadOnlyList<TouchData> ActiveTouches => _activeTouches;
        public event Action<TouchData> TouchSampled;

        public void Initialize()
        {
            EnhancedTouchSupport.Enable();
        }

        public void Tick()
        {
            _activeTouches.Clear();

            var touches = Touch.activeTouches;
            if (touches.Count == 0)
            {
                return;
            }

            for (var index = 0; index < touches.Count; index++)
            {
                var touch = touches[index];
                var data = new TouchData(
                    touch.touchId,
                    touch.phase,
                    touch.screenPosition,
                    touch.delta);

                _activeTouches.Add(data);
                TouchSampled?.Invoke(data);

                if (data.Phase != InputTouchPhase.Stationary)
                {
                    Debug.Log(
                        $"Touch {data.TouchId}: {data.Phase}, " +
                        $"position {data.ScreenPosition}, delta {data.Delta}");
                }
            }
        }

        public void Dispose()
        {
            TouchSampled = null;
            _activeTouches.Clear();
            EnhancedTouchSupport.Disable();
        }
    }
}
