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
#if UNITY_EDITOR
        private const int EditorMouseTouchId = int.MinValue;
#endif

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
            for (var index = 0; index < touches.Count; index++)
            {
                var touch = touches[index];
                Publish(new TouchData(
                    touch.touchId,
                    touch.phase,
                    touch.screenPosition,
                    touch.delta));
            }

#if UNITY_EDITOR
            if (touches.Count == 0)
            {
                SampleEditorMouse();
            }
#endif
        }

#if UNITY_EDITOR
        private void SampleEditorMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            var button = mouse.leftButton;
            if (!button.isPressed && !button.wasReleasedThisFrame)
            {
                return;
            }

            var delta = mouse.delta.ReadValue();
            var phase = button.wasPressedThisFrame
                ? InputTouchPhase.Began
                : button.wasReleasedThisFrame
                    ? InputTouchPhase.Ended
                    : delta.sqrMagnitude > 0f
                        ? InputTouchPhase.Moved
                        : InputTouchPhase.Stationary;

            Publish(new TouchData(
                EditorMouseTouchId,
                phase,
                mouse.position.ReadValue(),
                delta));
        }
#endif

        private void Publish(TouchData data)
        {
            _activeTouches.Add(data);
            TouchSampled?.Invoke(data);

            if (data.Phase != InputTouchPhase.Stationary)
            {
                Debug.Log(
                    $"Touch {data.TouchId}: {data.Phase}, " +
                    $"position {data.ScreenPosition}, delta {data.Delta}");
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
