using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Spellwright.UI
{
    /// <summary>
    /// Slot-machine text spin effect for category reveal.
    /// Fast phase: cycles random values at 30ms. Decel phase: slows to 250ms.
    /// Landing: punch-scale + amber flash + settle to real value. Total ~2s.
    /// </summary>
    public class TextSpinner : MonoBehaviour
    {
        [SerializeField] private TerminalThemeSO theme;

        private TextMeshProUGUI _targetText;
        private Coroutine _spinCoroutine;

        private static readonly string[] DecoyCategories = new[]
        {
            "ANIMALS", "SCIENCE", "NATURE", "FOOD", "MYTHOLOGY",
            "TOOLS", "EMOTIONS", "EVERYDAY", "HISTORY", "GEOGRAPHY",
            "MUSIC", "SPORTS", "TECHNOLOGY", "ART", "LITERATURE"
        };

        private static readonly string[] SpinSymbols = new[]
        {
            "\u2588\u2588\u2588", "\u2591\u2591\u2591", "\u2592\u2592\u2592", "\u2593\u2593\u2593",
            ">>>", "<<<", "***", "###", "???", "!!!"
        };

        /// <summary>
        /// Spin the target text to reveal the actual value with a slot-machine effect.
        /// </summary>
        public void SpinToValue(TextMeshProUGUI text, string realValue, string prefix = "")
        {
            _targetText = text;
            if (_spinCoroutine != null)
                StopCoroutine(_spinCoroutine);
            _spinCoroutine = StartCoroutine(SpinRoutine(realValue, prefix));
        }

        private IEnumerator SpinRoutine(string realValue, string prefix)
        {
            if (_targetText == null) yield break;

            Color normalColor = theme != null ? theme.phosphorDim : new Color(0f, 0.5f, 0.18f);
            Color spinColor = theme != null ? theme.phosphorGreen : new Color(0f, 1f, 0.33f);
            Color flashColor = theme != null ? theme.amberBright : new Color(1f, 0.75f, 0f);

            _targetText.color = spinColor;

            // Phase 1: Fast spin (0.8s at ~30ms per tick)
            float fastDuration = 0.8f;
            float elapsed = 0f;
            float tickInterval = 0.03f;
            float tickTimer = 0f;

            while (elapsed < fastDuration)
            {
                tickTimer += Time.deltaTime;
                if (tickTimer >= tickInterval)
                {
                    tickTimer = 0f;
                    bool useSymbol = Random.value > 0.5f;
                    string display = useSymbol
                        ? SpinSymbols[Random.Range(0, SpinSymbols.Length)]
                        : DecoyCategories[Random.Range(0, DecoyCategories.Length)];
                    _targetText.text = $"{prefix}{display}";
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Phase 2: Deceleration (0.8s, slowing from 50ms to 250ms)
            elapsed = 0f;
            float decelDuration = 0.8f;
            while (elapsed < decelDuration)
            {
                float t = elapsed / decelDuration;
                tickInterval = Mathf.Lerp(0.05f, 0.25f, t * t);
                tickTimer += Time.deltaTime;
                if (tickTimer >= tickInterval)
                {
                    tickTimer = 0f;
                    // Increasingly show the real value
                    if (Random.value < t * 0.7f)
                        _targetText.text = $"{prefix}{realValue}";
                    else
                        _targetText.text = $"{prefix}{DecoyCategories[Random.Range(0, DecoyCategories.Length)]}";
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Phase 3: Landing
            _targetText.text = $"{prefix}{realValue}";
            _targetText.color = flashColor;

            var rt = _targetText.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.DOPunchScale(Vector3.one * 0.15f, 0.3f, 8, 0.5f).SetUpdate(true);
            }

            // Flash amber then settle
            yield return new WaitForSeconds(0.4f);
            _targetText.DOColor(normalColor, 0.3f).SetUpdate(true);

            _spinCoroutine = null;
        }

        private void OnDisable()
        {
            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
                _spinCoroutine = null;
            }
            if (_targetText != null)
                DOTween.Kill(_targetText);
        }
    }
}
