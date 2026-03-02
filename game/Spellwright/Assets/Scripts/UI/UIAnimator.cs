using DG.Tweening;
using UnityEngine;

namespace Spellwright.UI
{
    /// <summary>
    /// Panel entrance animation: fade + slide up via CanvasGroup + DOTween.
    /// Attach to panel root GameObjects.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIAnimator : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float slideDistance = 40f;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Vector2 _restPosition;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _restPosition = _rectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            PlayEntrance();
        }

        public void PlayEntrance()
        {
            if (_canvasGroup == null || _rectTransform == null) return;

            DOTween.Kill(_canvasGroup);
            DOTween.Kill(_rectTransform);

            _canvasGroup.alpha = 0f;
            _rectTransform.anchoredPosition = _restPosition + Vector2.down * slideDistance;

            _canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutCubic).SetUpdate(true);
            _rectTransform.DOAnchorPos(_restPosition, fadeInDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private void OnDisable()
        {
            DOTween.Kill(_canvasGroup);
            DOTween.Kill(_rectTransform);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
            if (_rectTransform != null)
                _rectTransform.anchoredPosition = _restPosition;
        }
    }
}
