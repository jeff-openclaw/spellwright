using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.ScriptableObjects;
using Spellwright.Tomes;
using Spellwright.Tomes.Effects;
using UnityEditor;
using UnityEngine;

namespace Spellwright.Editor
{
    /// <summary>
    /// Menu-driven editor tests for the Tome/modifier system.
    /// Verifies slot management, effect behaviors, factory creation,
    /// and HP modifier integration without requiring Play mode.
    /// </summary>
    public static class TomeTests
    {
        // ── Run All ─────────────────────────────────────────

        [MenuItem("Spellwright/Tests/Run All Tome Tests")]
        public static void RunAll()
        {
            int passed = 0;
            int failed = 0;

            if (TestSlotManagement()) passed++; else failed++;
            if (TestVowelLensEffect()) passed++; else failed++;
            if (TestFirstLightEffect()) passed++; else failed++;
            if (TestEchoChamberEffect()) passed++; else failed++;
            if (TestThickSkinEffect()) passed++; else failed++;
            if (TestSecondWindEffect()) passed++; else failed++;
            if (TestTomeManagerFactory()) passed++; else failed++;
            if (TestHPModifierIntegration()) passed++; else failed++;

            Debug.Log($"[Tome Tests] ═══ RESULTS: {passed} passed, {failed} failed ═══");
        }

        // ── Test: Slot Management ────────────────────────────

        [MenuItem("Spellwright/Tests/Tome Slot Management")]
        public static bool TestSlotManagement()
        {
            Debug.Log("[Tome Tests] ── Slot Management ──");
            bool allPassed = true;
            var bus = new EventBus();
            var system = new TomeSystem(bus);

            // Equip up to MaxSlots
            for (int i = 0; i < TomeSystem.MaxSlots; i++)
            {
                var tome = MakeTome($"tome_{i}", $"Tome {i}");
                var effect = new FirstLightEffect();
                bool equipped = system.EquipTome(tome, effect);
                if (!equipped)
                {
                    Debug.LogError($"  FAIL: Could not equip tome in slot {i}.");
                    allPassed = false;
                }
            }
            if (system.Count == TomeSystem.MaxSlots)
            {
                Debug.Log($"  OK: Equipped {TomeSystem.MaxSlots} tomes (max slots).");
            }

            // 6th tome should fail
            var extraTome = MakeTome("tome_extra", "Extra");
            bool extraEquipped = system.EquipTome(extraTome, new FirstLightEffect());
            if (extraEquipped)
            {
                Debug.LogError("  FAIL: Should not equip beyond MaxSlots.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: 6th tome correctly rejected (slots full).");
            }

            // Duplicate ID should fail
            var dupTome = MakeTome("tome_0", "Duplicate");
            bool dupEquipped = system.EquipTome(dupTome, new FirstLightEffect());
            if (dupEquipped)
            {
                Debug.LogError("  FAIL: Duplicate tome ID should be rejected.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Duplicate tome ID correctly rejected.");
            }

            // Unequip
            bool removed = system.UnequipTome("tome_2");
            if (!removed || system.Count != TomeSystem.MaxSlots - 1)
            {
                Debug.LogError($"  FAIL: Unequip failed or count wrong. Count={system.Count}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Unequipped tome_2, count={system.Count}.");
            }

            // Unequip non-existent
            bool removedBad = system.UnequipTome("nonexistent");
            if (removedBad)
            {
                Debug.LogError("  FAIL: Unequip of non-existent tome should return false.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Unequip of non-existent tome returned false.");
            }

            // HasFreeSlot should now be true
            if (!system.HasFreeSlot)
            {
                Debug.LogError("  FAIL: HasFreeSlot should be true after unequip.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: HasFreeSlot = true after unequip.");
            }

            system.Dispose();
            LogResult("Slot Management", allPassed);
            return allPassed;
        }

        // ── Test: VowelLens Effect ───────────────────────────

        [MenuItem("Spellwright/Tests/Tome VowelLens")]
        public static bool TestVowelLensEffect()
        {
            Debug.Log("[Tome Tests] ── VowelLens Effect ──");
            bool allPassed = true;

            TomeTriggeredEvent captured = null;
            EventBus.Instance.Subscribe<TomeTriggeredEvent>(e => captured = e);

            var effect = new VowelLensEffect();

            // Start encounter with "bridge" — vowels at positions 3 (i), 6 (e)
            effect.OnEncounterStart(new EncounterStartedEvent { TargetWord = "bridge" });

            // Wrong guess triggers vowel reveal
            effect.OnWrongGuess(new GuessSubmittedEvent
            {
                Guess = "castle",
                Result = new GuessResult { IsCorrect = false, IsValidWord = true }
            });

            if (captured == null)
            {
                Debug.LogError("  FAIL: TomeTriggeredEvent not published.");
                allPassed = false;
            }
            else if (!captured.RevealedInfo.Contains("3") || !captured.RevealedInfo.Contains("6"))
            {
                Debug.LogError($"  FAIL: Expected vowel positions 3, 6. Got: \"{captured.RevealedInfo}\"");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Vowel positions revealed — \"{captured.RevealedInfo}\"");
            }

            if (captured != null && captured.TomeName != "Vowel Lens")
            {
                Debug.LogError($"  FAIL: TomeName should be 'Vowel Lens', got '{captured.TomeName}'");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: TomeName = 'Vowel Lens'.");
            }

            EventBus.Instance.Clear<TomeTriggeredEvent>();
            LogResult("VowelLens Effect", allPassed);
            return allPassed;
        }

        // ── Test: FirstLight Effect ──────────────────────────

        [MenuItem("Spellwright/Tests/Tome FirstLight")]
        public static bool TestFirstLightEffect()
        {
            Debug.Log("[Tome Tests] ── FirstLight Effect ──");
            bool allPassed = true;

            TomeTriggeredEvent captured = null;
            EventBus.Instance.Subscribe<TomeTriggeredEvent>(e => captured = e);

            var effect = new FirstLightEffect();
            effect.OnEncounterStart(new EncounterStartedEvent { TargetWord = "castle" });

            if (captured == null)
            {
                Debug.LogError("  FAIL: TomeTriggeredEvent not published on encounter start.");
                allPassed = false;
            }
            else if (!captured.RevealedInfo.Contains("C"))
            {
                Debug.LogError($"  FAIL: Expected first letter 'C'. Got: \"{captured.RevealedInfo}\"");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: First letter revealed — \"{captured.RevealedInfo}\"");
            }

            EventBus.Instance.Clear<TomeTriggeredEvent>();
            LogResult("FirstLight Effect", allPassed);
            return allPassed;
        }

        // ── Test: EchoChamber Effect ─────────────────────────

        [MenuItem("Spellwright/Tests/Tome EchoChamber")]
        public static bool TestEchoChamberEffect()
        {
            Debug.Log("[Tome Tests] ── EchoChamber Effect ──");
            bool allPassed = true;

            TomeTriggeredEvent captured = null;
            EventBus.Instance.Subscribe<TomeTriggeredEvent>(e => captured = e);

            var effect = new EchoChamberEffect();
            effect.OnEncounterStart(new EncounterStartedEvent { TargetWord = "bridge" });

            // "bricks" shares b, r, i with "bridge"
            effect.OnWrongGuess(new GuessSubmittedEvent
            {
                Guess = "bricks",
                Result = new GuessResult { IsCorrect = false, IsValidWord = true }
            });

            if (captured == null)
            {
                Debug.LogError("  FAIL: TomeTriggeredEvent not published.");
                allPassed = false;
            }
            else if (!captured.RevealedInfo.Contains("B") || !captured.RevealedInfo.Contains("R") || !captured.RevealedInfo.Contains("I"))
            {
                Debug.LogError($"  FAIL: Expected shared letters B, R, I. Got: \"{captured.RevealedInfo}\"");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Shared letters revealed — \"{captured.RevealedInfo}\"");
            }

            // Test with no overlap
            captured = null;
            effect.OnWrongGuess(new GuessSubmittedEvent
            {
                Guess = "fuzz",
                Result = new GuessResult { IsCorrect = false, IsValidWord = true }
            });

            if (captured == null)
            {
                Debug.LogError("  FAIL: TomeTriggeredEvent not published for no-overlap guess.");
                allPassed = false;
            }
            else if (!captured.RevealedInfo.Contains("No shared"))
            {
                Debug.LogError($"  FAIL: Expected 'No shared letters'. Got: \"{captured.RevealedInfo}\"");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: No overlap handled — \"{captured.RevealedInfo}\"");
            }

            EventBus.Instance.Clear<TomeTriggeredEvent>();
            LogResult("EchoChamber Effect", allPassed);
            return allPassed;
        }

        // ── Test: ThickSkin Effect ───────────────────────────

        [MenuItem("Spellwright/Tests/Tome ThickSkin")]
        public static bool TestThickSkinEffect()
        {
            Debug.Log("[Tome Tests] ── ThickSkin Effect ──");
            bool allPassed = true;
            var bus = new EventBus();
            var system = new TomeSystem(bus);

            var effect = new ThickSkinEffect(system);

            // Simulate encounter start via the system's event flow
            system.PendingMaxHPBonus = 0;
            effect.OnEncounterStart(new EncounterStartedEvent { TargetWord = "test" });

            if (system.PendingMaxHPBonus != 10)
            {
                Debug.LogError($"  FAIL: PendingMaxHPBonus should be 10, got {system.PendingMaxHPBonus}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: PendingMaxHPBonus = {system.PendingMaxHPBonus}");
            }

            // Second ThickSkin stacks
            var effect2 = new ThickSkinEffect(system);
            effect2.OnEncounterStart(new EncounterStartedEvent { TargetWord = "test" });

            if (system.PendingMaxHPBonus != 20)
            {
                Debug.LogError($"  FAIL: Stacked PendingMaxHPBonus should be 20, got {system.PendingMaxHPBonus}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Stacked PendingMaxHPBonus = {system.PendingMaxHPBonus}");
            }

            system.Dispose();
            LogResult("ThickSkin Effect", allPassed);
            return allPassed;
        }

        // ── Test: SecondWind Effect ──────────────────────────

        [MenuItem("Spellwright/Tests/Tome SecondWind")]
        public static bool TestSecondWindEffect()
        {
            Debug.Log("[Tome Tests] ── SecondWind Effect ──");
            bool allPassed = true;
            var bus = new EventBus();
            var system = new TomeSystem(bus);

            TomeTriggeredEvent captured = null;
            EventBus.Instance.Subscribe<TomeTriggeredEvent>(e => captured = e);

            var effect = new SecondWindEffect(system);
            effect.OnEncounterStart(new EncounterStartedEvent { TargetWord = "test" });

            // First wrong guess — should activate
            system.PendingHPLossReduction = 0;
            effect.OnWrongGuess(new GuessSubmittedEvent
            {
                Guess = "wrong",
                Result = new GuessResult { IsCorrect = false, IsValidWord = true }
            });

            if (system.PendingHPLossReduction < 9999)
            {
                Debug.LogError($"  FAIL: PendingHPLossReduction should be >= 9999, got {system.PendingHPLossReduction}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: First wrong guess — PendingHPLossReduction = {system.PendingHPLossReduction}");
            }

            if (captured == null)
            {
                Debug.LogError("  FAIL: TomeTriggeredEvent not published.");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: TomeTriggeredEvent published — \"{captured.RevealedInfo}\"");
            }

            // Second wrong guess — should NOT activate again
            captured = null;
            system.PendingHPLossReduction = 0;
            effect.OnWrongGuess(new GuessSubmittedEvent
            {
                Guess = "again",
                Result = new GuessResult { IsCorrect = false, IsValidWord = true }
            });

            if (system.PendingHPLossReduction != 0)
            {
                Debug.LogError($"  FAIL: Second wrong guess should not reduce HP loss. Got {system.PendingHPLossReduction}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Second wrong guess — no reduction (once per encounter).");
            }

            // New encounter resets
            effect.OnEncounterStart(new EncounterStartedEvent { TargetWord = "reset" });
            system.PendingHPLossReduction = 0;
            effect.OnWrongGuess(new GuessSubmittedEvent
            {
                Guess = "miss",
                Result = new GuessResult { IsCorrect = false, IsValidWord = true }
            });

            if (system.PendingHPLossReduction < 9999)
            {
                Debug.LogError("  FAIL: SecondWind should reset on new encounter.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: SecondWind resets on new encounter.");
            }

            system.Dispose();
            EventBus.Instance.Clear<TomeTriggeredEvent>();
            LogResult("SecondWind Effect", allPassed);
            return allPassed;
        }

        // ── Test: TomeManager Factory ────────────────────────

        [MenuItem("Spellwright/Tests/Tome Manager Factory")]
        public static bool TestTomeManagerFactory()
        {
            Debug.Log("[Tome Tests] ── TomeManager Factory ──");
            bool allPassed = true;

            // Use existing singleton if available, otherwise create temporary
            bool createdTemp = false;
            GameObject go = null;
            TomeManager manager;

            if (TomeManager.Instance != null)
            {
                manager = TomeManager.Instance;
            }
            else
            {
                go = new GameObject("TestTomeManager");
                manager = go.AddComponent<TomeManager>();
                createdTemp = true;
            }

            // Test each known effect class
            string[] effectNames = { "VowelLensEffect", "FirstLightEffect", "EchoChamberEffect", "ThickSkinEffect", "SecondWindEffect" };
            string[] expectedDisplayNames = { "Vowel Lens", "First Light", "Echo Chamber", "Thick Skin", "Second Wind" };

            for (int i = 0; i < effectNames.Length; i++)
            {
                var effect = manager.CreateEffect(effectNames[i]);
                if (effect == null)
                {
                    Debug.LogError($"  FAIL: Factory returned null for \"{effectNames[i]}\"");
                    allPassed = false;
                }
                else if (effect.DisplayName != expectedDisplayNames[i])
                {
                    Debug.LogError($"  FAIL: Expected DisplayName \"{expectedDisplayNames[i]}\", got \"{effect.DisplayName}\"");
                    allPassed = false;
                }
                else
                {
                    Debug.Log($"  OK: \"{effectNames[i]}\" → \"{effect.DisplayName}\"");
                }
            }

            // Unknown class should return null
            var unknown = manager.CreateEffect("NonExistentEffect");
            if (unknown != null)
            {
                Debug.LogError("  FAIL: Unknown effect class should return null.");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Unknown effect class returns null.");
            }

            // Test EquipTome flow with a real TomeDataSO search
            var tomeGuids = AssetDatabase.FindAssets("t:TomeDataSO");
            if (tomeGuids.Length > 0 && manager.TomeSystem != null)
            {
                var tomePath = AssetDatabase.GUIDToAssetPath(tomeGuids[0]);
                var tomeData = AssetDatabase.LoadAssetAtPath<TomeDataSO>(tomePath);
                if (tomeData != null)
                {
                    // Only test if the effect class is one we know
                    var testEffect = manager.CreateEffect(tomeData.effectClassName);
                    if (testEffect != null)
                    {
                        bool equipped = manager.EquipTome(tomeData);
                        if (!equipped)
                        {
                            Debug.LogError($"  FAIL: Could not equip TomeDataSO \"{tomeData.displayName}\"");
                            allPassed = false;
                        }
                        else
                        {
                            Debug.Log($"  OK: Equipped TomeDataSO \"{tomeData.displayName}\"");
                            var names = manager.GetActiveEffectNames();
                            if (names.Count != 1)
                            {
                                Debug.LogError($"  FAIL: GetActiveEffectNames() should return 1, got {names.Count}");
                                allPassed = false;
                            }
                            else
                            {
                                Debug.Log($"  OK: GetActiveEffectNames() = [\"{names[0]}\"]");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"  SKIP: TomeDataSO \"{tomeData.displayName}\" has unknown effect \"{tomeData.effectClassName}\"");
                    }
                }
            }
            else if (manager.TomeSystem == null)
            {
                Debug.Log("  SKIP: TomeSystem not initialized (edit mode singleton).");
            }
            else
            {
                Debug.Log("  SKIP: No TomeDataSO assets found in project.");
            }

            if (createdTemp && go != null)
                Object.DestroyImmediate(go);

            LogResult("TomeManager Factory", allPassed);
            return allPassed;
        }

        // ── Test: HP Modifier Integration ────────────────────

        [MenuItem("Spellwright/Tests/Tome HP Modifiers")]
        public static bool TestHPModifierIntegration()
        {
            Debug.Log("[Tome Tests] ── HP Modifier Integration ──");
            bool allPassed = true;
            var bus = new EventBus();
            var system = new TomeSystem(bus);

            // Equip ThickSkin and SecondWind
            var thickSkinTome = MakeTome("thick_skin", "Thick Skin", "ThickSkinEffect");
            var secondWindTome = MakeTome("second_wind", "Second Wind", "SecondWindEffect");

            system.EquipTome(thickSkinTome, new ThickSkinEffect(system));
            system.EquipTome(secondWindTome, new SecondWindEffect(system));

            // Publish encounter start — ThickSkin should set PendingMaxHPBonus
            bus.Publish(new EncounterStartedEvent { TargetWord = "castle" });

            if (system.PendingMaxHPBonus != 10)
            {
                Debug.LogError($"  FAIL: After encounter start, PendingMaxHPBonus should be 10, got {system.PendingMaxHPBonus}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: PendingMaxHPBonus = 10 after encounter start.");
            }

            // Publish wrong guess — SecondWind should set PendingHPLossReduction
            bus.Publish(new GuessSubmittedEvent
            {
                Guess = "wrong",
                Result = new GuessResult { IsCorrect = false, IsValidWord = true }
            });

            if (system.PendingHPLossReduction < 9999)
            {
                Debug.LogError($"  FAIL: After wrong guess, PendingHPLossReduction should be >= 9999, got {system.PendingHPLossReduction}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: PendingHPLossReduction = {system.PendingHPLossReduction} after first wrong guess.");
            }

            // Second wrong guess — SecondWind should NOT trigger again
            bus.Publish(new GuessSubmittedEvent
            {
                Guess = "wrong2",
                Result = new GuessResult { IsCorrect = false, IsValidWord = true }
            });

            if (system.PendingHPLossReduction != 0)
            {
                Debug.LogError($"  FAIL: Second wrong guess PendingHPLossReduction should be 0, got {system.PendingHPLossReduction}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: PendingHPLossReduction = 0 after second wrong guess.");
            }

            // GetActiveTomeEffectNames
            var names = system.GetActiveTomeEffectNames();
            if (names.Count != 2)
            {
                Debug.LogError($"  FAIL: Expected 2 active effects, got {names.Count}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Active effects: [{string.Join(", ", names)}]");
            }

            system.Dispose();
            bus.Clear();
            LogResult("HP Modifier Integration", allPassed);
            return allPassed;
        }

        // ── Helpers ─────────────────────────────────────────

        private static TomeInstance MakeTome(string id, string name, string effectClass = "FirstLightEffect")
        {
            return new TomeInstance
            {
                TomeId = id,
                TomeName = name,
                Rarity = TomeRarity.Common,
                Category = TomeCategory.Insight,
                EffectClassName = effectClass
            };
        }

        private static void LogResult(string testName, bool passed)
        {
            if (passed)
                Debug.Log($"[Tome Tests] PASS: {testName}");
            else
                Debug.LogError($"[Tome Tests] FAIL: {testName}");
        }
    }
}
