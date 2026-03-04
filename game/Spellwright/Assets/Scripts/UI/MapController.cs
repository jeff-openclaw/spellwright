using System.Collections.Generic;
using System.Linq;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.ScriptableObjects;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellwright.UI
{
    /// <summary>
    /// UI Toolkit-based map screen controller. Renders an ASCII dungeon map
    /// with pipe-connected room nodes, HP block bar, and outcome summaries.
    /// </summary>
    public class MapController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private GameConfigSO gameConfig;

        [Header("Animation")]
        [SerializeField] private float staggerDelayMs = 60f;
        [SerializeField] private float glowIntervalMs = 550f;

        private VisualElement _root;
        private Label _waveLabel;
        private Label _hpLabel;
        private Label _goldLabel;
        private Label _scoreLabel;
        private Label _rivalInfoLabel;
        private ScrollView _nodeContainer;
        private Button _proceedButton;
        private Button _langToggleButton;

        private readonly List<DungeonNodeEntry> _nodeEntries = new();
        private IVisualElementScheduledItem _glowSchedule;
        private bool _glowBright = true;
        private int _currentGlowNodeIndex = -1;

        private void OnEnable()
        {
            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            CacheElements();
            WireEvents();
            SubscribeEventBus();
            RefreshMap();
        }

        private void OnDisable()
        {
            UnwireEvents();
            UnsubscribeEventBus();
            StopGlow();
        }

        private void CacheElements()
        {
            _waveLabel = _root.Q<Label>("wave");
            _hpLabel = _root.Q<Label>("hp");
            _goldLabel = _root.Q<Label>("gold");
            _scoreLabel = _root.Q<Label>("score");
            _rivalInfoLabel = _root.Q<Label>("rival-info");
            _nodeContainer = _root.Q<ScrollView>("node-container");
            _proceedButton = _root.Q<Button>("proceed");
            _langToggleButton = _root.Q<Button>("lang-toggle");
        }

        private void WireEvents()
        {
            if (_proceedButton != null)
                _proceedButton.clicked += OnProceedClicked;
            if (_langToggleButton != null)
                _langToggleButton.clicked += OnLanguageClicked;
        }

        private void UnwireEvents()
        {
            if (_proceedButton != null)
                _proceedButton.clicked -= OnProceedClicked;
            if (_langToggleButton != null)
                _langToggleButton.clicked -= OnLanguageClicked;
        }

        private void SubscribeEventBus()
        {
            EventBus.Instance.Subscribe<RunStartedEvent>(OnRunStarted);
            EventBus.Instance.Subscribe<RunStateChangedEvent>(OnRunStateChanged);
        }

        private void UnsubscribeEventBus()
        {
            EventBus.Instance.Unsubscribe<RunStartedEvent>(OnRunStarted);
            EventBus.Instance.Unsubscribe<RunStateChangedEvent>(OnRunStateChanged);
        }

        private void OnRunStarted(RunStartedEvent evt) => RefreshMap();

        private void OnRunStateChanged(RunStateChangedEvent evt)
        {
            UpdateNodeStates();
            UpdateStats();
        }

        // ── Map Building ────────────────────────────────────

        public void RefreshMap()
        {
            StopGlow();
            ClearNodes();

            if (Run.RunManager.Instance == null || !Run.RunManager.Instance.IsRunActive)
                return;

            var sequence = Run.RunManager.Instance.NodeSequence;
            for (int i = 0; i < sequence.Count; i++)
            {
                // Add pipe connector between nodes
                if (i > 0)
                    AddPipeConnector();

                CreateDungeonNode(i, sequence[i], i == sequence.Count - 1);
            }

            UpdateNodeStates();
            UpdateStats();
            UpdateLanguageButtonVisibility();
            PlayEntranceAnimation();
        }

        private void AddPipeConnector()
        {
            var connector = new VisualElement();
            connector.AddToClassList("map-screen__dungeon-connector");

            var pipe = new Label("\u2502");
            pipe.AddToClassList("map-screen__border-char");

            var spacer = new Label("\u2551");
            spacer.AddToClassList("map-screen__connector-pipe");

            connector.Add(spacer);

            _nodeContainer.Add(connector);
        }

        private void CreateDungeonNode(int index, NodeType nodeType, bool isLast)
        {
            bool isBoss = nodeType == NodeType.Boss;

            var row = new VisualElement();
            row.AddToClassList("map-screen__dungeon-row");
            row.AddToClassList("stagger-item");

            // Left pipe
            var leftPipe = new Label("\u2551");
            leftPipe.AddToClassList("map-screen__dungeon-pipe");
            row.Add(leftPipe);

            // Node content
            var node = new VisualElement();
            node.AddToClassList("map-screen__dungeon-node");

            // Indicator [✓] [▶] [ ] [☠]
            var indicator = new Label("[ ]");
            indicator.AddToClassList("map-screen__node-indicator");
            node.Add(indicator);

            // Room label
            string roomText = isBoss
                ? "B O S S"
                : $"ROOM {index + 1:D2}";
            var roomLabel = new Label(roomText);
            roomLabel.AddToClassList("map-screen__node-room");
            node.Add(roomLabel);

            // Separator
            var sep = new Label("\u2500\u2500");
            sep.AddToClassList("map-screen__node-indicator");
            sep.style.color = new StyleColor(new Color(0.12f, 1f, 0.45f, 0.3f));
            node.Add(sep);

            // Outcome text
            var outcome = new Label(isBoss ? "??????????????" : "\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591");
            outcome.AddToClassList("map-screen__node-outcome");
            node.Add(outcome);

            row.Add(node);

            // Right pipe
            var rightPipe = new Label("\u2551");
            rightPipe.AddToClassList("map-screen__dungeon-pipe");
            row.Add(rightPipe);

            _nodeContainer.Add(row);

            // Dossier panel (hidden by default, expandable on click)
            var dossier = new VisualElement();
            dossier.AddToClassList("map-screen__dossier");
            _nodeContainer.Add(dossier);

            var entry = new DungeonNodeEntry
            {
                Row = row,
                Node = node,
                Indicator = indicator,
                RoomLabel = roomLabel,
                OutcomeLabel = outcome,
                Dossier = dossier,
                Index = index,
                Type = nodeType,
                DossierExpanded = false
            };

            // Register click to toggle dossier
            node.RegisterCallback<ClickEvent>(_ => ToggleDossier(entry));

            _nodeEntries.Add(entry);
        }

        // ── Node States ─────────────────────────────────────

        private void UpdateNodeStates()
        {
            if (Run.RunManager.Instance == null) return;

            StopGlow();
            int currentIndex = Run.RunManager.Instance.CurrentNodeIndex;
            var outcomes = Run.RunManager.Instance.NodeOutcomes;

            foreach (var entry in _nodeEntries)
            {
                bool isBoss = entry.Type == NodeType.Boss;

                // Remove all state classes
                RemoveStateClasses(entry);

                if (entry.Index < currentIndex)
                {
                    // Completed
                    entry.Node.AddToClassList("map-screen__dungeon-node--completed");
                    entry.Indicator.text = "[\u2713]"; // ✓

                    // Find outcome for this node
                    var nodeOutcome = outcomes.FirstOrDefault(o => o.NodeIndex == entry.Index);
                    if (nodeOutcome.Won)
                        entry.OutcomeLabel.text = $"SOLVED {nodeOutcome.GuessCount}/6 +{nodeOutcome.GoldEarned}g";
                    else
                        entry.OutcomeLabel.text = "FAILED";
                }
                else if (entry.Index == currentIndex)
                {
                    // Current
                    string stateClass = isBoss ? "map-screen__dungeon-node--current-boss" : "map-screen__dungeon-node--current";
                    entry.Node.AddToClassList(stateClass);
                    if (isBoss) entry.Node.AddToClassList("map-screen__dungeon-node--boss");
                    entry.Indicator.text = "[\u25B6]"; // ▶
                    entry.OutcomeLabel.text = isBoss ? "??????????????" : "\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588";
                }
                else
                {
                    // Future
                    entry.Node.AddToClassList("map-screen__dungeon-node--future");
                    if (isBoss) entry.Node.AddToClassList("map-screen__dungeon-node--boss");
                    entry.Indicator.text = isBoss ? "[\u2620]" : "[ ]"; // ☠ or empty
                    entry.OutcomeLabel.text = isBoss ? "??????????????" : "\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591";
                }
            }

            // Start breathing glow on current node
            if (currentIndex >= 0 && currentIndex < _nodeEntries.Count)
            {
                _currentGlowNodeIndex = currentIndex;
                _glowBright = true;
                StartGlow();
            }
        }

        // ── Stats ───────────────────────────────────────────

        private void UpdateStats()
        {
            if (Run.RunManager.Instance == null) return;

            if (_waveLabel != null)
                _waveLabel.text = $"WAVE {Run.RunManager.Instance.WaveNumber:D2}";

            if (_hpLabel != null)
            {
                int hp = Run.RunManager.Instance.CurrentHP;
                int maxHP = Run.RunManager.Instance.MaxHP;
                _hpLabel.text = $"HP {BuildHPBar(hp, maxHP)} {hp}/{maxHP}";
            }

            if (_goldLabel != null)
                _goldLabel.text = $"GOLD: {Run.RunManager.Instance.Gold}g";
            if (_scoreLabel != null)
                _scoreLabel.text = $"SCORE: {Run.RunManager.Instance.Score:N0}";

            // Rival info
            if (_rivalInfoLabel != null)
            {
                var rival = Run.RivalSystem.Instance;
                _rivalInfoLabel.text = rival != null && rival.HasRival
                    ? $"RIVAL: {rival.RivalDisplayName}"
                    : "";
            }
        }

        /// <summary>Builds an HP block bar using █ and ░ characters.</summary>
        private static string BuildHPBar(int current, int max)
        {
            const int barWidth = 10;
            int filled = max > 0 ? Mathf.RoundToInt((float)current / max * barWidth) : 0;
            filled = Mathf.Clamp(filled, 0, barWidth);
            return new string('\u2588', filled) + new string('\u2591', barWidth - filled);
        }

        // ── Language Toggle ─────────────────────────────────

        private void OnLanguageClicked()
        {
            if (gameConfig == null) return;

            gameConfig.language = gameConfig.language == GameLanguage.English
                ? GameLanguage.Romanian
                : GameLanguage.English;

            UpdateLanguageLabel();
            Debug.Log($"[MapController] Language set to {gameConfig.language}");
        }

        private void UpdateLanguageLabel()
        {
            if (_langToggleButton == null) return;
            _langToggleButton.text = gameConfig != null && gameConfig.language == GameLanguage.Romanian
                ? "RO" : "EN";
        }

        private void UpdateLanguageButtonVisibility()
        {
            if (_langToggleButton == null) return;

            bool show = Run.RunManager.Instance != null && Run.RunManager.Instance.CurrentNodeIndex == 0;
            if (show)
                _langToggleButton.RemoveFromClassList("map-screen__lang-btn--hidden");
            else
                _langToggleButton.AddToClassList("map-screen__lang-btn--hidden");

            UpdateLanguageLabel();
        }

        // ── Proceed ─────────────────────────────────────────

        private void OnProceedClicked()
        {
            if (Run.GameManager.Instance != null)
                Run.GameManager.Instance.ProceedToCurrentNode();
        }

        // ── Animations ──────────────────────────────────────

        private void PlayEntranceAnimation()
        {
            for (int i = 0; i < _nodeEntries.Count; i++)
            {
                var entry = _nodeEntries[i];
                long delay = (long)(i * staggerDelayMs);

                _root.schedule.Execute(() =>
                {
                    entry.Row.AddToClassList("stagger-visible");
                }).ExecuteLater(delay);
            }
        }

        private void StartGlow()
        {
            if (_currentGlowNodeIndex < 0 || _currentGlowNodeIndex >= _nodeEntries.Count)
                return;

            var node = _nodeEntries[_currentGlowNodeIndex].Node;
            node.AddToClassList("map-screen__dungeon-node--glow-bright");

            _glowSchedule = _root.schedule.Execute(() =>
            {
                _glowBright = !_glowBright;
                if (_glowBright)
                {
                    node.RemoveFromClassList("map-screen__dungeon-node--glow-dim");
                    node.AddToClassList("map-screen__dungeon-node--glow-bright");
                }
                else
                {
                    node.RemoveFromClassList("map-screen__dungeon-node--glow-bright");
                    node.AddToClassList("map-screen__dungeon-node--glow-dim");
                }
            }).Every((long)glowIntervalMs);
        }

        private void StopGlow()
        {
            _glowSchedule?.Pause();
            _glowSchedule = null;

            if (_currentGlowNodeIndex >= 0 && _currentGlowNodeIndex < _nodeEntries.Count)
            {
                var node = _nodeEntries[_currentGlowNodeIndex].Node;
                node.RemoveFromClassList("map-screen__dungeon-node--glow-bright");
                node.RemoveFromClassList("map-screen__dungeon-node--glow-dim");
            }
            _currentGlowNodeIndex = -1;
        }

        // ── Helpers ─────────────────────────────────────────

        private void ClearNodes()
        {
            _nodeContainer?.Clear();
            _nodeEntries.Clear();
            _expandedDossierIndex = -1;
        }

        private static void RemoveStateClasses(DungeonNodeEntry entry)
        {
            entry.Node.RemoveFromClassList("map-screen__dungeon-node--completed");
            entry.Node.RemoveFromClassList("map-screen__dungeon-node--current");
            entry.Node.RemoveFromClassList("map-screen__dungeon-node--current-boss");
            entry.Node.RemoveFromClassList("map-screen__dungeon-node--future");
            entry.Node.RemoveFromClassList("map-screen__dungeon-node--boss");
        }

        // ── Dossier ──────────────────────────────────────────

        private int _expandedDossierIndex = -1;

        private void ToggleDossier(DungeonNodeEntry entry)
        {
            if (Run.RunManager.Instance == null) return;
            int currentIndex = Run.RunManager.Instance.CurrentNodeIndex;

            // Don't expand completed nodes
            if (entry.Index < currentIndex) return;

            // Collapse any existing expanded dossier
            if (_expandedDossierIndex >= 0 && _expandedDossierIndex < _nodeEntries.Count)
            {
                var prev = _nodeEntries[_expandedDossierIndex];
                prev.Dossier.RemoveFromClassList("map-screen__dossier--visible");
                prev.DossierExpanded = false;
            }

            // Toggle this one
            if (entry.DossierExpanded || entry.Index == _expandedDossierIndex)
            {
                _expandedDossierIndex = -1;
                return;
            }

            // Build dossier content
            BuildDossierContent(entry);
            entry.Dossier.AddToClassList("map-screen__dossier--visible");
            entry.DossierExpanded = true;
            _expandedDossierIndex = entry.Index;
        }

        private void BuildDossierContent(DungeonNodeEntry entry)
        {
            entry.Dossier.Clear();
            bool isBoss = entry.Type == NodeType.Boss;

            var gm = Run.GameManager.Instance;
            if (gm == null) return;

            var npcData = gm.PreviewNPCForNode(entry.Index, entry.Type);
            string category = gm.PreviewCategoryForNode(entry.Index);

            if (isBoss)
            {
                // Boss: classified dossier with increasing legibility
                int encountersWon = Run.RunManager.Instance?.EncountersWon ?? 0;
                BuildBossDossier(entry, npcData, encountersWon);
            }
            else
            {
                BuildRegularDossier(entry, npcData, category);
            }

            // Add separator
            var sep = new Label("\u2560" + new string('\u2550', 36));
            sep.AddToClassList("map-screen__dossier-sep");
            entry.Dossier.Add(sep);
        }

        private void BuildRegularDossier(DungeonNodeEntry entry, NPCDataSO npc, string category)
        {
            string npcName = npc != null ? npc.displayName : "Unknown";
            float diff = npc != null ? npc.difficultyModifier : 1.0f;

            // Redacted NPC name: show first and last char, block rest
            string redactedName = RedactText(npcName, 0.4f);
            AddDossierLine(entry.Dossier, $"SUBJECT: {redactedName}", "map-screen__dossier-line--subject");

            // Threat level bar
            int threatLevel = Mathf.RoundToInt(diff * 3);
            threatLevel = Mathf.Clamp(threatLevel, 1, 5);
            string threatBar = new string('\u2593', threatLevel) + new string('\u2591', 5 - threatLevel);
            string threatName = threatLevel <= 2 ? "LOW" : threatLevel <= 3 ? "MODERATE" : "HIGH";
            AddDossierLine(entry.Dossier, $"THREAT:  {threatBar} ({threatName})", "map-screen__dossier-line--threat");

            // Garbled category
            string garbledCategory = GarbleText(category, 0.5f);
            AddDossierLine(entry.Dossier, $"CATEGORY: {garbledCategory}", "map-screen__dossier-line--category");

            // Purchasable intel lines
            var intel = Run.RunManager.Instance?.GetNodeIntel(entry.Index);
            if (intel != null)
            {
                AddIntelSeparator(entry.Dossier);
                AddIntelLine(entry, intel, IntelType.WordLength, "Word length",
                    $"Word length: {intel.WordLength} letters");
                AddIntelLine(entry, intel, IntelType.FirstLetter, "First letter",
                    $"First letter: {intel.FirstLetter}");
                AddIntelLine(entry, intel, IntelType.Weakness, "NPC weakness",
                    $"Weakness: {intel.WeaknessHint}");
            }
        }

        private void BuildBossDossier(DungeonNodeEntry entry, NPCDataSO npc, int encountersWon)
        {
            // Boss becomes more legible with each encounter won
            float legibility = Mathf.Clamp01(encountersWon / 5f);

            if (legibility < 0.2f)
            {
                AddDossierLine(entry.Dossier, "[CLASSIFIED]", "map-screen__dossier-line--classified");
                AddDossierLine(entry.Dossier, "SUBJECT: \u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588", "map-screen__dossier-line--classified");
                AddDossierLine(entry.Dossier, "THREAT:  \u2588\u2588\u2588\u2588\u2588 (???)", "map-screen__dossier-line--classified");
            }
            else
            {
                string bossName = npc != null ? npc.displayName : "???";
                string redacted = RedactText(bossName, legibility);
                AddDossierLine(entry.Dossier, $"SUBJECT: {redacted}", "map-screen__dossier-line--classified");

                float diff = npc != null ? npc.difficultyModifier : 2.0f;
                int threat = Mathf.RoundToInt(diff * 3);
                threat = Mathf.Clamp(threat, 3, 5);
                string bar = new string('\u2593', threat) + new string('\u2591', 5 - threat);
                AddDossierLine(entry.Dossier, $"THREAT:  {bar} (EXTREME)", "map-screen__dossier-line--classified");

                if (legibility > 0.5f)
                    AddDossierLine(entry.Dossier, "CAUTION: Defeat means run termination", "map-screen__dossier-line--classified");
            }
        }

        private static void AddDossierLine(VisualElement parent, string text, string className)
        {
            var line = new Label(text);
            line.AddToClassList("map-screen__dossier-line");
            line.AddToClassList(className);
            parent.Add(line);
        }

        /// <summary>Redacts text, keeping a fraction of characters visible.</summary>
        private static string RedactText(string text, float showFraction)
        {
            if (string.IsNullOrEmpty(text)) return "\u2588\u2588\u2588";
            var chars = text.ToCharArray();
            int toShow = Mathf.Max(1, Mathf.RoundToInt(chars.Length * showFraction));

            // Always show first char, then random others
            var visible = new HashSet<int> { 0 };
            var rng = new System.Random(text.GetHashCode());
            while (visible.Count < toShow)
                visible.Add(rng.Next(chars.Length));

            for (int i = 0; i < chars.Length; i++)
            {
                if (!visible.Contains(i) && chars[i] != ' ')
                    chars[i] = '\u2588'; // █
            }
            return new string(chars);
        }

        /// <summary>Garbles text, replacing a fraction of characters with random ASCII.</summary>
        private static string GarbleText(string text, float garbleFraction)
        {
            if (string.IsNullOrEmpty(text)) return "???";
            var chars = text.ToCharArray();
            var rng = new System.Random(text.GetHashCode() + 42);
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] != ' ' && rng.NextDouble() < garbleFraction)
                    chars[i] = (char)('a' + rng.Next(26));
            }
            return new string(chars);
        }

        private void CollapseAllDossiers()
        {
            foreach (var entry in _nodeEntries)
            {
                entry.Dossier.RemoveFromClassList("map-screen__dossier--visible");
                entry.DossierExpanded = false;
            }
            _expandedDossierIndex = -1;
        }

        // ── Intel Lines ──────────────────────────────────────

        private static void AddIntelSeparator(VisualElement parent)
        {
            var sep = new Label("\u2500\u2500\u2500 CLASSIFIED INTEL \u2500\u2500\u2500");
            sep.AddToClassList("map-screen__dossier-line");
            sep.AddToClassList("map-screen__intel-separator");
            parent.Add(sep);
        }

        private void AddIntelLine(DungeonNodeEntry entry, NodeIntelData intel, IntelType type,
            string lockedLabel, string unlockedText)
        {
            var row = new VisualElement();
            row.AddToClassList("map-screen__intel-row");

            if (intel.Unlocked.Contains(type))
            {
                // Already unlocked — show green text
                var label = new Label(unlockedText);
                label.AddToClassList("map-screen__intel-text");
                label.AddToClassList("map-screen__intel-text--unlocked");
                row.Add(label);
            }
            else
            {
                // Locked — show redacted text + cost button
                var label = new Label($"{lockedLabel}: \u2588\u2588\u2588\u2588\u2588\u2588");
                label.AddToClassList("map-screen__intel-text");
                label.AddToClassList("map-screen__intel-text--locked");
                row.Add(label);

                int encounterNumber = entry.Index + 1;
                int cost = gameConfig != null ? gameConfig.GetIntelCost(type, encounterNumber) : 5;

                var btn = new Button();
                btn.text = $"[{cost}g UNLOCK]";
                btn.AddToClassList("map-screen__intel-btn");

                // Check if player can afford
                int gold = Run.RunManager.Instance?.Gold ?? 0;
                if (gold < cost)
                    btn.AddToClassList("map-screen__intel-btn--disabled");

                btn.clicked += () => OnIntelUnlockClicked(entry, type, label, btn, unlockedText);
                row.Add(btn);
            }

            entry.Dossier.Add(row);
        }

        private void OnIntelUnlockClicked(DungeonNodeEntry entry, IntelType type,
            Label textLabel, Button btn, string unlockedText)
        {
            if (Run.RunManager.Instance == null) return;
            if (!Run.RunManager.Instance.TryUnlockIntel(entry.Index, type)) return;

            // Update visuals immediately
            textLabel.text = unlockedText;
            textLabel.RemoveFromClassList("map-screen__intel-text--locked");
            textLabel.AddToClassList("map-screen__intel-text--unlocked");

            // Flash the button then hide it
            btn.text = "[UNLOCKED]";
            btn.RemoveFromClassList("map-screen__intel-btn--disabled");
            btn.AddToClassList("map-screen__intel-btn--unlocked");
            btn.SetEnabled(false);

            // Update gold display
            UpdateStats();
        }

        private class DungeonNodeEntry
        {
            public VisualElement Row;
            public VisualElement Node;
            public Label Indicator;
            public Label RoomLabel;
            public Label OutcomeLabel;
            public VisualElement Dossier;
            public int Index;
            public NodeType Type;
            public bool DossierExpanded;
        }
    }
}
