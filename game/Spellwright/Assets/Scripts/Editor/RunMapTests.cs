using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Run;
using UnityEditor;
using UnityEngine;

namespace Spellwright.Editor
{
    /// <summary>
    /// Menu-driven editor tests for Run Manager, Map, and Game state machine.
    /// </summary>
    public static class RunMapTests
    {
        [MenuItem("Spellwright/Tests/Run All RunMap Tests")]
        public static void RunAll()
        {
            int passed = 0;
            int failed = 0;

            if (TestNodeSequenceGeneration()) passed++; else failed++;
            if (TestGoldOperations()) passed++; else failed++;
            if (TestHealOperation()) passed++; else failed++;
            if (TestNodeAdvancement()) passed++; else failed++;
            if (TestRunEndOnHPDepletion()) passed++; else failed++;
            if (TestGameStateTransitions()) passed++; else failed++;

            Debug.Log($"[RunMap Tests] === RESULTS: {passed} passed, {failed} failed ===");
        }

        [MenuItem("Spellwright/Tests/Run Node Sequence")]
        public static bool TestNodeSequenceGeneration()
        {
            Debug.Log("[RunMap Tests] -- Node Sequence Generation --");
            bool allPassed = true;

            var go = new GameObject("TestRunManager");
            var rm = go.AddComponent<RunManager>();
            rm.StartRun();

            // Expected: E-E-S-E-E-E-S-E-B (9 nodes)
            var seq = rm.NodeSequence;
            if (seq.Count != 9)
            {
                Debug.LogError($"  FAIL: Expected 9 nodes, got {seq.Count}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Node count = {seq.Count}");
            }

            // Verify pattern
            NodeType[] expected = {
                NodeType.Encounter, NodeType.Encounter, NodeType.Shop,
                NodeType.Encounter, NodeType.Encounter, NodeType.Encounter, NodeType.Shop,
                NodeType.Encounter, NodeType.Boss
            };

            for (int i = 0; i < Mathf.Min(seq.Count, expected.Length); i++)
            {
                if (seq[i] != expected[i])
                {
                    Debug.LogError($"  FAIL: Node {i} expected {expected[i]}, got {seq[i]}");
                    allPassed = false;
                }
            }

            if (allPassed)
                Debug.Log("  OK: Pattern matches E-E-S-E-E-E-S-E-B");

            // Boss at end
            if (seq.Count > 0 && seq[seq.Count - 1] == NodeType.Boss)
                Debug.Log("  OK: Boss at end of sequence");
            else
            {
                Debug.LogError("  FAIL: Last node should be Boss");
                allPassed = false;
            }

            Object.DestroyImmediate(go);
            LogResult("Node Sequence Generation", allPassed);
            return allPassed;
        }

        [MenuItem("Spellwright/Tests/Run Gold Operations")]
        public static bool TestGoldOperations()
        {
            Debug.Log("[RunMap Tests] -- Gold Operations --");
            bool allPassed = true;

            var go = new GameObject("TestRunManager");
            var rm = go.AddComponent<RunManager>();
            rm.StartRun();

            // Initial gold should be 0 (default config)
            if (rm.Gold != 0)
            {
                Debug.LogError($"  FAIL: Starting gold should be 0, got {rm.Gold}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Starting gold = 0");
            }

            // Add gold
            rm.AddGold(10);
            if (rm.Gold != 10)
            {
                Debug.LogError($"  FAIL: After adding 10, gold should be 10, got {rm.Gold}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: AddGold(10) → Gold = 10");
            }

            // Spend gold (sufficient)
            bool spent = rm.SpendGold(7);
            if (!spent || rm.Gold != 3)
            {
                Debug.LogError($"  FAIL: SpendGold(7) should succeed, gold should be 3, got {rm.Gold}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: SpendGold(7) → Gold = 3");
            }

            // Spend gold (insufficient)
            bool spentFail = rm.SpendGold(100);
            if (spentFail)
            {
                Debug.LogError("  FAIL: SpendGold(100) should fail with only 3 gold");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: SpendGold(100) correctly rejected (insufficient)");
            }

            Object.DestroyImmediate(go);
            LogResult("Gold Operations", allPassed);
            return allPassed;
        }

        [MenuItem("Spellwright/Tests/Run Heal Operation")]
        public static bool TestHealOperation()
        {
            Debug.Log("[RunMap Tests] -- Heal Operation --");
            bool allPassed = true;

            var go = new GameObject("TestRunManager");
            var rm = go.AddComponent<RunManager>();
            rm.StartRun();

            int maxHP = rm.MaxHP;

            // Apply damage directly
            rm.TakeDamage(30);

            int damagedHP = rm.CurrentHP;
            if (damagedHP != maxHP - 30)
            {
                Debug.LogError($"  FAIL: After 30 damage, HP should be {maxHP - 30}, got {damagedHP}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Damaged HP = {damagedHP}");
            }

            // Heal partially
            rm.Heal(10);
            if (rm.CurrentHP != damagedHP + 10)
            {
                Debug.LogError($"  FAIL: After heal 10, HP should be {damagedHP + 10}, got {rm.CurrentHP}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Healed 10 → HP = {rm.CurrentHP}");
            }

            // Heal beyond max (should cap)
            rm.Heal(9999);
            if (rm.CurrentHP != maxHP)
            {
                Debug.LogError($"  FAIL: Over-heal should cap at {maxHP}, got {rm.CurrentHP}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Over-heal capped at MaxHP = {rm.CurrentHP}");
            }

            Object.DestroyImmediate(go);
            LogResult("Heal Operation", allPassed);
            return allPassed;
        }

        [MenuItem("Spellwright/Tests/Run Node Advancement")]
        public static bool TestNodeAdvancement()
        {
            Debug.Log("[RunMap Tests] -- Node Advancement --");
            bool allPassed = true;

            var go = new GameObject("TestRunManager");
            var rm = go.AddComponent<RunManager>();
            rm.StartRun();

            if (rm.CurrentNodeIndex != 0)
            {
                Debug.LogError($"  FAIL: Initial node index should be 0, got {rm.CurrentNodeIndex}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Starting at node 0");
            }

            // Advance through all nodes
            int nodeCount = rm.NodeSequence.Count;
            for (int i = 0; i < nodeCount - 1; i++)
            {
                rm.AdvanceNode();
            }

            if (rm.CurrentNodeIndex != nodeCount - 1)
            {
                Debug.LogError($"  FAIL: After advancing {nodeCount - 1} times, should be at {nodeCount - 1}, got {rm.CurrentNodeIndex}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Advanced to node {rm.CurrentNodeIndex} (Boss)");
            }

            if (rm.CurrentNodeType != NodeType.Boss)
            {
                Debug.LogError($"  FAIL: Last node should be Boss, got {rm.CurrentNodeType}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Current node is Boss");
            }

            // Advancing past last node should end run (victory)
            bool runEndedCaptured = false;
            bool wonCaptured = false;
            EventBus.Instance.Subscribe<RunEndedEvent>(e => { runEndedCaptured = true; wonCaptured = e.Won; });

            rm.AdvanceNode();

            if (!runEndedCaptured)
            {
                Debug.LogError("  FAIL: RunEndedEvent not published after completing all nodes");
                allPassed = false;
            }
            else if (!wonCaptured)
            {
                Debug.LogError("  FAIL: Run should end with Won=true");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Run ended with victory after all nodes complete");
            }

            EventBus.Instance.Clear<RunEndedEvent>();
            Object.DestroyImmediate(go);
            LogResult("Node Advancement", allPassed);
            return allPassed;
        }

        [MenuItem("Spellwright/Tests/Run HP Depletion")]
        public static bool TestRunEndOnHPDepletion()
        {
            Debug.Log("[RunMap Tests] -- HP Depletion --");
            bool allPassed = true;

            var go = new GameObject("TestRunManager");
            var rm = go.AddComponent<RunManager>();
            rm.StartRun();

            bool runEndedCaptured = false;
            bool wonCaptured = true;
            EventBus.Instance.Subscribe<RunEndedEvent>(e => { runEndedCaptured = true; wonCaptured = e.Won; });

            // Deplete HP to 0 via direct method
            rm.TakeDamage(rm.CurrentHP);

            if (!runEndedCaptured)
            {
                Debug.LogError("  FAIL: RunEndedEvent not published on HP depletion");
                allPassed = false;
            }
            else if (wonCaptured)
            {
                Debug.LogError("  FAIL: Run should end with Won=false on HP depletion");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Run ended with defeat on HP depletion");
            }

            if (rm.IsRunActive)
            {
                Debug.LogError("  FAIL: IsRunActive should be false after HP depletion");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: IsRunActive = false");
            }

            EventBus.Instance.Clear<RunEndedEvent>();
            Object.DestroyImmediate(go);
            LogResult("HP Depletion", allPassed);
            return allPassed;
        }

        [MenuItem("Spellwright/Tests/Run Game State Transitions")]
        public static bool TestGameStateTransitions()
        {
            Debug.Log("[RunMap Tests] -- Game State Transitions --");
            bool allPassed = true;

            var go = new GameObject("TestGameManager");
            var gm = go.AddComponent<GameManager>();

            // Initial state after creation
            if (gm.CurrentState != GameState.MainMenu)
            {
                Debug.LogError($"  FAIL: Initial state should be MainMenu, got {gm.CurrentState}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Initial state = MainMenu");
            }

            // Transition to Map
            gm.TransitionTo(GameState.Map);
            if (gm.CurrentState != GameState.Map)
            {
                Debug.LogError($"  FAIL: Should be Map, got {gm.CurrentState}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Transitioned to Map");
            }

            // Transition to Encounter
            gm.TransitionTo(GameState.Encounter);
            if (gm.CurrentState != GameState.Encounter)
            {
                Debug.LogError($"  FAIL: Should be Encounter, got {gm.CurrentState}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Transitioned to Encounter");
            }

            // Transition to RunEnd
            gm.TransitionTo(GameState.RunEnd);
            if (gm.CurrentState != GameState.RunEnd)
            {
                Debug.LogError($"  FAIL: Should be RunEnd, got {gm.CurrentState}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Transitioned to RunEnd");
            }

            // Transition to MainMenu
            gm.TransitionTo(GameState.MainMenu);
            if (gm.CurrentState != GameState.MainMenu)
            {
                Debug.LogError($"  FAIL: Should be MainMenu, got {gm.CurrentState}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Transitioned to MainMenu");
            }

            // Verify GameStateChangedEvent is published
            GameStateChangedEvent capturedEvent = null;
            EventBus.Instance.Subscribe<GameStateChangedEvent>(e => capturedEvent = e);

            gm.TransitionTo(GameState.Shop);

            if (capturedEvent == null)
            {
                Debug.LogError("  FAIL: GameStateChangedEvent not published");
                allPassed = false;
            }
            else if (capturedEvent.NewState != GameState.Shop)
            {
                Debug.LogError($"  FAIL: Event NewState should be Shop, got {capturedEvent.NewState}");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: GameStateChangedEvent published correctly");
            }

            EventBus.Instance.Clear<GameStateChangedEvent>();
            Object.DestroyImmediate(go);
            LogResult("Game State Transitions", allPassed);
            return allPassed;
        }

        private static void LogResult(string testName, bool passed)
        {
            if (passed)
                Debug.Log($"[RunMap Tests] PASS: {testName}");
            else
                Debug.LogError($"[RunMap Tests] FAIL: {testName}");
        }
    }
}
