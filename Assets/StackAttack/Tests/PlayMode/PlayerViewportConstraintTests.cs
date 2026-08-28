using System.Collections;
using NUnit.Framework;
using StackAttack.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace StackAttack.Tests
{
    public sealed class PlayerViewportConstraintTests
    {
        private Camera _previousMainCamera;
        private GameObject _cameraObject;
        private GameObject _playerObject;

        [SetUp]
        public void SetUp()
        {
            _previousMainCamera = Camera.main;
            if (_previousMainCamera != null)
            {
                _previousMainCamera.tag = "Untagged";
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerObject != null)
            {
                Object.DestroyImmediate(_playerObject);
            }

            if (_cameraObject != null)
            {
                Object.DestroyImmediate(_cameraObject);
            }

            if (_previousMainCamera != null)
            {
                _previousMainCamera.tag = "MainCamera";
            }
        }

        [UnityTest]
        public IEnumerator Movement_KeepsWholeColliderInsideBothViewportEdges()
        {
            CreateTestRig(16f / 9f);
            var movement = _playerObject.GetComponent<PlayerMovement>();
            var body = _playerObject.GetComponent<Rigidbody2D>();
            var boundsCollider = _playerObject.GetComponent<BoxCollider2D>();
            var initialY = body.position.y;

            movement.Move(100000f);
            yield return new WaitForFixedUpdate();
            var movingTilt = Mathf.Abs(Mathf.DeltaAngle(0f, body.rotation));
            Assert.That(movingTilt, Is.GreaterThan(0f));

            for (var index = 0; index < 60; index++)
            {
                movement.Move(100000f);
                yield return new WaitForFixedUpdate();
            }

            var rightLimit = ViewportWorldX(
                _cameraObject.GetComponent<Camera>(),
                1f - movement.HorizontalViewportMargin,
                _playerObject.transform.position);
            Assert.That(boundsCollider.bounds.max.x, Is.LessThanOrEqualTo(rightLimit + 0.01f));
            Assert.That(body.position.y, Is.GreaterThan(initialY));

            for (var index = 0; index < 20; index++)
            {
                movement.Move(100000f);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(Mathf.Abs(movement.CurrentSwerveSpeed), Is.LessThan(0.01f));
            Assert.That(
                Mathf.Abs(Mathf.DeltaAngle(0f, body.rotation)),
                Is.LessThan(movingTilt));

            var rightEdgePosition = body.position.x;
            movement.Move(-100000f);
            yield return new WaitForFixedUpdate();

            Assert.That(body.position.x, Is.LessThan(rightEdgePosition));
            Assert.That(movement.CurrentSwerveSpeed, Is.LessThan(0f));

            for (var index = 0; index < 100; index++)
            {
                movement.Move(-100000f);
                yield return new WaitForFixedUpdate();
            }

            var leftLimit = ViewportWorldX(
                _cameraObject.GetComponent<Camera>(),
                movement.HorizontalViewportMargin,
                _playerObject.transform.position);
            Assert.That(boundsCollider.bounds.min.x, Is.GreaterThanOrEqualTo(leftLimit - 0.01f));
        }

        [UnityTest]
        public IEnumerator AspectChange_ReclampsAndCentersWhenViewportIsTooNarrow()
        {
            CreateTestRig(16f / 9f);
            var camera = _cameraObject.GetComponent<Camera>();
            var movement = _playerObject.GetComponent<PlayerMovement>();
            var body = _playerObject.GetComponent<Rigidbody2D>();
            var boundsCollider = _playerObject.GetComponent<BoxCollider2D>();

            for (var index = 0; index < 60; index++)
            {
                movement.Move(100000f);
                yield return new WaitForFixedUpdate();
            }

            var widePositionX = body.position.x;
            camera.aspect = 0.5f;
            yield return new WaitForFixedUpdate();

            var narrowRightLimit = ViewportWorldX(
                camera,
                1f - movement.HorizontalViewportMargin,
                _playerObject.transform.position);
            Assert.That(body.position.x, Is.LessThan(widePositionX));
            Assert.That(
                boundsCollider.bounds.max.x,
                Is.LessThanOrEqualTo(narrowRightLimit + 0.01f));

            camera.aspect = 0.1f;
            yield return new WaitForFixedUpdate();

            var viewportCenter = ViewportWorldX(
                camera,
                0.5f,
                _playerObject.transform.position);
            Assert.That(
                boundsCollider.bounds.center.x,
                Is.EqualTo(viewportCenter).Within(0.01f));
        }

        private void CreateTestRig(float aspect)
        {
            _cameraObject = new GameObject(
                "Viewport Test Camera",
                typeof(Camera));
            _cameraObject.tag = "MainCamera";
            _cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = _cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.aspect = aspect;

            _playerObject = new GameObject(
                "Viewport Test Player",
                typeof(Rigidbody2D),
                typeof(BoxCollider2D),
                typeof(PlayerMovement));
            var body = _playerObject.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            var boundsCollider = _playerObject.GetComponent<BoxCollider2D>();
            boundsCollider.size = new Vector2(2f, 1f);
            boundsCollider.offset = new Vector2(0.25f, 0f);
            _playerObject.GetComponent<PlayerMovement>().Initialize();
        }

        private static float ViewportWorldX(
            Camera camera,
            float viewportX,
            Vector3 playerPosition)
        {
            var cameraTransform = camera.transform;
            var distance = Vector3.Dot(
                playerPosition - cameraTransform.position,
                cameraTransform.forward);
            return camera.ViewportToWorldPoint(
                new Vector3(viewportX, 0.5f, distance)).x;
        }
    }
}
