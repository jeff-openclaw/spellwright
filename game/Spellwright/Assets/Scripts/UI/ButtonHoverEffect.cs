using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spellwright.UI
{
    /// <summary>
    /// Enhanced button hover: scale + background color brighten + border glow.
    /// Creates a satisfying, tactile hover feel inspired by Balatro's button juice.
    /// </summary>
    public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.06f;
        [SerializeField] private float pressScale = 0.95f;
        [SerializeField] private float hoverDuration = 0.12f;
        [SerializeField] private float pressDuration = 0.06f;

        private Vector3 _originalScale;
        private Image _bg;
        private Color _originalBgColor;
        private Outline _outline;
        private Color _originalOutlineColor;
        private bool _isHovered;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _bg = GetComponent<Image>();
            if (_bg != null)
                _originalBgColor = _bg.color;
            _outline = GetComponent<Outline>();
            if (_outline != null)
                _originalOutlineColor = _outline.effectColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            DOTween.Kill(transform);
            transform.DOScale(_originalScale * hoverScale, hoverDuration).SetEase(Ease.OutBack).SetUpdate(true);

            // Brighten background on hover
            if (_bg != null)
            {
                DOTween.Kill(_bg);
                Color hoverColor = new Color(
                    Mathf.Min(_originalBgColor.r + 0.04f, 1f),
                    Mathf.Min(_originalBgColor.g + 0.12f, 1f),
                    Mathf.Min(_originalBgColor.b + 0.05f, 1f),
                    Mathf.Min(_originalBgColor.a + 0.05f, 1f));
                _bg.DOColor(hoverColor, hoverDuration).SetUpdate(true);
            }

            // Brighten border on hover
            if (_outline != null)
            {
                Color brightBorder = new Color(
                    Mathf.Min(_originalOutlineColor.r + 0.1f, 1f),
                    Mathf.Min(_originalOutlineColor.g + 0.3f, 1f),
                    Mathf.Min(_originalOutlineColor.b + 0.1f, 1f),
                    1f);
                _outline.effectColor = brightBorder;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            DOTween.Kill(transform);
            transform.DOScale(_originalScale, hoverDuration).SetEase(Ease.OutCubic).SetUpdate(true);

            if (_bg != null)
            {
                DOTween.Kill(_bg);
                _bg.DOColor(_originalBgColor, hoverDuration).SetUpdate(true);
            }

            if (_outline != null)
                _outline.effectColor = _originalOutlineColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            DOTween.Kill(transform);
            transform.DOScale(_originalScale * pressScale, pressDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            DOTween.Kill(transform);
            float targetScale = _isHovered ? hoverScale : 1f;
            transform.DOScale(_originalScale * targetScale, hoverDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void OnDisable()
        {
            DOTween.Kill(transform);
            transform.localScale = _originalScale;

            if (_bg != null)
            {
                DOTween.Kill(_bg);
                _bg.color = _originalBgColor;
            }

            if (_outline != null)
                _outline.effectColor = _originalOutlineColor;
        }
    }
}
