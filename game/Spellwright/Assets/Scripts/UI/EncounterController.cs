using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Encounter;
using Spellwright.Run;
using Spellwright.Tomes;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellwright.UI
{
    /// <summary>
    /// UI Toolkit-based encounter screen controller. Replaces the uGUI EncounterUI.
    /// Renders tile board, clue display, input area, stats, history, result overlay.
    /// </summary>
    public class EncounterController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        [Header("Animation")]
        [SerializeField] private float typewriterSpeedMs = 30f;
        [SerializeField] private float flashDurationMs = 200f;
        [SerializeField] private float tileRevealDurationMs = 300f;
        [SerializeField] private float cascadeDelayMs = 50f;
        [SerializeField] private float hpBarLerpSpeed = 5f;

        private VisualElement _root;

        // Cached elements
        private VisualElement _tileGrid;
        private Label _categoryLabel;
        private Label _npcPortraitLabel;
        private Label _npcNameLabel;
        private Label _clueNumberLabel;
        private Label _clueTextLabel;
        private Label _hpTextLabel;
        private VisualElement _hpBarFill;
        private Label _goldTextLabel;
        private Label _guessesTextLabel;
        private Label _scoreTextLabel;
        private TextField _guessInput;
        private Button _submitBtn;
        private Button _solveBtn;
        private Label _inputModeLabel;
        private Label _historyTextLabel;
        private Label _tomeInfoLabel;
        private VisualElement _guessedLetters;
        private VisualElement _resultOverlay;
        private Label _resultBanner;
        private Label _resultTitle;
        private Label _resultDetails;
        private Button _continueBtn;
        private VisualElement _flashOverlay;
        private Label _signalStatusLabel;
        private VisualElement _ultimatumOverlay;
        private Label _ultimatumTextLabel;
        private Label _countdownTextLabel;
        private VisualElement _bargainOverlay;
        private Label _bargainFlavorLabel;
        private Label _bargainDescriptionLabel;
        private Label _bargainCostLabel;
        private Button _bargainAcceptBtn;
        private VisualElement _bargainTimerFill;

        // State
        private EncounterManager _encounter;
        private bool _isProcessing;
        private bool _isBossEncounter;
        private bool _ultimatumActive;
        private bool _bargainActive;
        private BargainEffect _pendingBargainEffect;
        private IVisualElementScheduledItem _bargainTimerSchedule;
        private float _targetHPFill;
        private float _currentHPFill;
        private int _lastDisplayedGold;
        private int _lastDisplayedScore;
        private IVisualElementScheduledItem _typewriterSchedule;
        private IVisualElementScheduledItem _hpLerpSchedule;
        private string _typewriterFullText;
        private int _typewriterIndex;

        // Tile data
        private readonly List<VisualElement> _tileElements = new();
        private readonly List<Label> _tileLetterLabels = new();
        private readonly List<Label> _tilePatternLabels = new();
        private readonly Dictionary<char, Label> _letterLabels = new();

        // NPC portrait expression tracking
        private string _npcId;
        private bool _npcIsBoss;
        private NPCExpression _baseExpression = NPCExpression.Neutral;
        private IVisualElementScheduledItem _expressionRevertSchedule;
        private IVisualElementScheduledItem _countdownSchedule;

        private void OnEnable()
        {
            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            _encounter = FindAnyObjectByType<EncounterManager>();

            CacheElements();
            WireEvents();
            SubscribeEventBus();
            BuildGuessedLetters();
            StartHPLerp();
        }

        private void OnDisable()
        {
            UnwireEvents();
            UnsubscribeEventBus();
            StopTypewriter();
            _hpLerpSchedule?.Pause();
            _expressionRevertSchedule?.Pause();
            _countdownSchedule?.Pause();
            _bargainTimerSchedule?.Pause();
        }

        private void CacheElements()
        {
            _tileGrid = _root.Q("tile-grid");
            _categoryLabel = _root.Q<Label>("category");
            _npcPortraitLabel = _root.Q<Label>("npc-portrait");
            _npcNameLabel = _root.Q<Label>("npc-name");
            _clueNumberLabel = _root.Q<Label>("clue-number");
            _clueTextLabel = _root.Q<Label>("clue-text");
            _hpTextLabel = _root.Q<Label>("hp-text");
            _hpBarFill = _root.Q("hp-bar-fill");
            _goldTextLabel = _root.Q<Label>("gold-text");
            _guessesTextLabel = _root.Q<Label>("guesses-text");
            _scoreTextLabel = _root.Q<Label>("score-text");
            _guessInput = _root.Q<TextField>("guess-input");
            _submitBtn = _root.Q<Button>("submit-btn");
            _solveBtn = _root.Q<Button>("solve-btn");
            _inputModeLabel = _root.Q<Label>("input-mode");
            _historyTextLabel = _root.Q<Label>("history-text");
            _tomeInfoLabel = _root.Q<Label>("tome-info");
            _guessedLetters = _root.Q("guessed-letters");
            _resultOverlay = _root.Q("result-overlay");
            _resultBanner = _root.Q<Label>("result-banner");
            _resultTitle = _root.Q<Label>("result-title");
            _resultDetails = _root.Q<Label>("result-details");
            _continueBtn = _root.Q<Button>("continue-btn");
            _flashOverlay = _root.Q("flash-overlay");
            _signalStatusLabel = _root.Q<Label>("signal-status");
            _ultimatumOverlay = _root.Q("ultimatum-overlay");
            _ultimatumTextLabel = _root.Q<Label>("ultimatum-text");
            _countdownTextLabel = _root.Q<Label>("countdown-text");
            _bargainOverlay = _root.Q("bargain-overlay");
            _bargainFlavorLabel = _root.Q<Label>("bargain-flavor");
            _bargainDescriptionLabel = _root.Q<Label>("bargain-description");
            _bargainCostLabel = _root.Q<Label>("bargain-cost");
            _bargainAcceptBtn = _root.Q<Button>("bargain-accept");
            _bargainTimerFill = _root.Q("bargain-timer-fill");
        }

        private void WireEvents()
        {
            if (_submitBtn != null)
                _submitBtn.clicked += OnSubmitClicked;
            if (_solveBtn != null)
                _solveBtn.clicked += OnSolveToggle;
            if (_continueBtn != null)
                _continueBtn.clicked += OnContinueClicked;
            if (_guessInput != null)
            {
                _guessInput.RegisterCallback<KeyDownEvent>(OnInputKeyDown);
                _guessInput.RegisterValueChangedCallback(OnInputChanged);
            }
            if (_bargainAcceptBtn != null)
                _bargainAcceptBtn.clicked += OnBargainAcceptClicked;
        }

        private void UnwireEvents()
        {
            if (_submitBtn != null)
                _submitBtn.clicked -= OnSubmitClicked;
            if (_solveBtn != null)
                _solveBtn.clicked -= OnSolveToggle;
            if (_continueBtn != null)
                _continueBtn.clicked -= OnContinueClicked;
            if (_guessInput != null)
            {
                _guessInput.UnregisterCallback<KeyDownEvent>(OnInputKeyDown);
                _guessInput.UnregisterValueChangedCallback(OnInputChanged);
            }
            if (_bargainAcceptBtn != null)
                _bargainAcceptBtn.clicked -= OnBargainAcceptClicked;
        }

        private void SubscribeEventBus()
        {
            EventBus.Instance.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Subscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Subscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Subscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Subscribe<HPChangedEvent>(OnHPChanged);
            EventBus.Instance.Subscribe<TomeTriggeredEvent>(OnTomeTriggered);
            EventBus.Instance.Subscribe<BossIntroEvent>(OnBossIntro);
            EventBus.Instance.Subscribe<LetterRevealedEvent>(OnLetterRevealed);
            EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Instance.Subscribe<DifficultyShiftChangedEvent>(OnDifficultyShiftChanged);
            EventBus.Instance.Subscribe<UltimatumTriggeredEvent>(OnUltimatumTriggered);
            EventBus.Instance.Subscribe<UltimatumLineReceivedEvent>(OnUltimatumLineReceived);
            EventBus.Instance.Subscribe<UltimatumExpiredEvent>(OnUltimatumExpired);
            EventBus.Instance.Subscribe<RivalEncounterStartedEvent>(OnRivalEncounterStarted);
            EventBus.Instance.Subscribe<BargainOfferedEvent>(OnBargainOffered);
            EventBus.Instance.Subscribe<BargainExpiredEvent>(OnBargainExpired);
        }

        private void UnsubscribeEventBus()
        {
            EventBus.Instance.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Unsubscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Unsubscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Unsubscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Unsubscribe<HPChangedEvent>(OnHPChanged);
            EventBus.Instance.Unsubscribe<TomeTriggeredEvent>(OnTomeTriggered);
            EventBus.Instance.Unsubscribe<BossIntroEvent>(OnBossIntro);
            EventBus.Instance.Unsubscribe<LetterRevealedEvent>(OnLetterRevealed);
            EventBus.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Instance.Unsubscribe<DifficultyShiftChangedEvent>(OnDifficultyShiftChanged);
            EventBus.Instance.Unsubscribe<UltimatumTriggeredEvent>(OnUltimatumTriggered);
            EventBus.Instance.Unsubscribe<UltimatumLineReceivedEvent>(OnUltimatumLineReceived);
            EventBus.Instance.Unsubscribe<UltimatumExpiredEvent>(OnUltimatumExpired);
            EventBus.Instance.Unsubscribe<RivalEncounterStartedEvent>(OnRivalEncounterStarted);
            EventBus.Instance.Unsubscribe<BargainOfferedEvent>(OnBargainOffered);
            EventBus.Instance.Unsubscribe<BargainExpiredEvent>(OnBargainExpired);
        }

        // ── Event Handlers ──────────────────────────────────

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState == GameState.Encounter || evt.NewState == GameState.Boss)
            {
                // Re-find encounter manager in case it was recreated
                if (_encounter == null)
                    _encounter = FindAnyObjectByType<EncounterManager>();

                // Focus input
                FocusInput();
            }
        }

        private void OnEncounterStarted(EncounterStartedEvent evt)
        {
            // Hide result overlay and bargain
            HideResult();
            HideBargain();

            // NPC info
            if (_npcNameLabel != null)
            {
                _npcNameLabel.text = evt.NPC.DisplayName;
                _npcNameLabel.RemoveFromClassList("encounter-screen__npc-name--boss");
                _npcNameLabel.RemoveFromClassList("encounter-screen__npc-name--rival");
            }

            // Portrait
            SetNPC(evt.NPC.DisplayName, evt.NPC.IsBoss);

            // Category
            if (_categoryLabel != null)
                _categoryLabel.text = evt.Category.ToUpperInvariant();

            // Build tile board
            if (_encounter?.Board != null)
                BuildTileBoard(_encounter.Board);

            // Reset guessed letters
            ResetGuessedLetters();

            // Reset clue + signal status
            if (_clueTextLabel != null) _clueTextLabel.text = "...";
            if (_clueNumberLabel != null) _clueNumberLabel.text = "";
            if (_signalStatusLabel != null)
            {
                _signalStatusLabel.text = "";
                _signalStatusLabel.RemoveFromClassList("encounter-screen__signal-status--mercy");
                _signalStatusLabel.RemoveFromClassList("encounter-screen__signal-status--cruel");
            }

            // Hide ultimatum if active
            HideUltimatum();

            // Clear history + tomes
            if (_historyTextLabel != null) _historyTextLabel.text = "";

            _currentHPFill = 0f;
            _lastDisplayedGold = RunManager.Instance != null ? RunManager.Instance.Gold : 0;
            _lastDisplayedScore = RunManager.Instance != null ? RunManager.Instance.Score : 0;
            UpdateStatus();
            UpdateTomeInfo();

            // Enable input
            SetInputEnabled(true);
            if (_inputModeLabel != null) _inputModeLabel.text = "LETTER MODE";

            FocusInput();
        }

        private void OnClueReceived(ClueReceivedEvent evt)
        {
            if (_clueNumberLabel != null)
            {
                string fallback = evt.Clue.UsedFallbackModel ? " *" : "";
                _clueNumberLabel.text = $"Clue #{evt.ClueNumber}{fallback}";
            }

            if (_clueTextLabel != null)
                StartTypewriter(_clueTextLabel, evt.Clue.Clue);
        }

        private void OnGuessSubmitted(GuessSubmittedEvent evt)
        {
            // Build history entry
            if (_historyTextLabel != null)
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

                if (_historyTextLabel.text.Length > 0)
                    _historyTextLabel.text += "\n";
                _historyTextLabel.text += entry;
            }

            // Update guessed letters for letter guesses
            if (evt.Result.GuessType == GuessType.Letter)
                MarkLetterGuessed(evt.Result.GuessedLetter, evt.Result.IsLetterInPhrase);

            // Flash effects
            if (evt.Result.IsCorrect || (evt.Result.GuessType == GuessType.Letter && evt.Result.IsLetterInPhrase))
            {
                Flash("encounter-screen__flash-overlay--success");
                SetPortraitExpression(NPCExpression.Impressed, temporary: true);
            }
            else if (evt.Result.IsValidWord || (evt.Result.GuessType == GuessType.Letter && !evt.Result.IsLetterAlreadyGuessed))
            {
                Flash("encounter-screen__flash-overlay--damage");
                SetPortraitExpression(NPCExpression.Amused, temporary: true);
            }

            UpdateStatus();
        }

        private void OnLetterRevealed(LetterRevealedEvent evt)
        {
            if (evt.RevealedPositions == null) return;
            RevealTilesAnimated(evt.RevealedPositions);
        }

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            HideUltimatum();
            SetInputEnabled(false);
            RevealAllAnimated();

            if (_isBossEncounter)
            {
                _isBossEncounter = false;
                ResetBossUI();
            }

            SetPortraitExpression(evt.Won ? NPCExpression.Defeated : NPCExpression.Victorious, temporary: false);
            ShowResult(evt);
        }

        private void OnBossIntro(BossIntroEvent evt)
        {
            _isBossEncounter = true;
            ApplyBossUI();
            Flash("encounter-screen__flash-overlay--boss");

            _baseExpression = NPCExpression.Angry;
            SetPortraitExpression(NPCExpression.Angry, temporary: false);

            if (_clueTextLabel != null)
                StartTypewriter(_clueTextLabel, evt.IntroText);
        }

        private void OnHPChanged(HPChangedEvent evt)
        {
            _targetHPFill = evt.MaxHP > 0 ? (float)evt.NewHP / evt.MaxHP : 0f;

            if (_hpTextLabel != null)
                _hpTextLabel.text = $"{evt.NewHP}/{evt.MaxHP}";

            if (evt.NewHP < evt.OldHP)
            {
                Flash("encounter-screen__flash-overlay--damage");
                float hpPercent = evt.MaxHP > 0 ? (float)evt.NewHP / evt.MaxHP : 1f;
                if (hpPercent < 0.25f && _baseExpression != NPCExpression.Angry)
                {
                    _baseExpression = NPCExpression.Angry;
                    SetPortraitExpression(NPCExpression.Angry, temporary: false);
                }
            }
        }

        private void OnTomeTriggered(TomeTriggeredEvent evt)
        {
            if (_historyTextLabel != null)
            {
                if (_historyTextLabel.text.Length > 0)
                    _historyTextLabel.text += "\n";
                _historyTextLabel.text += $"<{evt.TomeName}> {evt.RevealedInfo}";
            }
        }

        private void OnDifficultyShiftChanged(DifficultyShiftChangedEvent evt)
        {
            if (_signalStatusLabel == null) return;

            _signalStatusLabel.RemoveFromClassList("encounter-screen__signal-status--mercy");
            _signalStatusLabel.RemoveFromClassList("encounter-screen__signal-status--cruel");

            switch (evt.Shift)
            {
                case DifficultyShift.Mercy:
                    _signalStatusLabel.text = "[SIGNAL: BOOSTED]";
                    _signalStatusLabel.AddToClassList("encounter-screen__signal-status--mercy");
                    break;
                case DifficultyShift.Cruel:
                    _signalStatusLabel.text = "[SIGNAL: DEGRADED]";
                    _signalStatusLabel.AddToClassList("encounter-screen__signal-status--cruel");
                    break;
                default:
                    _signalStatusLabel.text = "";
                    break;
            }
        }

        private void OnRivalEncounterStarted(RivalEncounterStartedEvent evt)
        {
            // Show [RIVAL] tag next to NPC name
            if (_npcNameLabel != null)
            {
                _npcNameLabel.text += " [RIVAL]";
                _npcNameLabel.AddToClassList("encounter-screen__npc-name--rival");
            }
        }

        // ── Ultimatum ────────────────────────────────────────

        private void OnUltimatumTriggered(UltimatumTriggeredEvent evt)
        {
            _ultimatumActive = true;

            // Show overlay
            _ultimatumOverlay?.AddToClassList("encounter-screen__ultimatum-overlay--visible");

            // Clear text until LLM line arrives
            if (_ultimatumTextLabel != null)
            {
                _ultimatumTextLabel.text = "";
                _ultimatumTextLabel.RemoveFromClassList("encounter-screen__ultimatum-text--visible");
            }

            // Start countdown display
            StartCountdownDisplay();

            // Flash effect
            Flash("encounter-screen__flash-overlay--boss");
        }

        private void OnUltimatumLineReceived(UltimatumLineReceivedEvent evt)
        {
            if (_ultimatumTextLabel != null)
            {
                _ultimatumTextLabel.text = evt.Line;
                _ultimatumTextLabel.AddToClassList("encounter-screen__ultimatum-text--visible");
            }
        }

        private void OnUltimatumExpired(UltimatumExpiredEvent evt)
        {
            HideUltimatum();
            Flash("encounter-screen__flash-overlay--damage");
        }

        private void StartCountdownDisplay()
        {
            var ultimatumSystem = FindAnyObjectByType<UltimatumSystem>();
            if (ultimatumSystem == null) return;

            _countdownSchedule?.Pause();
            _countdownSchedule = _root.schedule.Execute(() =>
            {
                if (!_ultimatumActive || ultimatumSystem == null || !ultimatumSystem.IsActive)
                {
                    HideUltimatum();
                    _countdownSchedule?.Pause();
                    return;
                }

                int seconds = Mathf.CeilToInt(ultimatumSystem.TimeRemaining);
                if (_countdownTextLabel != null)
                {
                    _countdownTextLabel.text = $"> FINAL ANSWER IN: {seconds}s";
                    // Blink when under 5 seconds
                    if (seconds <= 5 && seconds % 2 == 0)
                        _countdownTextLabel.AddToClassList("encounter-screen__countdown-text--blink");
                    else
                        _countdownTextLabel.RemoveFromClassList("encounter-screen__countdown-text--blink");
                }
            }).Every(250);
        }

        private void HideUltimatum()
        {
            _ultimatumActive = false;
            _countdownSchedule?.Pause();
            _ultimatumOverlay?.RemoveFromClassList("encounter-screen__ultimatum-overlay--visible");
            _ultimatumTextLabel?.RemoveFromClassList("encounter-screen__ultimatum-text--visible");
            _countdownTextLabel?.RemoveFromClassList("encounter-screen__countdown-text--blink");
        }

        // ── Input ───────────────────────────────────────────

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                evt.StopPropagation();
                OnSubmitClicked();
            }
        }

        private void OnInputChanged(ChangeEvent<string> evt)
        {
            if (_inputModeLabel != null)
            {
                bool isLetterMode = evt.newValue.Length <= 1;
                _inputModeLabel.text = isLetterMode ? "LETTER MODE" : "SOLVE MODE";
            }
        }

        private async void OnSubmitClicked()
        {
            if (_encounter == null || _isProcessing || !_encounter.IsActive) return;

            var guess = _guessInput?.value?.Trim() ?? "";
            if (string.IsNullOrEmpty(guess)) return;

            _isProcessing = true;
            if (_submitBtn != null) _submitBtn.SetEnabled(false);
            if (_guessInput != null) _guessInput.value = "";

            try
            {
                await _encounter.SubmitGuess(guess);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EncounterController] Error submitting guess: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
                if (_encounter != null && _encounter.IsActive)
                {
                    if (_submitBtn != null) _submitBtn.SetEnabled(true);
                    FocusInput();
                }
            }
        }

        private void OnSolveToggle()
        {
            // Toggle solve button text
            if (_solveBtn != null)
            {
                bool isSolveMode = _solveBtn.text == "[ LETTER ]";
                _solveBtn.text = isSolveMode ? "[ SOLVE ]" : "[ LETTER ]";
                if (_inputModeLabel != null)
                    _inputModeLabel.text = isSolveMode ? "LETTER MODE" : "SOLVE MODE";
            }

            if (_guessInput != null)
            {
                _guessInput.value = "";
                FocusInput();
            }
        }

        private void OnContinueClicked()
        {
            HideResult();
            if (GameManager.Instance != null)
                GameManager.Instance.GoToShop();
        }

        private void SetInputEnabled(bool enabled)
        {
            if (_submitBtn != null) _submitBtn.SetEnabled(enabled);
            if (_solveBtn != null) _solveBtn.SetEnabled(enabled);
            if (_guessInput != null) _guessInput.SetEnabled(enabled);
        }

        private void FocusInput()
        {
            _guessInput?.schedule.Execute(() => _guessInput?.Focus()).ExecuteLater(50);
        }

        // ── Tile Board ──────────────────────────────────────

        private void BuildTileBoard(BoardState board)
        {
            ClearTileBoard();
            if (board == null || board.Tiles.Length == 0) return;

            var tiles = board.Tiles;

            // Use same WoF grid algorithm as TileBoardUI
            int middleCols = 14;
            int edgeCols = middleCols - 2;
            int minRows = 4;

            var words = SplitIntoWords(tiles);
            const int GAP = -1;
            var contentRows = new List<List<int>>();
            var currentRow = new List<int>();
            int currentCols = 0;

            foreach (var word in words)
            {
                int gap = currentCols > 0 ? 1 : 0;
                if (currentCols > 0 && currentCols + gap + word.Count > middleCols)
                {
                    contentRows.Add(currentRow);
                    currentRow = new List<int>();
                    currentCols = 0;
                    gap = 0;
                }
                if (gap > 0) { currentRow.Add(GAP); currentCols++; }
                foreach (int idx in word) { currentRow.Add(idx); currentCols++; }
            }
            if (currentRow.Count > 0) contentRows.Add(currentRow);

            int totalRows = Mathf.Max(minRows, contentRows.Count);
            int[] rowColCounts = new int[totalRows];
            for (int i = 0; i < totalRows; i++)
                rowColCounts[i] = (i == 0 || i == totalRows - 1) ? edgeCols : middleCols;

            int startRow = Mathf.Max(0, (totalRows - contentRows.Count) / 2);

            // Pre-fill lists
            for (int i = 0; i < tiles.Length; i++)
            {
                _tileElements.Add(null);
                _tileLetterLabels.Add(null);
                _tilePatternLabels.Add(null);
            }

            for (int r = 0; r < totalRows; r++)
            {
                int cols = rowColCounts[r];
                int contentRowIdx = r - startRow;
                bool hasContent = contentRowIdx >= 0 && contentRowIdx < contentRows.Count;

                var rowEl = new VisualElement();
                rowEl.AddToClassList("encounter-tile-row");

                if (hasContent)
                {
                    var rowContent = contentRows[contentRowIdx];
                    int contentWidth = rowContent.Count;
                    int effectiveCols = Mathf.Max(cols, contentWidth);
                    int leftPad = (effectiveCols - contentWidth) / 2;

                    for (int c = 0; c < effectiveCols; c++)
                    {
                        int ci = c - leftPad;
                        if (ci >= 0 && ci < rowContent.Count)
                        {
                            int item = rowContent[ci];
                            if (item == GAP)
                            {
                                rowEl.Add(CreateEmptyTile());
                            }
                            else
                            {
                                var (tile, letterLabel, patternLabel) = CreateTile(tiles[item], item);
                                rowEl.Add(tile);
                                _tileElements[item] = tile;
                                _tileLetterLabels[item] = letterLabel;
                                _tilePatternLabels[item] = patternLabel;

                                if (tiles[item].State == TileState.Revealed)
                                    RevealTileImmediate(item);
                            }
                        }
                        else
                        {
                            rowEl.Add(CreateEmptyTile());
                        }
                    }
                }
                else
                {
                    for (int c = 0; c < cols; c++)
                        rowEl.Add(CreateEmptyTile());
                }

                _tileGrid.Add(rowEl);
            }
        }

        private (VisualElement tile, Label letter, Label pattern) CreateTile(Tile tileData, int index)
        {
            var tile = new VisualElement();
            tile.AddToClassList("encounter-tile");
            tile.name = $"tile-{index}";

            var pattern = new Label("\u25C6");
            pattern.AddToClassList("encounter-tile__pattern");
            tile.Add(pattern);

            var letter = new Label("");
            letter.AddToClassList("encounter-tile__letter");
            tile.Add(letter);

            return (tile, letter, pattern);
        }

        private VisualElement CreateEmptyTile()
        {
            var tile = new VisualElement();
            tile.AddToClassList("encounter-tile");
            tile.AddToClassList("encounter-tile--empty");
            return tile;
        }

        private void ClearTileBoard()
        {
            _tileGrid?.Clear();
            _tileElements.Clear();
            _tileLetterLabels.Clear();
            _tilePatternLabels.Clear();
        }

        private void RevealTileImmediate(int index)
        {
            if (index < 0 || index >= _tileElements.Count || _tileElements[index] == null) return;

            var tile = _tileElements[index];
            tile.AddToClassList("encounter-tile--revealed");

            var letter = _tileLetterLabels[index];
            if (letter != null)
                letter.text = GetTileChar(index);
        }

        private void RevealTilesAnimated(List<int> positions)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                int idx = positions[i];
                if (idx < 0 || idx >= _tileElements.Count || _tileElements[idx] == null) continue;

                long delay = (long)(i * 50);
                int capturedIdx = idx;

                _root.schedule.Execute(() =>
                {
                    // Flash phase
                    var tile = _tileElements[capturedIdx];
                    if (tile == null) return;

                    tile.AddToClassList("encounter-tile--flash");

                    _root.schedule.Execute(() =>
                    {
                        tile.RemoveFromClassList("encounter-tile--flash");
                        RevealTileImmediate(capturedIdx);
                        tile.AddToClassList("encounter-tile--glow");

                        // Remove glow after a moment
                        _root.schedule.Execute(() =>
                        {
                            tile.RemoveFromClassList("encounter-tile--glow");
                        }).ExecuteLater(2000);
                    }).ExecuteLater((long)tileRevealDurationMs);
                }).ExecuteLater(delay);
            }
        }

        private void RevealAllAnimated()
        {
            for (int i = 0; i < _tileElements.Count; i++)
            {
                if (_tileElements[i] == null) continue;
                int capturedIdx = i;
                long delay = (long)(i * cascadeDelayMs);

                _root.schedule.Execute(() =>
                {
                    var tile = _tileElements[capturedIdx];
                    if (tile == null) return;

                    tile.AddToClassList("encounter-tile--flash");
                    _root.schedule.Execute(() =>
                    {
                        tile.RemoveFromClassList("encounter-tile--flash");
                        RevealTileImmediate(capturedIdx);
                    }).ExecuteLater((long)(tileRevealDurationMs / 2));
                }).ExecuteLater(delay);
            }
        }

        private string GetTileChar(int index)
        {
            if (_encounter?.Board?.Tiles != null && index < _encounter.Board.Tiles.Length)
                return _encounter.Board.Tiles[index].Character.ToString().ToUpperInvariant();
            return "?";
        }

        private static List<List<int>> SplitIntoWords(Tile[] tiles)
        {
            var words = new List<List<int>>();
            var current = new List<int>();

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].Type == TileType.Space)
                {
                    if (current.Count > 0)
                    {
                        words.Add(current);
                        current = new List<int>();
                    }
                }
                else
                {
                    current.Add(i);
                }
            }
            if (current.Count > 0) words.Add(current);

            return words;
        }

        // ── Guessed Letters ─────────────────────────────────

        private void BuildGuessedLetters()
        {
            if (_guessedLetters == null) return;
            _guessedLetters.Clear();
            _letterLabels.Clear();

            for (char c = 'A'; c <= 'Z'; c++)
            {
                var label = new Label(c.ToString());
                label.AddToClassList("encounter-letter");
                _guessedLetters.Add(label);
                _letterLabels[char.ToLowerInvariant(c)] = label;
            }
        }

        private void MarkLetterGuessed(char letter, bool wasInPhrase)
        {
            char lower = char.ToLowerInvariant(letter);
            if (_letterLabels.TryGetValue(lower, out var label))
            {
                label.RemoveFromClassList("encounter-letter--hit");
                label.RemoveFromClassList("encounter-letter--miss");
                label.AddToClassList(wasInPhrase ? "encounter-letter--hit" : "encounter-letter--miss");
            }
        }

        private void ResetGuessedLetters()
        {
            foreach (var kvp in _letterLabels)
            {
                kvp.Value.RemoveFromClassList("encounter-letter--hit");
                kvp.Value.RemoveFromClassList("encounter-letter--miss");
            }
        }

        // ── NPC Portrait ────────────────────────────────────

        private void SetNPC(string npcId, bool isBoss)
        {
            _npcId = npcId;
            _npcIsBoss = isBoss;
            _baseExpression = NPCExpression.Neutral;
            SetPortraitExpression(NPCExpression.Neutral, temporary: false);
        }

        private void SetPortraitExpression(NPCExpression expression, bool temporary)
        {
            if (_npcPortraitLabel == null) return;

            string art = NPCPortraitData.GetPortrait(_npcId, expression);
            _npcPortraitLabel.text = art;

            if (temporary)
            {
                _expressionRevertSchedule?.Pause();
                _expressionRevertSchedule = _root.schedule.Execute(() =>
                {
                    SetPortraitExpression(_baseExpression, temporary: false);
                }).ExecuteLater(2000);
            }
        }

        // ── Boss UI ─────────────────────────────────────────

        private void ApplyBossUI()
        {
            _npcNameLabel?.AddToClassList("encounter-screen__npc-name--boss");
            _clueTextLabel?.AddToClassList("encounter-screen__clue-text--boss");
        }

        private void ResetBossUI()
        {
            _npcNameLabel?.RemoveFromClassList("encounter-screen__npc-name--boss");
            _clueTextLabel?.RemoveFromClassList("encounter-screen__clue-text--boss");
        }

        // ── Result Overlay ──────────────────────────────────

        private void ShowResult(EncounterEndedEvent evt)
        {
            if (_resultOverlay == null) return;

            _resultOverlay.AddToClassList("encounter-screen__result-overlay--visible");

            // Banner
            if (_resultBanner != null)
            {
                _resultBanner.text = ASCIIBanners.GetResultBanner(evt.Won);
                _resultBanner.RemoveFromClassList("encounter-screen__result-title--win");
                _resultBanner.RemoveFromClassList("encounter-screen__result-title--loss");
                _resultBanner.AddToClassList(evt.Won ? "encounter-screen__result-title--win" : "encounter-screen__result-title--loss");
            }

            // Title
            if (_resultTitle != null)
            {
                string bossTag = evt.IsBoss ? "BOSS " : "";
                _resultTitle.text = evt.Won ? $"{bossTag}WORD FOUND!" : $"{bossTag}WORD LOST";
                _resultTitle.RemoveFromClassList("encounter-screen__result-title--win");
                _resultTitle.RemoveFromClassList("encounter-screen__result-title--loss");
                _resultTitle.AddToClassList(evt.Won ? "encounter-screen__result-title--win" : "encounter-screen__result-title--loss");
            }

            // Details (typewriter with delay)
            if (_resultDetails != null)
            {
                string word = evt.TargetWord.ToUpperInvariant();
                string details = $"The word was: {word}\nGuesses used: {evt.GuessCount}";
                if (evt.Won && evt.Score > 0)
                    details += $"\nScore: +{evt.Score}";

                _resultDetails.text = "";
                string capturedDetails = details;
                _root.schedule.Execute(() =>
                {
                    StartTypewriter(_resultDetails, capturedDetails);
                }).ExecuteLater(600);
            }

            // Continue button (hidden for boss)
            if (_continueBtn != null)
            {
                if (evt.IsBoss)
                {
                    _continueBtn.style.display = DisplayStyle.None;
                }
                else
                {
                    _continueBtn.style.display = DisplayStyle.Flex;
                    _continueBtn.RemoveFromClassList("encounter-screen__continue-btn--visible");
                    _root.schedule.Execute(() =>
                    {
                        _continueBtn.AddToClassList("encounter-screen__continue-btn--visible");
                    }).ExecuteLater(1200);
                }
            }
        }

        private void HideResult()
        {
            _resultOverlay?.RemoveFromClassList("encounter-screen__result-overlay--visible");
            _continueBtn?.RemoveFromClassList("encounter-screen__continue-btn--visible");
        }

        // ── Status ──────────────────────────────────────────

        private void UpdateStatus()
        {
            if (_encounter == null) return;

            if (_hpTextLabel != null)
                _hpTextLabel.text = $"{_encounter.CurrentHP}/{_encounter.MaxHP}";

            _targetHPFill = _encounter.MaxHP > 0 ? (float)_encounter.CurrentHP / _encounter.MaxHP : 0f;
            if (_currentHPFill == 0f)
                _currentHPFill = _targetHPFill;

            UpdateHPBarVisual();

            if (_goldTextLabel != null && RunManager.Instance != null)
            {
                _goldTextLabel.text = $"{RunManager.Instance.Gold}g";
                _lastDisplayedGold = RunManager.Instance.Gold;
            }

            if (_guessesTextLabel != null)
                _guessesTextLabel.text = $"Guesses: {_encounter.GuessesRemaining}";

            if (_scoreTextLabel != null && RunManager.Instance != null)
            {
                _scoreTextLabel.text = $"Score: {RunManager.Instance.Score}";
                _lastDisplayedScore = RunManager.Instance.Score;
            }
        }

        private void UpdateTomeInfo()
        {
            if (_tomeInfoLabel == null) return;
            if (TomeManager.Instance?.TomeSystem == null)
            {
                _tomeInfoLabel.text = "";
                return;
            }

            var tomes = TomeManager.Instance.TomeSystem.GetEquippedTomes();
            if (tomes.Count == 0)
            {
                _tomeInfoLabel.text = "No tomes equipped";
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var tome in tomes)
            {
                if (sb.Length > 0) sb.Append("\n");
                sb.Append($"* {tome.TomeName}");
            }
            _tomeInfoLabel.text = sb.ToString();
        }

        // ── HP Bar Lerp ─────────────────────────────────────

        private void StartHPLerp()
        {
            _hpLerpSchedule = _root.schedule.Execute(() =>
            {
                if (Mathf.Abs(_currentHPFill - _targetHPFill) > 0.001f)
                {
                    _currentHPFill = Mathf.Lerp(_currentHPFill, _targetHPFill, Time.deltaTime * hpBarLerpSpeed);
                    UpdateHPBarVisual();
                }
            }).Every(16); // ~60fps
        }

        private void UpdateHPBarVisual()
        {
            if (_hpBarFill == null) return;

            _hpBarFill.style.width = Length.Percent(_currentHPFill * 100f);

            // Update color class based on HP level
            _hpBarFill.RemoveFromClassList("encounter-screen__hp-bar-fill--low");
            _hpBarFill.RemoveFromClassList("encounter-screen__hp-bar-fill--critical");

            if (_currentHPFill < 0.25f)
                _hpBarFill.AddToClassList("encounter-screen__hp-bar-fill--critical");
            else if (_currentHPFill < 0.5f)
                _hpBarFill.AddToClassList("encounter-screen__hp-bar-fill--low");
        }

        // ── Typewriter ──────────────────────────────────────

        private void StartTypewriter(Label target, string fullText)
        {
            StopTypewriter();
            _typewriterFullText = fullText;
            _typewriterIndex = 0;
            target.text = "";

            _typewriterSchedule = _root.schedule.Execute(() =>
            {
                if (_typewriterIndex < _typewriterFullText.Length)
                {
                    target.text += _typewriterFullText[_typewriterIndex];
                    _typewriterIndex++;
                }
                else
                {
                    StopTypewriter();
                }
            }).Every((long)typewriterSpeedMs);
        }

        private void StopTypewriter()
        {
            _typewriterSchedule?.Pause();
            _typewriterSchedule = null;
        }

        // ── Flash ───────────────────────────────────────────

        private void Flash(string flashClass)
        {
            if (_flashOverlay == null) return;

            // Remove all flash classes
            _flashOverlay.RemoveFromClassList("encounter-screen__flash-overlay--damage");
            _flashOverlay.RemoveFromClassList("encounter-screen__flash-overlay--success");
            _flashOverlay.RemoveFromClassList("encounter-screen__flash-overlay--boss");

            _flashOverlay.AddToClassList(flashClass);

            _root.schedule.Execute(() =>
            {
                _flashOverlay.RemoveFromClassList(flashClass);
            }).ExecuteLater((long)flashDurationMs);
        }

        // ── Bargain ──────────────────────────────────────────

        private void OnBargainOffered(BargainOfferedEvent evt)
        {
            if (_bargainOverlay == null) return;

            _bargainActive = true;
            _pendingBargainEffect = evt.Effect;

            if (_bargainFlavorLabel != null)
                _bargainFlavorLabel.text = $"\"{evt.NpcFlavorText}\"";
            if (_bargainDescriptionLabel != null)
                _bargainDescriptionLabel.text = evt.Description;
            if (_bargainCostLabel != null)
                _bargainCostLabel.text = string.IsNullOrEmpty(evt.CostDescription) ? "" : $"Cost: {evt.CostDescription}";
            if (_bargainTimerFill != null)
                _bargainTimerFill.style.width = Length.Percent(100);

            _bargainOverlay.AddToClassList("encounter-screen__bargain-overlay--visible");

            // Start timer fill animation
            var bargainSystem = FindAnyObjectByType<MoodBargainSystem>();
            if (bargainSystem != null)
            {
                _bargainTimerSchedule?.Pause();
                _bargainTimerSchedule = _root.schedule.Execute(() =>
                {
                    if (!_bargainActive || bargainSystem == null) return;
                    float pct = bargainSystem.Duration > 0
                        ? (bargainSystem.TimeRemaining / bargainSystem.Duration) * 100f
                        : 0f;
                    if (_bargainTimerFill != null)
                        _bargainTimerFill.style.width = Length.Percent(Mathf.Max(0, pct));
                }).Every(250);
            }
        }

        private void OnBargainAcceptClicked()
        {
            if (!_bargainActive) return;
            _bargainActive = false;

            EventBus.Instance.Publish(new BargainAcceptedEvent { Effect = _pendingBargainEffect });
            HideBargain();
        }

        private void OnBargainExpired(BargainExpiredEvent evt)
        {
            _bargainActive = false;
            HideBargain();
        }

        private void HideBargain()
        {
            _bargainActive = false;
            _bargainTimerSchedule?.Pause();
            _bargainTimerSchedule = null;
            _bargainOverlay?.RemoveFromClassList("encounter-screen__bargain-overlay--visible");
        }
    }
}
