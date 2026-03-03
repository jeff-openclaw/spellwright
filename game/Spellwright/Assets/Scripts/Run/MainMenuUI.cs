using System.Collections;
using DG.Tweening;
using Spellwright.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.Run
{
    /// <summary>
    /// Main menu with staggered entrance animations, decorative title with glow,
    /// typewriter subtitle, blinking cursor, and version text.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Button startButton;
        [SerializeField] private TextMeshProUGUI cursorBlink;
        [SerializeField] private TextMeshProUGUI versionText;

        [Header("Theme")]
        [SerializeField] private TerminalThemeSO theme;

        [Header("Entrance Animation")]
        [SerializeField] private float titleDelay = 0.2f;
        [SerializeField] private float subtitleDelay = 0.6f;
        [SerializeField] private float buttonDelay = 1.0f;
        [SerializeField] private float versionDelay = 1.2f;
        [SerializeField] private float typewriterSpeed = 0.04f;

        private Coroutine _blinkCoroutine;
        private Coroutine _typewriterCoroutine;

        private void OnEnable()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);

            PlayEntranceSequence();
        }

        private void OnDisable()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);

            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
                _blinkCoroutine = null;
            }

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            // Kill all tweens
            if (titleText != null) DOTween.Kill(titleText);
            if (subtitleText != null) DOTween.Kill(subtitleText);
            if (versionText != null) DOTween.Kill(versionText);
            if (startButton != null) DOTween.Kill(startButton.GetComponent<RectTransform>());
        }

        private void PlayEntranceSequence()
        {
            // Title: fade in + scale from slightly larger
            if (titleText != null)
            {
                titleText.text = "SPELLWRIGHT";
                titleText.alpha = 0f;
                var titleRT = titleText.GetComponent<RectTransform>();
                if (titleRT != null)
                {
                    titleRT.localScale = Vector3.one * 1.1f;
                    DOTween.Sequence()
                        .SetDelay(titleDelay)
                        .Append(titleText.DOFade(1f, 0.6f).SetEase(Ease.OutCubic))
                        .Join(titleRT.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutBack))
                        .SetUpdate(true);
                }
            }

            // Subtitle: typewriter reveal
            if (subtitleText != null)
            {
                subtitleText.text = "";
                _typewriterCoroutine = StartCoroutine(TypewriterSubtitle(subtitleDelay));
            }

            // Start button: scale in from zero with bounce
            if (startButton != null)
            {
                var btnRT = startButton.GetComponent<RectTransform>();
                if (btnRT != null)
                {
                    btnRT.localScale = Vector3.zero;
                    btnRT.DOScale(Vector3.one, 0.4f)
                        .SetDelay(buttonDelay)
                        .SetEase(Ease.OutBack)
                        .SetUpdate(true);
                }
            }

            // Version text: fade in
            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
                versionText.alpha = 0f;
                versionText.DOFade(0.5f, 0.4f)
                    .SetDelay(versionDelay)
                    .SetUpdate(true);
            }

            // Cursor blink starts after subtitle
            if (cursorBlink != null)
            {
                cursorBlink.enabled = false;
                _blinkCoroutine = StartCoroutine(DelayedBlinkStart(subtitleDelay + 1.2f));
            }
        }

        private IEnumerator TypewriterSubtitle(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            string fullText = "A Word-Guessing Roguelike";
            // Use a brighter color for readability
            Color subtitleColor = theme != null
                ? Color.Lerp(theme.phosphorDim, theme.phosphorGreen, 0.85f)
                : new Color(0.10f, 0.90f, 0.40f);
            if (subtitleText != null)
                subtitleText.color = subtitleColor;

            for (int i = 0; i < fullText.Length; i++)
            {
                if (subtitleText != null)
                    subtitleText.text += fullText[i];
                yield return new WaitForSecondsRealtime(typewriterSpeed);
            }

            _typewriterCoroutine = null;
        }

        private IEnumerator DelayedBlinkStart(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            while (true)
            {
                if (cursorBlink != null)
                    cursorBlink.enabled = !cursorBlink.enabled;
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        private IEnumerator BlinkCursor()
        {
            while (true)
            {
                if (cursorBlink != null)
                    cursorBlink.enabled = !cursorBlink.enabled;
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void OnStartClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewRun();
        }
    }
}
