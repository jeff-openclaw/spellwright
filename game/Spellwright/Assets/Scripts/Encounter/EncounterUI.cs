using System.Collections;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Run;
using Spellwright.Tomes;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Production encounter UI with juice effects: typewriter, screen shake,
    /// letter reveal animation, HP bar animation, damage/success flashes.
    /// Replaces EncounterTestUI for the actual game flow.
    /// </summary>
    public class EncounterUI : MonoBehaviour
    {
        [Header("NPC Info")]
        [SerializeField] private Text npcNameText;
        [SerializeField] private Text npcArchetypeText;

        [Header("Word Display")]
        [SerializeField] private Text blanksText;
        [SerializeField] private Text categoryText;

        [Header("Clue Display")]
        [SerializeField] private Text clueText;
        [SerializeField] private Text clueNumberText;

        [Header("Input")]
        [SerializeField] private InputField guessInput;
        [SerializeField] private Button submitButton;

        [Header("Status")]
        [SerializeField] private Text hpText;
        [SerializeField] private Image hpBarFill;
        [SerializeField] private Text goldText;
        [SerializeField] private Text guessesText;
        [SerializeField] private Text scoreText;

        [Header("Guess History")]
        [SerializeField] private Text historyText;

        [Header("Tome Info")]
        [SerializeField] private Text tomeInfoText;

        [Header("Result Overlay")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultDetailsText;
        [SerializeField] private Button continueButton;

        [Header("Flash Overlay")]
        [SerializeField] private Image flashOverlay;

        [Header("Juice Settings")]
        [SerializeField] private float typewriterSpeed = 0.03f;
        [SerializeField] private float shakeIntensity = 8f;
        [SerializeField] private float shakeDuration = 0.3f;
        [SerializeField] private float letterRevealDuration = 0.3f;
        [SerializeField] private float hpBarLerpSpeed = 5f;
        [SerializeField] private float flashDuration = 0.2f;

        // State
        private EncounterManager _encounter;
        private bool _isProcessing;
        private bool _isBossEncounter;
        private string _currentBlanks;
        private float _targetHPFill;
        private float _currentHPFill;
        private Coroutine _typewriterCoroutine;
        private Coroutine _shakeCoroutine;
        private Coroutine _flashCoroutine;
        private RectTransform _shakeTarget;
        private Vector2 _shakeOriginalPos;

        // Boss styling
        private static readonly Color BossAccentColor = new Color(0.85f, 0.1f, 0.1f, 1f);
        private static readonly Color NormalTextColor = Color.white;
        private static readonly Color DamageFlashColor = new Color(0.8f, 0.1f, 0.1f, 0.4f);
        private static readonly Color SuccessFlashColor = new Color(0.1f, 0.8f, 0.2f, 0.4f);
        private static readonly Color BossIntroFlashColor = new Color(0.5f, 0.0f, 0.0f, 0.3f);

        private void Start()
        {
            _encounter = FindAnyObjectByType<EncounterManager>();

            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmitClicked);
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            // Set up shake target (the main panel's RectTransform)
            _shakeTarget = GetComponent<RectTransform>();
            if (_shakeTarget != null)
                _shakeOriginalPos = _shakeTarget.anchoredPosition;

            // Subscribe to events
            EventBus.Instance.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Subscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Subscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Subscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Subscribe<HPChangedEvent>(OnHPChanged);
            EventBus.Instance.Subscribe<TomeTriggeredEvent>(OnTomeTriggered);
            EventBus.Instance.Subscribe<BossIntroEvent>(OnBossIntro);

            // Initial state
            ClearDisplay();
            if (flashOverlay != null)
            {
                flashOverlay.color = new Color(0, 0, 0, 0);
                flashOverlay.raycastTarget = false;
            }
            if (resultPanel != null)
                resultPanel.SetActive(false);

            // Handle input submission with Enter key
            if (guessInput != null)
                guessInput.onEndEdit.AddListener(OnInputEndEdit);
        }

        private void OnDestroy()
        {
            EventBus.Instance.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Unsubscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Unsubscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Unsubscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Unsubscribe<HPChangedEvent>(OnHPChanged);
            EventBus.Instance.Unsubscribe<TomeTriggeredEvent>(OnTomeTriggered);
            EventBus.Instance.Unsubscribe<BossIntroEvent>(OnBossIntro);
        }

        private void Update()
        {
            // Smooth HP bar animation
            if (hpBarFill != null && Mathf.Abs(_currentHPFill - _targetHPFill) > 0.001f)
            {
                _currentHPFill = Mathf.Lerp(_currentHPFill, _targetHPFill, Time.deltaTime * hpBarLerpSpeed);
                hpBarFill.fillAmount = _currentHPFill;
            }

            // Focus input field when encounter is active
            if (_encounter != null && _encounter.IsActive && guessInput != null
                && !_isProcessing && !guessInput.isFocused
                && (resultPanel == null || !resultPanel.activeSelf))
            {
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

            // Category
            if (categoryText != null)
                categoryText.text = $"Category: {evt.Category}";

            // Build blanks display
            _currentBlanks = BuildBlanks(evt.TargetWord.Length);
            if (blanksText != null)
            {
                blanksText.text = "";
                StartCoroutine(RevealLetters(_currentBlanks));
            }

            // Reset clue
            if (clueText != null) clueText.text = "...";
            if (clueNumberText != null) clueNumberText.text = "";

            // Clear history
            if (historyText != null) historyText.text = "";

            // Update status
            UpdateStatus();
            UpdateTomeInfo();

            // Reset HP bar for clean animation start
            _currentHPFill = 0f;

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

            // Typewriter effect for clue text
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
                string icon = evt.Result.IsCorrect ? ">>>" : evt.Result.IsValidWord ? " X " : " ? ";
                string entry = $"[{icon}] {evt.Guess.ToUpperInvariant()}";
                if (evt.Result.IsValidWord && !evt.Result.IsCorrect)
                    entry += $"  ({evt.Result.LettersCorrect} correct)";
                else if (!evt.Result.IsValidWord)
                    entry += "  (invalid word)";

                if (historyText.text.Length > 0)
                    historyText.text += "\n";
                historyText.text += entry;
            }

            // Juice effects based on result
            if (evt.Result.IsCorrect)
            {
                Flash(SuccessFlashColor);
            }
            else if (evt.Result.IsValidWord)
            {
                Flash(DamageFlashColor);
                ScreenShake();
            }

            UpdateStatus();
        }

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            // Disable input
            if (submitButton != null) submitButton.interactable = false;
            if (guessInput != null) guessInput.interactable = false;

            // Reveal the word
            if (blanksText != null)
                blanksText.text = FormatWord(evt.TargetWord);

            // Reset boss styling
            if (_isBossEncounter)
            {
                _isBossEncounter = false;
                ResetBossUI();
            }

            // Show result panel
            ShowResult(evt);
        }

        private void OnBossIntro(BossIntroEvent evt)
        {
            _isBossEncounter = true;
            ApplyBossUI();
            Flash(BossIntroFlashColor);

            if (clueText != null)
            {
                if (_typewriterCoroutine != null)
                    StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = StartCoroutine(TypewriterReveal(clueText, evt.IntroText));
            }
        }

        private void OnHPChanged(HPChangedEvent evt)
        {
            // Animate HP bar
            _targetHPFill = evt.MaxHP > 0 ? (float)evt.NewHP / evt.MaxHP : 0f;

            if (hpText != null)
                hpText.text = $"{evt.NewHP}/{evt.MaxHP}";

            // Flash on damage
            if (evt.NewHP < evt.OldHP)
                Flash(DamageFlashColor);
        }

        private void OnTomeTriggered(TomeTriggeredEvent evt)
        {
            // Show tome effect in history
            if (historyText != null)
            {
                if (historyText.text.Length > 0)
                    historyText.text += "\n";
                historyText.text += $"<{evt.TomeName}> {evt.RevealedInfo}";
            }

            // Update blanks for First Light (reveals first letter)
            if (_currentBlanks != null && evt.TomeName == "First Light"
                && evt.RevealedInfo != null && evt.RevealedInfo.Length > 0)
            {
                char revealed = evt.RevealedInfo[evt.RevealedInfo.Length - 1];
                if (char.IsLetter(revealed))
                {
                    _currentBlanks = revealed.ToString().ToUpperInvariant() + _currentBlanks.Substring(1);
                    if (blanksText != null)
                        blanksText.text = _currentBlanks;
                }
            }
        }

        // ── Input Handling ───────────────────────────────────

        private void OnInputEndEdit(string text)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
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
                GameManager.Instance.ReturnToMap();
        }

        // ── Result Display ───────────────────────────────────

        private void ShowResult(EncounterEndedEvent evt)
        {
            if (resultPanel == null) return;

            resultPanel.SetActive(true);

            if (resultTitleText != null)
            {
                string bossTag = evt.IsBoss ? "BOSS " : "";
                if (evt.Won)
                {
                    resultTitleText.text = $"{bossTag}WORD FOUND!";
                    resultTitleText.color = new Color(0.2f, 0.9f, 0.3f);
                }
                else
                {
                    resultTitleText.text = $"{bossTag}WORD LOST";
                    resultTitleText.color = new Color(0.9f, 0.2f, 0.2f);
                }
            }

            if (resultDetailsText != null)
            {
                string word = evt.TargetWord.ToUpperInvariant();
                string details = $"The word was: {word}\nGuesses used: {evt.GuessCount}";
                if (evt.Won && evt.Score > 0)
                    details += $"\nScore: +{evt.Score}";
                resultDetailsText.text = details;
            }

            // Boss encounters: GameManager handles flow automatically
            // (boss win = ReturnToMap → RunEnd, boss loss = EndRun → RunEnd)
            // Non-boss: show continue button for player to return to map
            if (continueButton != null)
                continueButton.gameObject.SetActive(!evt.IsBoss);
        }

        // ── Juice Effects ────────────────────────────────────

        private IEnumerator TypewriterReveal(Text textComponent, string fullText)
        {
            textComponent.text = "";
            for (int i = 0; i < fullText.Length; i++)
            {
                textComponent.text += fullText[i];
                yield return new WaitForSeconds(typewriterSpeed);
            }
            _typewriterCoroutine = null;
        }

        private IEnumerator RevealLetters(string blanks)
        {
            if (blanksText == null) yield break;

            blanksText.text = "";
            for (int i = 0; i < blanks.Length; i++)
            {
                blanksText.text += blanks[i];
                if (blanks[i] != ' ')
                {
                    // Brief scale punch for each letter slot
                    blanksText.transform.localScale = Vector3.one * 1.1f;
                    yield return new WaitForSeconds(letterRevealDuration / blanks.Length);
                    blanksText.transform.localScale = Vector3.one;
                }
            }
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

                // Decay shake intensity over time
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
            if (npcNameText != null) npcNameText.color = BossAccentColor;
            if (blanksText != null) blanksText.color = BossAccentColor;
            if (clueText != null) clueText.color = BossAccentColor;
        }

        private void ResetBossUI()
        {
            if (npcNameText != null) npcNameText.color = NormalTextColor;
            if (blanksText != null) blanksText.color = NormalTextColor;
            if (clueText != null) clueText.color = NormalTextColor;
        }

        // ── Helpers ──────────────────────────────────────────

        private void ClearDisplay()
        {
            if (npcNameText != null) npcNameText.text = "";
            if (npcArchetypeText != null) npcArchetypeText.text = "";
            if (blanksText != null) blanksText.text = "";
            if (categoryText != null) categoryText.text = "";
            if (clueText != null) clueText.text = "Waiting for encounter...";
            if (clueNumberText != null) clueNumberText.text = "";
            if (historyText != null) historyText.text = "";
            if (hpText != null) hpText.text = "";
            if (goldText != null) goldText.text = "";
            if (guessesText != null) guessesText.text = "";
            if (scoreText != null) scoreText.text = "";
            if (tomeInfoText != null) tomeInfoText.text = "";
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
                goldText.text = $"{RunManager.Instance.Gold}g";

            if (guessesText != null)
                guessesText.text = $"Guesses: {_encounter.GuessesRemaining}";

            if (scoreText != null && RunManager.Instance != null)
                scoreText.text = $"Score: {RunManager.Instance.Score}";
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

        private static string BuildBlanks(int length)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append('_');
            }
            return sb.ToString();
        }

        private static string FormatWord(string word)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < word.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(char.ToUpperInvariant(word[i]));
            }
            return sb.ToString();
        }
    }
}
