using UnityEngine;
using UnityEngine.UI;

namespace StackAttack.Systems
{
    public sealed class SceneTransitionView : MonoBehaviour
    {
        [SerializeField] private GameObject overlay;
        [SerializeField] private Text messageLabel;

        private Color _normalColor = Color.white;

        private void Awake()
        {
            if (messageLabel != null)
            {
                _normalColor = messageLabel.color;
            }

            Hide();
        }

        public void ShowLoading(string message)
        {
            if (messageLabel != null)
            {
                messageLabel.color = _normalColor;
                messageLabel.text = message;
            }

            if (overlay != null)
            {
                overlay.SetActive(true);
            }
        }

        public void ShowError(string message)
        {
            if (messageLabel != null)
            {
                messageLabel.color = new Color(1f, 0.55f, 0.55f);
                messageLabel.text = message;
            }

            if (overlay != null)
            {
                overlay.SetActive(true);
            }
        }

        public void Hide()
        {
            if (overlay != null)
            {
                overlay.SetActive(false);
            }
        }
    }
}
