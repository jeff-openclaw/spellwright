using System.Collections.Generic;
using Spellwright.Data;
using Spellwright.LLM;
using UnityEditor;
using UnityEngine;

namespace Spellwright.Editor
{
    /// <summary>
    /// Menu-driven editor tests for the NPC prompt pipeline.
    /// Verifies prompt assembly, clue sanitization, JSON/regex parsing,
    /// and boss mode constraint injection without requiring Play mode.
    /// </summary>
    public static class NPCPromptTests
    {
        // ── Run All ─────────────────────────────────────────

        [MenuItem("Spellwright/Tests/Run All NPC Prompt Tests")]
        public static void RunAll()
        {
            int passed = 0;
            int failed = 0;

            if (TestPromptAssembly()) passed++; else failed++;
            if (TestArchetypesProduceDistinctPrompts()) passed++; else failed++;
            if (TestClueSanitization()) passed++; else failed++;
            if (TestJsonParsing()) passed++; else failed++;
            if (TestRegexFallback()) passed++; else failed++;
            if (TestLastResortFallback()) passed++; else failed++;
            if (TestBossConstraintInjection()) passed++; else failed++;
            if (TestProgressiveClueNumbering()) passed++; else failed++;
            if (TestTomeModifierInjection()) passed++; else failed++;

            Debug.Log($"[NPC Tests] ═══ RESULTS: {passed} passed, {failed} failed ═══");
        }

        // ── Test: Prompt Assembly ───────────────────────────

        [MenuItem("Spellwright/Tests/Prompt Assembly")]
        public static bool TestPromptAssembly()
        {
            Debug.Log("[NPC Tests] ── Prompt Assembly ──");

            var archetypes = new[]
            {
                CreateNPC("Riddlemaster", NPCArchetype.Riddlemaster, "You are {displayName}, a wise {archetype}."),
                CreateNPC("Trickster Merchant", NPCArchetype.TricksterMerchant, "You are {displayName}, a sly {archetype}."),
                CreateNPC("Silent Librarian", NPCArchetype.SilentLibrarian, "You are {displayName}, a terse {archetype}.")
            };

            bool allPassed = true;
            foreach (var npc in archetypes)
            {
                var (system, user) = PromptBuilder.BuildCluePrompt(
                    npc, "bridge", "structures", 1,
                    new List<string>(), new List<string>());

                if (string.IsNullOrEmpty(system) || string.IsNullOrEmpty(user))
                {
                    Debug.LogError($"  FAIL: {npc.DisplayName} produced empty prompt.");
                    allPassed = false;
                    continue;
                }

                // System prompt must contain NPC identity
                if (!system.Contains(npc.DisplayName))
                {
                    Debug.LogError($"  FAIL: {npc.DisplayName} system prompt missing display name.");
                    allPassed = false;
                }

                // User message must contain the target word
                if (!user.Contains("bridge"))
                {
                    Debug.LogError($"  FAIL: {npc.DisplayName} user message missing target word.");
                    allPassed = false;
                }

                // Must contain JSON format instruction
                if (!system.Contains("JSON"))
                {
                    Debug.LogError($"  FAIL: {npc.DisplayName} system prompt missing JSON format.");
                    allPassed = false;
                }

                Debug.Log($"  OK: {npc.DisplayName} — system: {system.Length} chars, user: {user.Length} chars");
            }

            LogResult("Prompt Assembly", allPassed);
            return allPassed;
        }

        // ── Test: Distinct Archetypes ───────────────────────

        [MenuItem("Spellwright/Tests/Distinct Archetypes")]
        public static bool TestArchetypesProduceDistinctPrompts()
        {
            Debug.Log("[NPC Tests] ── Distinct Archetypes ──");

            var riddlemaster = CreateNPC("Riddlemaster", NPCArchetype.Riddlemaster,
                "You speak in riddles and metaphors. Formal, archaic register.");
            var merchant = CreateNPC("Trickster Merchant", NPCArchetype.TricksterMerchant,
                "You give sales pitches. Fast-talking, silver-tongued.");
            var librarian = CreateNPC("Silent Librarian", NPCArchetype.SilentLibrarian,
                "You define words clinically. Terse, dictionary-style.");

            var (sysR, _) = PromptBuilder.BuildCluePrompt(riddlemaster, "castle", "structures", 1, new List<string>(), new List<string>());
            var (sysM, _) = PromptBuilder.BuildCluePrompt(merchant, "castle", "structures", 1, new List<string>(), new List<string>());
            var (sysL, _) = PromptBuilder.BuildCluePrompt(librarian, "castle", "structures", 1, new List<string>(), new List<string>());

            bool allDistinct = sysR != sysM && sysM != sysL && sysR != sysL;

            if (!allDistinct)
            {
                Debug.LogError("  FAIL: Two or more archetypes produced identical system prompts.");
            }
            else
            {
                Debug.Log("  OK: All 3 archetypes produce distinct system prompts.");
                Debug.Log($"    Riddlemaster contains 'riddles': {sysR.Contains("riddles")}");
                Debug.Log($"    Merchant contains 'sales': {sysM.Contains("sales")}");
                Debug.Log($"    Librarian contains 'define': {sysL.Contains("define")}");
            }

            LogResult("Distinct Archetypes", allDistinct);
            return allDistinct;
        }

        // ── Test: Clue Sanitization ─────────────────────────

        [MenuItem("Spellwright/Tests/Clue Sanitization")]
        public static bool TestClueSanitization()
        {
            Debug.Log("[NPC Tests] ── Clue Sanitization ──");
            bool allPassed = true;

            // Should reject: clue contains the exact target word
            var directLeak = "{\"clue\": \"This is a bridge over water.\", \"mood\": \"neutral\"}";
            var result1 = ResponseParser.ParseClueResponse(directLeak, "bridge");
            if (result1 != null)
            {
                Debug.LogError("  FAIL: Direct word leak ('bridge') was not caught.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Direct word leak rejected.");
            }

            // Should reject: case-insensitive match
            var caseLeak = "{\"clue\": \"Look at that BRIDGE!\", \"mood\": \"excited\"}";
            var result2 = ResponseParser.ParseClueResponse(caseLeak, "bridge");
            if (result2 != null)
            {
                Debug.LogError("  FAIL: Case-insensitive leak ('BRIDGE') was not caught.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Case-insensitive leak rejected.");
            }

            // Should accept: word is substring of another word (not standalone)
            var substringOk = "{\"clue\": \"Abridged versions tell less.\", \"mood\": \"neutral\"}";
            var result3 = ResponseParser.ParseClueResponse(substringOk, "bridge");
            if (result3 == null)
            {
                Debug.LogError("  FAIL: 'Abridged' was incorrectly rejected (substring false positive).");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Substring 'abridged' correctly allowed.");
            }

            // Should accept: safe clue with no target word
            var safeClue = "{\"clue\": \"A structure that spans a river.\", \"mood\": \"cryptic\"}";
            var result4 = ResponseParser.ParseClueResponse(safeClue, "bridge");
            if (result4 == null)
            {
                Debug.LogError("  FAIL: Safe clue was incorrectly rejected.");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Safe clue accepted — \"{result4.Clue}\"");
            }

            // Edge: null/empty target word should not reject
            var noTarget = "{\"clue\": \"Something cool.\", \"mood\": \"neutral\"}";
            var result5 = ResponseParser.ParseClueResponse(noTarget, null);
            if (result5 == null)
            {
                Debug.LogError("  FAIL: Null target word caused rejection.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Null target word does not reject.");
            }

            LogResult("Clue Sanitization", allPassed);
            return allPassed;
        }

        // ── Test: JSON Parsing ──────────────────────────────

        [MenuItem("Spellwright/Tests/JSON Parsing")]
        public static bool TestJsonParsing()
        {
            Debug.Log("[NPC Tests] ── JSON Parsing ──");
            bool allPassed = true;

            // Clean JSON
            var clean = "{\"clue\": \"A towering fortress of stone.\", \"mood\": \"cryptic\"}";
            var r1 = ResponseParser.ParseClueResponse(clean);
            if (r1 == null || r1.Clue != "A towering fortress of stone." || r1.Mood != "cryptic")
            {
                Debug.LogError($"  FAIL: Clean JSON parse — got {r1?.Clue ?? "null"}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Clean JSON — clue=\"{r1.Clue}\", mood=\"{r1.Mood}\"");
            }

            // JSON wrapped in markdown code fences
            var fenced = "```json\n{\"clue\": \"Hidden among pages.\", \"mood\": \"neutral\"}\n```";
            var r2 = ResponseParser.ParseClueResponse(fenced);
            if (r2 == null || r2.Clue != "Hidden among pages.")
            {
                Debug.LogError($"  FAIL: Fenced JSON parse — got {r2?.Clue ?? "null"}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Fenced JSON — clue=\"{r2.Clue}\"");
            }

            // JSON with extra whitespace
            var spaced = "  { \"clue\" :  \"Spaces everywhere.\" , \"mood\" : \"amused\" }  ";
            var r3 = ResponseParser.ParseClueResponse(spaced);
            if (r3 == null || r3.Clue != "Spaces everywhere.")
            {
                Debug.LogError($"  FAIL: Spaced JSON parse — got {r3?.Clue ?? "null"}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Spaced JSON — clue=\"{r3.Clue}\"");
            }

            // JSON with extra fields (should still parse)
            var extraFields = "{\"clue\": \"With extra data.\", \"mood\": \"precise\", \"difficulty_hint\": \"oblique\"}";
            var r4 = ResponseParser.ParseClueResponse(extraFields);
            if (r4 == null || r4.Clue != "With extra data.")
            {
                Debug.LogError($"  FAIL: Extra fields JSON — got {r4?.Clue ?? "null"}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Extra fields JSON — clue=\"{r4.Clue}\"");
            }

            // Empty/null input
            var r5 = ResponseParser.ParseClueResponse(null);
            var r6 = ResponseParser.ParseClueResponse("");
            var r7 = ResponseParser.ParseClueResponse("   ");
            if (r5 != null || r6 != null || r7 != null)
            {
                Debug.LogError("  FAIL: Empty/null input should return null.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Empty/null/whitespace inputs return null.");
            }

            LogResult("JSON Parsing", allPassed);
            return allPassed;
        }

        // ── Test: Regex Fallback ────────────────────────────

        [MenuItem("Spellwright/Tests/Regex Fallback")]
        public static bool TestRegexFallback()
        {
            Debug.Log("[NPC Tests] ── Regex Fallback ──");
            bool allPassed = true;

            // Malformed JSON that regex should still handle
            var malformed = "Sure! Here's your clue: {\"clue\": \"It crosses rivers.\", \"mood\": \"amused\"} Hope that helps!";
            var r1 = ResponseParser.ParseClueResponse(malformed);
            if (r1 == null || r1.Clue != "It crosses rivers.")
            {
                Debug.LogError($"  FAIL: Malformed JSON regex — got {r1?.Clue ?? "null"}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Malformed JSON regex — clue=\"{r1.Clue}\", mood=\"{r1.Mood}\"");
            }

            // Plain text (no JSON at all) — last resort parsing
            var plainText = "A structure spanning a gap. Used by many travelers daily.";
            var r2 = ResponseParser.ParseClueResponse(plainText);
            if (r2 == null)
            {
                Debug.LogError("  FAIL: Plain text last resort returned null.");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Plain text last resort — clue=\"{r2.Clue}\", mood=\"{r2.Mood}\"");
            }

            // Mood defaults to "neutral" when missing from regex
            var noMood = "{\"clue\": \"Something old.\"}";
            var r3 = ResponseParser.ParseClueResponse(noMood);
            if (r3 == null || r3.Mood != "neutral")
            {
                Debug.LogError($"  FAIL: Missing mood should default to 'neutral' — got {r3?.Mood ?? "null"}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Missing mood defaults to 'neutral'.");
            }

            LogResult("Regex Fallback", allPassed);
            return allPassed;
        }

        // ── Test: Last Resort Fallback ──────────────────────

        [MenuItem("Spellwright/Tests/Last Resort Fallback")]
        public static bool TestLastResortFallback()
        {
            Debug.Log("[NPC Tests] ── Last Resort Fallback ──");
            bool allPassed = true;

            // Single sentence
            var single = "Just one sentence here";
            var r1 = ResponseParser.ParseClueResponse(single);
            if (r1 == null || !r1.Clue.Contains("Just one sentence here"))
            {
                Debug.LogError($"  FAIL: Single sentence — got {r1?.Clue ?? "null"}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Single sentence — \"{r1.Clue}\"");
            }

            // Multiple sentences — should take first two
            var multi = "First sentence. Second sentence. Third sentence.";
            var r2 = ResponseParser.ParseClueResponse(multi);
            if (r2 == null || !r2.Clue.Contains("First sentence") || !r2.Clue.Contains("Second sentence"))
            {
                Debug.LogError($"  FAIL: Multi sentence — got {r2?.Clue ?? "null"}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Multi sentence — \"{r2.Clue}\"");
            }

            LogResult("Last Resort Fallback", allPassed);
            return allPassed;
        }

        // ── Test: Boss Mode Constraint ──────────────────────

        [MenuItem("Spellwright/Tests/Boss Constraint")]
        public static bool TestBossConstraintInjection()
        {
            Debug.Log("[NPC Tests] ── Boss Constraint Injection ──");
            bool allPassed = true;

            // Boss NPC with 3-word constraint
            var boss = new NPCPromptData
            {
                DisplayName = "The Whisperer",
                Archetype = NPCArchetype.Riddlemaster,
                SystemPromptTemplate = "You speak in fragments. Exactly three words at a time.",
                DifficultyModifier = 1.5f,
                IsBoss = true,
                BossConstraint = "Your clues must be exactly 3 words."
            };

            var (system, user) = PromptBuilder.BuildCluePrompt(
                boss, "castle", "structures", 1,
                new List<string>(), new List<string>());

            // System prompt must contain boss constraint
            if (!system.Contains("BOSS CONSTRAINT"))
            {
                Debug.LogError("  FAIL: Boss system prompt missing 'BOSS CONSTRAINT' label.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Boss constraint label present.");
            }

            if (!system.Contains("exactly 3 words"))
            {
                Debug.LogError("  FAIL: Boss system prompt missing '3 words' constraint text.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: 3-word constraint text present.");
            }

            // Non-boss NPC should NOT have boss constraint
            var regularNPC = CreateNPC("Regular NPC", NPCArchetype.Riddlemaster, "Just a regular NPC.");
            var (regularSystem, _) = PromptBuilder.BuildCluePrompt(
                regularNPC, "castle", "structures", 1,
                new List<string>(), new List<string>());

            if (regularSystem.Contains("BOSS CONSTRAINT"))
            {
                Debug.LogError("  FAIL: Non-boss NPC has 'BOSS CONSTRAINT' in prompt.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Non-boss NPC does not have boss constraint.");
            }

            LogResult("Boss Constraint", allPassed);
            return allPassed;
        }

        // ── Test: Progressive Clue Numbering ────────────────

        [MenuItem("Spellwright/Tests/Progressive Clues")]
        public static bool TestProgressiveClueNumbering()
        {
            Debug.Log("[NPC Tests] ── Progressive Clue Numbering ──");
            bool allPassed = true;

            var npc = CreateNPC("Riddlemaster", NPCArchetype.Riddlemaster, "You are {displayName}.");

            for (int i = 1; i <= 3; i++)
            {
                var guesses = new List<string>();
                if (i >= 2) guesses.Add("river");
                if (i >= 3) guesses.Add("road");

                var (system, user) = PromptBuilder.BuildCluePrompt(
                    npc, "bridge", "structures", i, guesses, new List<string>());

                if (!system.Contains($"clue #{i}"))
                {
                    Debug.LogError($"  FAIL: Clue {i} system prompt missing 'clue #{i}'.");
                    allPassed = false;
                }
                else
                {
                    Debug.Log($"  OK: Clue {i} system prompt contains 'clue #{i}'.");
                }

                if (!user.Contains($"clue #{i}"))
                {
                    Debug.LogError($"  FAIL: Clue {i} user message missing 'clue #{i}'.");
                    allPassed = false;
                }
                else
                {
                    Debug.Log($"  OK: Clue {i} user message contains 'clue #{i}'.");
                }

                // Check previous guesses appear in user message
                foreach (var guess in guesses)
                {
                    if (!user.Contains(guess))
                    {
                        Debug.LogError($"  FAIL: Clue {i} user message missing guess '{guess}'.");
                        allPassed = false;
                    }
                }
            }

            LogResult("Progressive Clues", allPassed);
            return allPassed;
        }

        // ── Test: Tome Modifier Injection ───────────────────

        [MenuItem("Spellwright/Tests/Tome Modifiers")]
        public static bool TestTomeModifierInjection()
        {
            Debug.Log("[NPC Tests] ── Tome Modifier Injection ──");
            bool allPassed = true;

            var npc = CreateNPC("Riddlemaster", NPCArchetype.Riddlemaster, "You are {displayName}.");
            var tomes = new List<string> { "Vowel Lens", "First Light" };

            var (system, _) = PromptBuilder.BuildCluePrompt(
                npc, "castle", "structures", 1,
                new List<string>(), tomes);

            if (!system.Contains("ACTIVE MODIFIERS"))
            {
                Debug.LogError("  FAIL: System prompt missing 'ACTIVE MODIFIERS' section.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: 'ACTIVE MODIFIERS' section present.");
            }

            foreach (var tome in tomes)
            {
                if (!system.Contains(tome))
                {
                    Debug.LogError($"  FAIL: Tome '{tome}' not found in system prompt.");
                    allPassed = false;
                }
                else
                {
                    Debug.Log($"  OK: Tome '{tome}' present in system prompt.");
                }
            }

            // Without tomes, should not have modifiers section
            var (noTomeSystem, _) = PromptBuilder.BuildCluePrompt(
                npc, "castle", "structures", 1,
                new List<string>(), new List<string>());

            if (noTomeSystem.Contains("ACTIVE MODIFIERS"))
            {
                Debug.LogError("  FAIL: Empty tome list still shows 'ACTIVE MODIFIERS'.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: No tomes → no modifiers section.");
            }

            LogResult("Tome Modifiers", allPassed);
            return allPassed;
        }

        // ── Helpers ─────────────────────────────────────────

        private static NPCPromptData CreateNPC(string name, NPCArchetype archetype, string template)
        {
            return new NPCPromptData
            {
                DisplayName = name,
                Archetype = archetype,
                SystemPromptTemplate = template,
                DifficultyModifier = 1.0f,
                IsBoss = false,
                BossConstraint = ""
            };
        }

        private static void LogResult(string testName, bool passed)
        {
            if (passed)
                Debug.Log($"[NPC Tests] PASS: {testName}");
            else
                Debug.LogError($"[NPC Tests] FAIL: {testName}");
        }
    }
}
