using System.Collections;
using DG.Tweening;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Run;
using Spellwright.Tomes;
using Spellwright.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Production encounter UI with juice effects: typewriter, screen shake,
    /// tile board, guessed letters tracker, letter reveal animation, HP bar animation.
    /// </summary>
    public class EncounterUI : MonoBehaviour
    {
        [Header("NPC Info")]
        [SerializeField] private TextMeshProUGUI npcNameText;
        [SerializeField] private TextMeshProUGUI npcArchetypeText;
        [SerializeField] private NPCPortraitUI npcPortraitUI;

        [Header("Tile Board")]
        [SerializeField] private TileBoardUI tileBoardUI;
        [SerializeField] private GuessedLettersUI guessedLettersUI;
        [SerializeField] private TextMeshProUGUI categoryText;

        [Header("Clue Display")]
        [SerializeField] private TextMeshProUGUI clueText;
        [SerializeField] private TextMeshProUGUI clueNumberText;

        [Header("Input")]
        [SerializeField] private TMP_InputField guessInput;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button solveButton;
        [SerializeField] private TextMeshProUGUI inputModeText;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Image hpBarFill;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI guessesText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Guess History")]
        [SerializeField] private TextMeshProUGUI historyText;

        [Header("Tome Info")]
        [SerializeField] private TextMeshProUGUI tomeInfoText;

        [Header("Result Overlay")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultBannerText;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultDetailsText;
        [SerializeField] private Button continueButton;

        [Header("Flash Overlay")]
        [SerializeField] private Image flashOverlay;

        [Header("Decorative")]
        [SerializeField] private TextMeshProUGUI terminalPrompt;

        [Header("Suspense")]
        [SerializeField] private SuspenseEffects suspenseEffects;

        [Header("Spinners & Counters")]
        [SerializeField] private TextSpinner textSpinner;
        [SerializeField] private AnimatedCounter animatedCounter;

        [Header("Theme")]
        [SerializeField] private TerminalThemeSO theme;

        [Header("Juice Settings")]
        [SerializeField] private float typewriterSpeed = 0.03f;
        [SerializeField] private float shakeIntensity = 8f;
        [SerializeField] private float shakeDuration = 0.3f;
        [SerializeField] private float hpBarLerpSpeed = 5f;
        [SerializeField] private float flashDuration = 0.2f;

        // State
        private EncounterManager _encounter;
        private bool _isProcessing;
        private bool _isBossEncounter;
        private bool _isSolveMode;
        private float _targetHPFill;
        private float _currentHPFill;
        private int _lastDisplayedGold;
        private int _lastDisplayedScore;
        private Coroutine _typewriterCoroutine;
        private Coroutine _shakeCoroutine;
        private Coroutine _flashCoroutine;
        private RectTransform _shakeTarget;
        private Vector2 _shakeOriginalPos;

        private void Awake()
        {
            _encounter = FindAnyObjectByType<EncounterManager>();

            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmitClicked);
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
            if (solveButton != null)
                solveButton.onClick.AddListener(OnSolveToggle);

            _shakeTarget = GetComponent<RectTransform>();
            if (_shakeTarget != null)
                _shakeOriginalPos = _shakeTarget.anchoredPosition;

            ClearDisplay();
            if (flashOverlay != null)
            {
                flashOverlay.color = new Color(0, 0, 0, 0);
                flashOverlay.raycastTarget = false;
            }
            if (resultPanel != null)
                resultPanel.SetActive(false);

            if (guessInput != null)
                guessInput.onSubmit.AddListener(OnInputSubmit);

            SetLetterMode();
        }

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Subscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Subscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Subscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Subscribe<HPChangedEvent>(OnHPChanged);
            EventBus.Instance.Subscribe<TomeTriggeredEvent>(OnTomeTriggered);
            EventBus.Instance.Subscribe<BossIntroEvent>(OnBossIntro);
            EventBus.Instance.Subscribe<LetterRevealedEvent>(OnLetterRevealed);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Unsubscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Unsubscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Unsubscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Unsubscribe<HPChangedEvent>(OnHPChanged);
            EventBus.Instance.Unsubscribe<TomeTriggeredEvent>(OnTomeTriggered);
            EventBus.Instance.Unsubscribe<BossIntroEvent>(OnBossIntro);
            EventBus.Instance.Unsubscribe<LetterRevealedEvent>(OnLetterRevealed);
        }

        private void Update()
        {
            // Smooth HP bar animation
            if (hpBarFill != null && Mathf.Abs(_currentHPFill - _targetHPFill) > 0.001f)
            {
                _currentHPFill = Mathf.Lerp(_currentHPFill, _targetHPFill, Time.deltaTime * hpBarLerpSpeed);
                hpBarFill.fillAmount = _currentHPFill;
                hpBarFill.color = theme != null
                    ? theme.GetHPBarColor(_currentHPFill)
                    : new Color(0f, 0.8f, 0.25f);
            }

            // Focus input field when encounter is active
            if (_encounter != null && _encounter.IsActive && guessInput != null
                && !_isProcessing && !guessInput.isFocused
                && (resultPanel == null || !resultPanel.activeSelf))
            {
                guessInput.ActivateInputField();
            }

            // Update mode indicator based on current input
            if (guessInput != null && inputModeText != null)
            {
                bool wouldBeLetter = guessInput.text.Length <= 1;
                inputModeText.text = wouldBeLetter ? "LETTER MODE" : "SOLVE MODE";
            }
        }

        // ── Input Mode ──────────────────────────────────────

        private void SetLetterMode()
        {
            _isSolveMode = false;
            if (guessInput != null)
            {
                guessInput.characterLimit = 0; // No limit — GuessProcessor determines type
                var placeholder = guessInput.placeholder as TextMeshProUGUI;
                if (placeholder != null)
                    placeholder.text = "Guess a letter or solve...";
            }
            if (inputModeText != null)
                inputModeText.text = "LETTER MODE";
            if (solveButton != null)
            {
                var label = solveButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = "SOLVE";
            }
        }

        private void SetSolveMode()
        {
            _isSolveMode = true;
            if (guessInput != null)
            {
                guessInput.characterLimit = 50;
                var placeholder = guessInput.placeholder as TextMeshProUGUI;
                if (placeholder != null)
                    placeholder.text = "Solve the phrase...";
            }
            if (inputModeText != null)
                inputModeText.text = "SOLVE MODE";
            if (solveButton != null)
            {
                var label = solveButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = "LETTER";
            }
        }

        private void OnSolveToggle()
        {
            if (_isSolveMode)
                SetLetterMode();
            else
                SetSolveMode();

            if (guessInput != null)
            {
                guessInput.text = "";
                guessInput.ActivateInputField();
            }
        }

        // ── Event Handlers ───────────────────────────────────

        private void OnEncounterStarted(EncounterStartedEvent evt)
        {
            if (resultPanel != null)
                resultPanel.SetActive(false);

            // NPC info
            if (npcNameText != null)
                npcNameText.text = evt.NPC.DisplayName;
            if (npcArchetypeText != null)
                npcArchetypeText.text = evt.NPC.Archetype.ToString();

            // Portrait
            if (npcPortraitUI != null)
                npcPortraitUI.SetNPC(evt.NPC.Archetype, evt.NPC.IsBoss);

            // Category (with spinner)
            if (categoryText != null)
            {
                if (textSpinner != null)
                    textSpinner.SpinToValue(categoryText, evt.Category, "\u2500\u2500 Category: ");
                else
                    categoryText.text = $"Category: {evt.Category}";
            }

            // Initialize tile board
            if (tileBoardUI != null && _encounter?.Board != null)
                tileBoardUI.InitializeBoard(_encounter.Board);

            // Reset guessed letters
            if (guessedLettersUI != null)
                guessedLettersUI.Reset();

            // Reset clue
            if (clueText != null) clueText.text = "...";
            if (clueNumberText != null) clueNumberText.text = "";

            // Clear history
            if (historyText != null) historyText.text = "";

            _currentHPFill = 0f;
            _lastDisplayedGold = RunManager.Instance != null ? RunManager.Instance.Gold : 0;
            _lastDisplayedScore = RunManager.Instance != null ? RunManager.Instance.Score : 0;
            UpdateStatus();
            UpdateTomeInfo();

            // Reset to letter mode
            SetLetterMode();

            // Enable input
            if (submitButton != null) submitButton.interactable = true;
            if (guessInput != null)
            {
                guessInput.text = "";
                guessInput.interactable = true;
                guessInput.ActivateInputField();
            }
        }

        private void OnClueReceived(ClueReceivedEvent evt)
        {
            if (clueNumberText != null)
            {
                string fallback = evt.Clue.UsedFallbackModel ? " *" : "";
                clueNumberText.text = $"Clue #{evt.ClueNumber}{fallback}";
            }

            if (clueText != null)
            {
                if (_typewriterCoroutine != null)
                    StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = StartCoroutine(TypewriterReveal(clueText, evt.Clue.Clue));
            }
        }

        private void OnGuessSubmitted(GuessSubmittedEvent evt)
        {
            // Add to history
            if (historyText != null)
            {
                string icon;
                string entry;

                if (evt.Result.GuessType == GuessType.Letter)
                {
                    icon = evt.Result.IsLetterInPhrase ? ">>>" : " X ";
                    entry = $"[{icon}] {evt.Result.GuessedLetter.ToString().ToUpperInvariant()}";
                    if (evt.Result.IsLetterInPhrase)
                        entry += $"  (x{evt.Result.LetterOccurrences})";
                    else if (evt.Result.IsLetterAlreadyGuessed)
                        entry += "  (already guessed)";
                }
                else
                {
                    icon = evt.Result.IsCorrect ? ">>>" : evt.Result.IsValidWord ? " X " : " ? ";
                    entry = $"[{icon}] {evt.Guess.ToUpperInvariant()}";
                    if (evt.Result.IsValidWord && !evt.Result.IsCorrect)
                        entry += $"  ({evt.Result.LettersCorrect} correct)";
                    else if (!evt.Result.IsValidWord)
                        entry += "  (invalid)";
                }

                if (historyText.text.Length > 0)
                    historyText.text += "\n";
                historyText.text += entry;
            }

            // Update guessed letters display for letter guesses
            if (evt.Result.GuessType == GuessType.Letter && guessedLettersUI != null)
            {
                guessedLettersUI.MarkLetterGuessed(evt.Result.GuessedLetter, evt.Result.IsLetterInPhrase);
            }

            // Juice effects
            Color damageFlash = theme != null ? theme.damageFlash : new Color(0.8f, 0.1f, 0.1f, 0.4f);
            Color successFlash = theme != null ? theme.successFlash : new Color(0.1f, 0.8f, 0.2f, 0.4f);

            if (evt.Result.IsCorrect || (evt.Result.GuessType == GuessType.Letter && evt.Result.IsLetterInPhrase))
            {
                Flash(successFlash);
            }
            else if (evt.Result.IsValidWord || (evt.Result.GuessType == GuessType.Letter && !evt.Result.IsLetterAlreadyGuessed))
            {
                Flash(damageFlash);
                ScreenShake();
            }

            UpdateStatus();
        }

        private void OnLetterRevealed(LetterRevealedEvent evt)
        {
            if (tileBoardUI != null && evt.RevealedPositions != null)
                tileBoardUI.RevealTilesAnimated(evt.RevealedPositions);
        }

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            if (submitButton != null) submitButton.interactable = false;
            if (guessInput != null) guessInput.interactable = false;

            // Reveal all tiles with cascade
            if (tileBoardUI != null)
                tileBoardUI.RevealAllAnimated();

            if (_isBossEncounter)
            {
                _isBossEncounter = false;
                ResetBossUI();
            }

            ShowResult(evt);
        }

        private void OnBossIntro(BossIntroEvent evt)
        {
            _isBossEncounter = true;
            ApplyBossUI();

            Color bossFlash = theme != null ? theme.bossIntroFlash : new Color(0.5f, 0f, 0f, 0.3f);
            Flash(bossFlash);

            if (clueText != null)
            {
                if (_typewriterCoroutine != null)
                    StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = StartCoroutine(TypewriterReveal(clueText, evt.IntroText));
            }
        }

        private void OnHPChanged(HPChangedEvent evt)
        {
            _targetHPFill = evt.MaxHP > 0 ? (float)evt.NewHP / evt.MaxHP : 0f;

            if (hpText != null)
                hpText.text = $"{evt.NewHP}/{evt.MaxHP}";

            if (evt.NewHP < evt.OldHP)
            {
                Color damageFlash = theme != null ? theme.damageFlash : new Color(0.8f, 0.1f, 0.1f, 0.4f);
                Flash(damageFlash);
            }
        }

        private void OnTomeTriggered(TomeTriggeredEvent evt)
        {
            if (historyText != null)
            {
                if (historyText.text.Length > 0)
                    historyText.text += "\n";
                historyText.text += $"<{evt.TomeName}> {evt.RevealedInfo}";
            }
        }

        // ── Input Handling ───────────────────────────────────

        private void OnInputSubmit(string text)
        {
            OnSubmitClicked();
        }

        private async void OnSubmitClicked()
        {
            if (_encounter == null || _isProcessing || !_encounter.IsActive) return;

            var guess = guessInput != null ? guessInput.text.Trim() : "";
            if (string.IsNullOrEmpty(guess)) return;

            _isProcessing = true;
            if (submitButton != null) submitButton.interactable = false;
            if (guessInput != null) guessInput.text = "";

            try
            {
                await _encounter.SubmitGuess(guess);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EncounterUI] Error submitting guess: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
                if (_encounter != null && _encounter.IsActive)
                {
                    if (submitButton != null) submitButton.interactable = true;
                    if (guessInput != null) guessInput.ActivateInputField();
                }
            }
        }

        private void OnContinueClicked()
        {
            if (resultPanel != null)
                resultPanel.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.GoToShop();
        }

        // ── Result Display ───────────────────────────────────

        private void ShowResult(EncounterEndedEvent evt)
        {
            if (resultPanel == null) return;

            // Use dramatic reveal if suspense effects available
            if (suspenseEffects != null)
            {
                suspenseEffects.PlayDramaticResult(() => ShowResultImmediate(evt));
                return;
            }

            ShowResultImmediate(evt);
        }

        private void ShowResultImmediate(EncounterEndedEvent evt)
        {
            if (resultPanel == null) return;

            resultPanel.SetActive(true);

            // ASCII banner (typed in)
            if (resultBannerText != null)
            {
                string banner = ASCIIBanners.GetResultBanner(evt.Won);
                resultBannerText.color = evt.Won
                    ? (theme != null ? theme.successColor : new Color(0.1f, 0.9f, 0.3f))
                    : (theme != null ? theme.damageColor : new Color(1f, 0.15f, 0.1f));
                if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = StartCoroutine(TypewriterReveal(resultBannerText, banner));
            }

            if (resultTitleText != null)
            {
                string bossTag = evt.IsBoss ? "BOSS " : "";
                if (evt.Won)
                {
                    resultTitleText.text = $"{bossTag}WORD FOUND!";
                    resultTitleText.color = theme != null ? theme.successColor : new Color(0.1f, 0.9f, 0.3f);
                }
                else
                {
                    resultTitleText.text = $"{bossTag}WORD LOST";
                    resultTitleText.color = theme != null ? theme.damageColor : new Color(1f, 0.15f, 0.1f);
                }

                // Fade in title
                resultTitleText.alpha = 0f;
                resultTitleText.DOFade(1f, 0.5f).SetDelay(0.3f).SetUpdate(true);
            }

            if (resultDetailsText != null)
            {
                string word = evt.TargetWord.ToUpperInvariant();
                string details = $"The word was: {word}\nGuesses used: {evt.GuessCount}";
                if (evt.Won && evt.Score > 0)
                    details += $"\nScore: +{evt.Score}";

                // Typewriter the details with delay
                resultDetailsText.text = "";
                StartCoroutine(DelayedTypewriter(resultDetailsText, details, 0.6f));
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(!evt.IsBoss);
                if (!evt.IsBoss)
                {
                    // Scale-in the continue button with delay
                    var btnRT = continueButton.GetComponent<RectTransform>();
                    if (btnRT != null)
                    {
                        btnRT.localScale = Vector3.zero;
                        btnRT.DOScale(Vector3.one, 0.3f)
                            .SetDelay(1.2f)
                            .SetEase(Ease.OutBack)
                            .SetUpdate(true);
                    }
                }
            }
        }

        private IEnumerator DelayedTypewriter(TextMeshProUGUI text, string content, float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return TypewriterReveal(text, content);
        }

        // ── Juice Effects ────────────────────────────────────

        private IEnumerator TypewriterReveal(TextMeshProUGUI textComponent, string fullText)
        {
            textComponent.text = "";
            for (int i = 0; i < fullText.Length; i++)
            {
                textComponent.text += fullText[i];
                yield return new WaitForSeconds(typewriterSpeed);
            }
            _typewriterCoroutine = null;
        }

        private void ScreenShake()
        {
            if (_shakeCoroutine != null)
                StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            if (_shakeTarget == null) yield break;

            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                float x = Random.Range(-1f, 1f) * shakeIntensity;
                float y = Random.Range(-1f, 1f) * shakeIntensity;
                float decay = 1f - (elapsed / shakeDuration);
                _shakeTarget.anchoredPosition = _shakeOriginalPos + new Vector2(x * decay, y * decay);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _shakeTarget.anchoredPosition = _shakeOriginalPos;
            _shakeCoroutine = null;
        }

        private void Flash(Color color)
        {
            if (flashOverlay == null) return;
            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine(color));
        }

        private IEnumerator FlashRoutine(Color color)
        {
            flashOverlay.color = color;
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                float t = elapsed / flashDuration;
                flashOverlay.color = Color.Lerp(color, new Color(color.r, color.g, color.b, 0), t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            flashOverlay.color = new Color(0, 0, 0, 0);
            _flashCoroutine = null;
        }

        // ── Boss UI Styling ─────────────────────────────────

        private void ApplyBossUI()
        {
            Color bossColor = theme != null ? theme.bossAccent : new Color(0.85f, 0.1f, 0.1f);
            if (npcNameText != null) npcNameText.color = bossColor;
            if (clueText != null) clueText.color = bossColor;
        }

        private void ResetBossUI()
        {
            Color normalColor = theme != null ? theme.phosphorGreen : new Color(0f, 1f, 0.33f);
            if (npcNameText != null) npcNameText.color = normalColor;
            if (clueText != null) clueText.color = normalColor;
        }

        // ── Helpers ──────────────────────────────────────────

        private void ClearDisplay()
        {
            if (npcNameText != null) npcNameText.text = "";
            if (npcArchetypeText != null) npcArchetypeText.text = "";
            if (categoryText != null) categoryText.text = "";
            if (clueText != null) clueText.text = "Waiting for encounter...";
            if (clueNumberText != null) clueNumberText.text = "";
            if (historyText != null) historyText.text = "";
            if (hpText != null) hpText.text = "";
            if (goldText != null) goldText.text = "";
            if (guessesText != null) guessesText.text = "";
            if (scoreText != null) scoreText.text = "";
            if (tomeInfoText != null) tomeInfoText.text = "";
            if (tileBoardUI != null) tileBoardUI.ClearBoard();
            if (guessedLettersUI != null) guessedLettersUI.Reset();
        }

        private void UpdateStatus()
        {
            if (_encounter == null) return;

            if (hpText != null)
                hpText.text = $"{_encounter.CurrentHP}/{_encounter.MaxHP}";

            if (hpBarFill != null)
            {
                _targetHPFill = _encounter.MaxHP > 0 ? (float)_encounter.CurrentHP / _encounter.MaxHP : 0f;
                if (_currentHPFill == 0f)
                {
                    _currentHPFill = _targetHPFill;
                    hpBarFill.fillAmount = _currentHPFill;
                }
            }

            if (goldText != null && RunManager.Instance != null)
            {
                int newGold = RunManager.Instance.Gold;
                if (animatedCounter != null && _lastDisplayedGold != newGold && _lastDisplayedGold > 0)
                {
                    Color flash = theme != null ? theme.amberBright : new Color(1f, 0.75f, 0f);
                    animatedCounter.AnimateToValue(goldText, _lastDisplayedGold, newGold, "{0}g", 0.5f, flash);
                }
                else
                {
                    goldText.text = $"{newGold}g";
                }
                _lastDisplayedGold = newGold;
            }

            if (guessesText != null)
                guessesText.text = $"Guesses: {_encounter.GuessesRemaining}";

            if (scoreText != null && RunManager.Instance != null)
            {
                int newScore = RunManager.Instance.Score;
                if (animatedCounter != null && _lastDisplayedScore != newScore && _lastDisplayedScore > 0)
                {
                    Color flash = theme != null ? theme.cyanInfo : new Color(0f, 0.85f, 0.85f);
                    animatedCounter.AnimateToValue(scoreText, _lastDisplayedScore, newScore, "Score: {0}", 0.5f, flash);
                }
                else
                {
                    scoreText.text = $"Score: {newScore}";
                }
                _lastDisplayedScore = newScore;
            }
        }

        private void UpdateTomeInfo()
        {
            if (tomeInfoText == null) return;
            if (TomeManager.Instance?.TomeSystem == null)
            {
                tomeInfoText.text = "";
                return;
            }

            var tomes = TomeManager.Instance.TomeSystem.GetEquippedTomes();
            if (tomes.Count == 0)
            {
                tomeInfoText.text = "No tomes equipped";
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var tome in tomes)
            {
                if (sb.Length > 0) sb.Append("\n");
                sb.Append($"* {tome.TomeName}");
            }
            tomeInfoText.text = sb.ToString();
        }
    }
}
