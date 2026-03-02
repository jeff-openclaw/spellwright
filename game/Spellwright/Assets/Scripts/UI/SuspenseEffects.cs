using System;
using System.Collections;
using DG.Tweening;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.UI
{
    /// <summary>
    /// Suspense effects system: clue wait pulse, HP heartbeat, dramatic result reveal,
    /// guess countdown warning, and boss tension CRT ramp.
    /// </summary>
    public class SuspenseEffects : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI clueText;
        [SerializeField] private Image hpBarFill;
        [SerializeField] private TextMeshProUGUI guessesText;
        [SerializeField] private Image flashOverlay;
        [SerializeField] private GameObject resultPanel;

        [Header("Theme")]
        [SerializeField] private TerminalThemeSO theme;

        // State
        private Tween _cluePulseTween;
        private Tween _heartbeatTween;
        private Tween _guessCountdownTween;
        private Coroutine _dramaCoroutine;
        private bool _isBossEncounter;
        private int _bossWrongGuesses;

        // CRT baseline values (saved on boss start)
        private float _baseScanlineIntensity;
        private float _baseChromaticAberration;

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Subscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Subscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Subscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Subscribe<HPChangedEvent>(OnHPChanged);
            EventBus.Instance.Subscribe<BossIntroEvent>(OnBossIntro);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Unsubscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Unsubscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Unsubscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Unsubscribe<HPChangedEvent>(OnHPChanged);
            EventBus.Instance.Unsubscribe<BossIntroEvent>(OnBossIntro);

            KillAllTweens();
        }

        // ── Clue Wait Pulse ─────────────────────────────────

        /// <summary>
        /// Start pulsing the clue text alpha while waiting for LLM response.
        /// </summary>
        public void StartCluePulse()
        {
            StopCluePulse();
            if (clueText == null) return;

            float speed = theme != null ? theme.cluePulseSpeed : 2f;
            _cluePulseTween = clueText.DOFade(0.3f, 0.5f / speed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        public void StopCluePulse()
        {
            _cluePulseTween?.Kill();
            _cluePulseTween = null;
            if (clueText != null)
                clueText.alpha = 1f;
        }

        // ── Heartbeat ───────────────────────────────────────

        private void StartHeartbeat()
        {
            if (_heartbeatTween != null) return;
            if (hpBarFill == null) return;

            float scale = theme != null ? theme.heartbeatScale : 1.06f;
            var rt = hpBarFill.GetComponent<RectTransform>();
            if (rt == null) return;

            _heartbeatTween = rt.DOScale(scale, 0.3f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        private void StopHeartbeat()
        {
            _heartbeatTween?.Kill();
            _heartbeatTween = null;
            if (hpBarFill != null)
            {
                var rt = hpBarFill.GetComponent<RectTransform>();
                if (rt != null) rt.localScale = Vector3.one;
            }
        }

        // ── Guess Countdown Warning ─────────────────────────

        private void StartGuessCountdown()
        {
            if (_guessCountdownTween != null) return;
            if (guessesText == null) return;

            Color warningColor = theme != null ? theme.damageColor : new Color(1f, 0.15f, 0.1f);
            Color normalColor = theme != null ? theme.phosphorGreen : new Color(0f, 1f, 0.33f);

            _guessCountdownTween = guessesText.DOColor(warningColor, 0.4f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        private void StopGuessCountdown()
        {
            _guessCountdownTween?.Kill();
            _guessCountdownTween = null;
            if (guessesText != null)
            {
                Color normalColor = theme != null ? theme.phosphorGreen : new Color(0f, 1f, 0.33f);
                guessesText.color = normalColor;
            }
        }

        // ── Dramatic Result ─────────────────────────────────

        /// <summary>
        /// Plays the drama sequence before showing the result panel.
        /// Screen dims -> hold -> flash -> callback to show result.
        /// </summary>
        public void PlayDramaticResult(Action onComplete)
        {
            if (_dramaCoroutine != null)
                StopCoroutine(_dramaCoroutine);
            _dramaCoroutine = StartCoroutine(DramaSequence(onComplete));
        }

        private IEnumerator DramaSequence(Action onComplete)
        {
            float duration = theme != null ? theme.dramaDuration : 1.2f;

            if (flashOverlay != null)
            {
                // Phase 1: Dim screen
                flashOverlay.raycastTarget = true;
                Color dimColor = new Color(0f, 0f, 0f, 0.6f);
                float dimTime = duration * 0.35f;
                float elapsed = 0f;
                while (elapsed < dimTime)
                {
                    float t = elapsed / dimTime;
                    flashOverlay.color = Color.Lerp(new Color(0, 0, 0, 0), dimColor, t);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                flashOverlay.color = dimColor;

                // Phase 2: Hold
                yield return new WaitForSeconds(duration * 0.3f);

                // Phase 3: Bright flash
                Color flashColor = theme != null
                    ? new Color(theme.phosphorGreen.r, theme.phosphorGreen.g, theme.phosphorGreen.b, 0.5f)
                    : new Color(0f, 1f, 0.33f, 0.5f);
                flashOverlay.color = flashColor;

                // Phase 4: Fade out
                float fadeTime = duration * 0.35f;
                elapsed = 0f;
                while (elapsed < fadeTime)
                {
                    float t = elapsed / fadeTime;
                    flashOverlay.color = Color.Lerp(flashColor, new Color(0, 0, 0, 0), t);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                flashOverlay.color = new Color(0, 0, 0, 0);
                flashOverlay.raycastTarget = false;
            }

            onComplete?.Invoke();
            _dramaCoroutine = null;
        }

        // ── Boss Tension Ramp ───────────────────────────────

        private void StartBossTension()
        {
            _isBossEncounter = true;
            _bossWrongGuesses = 0;

            if (CRTSettings.Instance != null)
            {
                _baseScanlineIntensity = CRTSettings.Instance.scanlineIntensity;
                _baseChromaticAberration = CRTSettings.Instance.chromaticAberration;
            }
        }

        private void RampBossTension()
        {
            if (!_isBossEncounter || CRTSettings.Instance == null) return;

            _bossWrongGuesses++;
            float ramp = Mathf.Min(_bossWrongGuesses * 0.03f, 0.2f);
            CRTSettings.Instance.scanlineIntensity = _baseScanlineIntensity + ramp;
            CRTSettings.Instance.chromaticAberration = Mathf.Min(
                _baseChromaticAberration + _bossWrongGuesses * 0.001f, 0.01f);
        }

        private void ResetBossTension()
        {
            if (CRTSettings.Instance != null)
            {
                CRTSettings.Instance.scanlineIntensity = _baseScanlineIntensity;
                CRTSettings.Instance.chromaticAberration = _baseChromaticAberration;
            }
            _isBossEncounter = false;
            _bossWrongGuesses = 0;
        }

        // ── Event Handlers ──────────────────────────────────

        private void OnEncounterStarted(EncounterStartedEvent evt)
        {
            KillAllTweens();
            StartCluePulse();
        }

        private void OnClueReceived(ClueReceivedEvent evt)
        {
            StopCluePulse();
        }

        private void OnGuessSubmitted(GuessSubmittedEvent evt)
        {
            // Start clue pulse again (next clue might come)
            StartCluePulse();

            // Check if guess was wrong for boss tension
            bool isWrong = !evt.Result.IsCorrect &&
                (evt.Result.GuessType == GuessType.Phrase ||
                 (evt.Result.GuessType == GuessType.Letter && !evt.Result.IsLetterInPhrase && !evt.Result.IsLetterAlreadyGuessed));

            if (isWrong && _isBossEncounter)
                RampBossTension();

            // Parse remaining guesses from guessesText to check threshold
            UpdateGuessCountdown();
        }

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            StopCluePulse();
            StopHeartbeat();
            StopGuessCountdown();
            if (_isBossEncounter)
                ResetBossTension();
        }

        private void OnHPChanged(HPChangedEvent evt)
        {
            float threshold = theme != null ? theme.heartbeatThreshold : 0.25f;
            float hpPercent = evt.MaxHP > 0 ? (float)evt.NewHP / evt.MaxHP : 1f;

            if (hpPercent <= threshold)
                StartHeartbeat();
            else
                StopHeartbeat();
        }

        private void OnBossIntro(BossIntroEvent evt)
        {
            StartBossTension();
        }

        private void UpdateGuessCountdown()
        {
            if (guessesText == null) return;

            // Parse the guesses text: "Guesses: N"
            string text = guessesText.text;
            int remaining = -1;
            if (text.StartsWith("Guesses: ") && int.TryParse(text.Substring(9), out int val))
                remaining = val;

            float threshold = theme != null ? theme.guessCountdownThreshold : 2;
            if (remaining >= 0 && remaining <= threshold)
                StartGuessCountdown();
            else
                StopGuessCountdown();
        }

        private void KillAllTweens()
        {
            StopCluePulse();
            StopHeartbeat();
            StopGuessCountdown();
            if (_dramaCoroutine != null)
            {
                StopCoroutine(_dramaCoroutine);
                _dramaCoroutine = null;
            }
        }
    }
}
