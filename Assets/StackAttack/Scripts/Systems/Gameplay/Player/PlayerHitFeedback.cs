using DG.Tweening;
using UnityEngine;

namespace StackAttack.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerHitFeedback : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer playerVisual;
        [SerializeField, Range(0f, 1f)] private float blinkAlpha = 0.2f;
        [SerializeField, Min(1)] private int blinkCount = 7;

        private PlayerHealth _health;
        private Sequence _blinkSequence;
        private Color _restingColor;

        public SpriteRenderer PlayerVisual => playerVisual;
        public float BlinkAlpha => blinkAlpha;
        public int BlinkCount => blinkCount;
        public bool IsBlinking => _blinkSequence?.IsActive() == true;
        public float CurrentBlinkDuration => IsBlinking
            ? _blinkSequence.Duration(true)
            : 0f;

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
            CachePlayerVisual();
            if (playerVisual == null)
            {
                throw new MissingReferenceException(
                    "PlayerHitFeedback requires the player's SpriteRenderer.");
            }

            _restingColor = playerVisual.color;
        }

        private void OnEnable()
        {
            _health.InvulnerabilityChanged += HandleInvulnerabilityChanged;
        }

        private void OnDisable()
        {
            _health.InvulnerabilityChanged -= HandleInvulnerabilityChanged;
            StopBlinking();
        }

        private void OnValidate()
        {
            blinkAlpha = Mathf.Clamp01(blinkAlpha);
            blinkCount = Mathf.Max(1, blinkCount);
            CachePlayerVisual();
        }

        private void HandleInvulnerabilityChanged(bool isInvulnerable)
        {
            if (isInvulnerable)
            {
                StartBlinking();
            }
            else
            {
                StopBlinking();
            }
        }

        private void StartBlinking()
        {
            StopBlinking();
            _restingColor = playerVisual.color;

            var duration = _health.InvulnerabilityDuration;
            if (duration <= 0f)
            {
                return;
            }

            var fadeDuration = duration / (blinkCount * 2f);
            _blinkSequence = DOTween.Sequence()
                .Append(playerVisual
                    .DOFade(blinkAlpha, fadeDuration)
                    .SetEase(Ease.Linear))
                .Append(playerVisual
                    .DOFade(_restingColor.a, fadeDuration)
                    .SetEase(Ease.Linear))
                .SetLoops(blinkCount)
                .SetTarget(this)
                .OnComplete(RestoreVisual);
        }

        private void StopBlinking()
        {
            if (_blinkSequence != null)
            {
                _blinkSequence.Kill(false);
                _blinkSequence = null;
            }

            RestoreVisual();
        }

        private void RestoreVisual()
        {
            if (playerVisual != null)
            {
                playerVisual.color = _restingColor;
            }
        }

        private void CachePlayerVisual()
        {
            if (playerVisual == null)
            {
                playerVisual = GetComponentInChildren<SpriteRenderer>(true);
            }
        }
    }
}
