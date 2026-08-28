using UnityEngine;

namespace StackAttack.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Forward Movement")]
        [SerializeField, Min(0f)] private float forwardSpeed = 5f;

        [Header("Swerve")]
        [SerializeField, Min(0f)] private float swerveSensitivity = 0.01f;
        [SerializeField, Min(0f)] private float maxSwerveSpeed = 10f;

        [Header("Viewport Constraint")]
        [SerializeField] private Collider2D horizontalBoundsCollider;
        [SerializeField, Range(0f, 0.49f)]
        private float horizontalViewportMargin = 0.02f;

        [Header("Swerve Tilt")]
        [SerializeField, Range(0f, 90f)] private float maxTiltDegrees = 60f;
        [SerializeField, Min(0f)] private float tiltSpeed = 360f;

        private Rigidbody2D _rigidbody;
        private Camera _mainCamera;
        private float _horizontalBoundsRadius;
        private float _pendingHorizontalTouchDelta;

        public bool IsInitialized { get; private set; }
        public float CurrentSwerveSpeed { get; private set; }
        public float CurrentTiltAngle { get; private set; }
        public float HorizontalViewportMargin => horizontalViewportMargin;

        public void Initialize()
        {
            CacheRigidbody();
            CacheViewportReferences();
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.gravityScale = 0f;
            _rigidbody.freezeRotation = false;
            _rigidbody.useFullKinematicContacts = true;
            CurrentTiltAngle = _rigidbody.rotation;
            IsInitialized = true;
        }

        public void Move(float horizontalTouchDelta)
        {
            if (!IsInitialized)
            {
                return;
            }

            _pendingHorizontalTouchDelta += horizontalTouchDelta;
        }

        private void FixedUpdate()
        {
            if (!IsInitialized)
            {
                return;
            }

            var fixedDeltaTime = Time.fixedDeltaTime;
            var swerveDistance = ConsumeSwerveDistance(fixedDeltaTime);
            var actualSwerveDistance = MovePlayer(
                swerveDistance,
                fixedDeltaTime);
            CurrentSwerveSpeed = fixedDeltaTime > 0f
                ? actualSwerveDistance / fixedDeltaTime
                : 0f;
            UpdateTilt(fixedDeltaTime);
        }

        private float ConsumeSwerveDistance(float fixedDeltaTime)
        {
            var requestedDistance =
                _pendingHorizontalTouchDelta * swerveSensitivity;
            var maxDistance = maxSwerveSpeed * fixedDeltaTime;
            var clampedDistance = Mathf.Clamp(
                requestedDistance,
                -maxDistance,
                maxDistance);

            _pendingHorizontalTouchDelta = 0f;
            return clampedDistance;
        }

        private float MovePlayer(float swerveDistance, float fixedDeltaTime)
        {
            var currentPosition = _rigidbody.position;
            var forwardMovement = Vector2.up * (forwardSpeed * fixedDeltaTime);
            var swerveMovement = Vector2.right * swerveDistance;
            var proposedPosition =
                currentPosition + forwardMovement + swerveMovement;
            var constrainedPosition = ConstrainToViewport(proposedPosition);

            _rigidbody.MovePosition(constrainedPosition);
            return constrainedPosition.x - currentPosition.x;
        }

        private Vector2 ConstrainToViewport(Vector2 proposedPosition)
        {
            var cameraTransform = _mainCamera.transform;
            var playerPosition = transform.position;
            var distanceFromCamera = Vector3.Dot(
                playerPosition - cameraTransform.position,
                cameraTransform.forward);
            var margin = Mathf.Clamp(horizontalViewportMargin, 0f, 0.49f);
            var leftPoint = _mainCamera.ViewportToWorldPoint(
                new Vector3(margin, 0.5f, distanceFromCamera));
            var rightPoint = _mainCamera.ViewportToWorldPoint(
                new Vector3(1f - margin, 0.5f, distanceFromCamera));
            var viewportLeft = Mathf.Min(leftPoint.x, rightPoint.x);
            var viewportRight = Mathf.Max(leftPoint.x, rightPoint.x);

            var colliderBounds = horizontalBoundsCollider.bounds;
            var bodyPositionX = _rigidbody.position.x;
            var minimumX = viewportLeft + _horizontalBoundsRadius;
            var maximumX = viewportRight - _horizontalBoundsRadius;

            if (minimumX <= maximumX)
            {
                proposedPosition.x = Mathf.Clamp(
                    proposedPosition.x,
                    minimumX,
                    maximumX);
                return proposedPosition;
            }

            var viewportCenter = (viewportLeft + viewportRight) * 0.5f;
            proposedPosition.x = bodyPositionX +
                                 (viewportCenter - colliderBounds.center.x);
            return proposedPosition;
        }

        private void UpdateTilt(float fixedDeltaTime)
        {
            var targetTiltAngle = CalculateTargetTiltAngle();
            CurrentTiltAngle = Mathf.MoveTowardsAngle(
                _rigidbody.rotation,
                targetTiltAngle,
                tiltSpeed * fixedDeltaTime);

            _rigidbody.MoveRotation(CurrentTiltAngle);
        }

        private float CalculateTargetTiltAngle()
        {
            if (maxSwerveSpeed <= 0f)
            {
                return 0f;
            }

            var normalizedSwerveSpeed = Mathf.Clamp(
                CurrentSwerveSpeed / maxSwerveSpeed,
                -1f,
                1f);

            // Positive X movement tilts clockwise, which is negative Z rotation.
            return -normalizedSwerveSpeed * maxTiltDegrees;
        }

        private void Awake()
        {
            CacheRigidbody();
        }

        private void OnValidate()
        {
            horizontalViewportMargin = Mathf.Clamp(
                horizontalViewportMargin,
                0f,
                0.49f);
        }

        private void CacheRigidbody()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }
        }

        private void CacheViewportReferences()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                throw new MissingReferenceException(
                    "PlayerMovement requires a camera tagged MainCamera.");
            }

            if (horizontalBoundsCollider != null &&
                horizontalBoundsCollider.enabled &&
                horizontalBoundsCollider.gameObject.activeInHierarchy)
            {
                CacheHorizontalBoundsRadius();
                return;
            }

            horizontalBoundsCollider = null;
            var colliders = GetComponentsInChildren<Collider2D>(true);
            for (var index = 0; index < colliders.Length; index++)
            {
                var candidate = colliders[index];
                if (candidate.enabled && candidate.gameObject.activeInHierarchy)
                {
                    horizontalBoundsCollider = candidate;
                    break;
                }
            }

            if (horizontalBoundsCollider == null)
            {
                throw new MissingReferenceException(
                    "PlayerMovement requires an enabled Collider2D for viewport bounds.");
            }

            CacheHorizontalBoundsRadius();
        }

        private void CacheHorizontalBoundsRadius()
        {
            var bounds = horizontalBoundsCollider.bounds;
            var bodyPosition = _rigidbody.position;
            var minimum = (Vector2)bounds.min;
            var maximum = (Vector2)bounds.max;
            _horizontalBoundsRadius = Mathf.Max(
                Vector2.Distance(bodyPosition, minimum),
                Vector2.Distance(bodyPosition, new Vector2(minimum.x, maximum.y)),
                Vector2.Distance(bodyPosition, new Vector2(maximum.x, minimum.y)),
                Vector2.Distance(bodyPosition, maximum));
        }
    }
}
