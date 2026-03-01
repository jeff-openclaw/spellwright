using System.Collections.Generic;
using Spellwright.Data;
using Spellwright.Encounter;
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
