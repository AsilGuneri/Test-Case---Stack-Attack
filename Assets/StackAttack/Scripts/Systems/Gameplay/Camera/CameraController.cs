using System;
using UnityEngine;
using VContainer.Unity;

namespace StackAttack.Gameplay
{
    public sealed class CameraController : IInitializable, ILateTickable
    {
        private readonly PlayerController _player;

        private Camera _mainCamera;
        private Transform _cameraTransform;
        private Transform _followTarget;
        private Vector3 _initialCameraPosition;
        private Vector3 _initialTargetPosition;
        private Vector3 _forwardAxis;
        private Quaternion _initialRotation;
        private bool _isInitialized;

        public CameraController(PlayerController player)
        {
            _player = player;
        }

        public void Initialize()
        {
            if (_player == null)
            {
                throw new ArgumentNullException(
                    nameof(_player),
                    "CameraController requires a PlayerController.");
            }

            _followTarget = _player.transform;
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                throw new InvalidOperationException(
                    "CameraController could not find a camera tagged MainCamera.");
            }

            _cameraTransform = _mainCamera.transform;
            _initialCameraPosition = _cameraTransform.position;
            _initialTargetPosition = _followTarget.position;
            _forwardAxis = Vector3.up;
            _initialRotation = _cameraTransform.rotation;
            _isInitialized = true;
        }

        public void LateTick()
        {
            if (!_isInitialized)
            {
                return;
            }

            var targetDisplacement = _followTarget.position - _initialTargetPosition;
            var forwardDistance = Vector3.Dot(targetDisplacement, _forwardAxis);
            var cameraPosition = _initialCameraPosition +
                                 (_forwardAxis * forwardDistance);

            _cameraTransform.SetPositionAndRotation(cameraPosition, _initialRotation);
        }
    }
}
