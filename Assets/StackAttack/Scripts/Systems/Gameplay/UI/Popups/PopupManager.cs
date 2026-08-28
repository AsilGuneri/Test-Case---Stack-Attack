using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace StackAttack.Game
{
    public interface IPopupManager
    {
        PopupBase CurrentPopup { get; }
        bool IsOpen(PopupType type);
        PopupBase Open(PopupType type, params object[] args);
        bool Close(PopupType type);
        void CloseCurrent();
    }

    [DisallowMultipleComponent]
    public sealed class PopupManager : MonoBehaviour, IPopupManager
    {
        [SerializeField] private Transform popupRoot;
        [SerializeField] private List<PopupPrefabEntry> popupPrefabs = new();

        private readonly Dictionary<PopupType, PopupBase> _prefabs = new();
        private IGameFlowManager _gameFlow;

        public PopupBase CurrentPopup { get; private set; }

        [Inject]
        public void Construct(IGameFlowManager gameFlow)
        {
            _gameFlow = gameFlow ?? throw new ArgumentNullException(nameof(gameFlow));
            BuildLookup();
        }

        public bool IsOpen(PopupType type)
        {
            return CurrentPopup != null && CurrentPopup.Type == type;
        }

        public PopupBase Open(PopupType type, params object[] args)
        {
            if (!_prefabs.TryGetValue(type, out var prefab))
            {
                throw new InvalidOperationException(
                    $"No popup prefab is registered for {type}.");
            }

            CloseCurrent();
            var popup = Instantiate(prefab, popupRoot);
            popup.name = prefab.name;
            CurrentPopup = popup;

            if (popup.PauseGame)
            {
                _gameFlow.EnterPopup();
            }

            try
            {
                popup.Open(this, type, args);
                return popup;
            }
            catch
            {
                CloseCurrent();
                throw;
            }
        }

        public bool Close(PopupType type)
        {
            if (!IsOpen(type))
            {
                return false;
            }

            CloseCurrent();
            return true;
        }

        public void CloseCurrent()
        {
            if (CurrentPopup == null)
            {
                return;
            }

            var popup = CurrentPopup;
            CurrentPopup = null;
            popup.NotifyClosed();
            if (popup.PauseGame)
            {
                _gameFlow.ExitPopup();
            }

            Destroy(popup.gameObject);
        }

        internal void Close(PopupBase popup)
        {
            if (popup == CurrentPopup)
            {
                CloseCurrent();
            }
        }

        private void OnDestroy()
        {
            if (CurrentPopup != null && CurrentPopup.PauseGame)
            {
                _gameFlow?.ExitPopup();
            }
        }

        private void BuildLookup()
        {
            if (popupRoot == null)
            {
                throw new MissingReferenceException(
                    "PopupManager requires a popup root.");
            }

            _prefabs.Clear();
            for (var index = 0; index < popupPrefabs.Count; index++)
            {
                var entry = popupPrefabs[index];
                if (entry.Prefab == null)
                {
                    throw new MissingReferenceException(
                        $"Popup prefab entry {index} is missing its prefab.");
                }

                if (!_prefabs.TryAdd(entry.Type, entry.Prefab))
                {
                    throw new InvalidOperationException(
                        $"Popup type {entry.Type} is registered more than once.");
                }
            }
        }
    }
}
