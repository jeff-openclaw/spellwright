using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.LLM;
using Spellwright.Run;
using Spellwright.ScriptableObjects;
using Spellwright.Tomes;
using UnityEngine;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Manages the word-guessing encounter lifecycle: word selection, clue generation,
    /// guess submission (letter or phrase), board state, scoring, HP tracking, and event publishing.
    /// </summary>
    public class EncounterManager : MonoBehaviour
    {
        [SerializeField] private GameConfigSO gameConfig;

        // ── State ────────────────────────────────────────────
        private WordEntry _targetWord;
        private NPCPromptData _npcData;
        private int _clueNumber;
        private List<string> _guesses = new List<string>();
        private List<string> _usedWords;
        private bool _isActive;
        private bool _isBoss;
        private int _currentHP;
        private int _maxHP;
        private BoardState _boardState;
        private bool _isFirstEncounter;

        // ── Pre-generation ───────────────────────────────────
        private Task<ClueResponse> _preGeneratedClue;
        private string _preGenTargetWord;

        // ── Properties ───────────────────────────────────────
        public bool IsActive => _isActive;
        public int TargetWordLength => _targetWord?.LetterCount ?? 0;
        public int CurrentClueNumber => _clueNumber;
        public int GuessesRemaining => gameConfig != null
            ? gameConfig.maxGuessesPerEncounter - _guesses.Count
            : 0;
        public int CurrentHP => _currentHP;
        public int MaxHP => _maxHP;
        public BoardState Board => _boardState;

        // ── Lifecycle ────────────────────────────────────────

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<TomeRevealRequestEvent>(OnTomeRevealRequest);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<TomeRevealRequestEvent>(OnTomeRevealRequest);
        }

        // ── Encounter Lifecycle ──────────────────────────────

        public async void StartEncounter(WordPoolSO pool, NPCDataSO npc, List<string> usedWords, int difficulty)
        {
            StartEncounterInternal(pool, npc, usedWords, pool.GetWordsByDifficulty(difficulty));
            await PostStartEncounter();
        }

        public async void StartEncounter(WordPoolSO pool, NPCDataSO npc, List<string> usedWords, int minDifficulty, int maxDifficulty)
        {
            StartEncounterInternal(pool, npc, usedWords, pool.GetWordsByDifficultyRange(minDifficulty, maxDifficulty));
            await PostStartEncounter();
        }

        /// <summary>Starts an encounter with pre-filtered word candidates (used for tutorial/first encounter).</summary>
        public async void StartEncounter(List<WordEntry> candidates, NPCDataSO npc, List<string> usedWords, bool isFirstEncounter = false)
        {
            _isFirstEncounter = isFirstEncounter;
            StartEncounterInternal(null, npc, usedWords, candidates);
            await PostStartEncounter();
        }

        private void StartEncounterInternal(WordPoolSO pool, NPCDataSO npc, List<string> usedWords, List<WordEntry> wordCandidates)
        {
            DiscardPreGeneratedClue();
            // Reset first-encounter flag unless explicitly set before this call
            if (pool != null) _isFirstEncounter = false;

            var candidates = wordCandidates
                .Where(w => !usedWords.Contains(w.Word))
                .ToList();

            if (candidates.Count == 0)
            {
                Debug.LogWarning("[EncounterManager] No words available for this difficulty/pool.");
                return;
            }

            _targetWord = candidates[Random.Range(0, candidates.Count)];
            _npcData = npc.ToPromptData();
            _isBoss = npc.isBoss;
            _clueNumber = 0;
            _guesses.Clear();
            _usedWords = usedWords;
            _isActive = true;

            // Create the tile board
            _boardState = new BoardState(_targetWord.Word);
        }

        private async Task PostStartEncounter()
        {
            if (_targetWord == null) return;

            // Initialize HP from RunManager if available, otherwise from config
            if (RunManager.Instance != null && RunManager.Instance.IsRunActive)
            {
                _currentHP = RunManager.Instance.CurrentHP;
                _maxHP = RunManager.Instance.MaxHP;
            }
            else if (_maxHP <= 0)
            {
                _maxHP = gameConfig != null ? gameConfig.startingHP : 100;
                _currentHP = _maxHP;
            }

            EventBus.Instance.Publish(new EncounterStartedEvent
            {
                TargetWord = _targetWord.Word,
                Category = _targetWord.Category,
                NPC = _npcData,
                IsPhrase = _targetWord.IsPhrase,
                WordCount = _targetWord.WordCount,
                LetterCount = _targetWord.LetterCount,
                IsFirstEncounter = _isFirstEncounter
            });

            // Apply Tome HP bonuses
            if (TomeManager.Instance != null)
            {
                int hpBonus = TomeManager.Instance.TomeSystem.PendingMaxHPBonus;
                if (hpBonus > 0)
                {
                    int oldHP = _currentHP;
                    _maxHP += hpBonus;
                    _currentHP += hpBonus;
                    Debug.Log($"[EncounterManager] Tome HP bonus applied: +{hpBonus} (now {_currentHP}/{_maxHP})");

                    EventBus.Instance.Publish(new HPChangedEvent
                    {
                        OldHP = oldHP,
                        NewHP = _currentHP,
                        MaxHP = _maxHP
                    });
                }
            }

            Debug.Log($"[EncounterManager] Encounter started: \"{_targetWord.Word}\" ({_targetWord.Category}, difficulty {_targetWord.Difficulty}, phrase={_targetWord.IsPhrase}, firstEncounter={_isFirstEncounter})");

            if (_isBoss)
            {
                EventBus.Instance.Publish(new BossIntroEvent
                {
                    BossName = _npcData.DisplayName,
                    IntroText = $"{_npcData.DisplayName} awakens..."
                });
            }

            // Tutorial: reveal first and last letters for the first encounter
            if (_isFirstEncounter && _boardState != null)
            {
                var tutorialRevealed = new List<int>();
                int first = _boardState.RevealFirstLetter();
                if (first >= 0) tutorialRevealed.Add(first);
                int last = _boardState.RevealLastLetter();
                if (last >= 0) tutorialRevealed.Add(last);

                if (tutorialRevealed.Count > 0)
                {
                    Debug.Log($"[EncounterManager] Tutorial: revealed first+last letters ({tutorialRevealed.Count} tiles)");
                    EventBus.Instance.Publish(new LetterRevealedEvent
                    {
                        RevealedPositions = tutorialRevealed,
                        RevealedLetter = '\0',
                        Source = "tutorial"
                    });
                }
            }

            TryPreGenerateNextClue();
            await RequestNextClue();
        }

        /// <summary>
        /// Requests the next clue from the LLM (or fallback).
        /// After receiving the clue, reveals letters on the board per config.
        /// </summary>
        public async Task RequestNextClue()
        {
            _clueNumber++;

            if (LLMManager.Instance == null || !LLMManager.Instance.IsReady)
            {
                Debug.LogWarning("[EncounterManager] LLMManager not ready.");
                return;
            }

            ClueResponse clue = null;

            if (_preGeneratedClue != null && _preGenTargetWord == _targetWord.Word)
            {
                Debug.Log("[EncounterManager] Using pre-generated clue.");
                clue = await _preGeneratedClue;
                _preGeneratedClue = null;
                _preGenTargetWord = null;
            }

            if (clue == null)
            {
                DiscardPreGeneratedClue();

                clue = await LLMManager.Instance.GenerateClueAsync(
                    _npcData,
                    _targetWord.Word,
                    _targetWord.Category,
                    _clueNumber,
                    _guesses,
                    TomeManager.Instance?.GetActiveEffectNames() ?? new List<string>()
                );
            }

            EventBus.Instance.Publish(new ClueReceivedEvent
            {
                Clue = clue,
                ClueNumber = _clueNumber
            });

            // Reveal letters as clue bonus
            if (_boardState != null && gameConfig != null)
            {
                int toReveal = gameConfig.lettersRevealedPerClue;
                var revealed = new List<int>();
                for (int i = 0; i < toReveal; i++)
                {
                    int idx = _boardState.RevealRandomHidden();
                    if (idx >= 0) revealed.Add(idx);
                }
                if (revealed.Count > 0)
                {
                    EventBus.Instance.Publish(new LetterRevealedEvent
                    {
                        RevealedPositions = revealed,
                        RevealedLetter = '\0',
                        Source = "clue"
                    });

                    // Check auto-win after clue reveals
                    if (_boardState.IsFullyRevealed())
                    {
                        int score = CalculateScore(_targetWord.LetterCount, _guesses.Count + 1);
                        _usedWords?.Add(_targetWord.Word);
                        EndEncounter(true, score);
                    }
                }
            }

            TryPreGenerateNextClue();
        }

        /// <summary>
        /// Submits a player guess and processes the result.
        /// Letter guesses that hit do NOT consume a guess slot. Only misses and phrase attempts count.
        /// </summary>
        public async Task<GuessResult> SubmitGuess(string guess)
        {
            if (!_isActive) return null;

            var language = gameConfig != null ? gameConfig.language : GameLanguage.English;
            var guessType = GuessProcessor.DetermineGuessType(guess);

            GuessResult result;

            if (guessType == GuessType.Letter)
            {
                char letter = guess.Trim().ToLowerInvariant()[0];

                // Check if already guessed
                if (_boardState != null && _boardState.IsLetterAlreadyGuessed(letter))
                {
                    bool ro = language == GameLanguage.Romanian;
                    return new GuessResult
                    {
                        GuessedWord = letter.ToString(),
                        GuessType = GuessType.Letter,
                        GuessedLetter = letter,
                        IsCorrect = false,
                        IsValidWord = false,
                        IsLetterAlreadyGuessed = true,
                        Feedback = ro ? "Ai incercat deja aceasta litera!" : "You already guessed that letter!"
                    };
                }

                result = GuessProcessor.ProcessLetterGuess(letter, _targetWord.Word, language);

                // Mark letter as guessed on the board
                _boardState?.MarkLetterGuessed(letter);

                if (result.IsLetterInPhrase)
                {
                    // Hit — reveal tiles, do NOT consume a guess, but costs HP
                    if (_boardState != null)
                    {
                        int revealed = _boardState.RevealAllOfLetter(letter);
                        var positions = _boardState.GetPositionsOfLetter(letter);

                        EventBus.Instance.Publish(new LetterRevealedEvent
                        {
                            RevealedPositions = positions,
                            RevealedLetter = letter,
                            Source = "guess"
                        });
                    }

                    EventBus.Instance.Publish(new GuessSubmittedEvent
                    {
                        Guess = result.GuessedWord,
                        Result = result
                    });

                    // Correct letter still costs HP (buying information has a price)
                    ApplyHPLoss(gameConfig != null ? gameConfig.hpCostPerCorrectLetter : 5);

                    // Check auto-win
                    if (_boardState != null && _boardState.IsFullyRevealed())
                    {
                        result.IsCorrect = true;
                        int score = CalculateScore(_targetWord.LetterCount, _guesses.Count + 1);
                        _usedWords?.Add(_targetWord.Word);
                        EndEncounter(true, score);
                    }

                    return result;
                }
                else
                {
                    // Miss — costs a guess, HP loss, consolation reveal
                    _guesses.Add(result.GuessedWord);

                    EventBus.Instance.Publish(new GuessSubmittedEvent
                    {
                        Guess = result.GuessedWord,
                        Result = result
                    });

                    ApplyHPLoss(gameConfig != null ? gameConfig.hpLostPerWrongLetter : 2);

                    // Consolation reveal
                    if (gameConfig != null && gameConfig.consolationRevealOnWrongLetter && _boardState != null)
                    {
                        int idx = _boardState.RevealRandomHidden();
                        if (idx >= 0)
                        {
                            EventBus.Instance.Publish(new LetterRevealedEvent
                            {
                                RevealedPositions = new List<int> { idx },
                                RevealedLetter = '\0',
                                Source = "consolation"
                            });

                            // Check auto-win after consolation reveal
                            if (_boardState.IsFullyRevealed())
                            {
                                int score = CalculateScore(_targetWord.LetterCount, _guesses.Count);
                                _usedWords?.Add(_targetWord.Word);
                                EndEncounter(true, score);
                                return result;
                            }
                        }
                    }

                    // Letter misses do NOT request a new clue — just return
                    return result;
                }
            }
            else
            {
                // Phrase guess
                result = GuessProcessor.ProcessPhraseGuess(guess, _targetWord.Word, language);

                if (!result.IsValidWord)
                {
                    // Invalid phrase — don't count
                    EventBus.Instance.Publish(new GuessSubmittedEvent
                    {
                        Guess = result.GuessedWord,
                        Result = result
                    });
                    return result;
                }

                _guesses.Add(result.GuessedWord);

                EventBus.Instance.Publish(new GuessSubmittedEvent
                {
                    Guess = result.GuessedWord,
                    Result = result
                });

                if (result.IsCorrect)
                {
                    // Reveal all tiles
                    if (_boardState != null)
                    {
                        var positions = _boardState.RevealAll();
                        EventBus.Instance.Publish(new LetterRevealedEvent
                        {
                            RevealedPositions = positions,
                            RevealedLetter = '\0',
                            Source = "guess"
                        });
                    }

                    int score = CalculateScore(_targetWord.LetterCount, _guesses.Count);
                    _usedWords?.Add(_targetWord.Word);
                    EndEncounter(true, score);
                    return result;
                }

                // Wrong phrase — HP loss
                ApplyHPLoss(gameConfig != null ? gameConfig.hpLostPerWrongPhrase : 5);
            }

            // Check if max guesses reached
            int maxGuesses = gameConfig != null ? gameConfig.maxGuessesPerEncounter : 6;
            if (_guesses.Count >= maxGuesses)
            {
                EndEncounter(false, 0);
                return result;
            }

            // Request next clue
            await RequestNextClue();
            return result;
        }

        // ── HP Loss Helper ──────────────────────────────────

        private void ApplyHPLoss(int baseLoss)
        {
            int hpLoss = baseLoss;
            if (TomeManager.Instance != null)
            {
                int reduction = TomeManager.Instance.TomeSystem.PendingHPLossReduction;
                if (reduction > 0)
                    hpLoss = Mathf.Max(0, hpLoss - reduction);
            }
            int oldHP = _currentHP;
            _currentHP = Mathf.Max(0, _currentHP - hpLoss);

            EventBus.Instance.Publish(new HPChangedEvent
            {
                OldHP = oldHP,
                NewHP = _currentHP,
                MaxHP = _maxHP
            });
        }

        // ── Tome Reveal Handler ─────────────────────────────

        private void OnTomeRevealRequest(TomeRevealRequestEvent evt)
        {
            if (_boardState == null || !_isActive) return;

            List<int> revealed;
            switch (evt.Type)
            {
                case RevealType.FirstLetter:
                    int idx = _boardState.RevealFirstLetter();
                    revealed = idx >= 0 ? new List<int> { idx } : new List<int>();
                    break;
                case RevealType.Vowels:
                    revealed = _boardState.RevealAllVowels();
                    break;
                case RevealType.SpecificLetters:
                    revealed = _boardState.RevealSpecificLetters(evt.Letters ?? new List<char>());
                    break;
                case RevealType.Random:
                    revealed = new List<int>();
                    for (int i = 0; i < evt.Count; i++)
                    {
                        int ri = _boardState.RevealRandomHidden();
                        if (ri >= 0) revealed.Add(ri);
                    }
                    break;
                default:
                    revealed = new List<int>();
                    break;
            }

            if (revealed.Count > 0)
            {
                EventBus.Instance.Publish(new LetterRevealedEvent
                {
                    RevealedPositions = revealed,
                    RevealedLetter = '\0',
                    Source = "tome"
                });

                // Check auto-win after tome reveals
                if (_boardState.IsFullyRevealed())
                {
                    int score = CalculateScore(_targetWord.LetterCount, _guesses.Count + 1);
                    _usedWords?.Add(_targetWord.Word);
                    EndEncounter(true, score);
                }
            }
        }

        // ── End Encounter ───────────────────────────────────

        private void EndEncounter(bool won, int score = 0)
        {
            _isActive = false;
            DiscardPreGeneratedClue();

            EventBus.Instance.Publish(new EncounterEndedEvent
            {
                Won = won,
                IsBoss = _isBoss,
                TargetWord = _targetWord.Word,
                GuessCount = _guesses.Count,
                Score = score
            });
        }

        // ── Clue Pre-generation ──────────────────────────────

        private void TryPreGenerateNextClue()
        {
            if (!_isActive) return;
            if (LLMManager.Instance == null || !LLMManager.Instance.IsReady) return;

            int maxClues = gameConfig != null ? gameConfig.maxGuessesPerEncounter : 6;
            int nextClueNumber = _clueNumber + 1;
            if (nextClueNumber > maxClues) return;

            _preGenTargetWord = _targetWord.Word;
            var guessSnapshot = new List<string>(_guesses);

            Debug.Log($"[EncounterManager] Pre-generating clue #{nextClueNumber} for \"{_preGenTargetWord}\"");

            _preGeneratedClue = LLMManager.Instance.GenerateClueAsync(
                _npcData,
                _targetWord.Word,
                _targetWord.Category,
                nextClueNumber,
                guessSnapshot,
                TomeManager.Instance?.GetActiveEffectNames() ?? new List<string>()
            );
        }

        private void DiscardPreGeneratedClue()
        {
            _preGeneratedClue = null;
            _preGenTargetWord = null;
        }

        // ── Scoring ──────────────────────────────────────────

        public static int CalculateScore(int wordLength, int guessNumber)
        {
            int baseScore = wordLength * 10;
            float multiplier = guessNumber switch
            {
                1 => 3.0f,
                2 => 2.0f,
                3 => 1.5f,
                _ => 1.0f
            };
            return (int)(baseScore * multiplier);
        }

        public void ResetHP()
        {
            _maxHP = gameConfig != null ? gameConfig.startingHP : 100;
            _currentHP = _maxHP;
        }

        public void SetHP(int current, int max)
        {
            _currentHP = current;
            _maxHP = max;
        }
    }
}
