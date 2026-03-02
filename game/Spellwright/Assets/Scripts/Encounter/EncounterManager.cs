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
    /// guess submission, scoring, HP tracking, and event publishing.
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

        // ── Encounter Lifecycle ──────────────────────────────

        /// <summary>
        /// Starts a new encounter: selects a word, publishes the start event,
        /// and requests the first clue.
        /// </summary>
        /// <param name="pool">The word pool to draw from.</param>
        /// <param name="npc">The NPC data for prompt generation.</param>
        /// <param name="usedWords">Words already used in this run (to avoid repeats).</param>
        /// <param name="difficulty">Difficulty level (1-5) for word filtering.</param>
        public async void StartEncounter(WordPoolSO pool, NPCDataSO npc, List<string> usedWords, int difficulty)
        {
            StartEncounterInternal(pool, npc, usedWords, pool.GetWordsByDifficulty(difficulty));
            await PostStartEncounter();
        }

        /// <summary>
        /// Starts a boss encounter using a difficulty range for word selection.
        /// </summary>
        public async void StartEncounter(WordPoolSO pool, NPCDataSO npc, List<string> usedWords, int minDifficulty, int maxDifficulty)
        {
            StartEncounterInternal(pool, npc, usedWords, pool.GetWordsByDifficultyRange(minDifficulty, maxDifficulty));
            await PostStartEncounter();
        }

        private void StartEncounterInternal(WordPoolSO pool, NPCDataSO npc, List<string> usedWords, List<WordEntry> wordCandidates)
        {
            // Discard any pending pre-generated clue
            DiscardPreGeneratedClue();

            // Filter out already-used words
            var candidates = wordCandidates
                .Where(w => !usedWords.Contains(w.Word))
                .ToList();

            if (candidates.Count == 0)
            {
                Debug.LogWarning("[EncounterManager] No words available for this difficulty/pool.");
                return;
            }

            // Random selection
            _targetWord = candidates[Random.Range(0, candidates.Count)];
            _npcData = npc.ToPromptData();
            _isBoss = npc.isBoss;
            _clueNumber = 0;
            _guesses.Clear();
            _usedWords = usedWords;
            _isActive = true;
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
                NPC = _npcData
            });

            // Apply Tome HP bonuses (effects write modifiers during event dispatch above)
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

            Debug.Log($"[EncounterManager] Encounter started: \"{_targetWord.Word}\" ({_targetWord.Category}, difficulty {_targetWord.Difficulty})");

            // Boss intro event before first clue
            if (_isBoss)
            {
                EventBus.Instance.Publish(new BossIntroEvent
                {
                    BossName = _npcData.DisplayName,
                    IntroText = $"{_npcData.DisplayName} awakens..."
                });
            }

            // Start pre-generating the first clue immediately
            TryPreGenerateNextClue();

            await RequestNextClue();
        }

        /// <summary>
        /// Requests the next clue from the LLM (or fallback).
        /// Uses a pre-generated clue if one is available and context matches.
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

            // Check if we have a valid pre-generated clue for the current word
            if (_preGeneratedClue != null && _preGenTargetWord == _targetWord.Word)
            {
                Debug.Log("[EncounterManager] Using pre-generated clue.");
                clue = await _preGeneratedClue;
                _preGeneratedClue = null;
                _preGenTargetWord = null;
            }

            // If no valid pre-gen, generate normally
            if (clue == null)
            {
                // Discard stale pre-gen if context changed
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

            // Try to pre-generate the next clue in the background
            TryPreGenerateNextClue();
        }

        /// <summary>
        /// Submits a player guess and processes the result.
        /// </summary>
        /// <returns>The evaluated <see cref="GuessResult"/>.</returns>
        public async Task<GuessResult> SubmitGuess(string guess)
        {
            if (!_isActive) return null;

            var language = gameConfig != null ? gameConfig.language : Data.GameLanguage.English;
            var result = GuessProcessor.Process(guess, _targetWord.Word, language);
            _guesses.Add(result.GuessedWord);

            EventBus.Instance.Publish(new GuessSubmittedEvent
            {
                Guess = result.GuessedWord,
                Result = result
            });

            if (result.IsCorrect)
            {
                int score = CalculateScore(_targetWord.Word.Length, _guesses.Count);
                _usedWords?.Add(_targetWord.Word);
                EndEncounter(true, score);
                return result;
            }

            if (!result.IsValidWord)
            {
                // Invalid words don't count as a guess attempt
                _guesses.RemoveAt(_guesses.Count - 1);
                return result;
            }

            // Wrong but valid guess — deduct HP (reduced by Tome effects)
            int hpLoss = gameConfig != null ? gameConfig.hpLostPerWrongGuess : 15;
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

            // Check if max guesses reached
            int maxGuesses = gameConfig != null ? gameConfig.maxGuessesPerEncounter : 6;
            if (_guesses.Count >= maxGuesses)
            {
                EndEncounter(false, 0);
                return result;
            }

            // Request next clue for the next attempt
            await RequestNextClue();
            return result;
        }

        /// <summary>
        /// Ends the encounter and publishes the result event.
        /// </summary>
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

        /// <summary>
        /// Starts generating the next clue in the background if we haven't
        /// reached the max clue count yet.
        /// </summary>
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

        /// <summary>
        /// Discards any pending pre-generated clue.
        /// </summary>
        private void DiscardPreGeneratedClue()
        {
            _preGeneratedClue = null;
            _preGenTargetWord = null;
        }

        // ── Scoring ──────────────────────────────────────────

        /// <summary>
        /// Calculates the score for a correct guess.
        /// Base = wordLength x 10; multiplier: 1st=3x, 2nd=2x, 3rd=1.5x, 4+=1x.
        /// </summary>
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

        /// <summary>
        /// Resets HP to the configured starting value. Call at the start of a run.
        /// </summary>
        public void ResetHP()
        {
            _maxHP = gameConfig != null ? gameConfig.startingHP : 100;
            _currentHP = _maxHP;
        }

        /// <summary>
        /// Sets HP directly. Useful for test UI or run state restoration.
        /// </summary>
        public void SetHP(int current, int max)
        {
            _currentHP = current;
            _maxHP = max;
        }
    }
}
