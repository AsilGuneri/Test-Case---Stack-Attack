using System;
using UnityEngine;

namespace StackAttack.Game
{
    public abstract class PopupBase : MonoBehaviour
    {
        [SerializeField] private bool pauseGame;

        private PopupManager _manager;

        public PopupType Type { get; private set; }
        public bool PauseGame => pauseGame;
        public bool IsOpen => _manager != null;

        internal void Open(
            PopupManager manager,
            PopupType type,
            object[] args)
        {
            _manager = manager;
            Type = type;
            OnOpened(args ?? Array.Empty<object>());
        }

        internal void NotifyClosed()
        {
            OnClosed();
            _manager = null;
        }

        public void Close()
        {
            _manager?.Close(this);
        }

        protected abstract void OnOpened(object[] args);
        protected virtual void OnClosed() { }

        protected static T RequireArgument<T>(object[] args, int index = 0)
        {
            if (args == null || index < 0 || index >= args.Length ||
                args[index] is not T value)
            {
                throw new ArgumentException(
                    $"Popup argument {index} must be {typeof(T).Name}.",
                    nameof(args));
            }

            return value;
        }
    }
}
