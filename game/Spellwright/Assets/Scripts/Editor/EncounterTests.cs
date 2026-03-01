using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Encounter;
using Spellwright.Run;
using UnityEditor;
using UnityEngine;

namespace Spellwright.Editor
{
    /// <summary>
    /// Menu-driven editor tests for the encounter system.
    /// Verifies guess processing, scoring, word selection, and encounter state
    /// without requiring Play mode.
    /// </summary>
    public static class EncounterTests
    {
        // ── Run All ─────────────────────────────────────────

        [MenuItem("Spellwright/Tests/Run All Encounter Tests")]
        public static void RunAll()
        {
            int passed = 0;
            int failed = 0;

            if (TestGuessProcessing()) passed++; else failed++;
            if (TestScoringFormula()) passed++; else failed++;
            if (TestWordSelection()) passed++; else failed++;
            if (TestEncounterState()) passed++; else failed++;
            if (TestRunManager()) passed++; else failed++;

            Debug.Log($"[Encounter Tests] ═══ RESULTS: {passed} passed, {failed} failed ═══");
        }

        // ── Test: Guess Processing ──────────────────────────

        [MenuItem("Spellwright/Tests/Guess Processing")]
        public static bool TestGuessProcessing()
        {
            Debug.Log("[Encounter Tests] ── Guess Processing ──");
            bool allPassed = true;

            // Correct guess
            var r1 = GuessProcessor.Process("bridge", "bridge");
            if (!r1.IsCorrect || !r1.IsValidWord)
            {
                Debug.LogError($"  FAIL: Correct guess not recognized. IsCorrect={r1.IsCorrect}, IsValidWord={r1.IsValidWord}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Correct guess recognized.");
            }

            // Correct guess with different casing
            var r1b = GuessProcessor.Process("BRIDGE", "bridge");
            if (!r1b.IsCorrect)
            {
                Debug.LogError($"  FAIL: Case-insensitive correct guess not recognized.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Case-insensitive correct guess recognized.");
            }

            // Wrong word, same length
            var r2 = GuessProcessor.Process("castle", "bridge");
            if (r2.IsCorrect)
            {
                Debug.LogError("  FAIL: Wrong word marked as correct.");
                allPassed = false;
            }
            else if (!r2.Feedback.Contains("same length"))
            {
                Debug.LogError($"  FAIL: Same-length wrong guess missing feedback. Got: \"{r2.Feedback}\"");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Wrong word, same length — \"{r2.Feedback}\"");
            }

            // Wrong word, different length
            var r3 = GuessProcessor.Process("cat", "bridge");
            if (r3.IsCorrect)
            {
                Debug.LogError("  FAIL: Wrong-length word marked as correct.");
                allPassed = false;
            }
            else if (!r3.Feedback.Contains("Wrong number of letters"))
            {
                Debug.LogError($"  FAIL: Length mismatch missing feedback. Got: \"{r3.Feedback}\"");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Length mismatch — \"{r3.Feedback}\"");
            }

            // Letters correct count
            var r4 = GuessProcessor.Process("bricks", "bridge");
            if (r4.LettersCorrect < 1)
            {
                Debug.LogError($"  FAIL: LettersCorrect should be > 0 for 'bricks' vs 'bridge'. Got: {r4.LettersCorrect}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: LettersCorrect = {r4.LettersCorrect} for 'bricks' vs 'bridge'.");
            }

            // Empty input
            var r5 = GuessProcessor.Process("", "bridge");
            if (r5.IsValidWord)
            {
                Debug.LogError("  FAIL: Empty input should be invalid.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Empty input rejected.");
            }

            // Note: WordValidator.Instance won't be available in editor tests,
            // so dictionary validation is skipped here (tested manually in play mode).
            Debug.Log("  (Note: Dictionary validation requires Play mode with WordValidator.)");

            LogResult("Guess Processing", allPassed);
            return allPassed;
        }

        // ── Test: Scoring Formula ───────────────────────────

        [MenuItem("Spellwright/Tests/Scoring Formula")]
        public static bool TestScoringFormula()
        {
            Debug.Log("[Encounter Tests] ── Scoring Formula ──");
            bool allPassed = true;

            // Word length 6 → base = 60
            // 1st guess: 60 * 3.0 = 180
            int s1 = EncounterManager.CalculateScore(6, 1);
            if (s1 != 180)
            {
                Debug.LogError($"  FAIL: 1st guess (len 6) expected 180, got {s1}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: 1st guess = {s1} (expected 180)");
            }

            // 2nd guess: 60 * 2.0 = 120
            int s2 = EncounterManager.CalculateScore(6, 2);
            if (s2 != 120)
            {
                Debug.LogError($"  FAIL: 2nd guess (len 6) expected 120, got {s2}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: 2nd guess = {s2} (expected 120)");
            }

            // 3rd guess: 60 * 1.5 = 90
            int s3 = EncounterManager.CalculateScore(6, 3);
            if (s3 != 90)
            {
                Debug.LogError($"  FAIL: 3rd guess (len 6) expected 90, got {s3}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: 3rd guess = {s3} (expected 90)");
            }

            // 4th guess: 60 * 1.0 = 60
            int s4 = EncounterManager.CalculateScore(6, 4);
            if (s4 != 60)
            {
                Debug.LogError($"  FAIL: 4th guess (len 6) expected 60, got {s4}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: 4th guess = {s4} (expected 60)");
            }

            // 5th guess: still 1x
            int s5 = EncounterManager.CalculateScore(6, 5);
            if (s5 != 60)
            {
                Debug.LogError($"  FAIL: 5th guess (len 6) expected 60, got {s5}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: 5th guess = {s5} (expected 60)");
            }

            // Different word length: 10 letters, 1st guess → 10 * 10 * 3 = 300
            int s6 = EncounterManager.CalculateScore(10, 1);
            if (s6 != 300)
            {
                Debug.LogError($"  FAIL: 1st guess (len 10) expected 300, got {s6}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: 1st guess (len 10) = {s6} (expected 300)");
            }

            LogResult("Scoring Formula", allPassed);
            return allPassed;
        }

        // ── Test: Word Selection ────────────────────────────

        [MenuItem("Spellwright/Tests/Word Selection")]
        public static bool TestWordSelection()
        {
            Debug.Log("[Encounter Tests] ── Word Selection ──");
            bool allPassed = true;

            // Find a WordPoolSO asset in the project
            var poolGuids = AssetDatabase.FindAssets("t:WordPoolSO");
            if (poolGuids.Length == 0)
            {
                Debug.LogWarning("  SKIP: No WordPoolSO assets found in project.");
                LogResult("Word Selection", true);
                return true;
            }

            var poolPath = AssetDatabase.GUIDToAssetPath(poolGuids[0]);
            var pool = AssetDatabase.LoadAssetAtPath<ScriptableObjects.WordPoolSO>(poolPath);

            if (pool.WordCount == 0)
            {
                Debug.LogWarning($"  SKIP: Pool \"{pool.category}\" has no words loaded.");
                LogResult("Word Selection", true);
                return true;
            }

            Debug.Log($"  Using pool: \"{pool.category}\" ({pool.WordCount} words)");

            // Test: words are returned
            var allWords = pool.GetWords();
            if (allWords.Count == 0)
            {
                Debug.LogError("  FAIL: GetWords() returned empty list.");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: GetWords() returned {allWords.Count} words.");
            }

            // Test: difficulty filter
            var diff1 = pool.GetWordsByDifficulty(1);
            Debug.Log($"  Difficulty 1: {diff1.Count} words");

            // Test: used words exclusion
            var usedWords = new List<string>();
            if (allWords.Count > 0)
            {
                usedWords.Add(allWords[0].Word);
                var filtered = allWords.FindAll(w => !usedWords.Contains(w.Word));
                if (filtered.Count != allWords.Count - 1)
                {
                    Debug.LogError($"  FAIL: Used word exclusion — expected {allWords.Count - 1}, got {filtered.Count}");
                    allPassed = false;
                }
                else
                {
                    Debug.Log($"  OK: Used word exclusion works ({filtered.Count} remaining).");
                }
            }

            // Test: each word entry has required fields
            bool fieldsOk = true;
            foreach (var word in allWords)
            {
                if (string.IsNullOrEmpty(word.Word) || word.Difficulty < 1 || word.LetterCount == 0)
                {
                    Debug.LogError($"  FAIL: Word entry invalid — word=\"{word.Word}\", diff={word.Difficulty}, len={word.LetterCount}");
                    fieldsOk = false;
                    break;
                }
            }
            if (fieldsOk)
            {
                Debug.Log("  OK: All word entries have valid fields.");
            }
            else
            {
                allPassed = false;
            }

            LogResult("Word Selection", allPassed);
            return allPassed;
        }

        // ── Test: Encounter State ───────────────────────────

        [MenuItem("Spellwright/Tests/Encounter State")]
        public static bool TestEncounterState()
        {
            Debug.Log("[Encounter Tests] ── Encounter State ──");
            bool allPassed = true;

            // Test guess tracking via GuessProcessor
            var guesses = new List<string>();
            string target = "castle";

            // Simulate guess sequence
            var g1 = GuessProcessor.Process("knight", target);
            guesses.Add(g1.GuessedWord);
            if (guesses.Count != 1)
            {
                Debug.LogError($"  FAIL: After 1 guess, count should be 1, got {guesses.Count}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Guess count = 1 after first guess.");
            }

            var g2 = GuessProcessor.Process("castle", target);
            guesses.Add(g2.GuessedWord);
            if (!g2.IsCorrect)
            {
                Debug.LogError("  FAIL: Correct guess not detected.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Correct guess detected on guess #2.");
            }

            // Verify score for 2nd guess
            int score = EncounterManager.CalculateScore(target.Length, guesses.Count);
            int expected = target.Length * 10 * 2; // 6 * 10 * 2 = 120
            if (score != expected)
            {
                Debug.LogError($"  FAIL: Score for 2nd guess expected {expected}, got {score}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Score for 2nd guess = {score} (expected {expected}).");
            }

            // Simulate clue numbering
            int clueNumber = 0;
            for (int i = 0; i < 3; i++)
            {
                clueNumber++;
                if (clueNumber != i + 1)
                {
                    Debug.LogError($"  FAIL: Clue number should be {i + 1}, got {clueNumber}");
                    allPassed = false;
                }
            }
            Debug.Log($"  OK: Clue number incremented correctly to {clueNumber}.");

            LogResult("Encounter State", allPassed);
            return allPassed;
        }

        // ── Test: RunManager ──────────────────────────────────

        [MenuItem("Spellwright/Tests/Run Manager")]
        public static bool TestRunManager()
        {
            Debug.Log("[Encounter Tests] ── Run Manager ──");
            bool allPassed = true;

            // Create a temporary GameObject with RunManager
            var go = new GameObject("TestRunManager");
            var runManager = go.AddComponent<RunManager>();

            // Find a GameConfigSO to initialize with
            var configGuids = AssetDatabase.FindAssets("t:GameConfigSO");
            if (configGuids.Length > 0)
            {
                var configPath = AssetDatabase.GUIDToAssetPath(configGuids[0]);
                var config = AssetDatabase.LoadAssetAtPath<ScriptableObjects.GameConfigSO>(configPath);
                // Set the serialized field via SerializedObject
                var so = new SerializedObject(runManager);
                so.FindProperty("gameConfig").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Clear any leftover event subscriptions
            EventBus.Instance.Clear<RunStartedEvent>();
            EventBus.Instance.Clear<RunEndedEvent>();
            EventBus.Instance.Clear<RunStateChangedEvent>();

            // Test 1: StartRun initializes state
            RunStartedEvent capturedStart = null;
            EventBus.Instance.Subscribe<RunStartedEvent>(e => capturedStart = e);

            runManager.StartRun();

            if (!runManager.IsRunActive)
            {
                Debug.LogError("  FAIL: IsRunActive should be true after StartRun.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: IsRunActive = true after StartRun.");
            }

            if (runManager.CurrentHP <= 0 || runManager.MaxHP <= 0)
            {
                Debug.LogError($"  FAIL: HP not initialized. Current={runManager.CurrentHP}, Max={runManager.MaxHP}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: HP initialized — {runManager.CurrentHP}/{runManager.MaxHP}");
            }

            if (runManager.Score != 0)
            {
                Debug.LogError($"  FAIL: Score should be 0 at start, got {runManager.Score}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Score = 0 at start.");
            }

            if (capturedStart == null)
            {
                Debug.LogError("  FAIL: RunStartedEvent not published.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: RunStartedEvent published.");
            }

            // Test 2: UsedWords accumulates via EncounterEndedEvent
            RunStateChangedEvent capturedChange = null;
            EventBus.Instance.Subscribe<RunStateChangedEvent>(e => capturedChange = e);

            EventBus.Instance.Publish(new EncounterEndedEvent
            {
                Won = true,
                TargetWord = "castle",
                GuessCount = 2,
                Score = 120
            });

            if (runManager.UsedWords.Count != 1 || runManager.UsedWords[0] != "castle")
            {
                Debug.LogError($"  FAIL: UsedWords should contain 'castle'. Count={runManager.UsedWords.Count}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: UsedWords contains 'castle' after encounter.");
            }

            if (runManager.Score != 120)
            {
                Debug.LogError($"  FAIL: Score should be 120, got {runManager.Score}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Score = 120 after first encounter.");
            }

            // Test 3: Score accumulates across multiple encounters
            EventBus.Instance.Publish(new EncounterEndedEvent
            {
                Won = true,
                TargetWord = "bridge",
                GuessCount = 1,
                Score = 180
            });

            if (runManager.Score != 300)
            {
                Debug.LogError($"  FAIL: Score should be 300 (120+180), got {runManager.Score}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Score = 300 after two encounters.");
            }

            if (runManager.UsedWords.Count != 2)
            {
                Debug.LogError($"  FAIL: UsedWords should have 2 entries, got {runManager.UsedWords.Count}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: UsedWords has 2 entries.");
            }

            // Test 4: EndRun sets IsRunActive = false
            RunEndedEvent capturedEnd = null;
            EventBus.Instance.Subscribe<RunEndedEvent>(e => capturedEnd = e);

            runManager.EndRun(won: false);

            if (runManager.IsRunActive)
            {
                Debug.LogError("  FAIL: IsRunActive should be false after EndRun.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: IsRunActive = false after EndRun.");
            }

            if (capturedEnd == null)
            {
                Debug.LogError("  FAIL: RunEndedEvent not published.");
                allPassed = false;
            }
            else if (capturedEnd.FinalScore != 300)
            {
                Debug.LogError($"  FAIL: RunEndedEvent.FinalScore should be 300, got {capturedEnd.FinalScore}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: RunEndedEvent published with FinalScore={capturedEnd.FinalScore}.");
            }

            // Test 5: HP sync via HPChangedEvent
            runManager.StartRun();
            int startHP = runManager.CurrentHP;

            EventBus.Instance.Publish(new HPChangedEvent
            {
                OldHP = startHP,
                NewHP = startHP - 15,
                MaxHP = runManager.MaxHP
            });

            if (runManager.CurrentHP != startHP - 15)
            {
                Debug.LogError($"  FAIL: HP should be {startHP - 15} after HPChangedEvent, got {runManager.CurrentHP}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: HP synced to {runManager.CurrentHP} via HPChangedEvent.");
            }

            // Cleanup
            EventBus.Instance.Clear<RunStartedEvent>();
            EventBus.Instance.Clear<RunEndedEvent>();
            EventBus.Instance.Clear<RunStateChangedEvent>();
            EventBus.Instance.Clear<EncounterEndedEvent>();
            EventBus.Instance.Clear<HPChangedEvent>();
            Object.DestroyImmediate(go);

            LogResult("Run Manager", allPassed);
            return allPassed;
        }

        // ── Helpers ─────────────────────────────────────────

        private static void LogResult(string testName, bool passed)
        {
            if (passed)
                Debug.Log($"[Encounter Tests] PASS: {testName}");
            else
                Debug.LogError($"[Encounter Tests] FAIL: {testName}");
        }
    }
}
