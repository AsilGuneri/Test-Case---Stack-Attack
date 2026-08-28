using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace StackAttack.Systems
{
    public sealed class AddressableSceneFlowService : MonoBehaviour, ISceneFlowService
    {
        private SceneCatalog _catalog;
        private LifetimeScope _baseScope;
        private SceneTransitionView _transitionView;
        private AsyncOperationHandle<SceneInstance> _currentSceneHandle;
        private bool _hasCurrentScene;

        public bool IsTransitioning { get; private set; }

        [Inject]
        public void Construct(
            SceneCatalog catalog,
            LifetimeScope baseScope,
            SceneTransitionView transitionView)
        {
            _catalog = catalog;
            _baseScope = baseScope;
            _transitionView = transitionView;
        }

        public bool TryShowMenu()
        {
            return TryBeginTransition(_catalog?.MenuScene, null, false);
        }

        public bool TryStartGame(GameStartRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return TryBeginTransition(_catalog?.GameScene, request, true);
        }

        private bool TryBeginTransition(
            AssetReference targetScene,
            GameStartRequest request,
            bool fallbackToMenu)
        {
            if (IsTransitioning)
            {
                return false;
            }

            if (targetScene == null || !targetScene.RuntimeKeyIsValid())
            {
                const string message = "The requested Addressable scene reference is missing or invalid.";
                Debug.LogError(message);
                _transitionView?.ShowError(message);
                return false;
            }

            StartCoroutine(TransitionRoutine(targetScene, request, fallbackToMenu));
            return true;
        }

        private IEnumerator TransitionRoutine(
            AssetReference targetScene,
            GameStartRequest request,
            bool fallbackToMenu)
        {
            IsTransitioning = true;
            _transitionView.ShowLoading("Loading...");

            // Give the persistent overlay one frame to render before scene work begins.
            yield return null;

            if (_hasCurrentScene)
            {
                var unloadSucceeded = false;
                Exception unloadException = null;
                var unloadHandle = Addressables.UnloadSceneAsync(_currentSceneHandle, true);
                unloadHandle.Completed += operation =>
                {
                    unloadSucceeded = operation.Status == AsyncOperationStatus.Succeeded;
                    unloadException = operation.OperationException;
                };

                yield return unloadHandle;

                if (!unloadSucceeded)
                {
                    var message = $"Could not unload the active Addressable scene: {unloadException?.Message}";
                    Debug.LogError(message);
                    _transitionView.ShowError(message);
                    IsTransitioning = false;
                    yield break;
                }

                _currentSceneHandle = default;
                _hasCurrentScene = false;
            }

            var loadSucceeded = false;
            Exception loadException = null;
            yield return LoadSceneRoutine(
                targetScene,
                request,
                success => loadSucceeded = success,
                exception => loadException = exception);

            if (!loadSucceeded && fallbackToMenu)
            {
                Debug.LogError(
                    $"GameScene failed to load. Returning to MenuScene. {loadException?.Message}");
                _transitionView.ShowLoading("Returning to menu...");

                yield return LoadSceneRoutine(
                    _catalog.MenuScene,
                    null,
                    success => loadSucceeded = success,
                    exception => loadException = exception);
            }

            if (loadSucceeded)
            {
                _transitionView.Hide();
            }
            else
            {
                var message = $"Unable to load the application scene: {loadException?.Message}";
                Debug.LogError(message);
                _transitionView.ShowError(message);
            }

            IsTransitioning = false;
        }

        private IEnumerator LoadSceneRoutine(
            AssetReference sceneReference,
            GameStartRequest request,
            Action<bool> setSuccess,
            Action<Exception> setException)
        {
            if (sceneReference == null || !sceneReference.RuntimeKeyIsValid())
            {
                setSuccess(false);
                setException(new InvalidOperationException("SceneCatalog contains an invalid scene reference."));
                yield break;
            }

            AsyncOperationHandle<SceneInstance> loadHandle;

            if (request == null)
            {
                using (LifetimeScope.EnqueueParent(_baseScope))
                {
                    loadHandle = Addressables.LoadSceneAsync(
                        sceneReference,
                        LoadSceneMode.Additive,
                        true);
                    yield return loadHandle;
                }
            }
            else
            {
                using (LifetimeScope.EnqueueParent(_baseScope))
                using (LifetimeScope.Enqueue(builder => builder.RegisterInstance(request)))
                {
                    loadHandle = Addressables.LoadSceneAsync(
                        sceneReference,
                        LoadSceneMode.Additive,
                        true);
                    yield return loadHandle;
                }
            }

            if (loadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                setSuccess(false);
                setException(loadHandle.OperationException);

                if (loadHandle.IsValid())
                {
                    Addressables.Release(loadHandle);
                }

                yield break;
            }

            _currentSceneHandle = loadHandle;
            _hasCurrentScene = true;
            SceneManager.SetActiveScene(loadHandle.Result.Scene);
            setSuccess(true);
            setException(null);
        }
    }
}
