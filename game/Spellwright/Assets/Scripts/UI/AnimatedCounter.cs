using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Spellwright.UI
{
    /// <summary>
    /// DOTween integer counter for Gold/Score display.
    /// Counts up/down to target over 0.5s with color flash on completion.
    /// </summary>
    public class AnimatedCounter : MonoBehaviour
    {
        [SerializeField] private TerminalThemeSO theme;

        private Tween _counterTween;

        /// <summary>
        /// Animate a text from current displayed integer to target value.
        /// </summary>
        /// <param name="text">The TMP text to animate.</param>
        /// <param name="fromValue">Starting integer value.</param>
        /// <param name="toValue">Target integer value.</param>
        /// <param name="format">Format string, e.g. "{0}g" or "Score: {0}".</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="flashColor">Optional color flash on completion.</param>
        public void AnimateToValue(TextMeshProUGUI text, int fromValue, int toValue,
            string format = "{0}", float duration = 0.5f, Color? flashColor = null)
        {
            if (text == null) return;

            _counterTween?.Kill();

            Color originalColor = text.color;
            int current = fromValue;

            _counterTween = DOTween.To(() => current, x =>
            {
                current = x;
                text.text = string.Format(format, current);
            }, toValue, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                text.text = string.Format(format, toValue);

                if (flashColor.HasValue)
                {
                    text.color = flashColor.Value;
                    text.DOColor(originalColor, 0.3f).SetUpdate(true);
                }
            });
        }

        private void OnDisable()
        {
            _counterTween?.Kill();
            _counterTween = null;
        }
    }
}
