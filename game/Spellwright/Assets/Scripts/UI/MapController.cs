using System.Collections.Generic;
using System.Linq;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.ScriptableObjects;
using Spellwright.Shop;
using Spellwright.Tomes;
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
        [SerializeField] private ShopManager shopManager;

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

        // Right pane
        private VisualElement _runLogContainer;
        private VisualElement _tomeLoadoutContainer;
        private VisualElement _bossWiretapContainer;

        private readonly List<DungeonNodeEntry> _nodeEntries = new();
        private IVisualElementScheduledItem _glowSchedule;
        private bool _glowBright = true;
        private int _currentGlowNodeIndex = -1;
        private int _lastWiretapFragments = -1;

        // Crucible state
        private string _crucibleSelectedA;
        private VisualElement _crucibleContainer;

        // Shop overlay
        private VisualElement _shopOverlay;
        private VisualElement _shopBuyItems;
        private VisualElement _shopServices;
        private Label _shopGoldLabel;
        private Label _shopFeedbackLabel;
        private Button _shopCloseButton;
        private Button _shopToggleButton;
        private bool _shopAutoOpen;
        private bool _shopOpen;

        // Signal waveform animation
        private IVisualElementScheduledItem _signalSchedule;
        private int _signalTick;

        // Ghost input echo
        private IVisualElementScheduledItem _ghostSchedule;
        private readonly List<Label> _ghostPool = new();
        private int _ghostNextIndex;
        private const int GhostPoolSize = 10;

        // Intercept transmission
        private VisualElement _interceptOverlay;
        private Label _interceptPayload;
        private Button _interceptDismissButton;

        private void OnEnable()
        {
            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            CacheElements();
            WireEvents();
            SubscribeEventBus();
            RefreshMap();

            // Auto-open shop if flagged (post-encounter)
            if (_shopAutoOpen)
            {
                _shopAutoOpen = false;
                _root?.schedule.Execute(OpenShop).ExecuteLater(300);
            }
        }

        private void OnDisable()
        {
            UnwireEvents();
            UnsubscribeEventBus();
            StopGlow();
            StopSignalAnimation();
            StopGhostEcho();
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
            _runLogContainer = _root.Q("run-log");
            _tomeLoadoutContainer = _root.Q("tome-loadout");
            _bossWiretapContainer = _root.Q("boss-wiretap");

            // Intercept overlay
            _interceptOverlay = _root.Q("intercept-overlay");
            _interceptPayload = _root.Q<Label>("intercept-payload");
            _interceptDismissButton = _root.Q<Button>("intercept-dismiss");

            // Shop overlay
            _shopOverlay = _root.Q("shop-overlay");
            _shopBuyItems = _root.Q("shop-buy-items");
            _shopServices = _root.Q("shop-services");
            _shopGoldLabel = _root.Q<Label>("shop-gold");
            _shopFeedbackLabel = _root.Q<Label>("shop-feedback");
            _shopCloseButton = _root.Q<Button>("shop-close");
            _shopToggleButton = _root.Q<Button>("shop-toggle");
        }

        private void WireEvents()
        {
            if (_proceedButton != null)
                _proceedButton.clicked += OnProceedClicked;
            if (_langToggleButton != null)
                _langToggleButton.clicked += OnLanguageClicked;
            if (_shopToggleButton != null)
                _shopToggleButton.clicked += OnShopToggleClicked;
            if (_shopCloseButton != null)
                _shopCloseButton.clicked += CloseShop;
            if (_shopOverlay != null)
                _shopOverlay.RegisterCallback<ClickEvent>(OnShopOverlayClicked);
            if (_interceptDismissButton != null)
                _interceptDismissButton.clicked += DismissIntercept;
            if (_interceptOverlay != null)
                _interceptOverlay.RegisterCallback<ClickEvent>(OnInterceptOverlayClicked);
        }

        private void UnwireEvents()
        {
            if (_proceedButton != null)
                _proceedButton.clicked -= OnProceedClicked;
            if (_langToggleButton != null)
                _langToggleButton.clicked -= OnLanguageClicked;
            if (_shopToggleButton != null)
                _shopToggleButton.clicked -= OnShopToggleClicked;
            if (_shopCloseButton != null)
                _shopCloseButton.clicked -= CloseShop;
            if (_shopOverlay != null)
                _shopOverlay.UnregisterCallback<ClickEvent>(OnShopOverlayClicked);
            if (_interceptDismissButton != null)
                _interceptDismissButton.clicked -= DismissIntercept;
            if (_interceptOverlay != null)
                _interceptOverlay.UnregisterCallback<ClickEvent>(OnInterceptOverlayClicked);
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
            RefreshRightPane();
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
            RefreshRightPane();
            PlayEntranceAnimation();
            StartSignalAnimation();
            StartGhostEcho();

            // Check for intercept transmission (delayed for entrance animation)
            _root?.schedule.Execute(CheckAndShowIntercept).ExecuteLater(1500);
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

            // Node content
            var node = new VisualElement();
            node.AddToClassList("map-screen__dungeon-node");

            // Indicator [✓] [▶] [ ] [☠]
            var indicator = new Label("[ ]");
            indicator.AddToClassList("map-screen__node-indicator");
            node.Add(indicator);

            bool isDeadDrop = nodeType == NodeType.DeadDrop;

            // Room label (file listing style)
            string roomText = isBoss
                ? "boss.enc.???"
                : isDeadDrop
                    ? "\u2592\u2592\u2592\u2592\u2592\u2592" // ▒▒▒▒▒▒
                    : $"room_{index + 1:D2}.enc";
            var roomLabel = new Label(roomText);
            roomLabel.AddToClassList("map-screen__node-room");
            node.Add(roomLabel);

            // File permissions
            var perms = new Label("---");
            perms.AddToClassList("map-screen__node-perms");
            node.Add(perms);

            // Signal waveform oscilloscope
            var signal = new Label("~~~~~~");
            signal.AddToClassList("map-screen__node-signal");
            node.Add(signal);

            // Outcome text
            var outcome = new Label("");
            outcome.AddToClassList("map-screen__node-outcome");
            node.Add(outcome);

            row.Add(node);
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
                PermsLabel = perms,
                SignalLabel = signal,
                OutcomeLabel = outcome,
                Dossier = dossier,
                Index = index,
                Type = nodeType,
                DossierExpanded = false
            };

            // Register click handler
            if (isDeadDrop)
                node.RegisterCallback<ClickEvent>(_ => OnDeadDropClicked(entry));
            else
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
                bool isDeadDrop = entry.Type == NodeType.DeadDrop;

                // Remove all state classes
                RemoveStateClasses(entry);

                if (entry.Index < currentIndex)
                {
                    // Completed
                    entry.Node.AddToClassList("map-screen__dungeon-node--completed");
                    if (isDeadDrop)
                    {
                        entry.Indicator.text = "[\u2713]";
                        entry.PermsLabel.text = "r--";
                        var drop = Run.RunManager.Instance.GetOrGenerateDeadDrop(entry.Index);
                        entry.OutcomeLabel.text = drop.Revealed ? "OPENED" : "SKIPPED";
                    }
                    else
                    {
                        entry.Indicator.text = "[\u2713]"; // ✓
                        entry.PermsLabel.text = "r--";

                        // Find outcome for this node
                        var nodeOutcome = outcomes.FirstOrDefault(o => o.NodeIndex == entry.Index);
                        if (nodeOutcome.Won)
                            entry.OutcomeLabel.text = $"+{nodeOutcome.GoldEarned}g";
                        else
                            entry.OutcomeLabel.text = "FAIL";
                    }
                }
                else if (entry.Index == currentIndex)
                {
                    // Current
                    if (isDeadDrop)
                    {
                        entry.Node.AddToClassList("map-screen__dungeon-node--dead-drop");
                        entry.Indicator.text = "[?]";
                        entry.PermsLabel.text = "r-x";
                    }
                    else
                    {
                        string stateClass = isBoss ? "map-screen__dungeon-node--current-boss" : "map-screen__dungeon-node--current";
                        entry.Node.AddToClassList(stateClass);
                        if (isBoss) entry.Node.AddToClassList("map-screen__dungeon-node--boss");
                        entry.Indicator.text = "[\u25B6]"; // ▶
                        entry.PermsLabel.text = "rwx";
                    }
                    entry.OutcomeLabel.text = "";
                }
                else
                {
                    // Future
                    entry.Node.AddToClassList("map-screen__dungeon-node--future");
                    if (isBoss) entry.Node.AddToClassList("map-screen__dungeon-node--boss");
                    if (isDeadDrop)
                    {
                        entry.Indicator.text = "[?]";
                        entry.PermsLabel.text = "???";
                    }
                    else
                    {
                        entry.Indicator.text = isBoss ? "[\u2620]" : "[ ]"; // ☠ or empty
                        entry.PermsLabel.text = isBoss ? "???" : "---";
                    }
                    entry.OutcomeLabel.text = "";
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

        // ── Right Pane ────────────────────────────────────────

        private void RefreshRightPane()
        {
            RefreshRunLog();
            RefreshTomeLoadout();
            RefreshBossWiretap();
        }

        private void RefreshRunLog()
        {
            if (_runLogContainer == null) return;
            _runLogContainer.Clear();

            var outcomes = Run.RunManager.Instance?.NodeOutcomes;
            if (outcomes == null || outcomes.Count == 0)
            {
                var empty = new Label("  (no encounters yet)");
                empty.AddToClassList("map-screen__log-entry");
                _runLogContainer.Add(empty);
                return;
            }

            foreach (var o in outcomes)
            {
                string status = o.Won ? "RC=0" : "RC=1";
                string line = $"  ENC_{o.NodeIndex + 1:D2} {status}";
                if (o.Won)
                    line += $" +{o.GoldEarned}g {o.GuessCount}/6";
                else
                    line += " FAIL";

                var label = new Label(line);
                label.AddToClassList("map-screen__log-entry");
                label.AddToClassList(o.Won ? "map-screen__log-entry--win" : "map-screen__log-entry--loss");
                _runLogContainer.Add(label);
            }
        }

        private void RefreshTomeLoadout()
        {
            if (_tomeLoadoutContainer == null) return;
            _tomeLoadoutContainer.Clear();
            _crucibleSelectedA = null;

            var tomeManager = TomeManager.Instance;
            if (tomeManager == null || tomeManager.TomeSystem == null)
            {
                var empty = new Label("  (no tomes equipped)");
                empty.AddToClassList("map-screen__tome-empty");
                _tomeLoadoutContainer.Add(empty);
                return;
            }

            var equipped = tomeManager.TomeSystem.GetEquippedTomes();
            int maxSlots = gameConfig != null ? gameConfig.maxTomeSlots : 5;

            for (int i = 0; i < maxSlots; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("map-screen__tome-entry");

                var slot = new Label($"  {i + 1}.");
                slot.AddToClassList("map-screen__tome-slot");
                row.Add(slot);

                if (i < equipped.Count)
                {
                    var name = new Label(equipped[i].TomeName);
                    name.AddToClassList("map-screen__tome-name");
                    row.Add(name);

                    var status = new Label("[ACT]");
                    status.AddToClassList("map-screen__tome-status");
                    row.Add(status);
                }
                else
                {
                    var empty = new Label("(empty)");
                    empty.AddToClassList("map-screen__tome-empty");
                    row.Add(empty);
                }

                _tomeLoadoutContainer.Add(row);
            }

            // Crucible section
            BuildCrucibleUI(equipped);
        }

        private void BuildCrucibleUI(IReadOnlyList<TomeInstance> equipped)
        {
            if (_tomeLoadoutContainer == null) return;

            var crucible = TomeCrucible.Instance;
            bool canFuse = crucible != null && crucible.CanFuse;

            // Crucible header
            var header = new Label(canFuse ? "> CRUCIBLE" : "> CRUCIBLE [LOCKED]");
            header.AddToClassList("map-screen__section-header");
            header.AddToClassList("map-screen__crucible-header");
            if (!canFuse) header.AddToClassList("map-screen__crucible-header--locked");
            _tomeLoadoutContainer.Add(header);

            if (!canFuse || equipped.Count < 2)
            {
                string reason = equipped.Count < 2 ? "Need 2+ tomes" : "Used this wave";
                var info = new Label($"  ({reason})");
                info.AddToClassList("map-screen__tome-empty");
                _tomeLoadoutContainer.Add(info);
                return;
            }

            var desc = new Label("  Select 2 tomes to fuse:");
            desc.AddToClassList("map-screen__crucible-desc");
            _tomeLoadoutContainer.Add(desc);

            _crucibleContainer = new VisualElement();
            _crucibleContainer.AddToClassList("map-screen__crucible-options");

            for (int i = 0; i < equipped.Count; i++)
            {
                var tome = equipped[i];
                var btn = new Button();
                btn.text = $"  [{tome.TomeName}]";
                btn.AddToClassList("map-screen__crucible-btn");
                btn.clicked += () => OnCrucibleTomeClicked(tome.TomeId, btn);
                _crucibleContainer.Add(btn);
            }

            _tomeLoadoutContainer.Add(_crucibleContainer);
        }

        private void OnCrucibleTomeClicked(string tomeId, Button btn)
        {
            if (_crucibleSelectedA == null)
            {
                // First selection
                _crucibleSelectedA = tomeId;
                btn.AddToClassList("map-screen__crucible-btn--selected");
            }
            else if (_crucibleSelectedA == tomeId)
            {
                // Deselect
                _crucibleSelectedA = null;
                btn.RemoveFromClassList("map-screen__crucible-btn--selected");
            }
            else
            {
                // Second selection — fuse
                var crucible = TomeCrucible.Instance;
                if (crucible == null) return;

                var result = crucible.FuseTomes(_crucibleSelectedA, tomeId);
                if (result != null)
                {
                    // Show result briefly, then refresh
                    _crucibleSelectedA = null;

                    if (_crucibleContainer != null)
                    {
                        _crucibleContainer.Clear();
                        var resultLabel = new Label($"  \u25B6 FORGED: {result.TomeName} ({result.Rarity})");
                        resultLabel.AddToClassList("map-screen__crucible-result");
                        _crucibleContainer.Add(resultLabel);
                    }

                    // Refresh after delay
                    _root?.schedule.Execute(RefreshRightPane).ExecuteLater(1500);
                }
            }
        }

        private void RefreshBossWiretap()
        {
            if (_bossWiretapContainer == null) return;
            _bossWiretapContainer.Clear();

            int encountersWon = Run.RunManager.Instance?.EncountersWon ?? 0;
            int totalEncounters = 5;
            int fragments = Mathf.Min(encountersWon, totalEncounters);

            // Get boss NPC data
            var gm = Run.GameManager.Instance;
            var bossNpc = gm?.PreviewNPCForNode(totalEncounters, NodeType.Boss);
            string bossName = bossNpc != null ? bossNpc.displayName : "???";
            string bossCategory = gm?.PreviewCategoryForNode(totalEncounters) ?? "???";
            float bossDiff = bossNpc != null ? bossNpc.difficultyModifier : 2.0f;

            bool hasNewFragment = fragments > _lastWiretapFragments && _lastWiretapFragments >= 0;
            _lastWiretapFragments = fragments;

            if (fragments == 0)
            {
                AddWiretapLine("  [SIGNAL TOO WEAK]", "map-screen__wiretap-bar");
                AddWiretapLine($"  [0/{totalEncounters} FRAGMENTS]", "map-screen__wiretap-progress");
                return;
            }

            // Fragment 1: heavily redacted name (gets clearer with more fragments)
            if (fragments >= 1)
            {
                float nameReveal = fragments >= 5 ? 1f : fragments >= 3 ? 0.6f : 0.2f;
                string redacted = RedactText(bossName, nameReveal);
                bool anim = hasNewFragment && (fragments == 1 || fragments == 3 || fragments == 5);
                AddWiretapLine($"  SUBJECT: {redacted}", "map-screen__wiretap-fragment", anim);
            }

            // Fragment 2: partially garbled category (clears at fragment 4)
            if (fragments >= 2)
            {
                float catReveal = fragments >= 4 ? 0.1f : 0.7f;
                string garbled = GarbleText(bossCategory, catReveal);
                bool anim = hasNewFragment && (fragments == 2 || fragments == 4);
                AddWiretapLine($"  CATEGORY: {garbled}", "map-screen__wiretap-fragment", anim);
            }

            // Fragment 3: threat level
            if (fragments >= 3)
            {
                int threat = Mathf.Clamp(Mathf.RoundToInt(bossDiff * 3), 3, 5);
                string bar = new string('\u2593', threat) + new string('\u2591', 5 - threat);
                bool anim = hasNewFragment && fragments == 3;
                AddWiretapLine($"  THREAT: {bar} (HIGH)", "map-screen__wiretap-fragment", anim);
            }

            // Fragment 4: personality/constraint
            if (fragments >= 4)
            {
                string constraint = bossNpc != null && !string.IsNullOrEmpty(bossNpc.bossConstraint)
                    ? bossNpc.bossConstraint
                    : "Clues are cryptic and minimal";
                AddWiretapLine($"  TACTIC: {constraint}", "map-screen__wiretap-fragment",
                    hasNewFragment && fragments == 4);
            }

            // Fragment 5: full intel
            if (fragments >= 5)
            {
                bool anim5 = hasNewFragment && fragments == 5;
                AddWiretapLine("  WARNING: Defeat = immediate run loss", "map-screen__wiretap-fragment", anim5);
                AddWiretapLine("  STATUS: FULL INTEL \u2014 READY", "map-screen__wiretap-fragment", anim5);
            }

            // Progress bar
            int barWidth = 20;
            int filled = Mathf.RoundToInt((float)fragments / totalEncounters * barWidth);
            string progressBar = new string('\u2593', filled) + new string('\u2591', barWidth - filled);
            AddWiretapLine($"  {progressBar}", "map-screen__wiretap-bar");
            AddWiretapLine($"  [{fragments}/{totalEncounters} FRAGMENTS]", "map-screen__wiretap-progress");
        }

        private void AddWiretapLine(string text, string className, bool animate = false)
        {
            var label = new Label(animate ? ScrambleText(text) : text);
            label.AddToClassList(className);
            _bossWiretapContainer.Add(label);

            if (animate && _root != null)
            {
                string target = text;
                int cycles = 0;
                var rng = new System.Random();
                _root.schedule.Execute(() =>
                {
                    cycles++;
                    if (cycles >= 8)
                    {
                        label.text = target;
                        return;
                    }
                    label.text = ScramblePartial(target, 1f - cycles / 8f, rng);
                }).Every(80).ForDuration(640);
            }
        }

        private static string ScrambleText(string text)
        {
            const string glitchChars = "!@#$%^&*<>{}[]|/\\~";
            var rng = new System.Random();
            var chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] != ' ' && chars[i] != ':' && chars[i] != '[' && chars[i] != ']')
                    chars[i] = glitchChars[rng.Next(glitchChars.Length)];
            }
            return new string(chars);
        }

        private static string ScramblePartial(string text, float scrambleFraction, System.Random rng)
        {
            const string glitchChars = "!@#$%^&*<>{}[]|/\\~";
            var chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] != ' ' && chars[i] != ':' && rng.NextDouble() < scrambleFraction)
                    chars[i] = glitchChars[rng.Next(glitchChars.Length)];
            }
            return new string(chars);
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
            StopSignalAnimation();
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
            entry.Node.RemoveFromClassList("map-screen__dungeon-node--dead-drop");
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

            // Wager section (only for current node)
            int currentIndex = Run.RunManager.Instance?.CurrentNodeIndex ?? -1;
            if (entry.Index == currentIndex)
                AddWagerSection(entry);
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

        // ── Wager Section ─────────────────────────────────────

        private void AddWagerSection(DungeonNodeEntry entry)
        {
            if (gameConfig == null) return;

            var sep = new Label("> STAKE \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            sep.AddToClassList("map-screen__dossier-line");
            sep.AddToClassList("map-screen__wager-separator");
            entry.Dossier.Add(sep);

            var container = new VisualElement();
            container.AddToClassList("map-screen__wager-container");

            int gold = Run.RunManager.Instance?.Gold ?? 0;

            for (int i = 0; i < gameConfig.wagerCosts.Length; i++)
            {
                int cost = gameConfig.wagerCosts[i];
                float mult = i < gameConfig.wagerMultipliers.Length ? gameConfig.wagerMultipliers[i] : 1f;
                int dmg = i < gameConfig.wagerDamageBonus.Length ? gameConfig.wagerDamageBonus[i] : 0;

                var btn = new Button();
                string label = cost == 0
                    ? $"[SKIP] {mult:F1}x"
                    : $"[{cost}g] {mult:F1}x +{dmg}dmg";
                btn.text = label;
                btn.AddToClassList("map-screen__wager-btn");

                if (cost == 0)
                    btn.AddToClassList("map-screen__wager-btn--safe");
                else if (cost > gold)
                    btn.AddToClassList("map-screen__wager-btn--disabled");

                int tierIndex = i;
                btn.clicked += () => OnWagerClicked(tierIndex, container);
                container.Add(btn);
            }

            entry.Dossier.Add(container);
        }

        private void OnWagerClicked(int tierIndex, VisualElement container)
        {
            if (Run.RunManager.Instance == null) return;
            if (!Run.RunManager.Instance.PlaceWager(tierIndex)) return;

            // Disable all wager buttons and mark selected
            foreach (var child in container.Children())
            {
                if (child is Button btn)
                {
                    btn.SetEnabled(false);
                    btn.AddToClassList("map-screen__wager-btn--disabled");
                }
            }

            // Highlight the selected tier
            int idx = 0;
            foreach (var child in container.Children())
            {
                if (child is Button btn && idx == tierIndex)
                {
                    btn.RemoveFromClassList("map-screen__wager-btn--disabled");
                    btn.AddToClassList("map-screen__wager-btn--selected");
                }
                idx++;
            }

            UpdateStats();
        }

        // ── Signal Waveform Oscilloscope ─────────────────────

        private static readonly char[] WaveCharsClean = { '~', '\u223F', '\u223E', '~', '\u223F', '\u223E' }; // ~ ∿ ∾
        private static readonly char[] WaveCharsNoisy = { '\u224B', '\u2307', '\u2248', '\u2261', '\u224B', '\u2307' }; // ≋ ⌇ ≈ ≡
        private const int SignalWidth = 6;

        private void StartSignalAnimation()
        {
            StopSignalAnimation();
            if (_root == null) return;

            _signalTick = 0;
            _signalSchedule = _root.schedule.Execute(() =>
            {
                _signalTick++;
                UpdateAllSignals();
            }).Every(200);
        }

        private void StopSignalAnimation()
        {
            _signalSchedule?.Pause();
            _signalSchedule = null;
        }

        private void UpdateAllSignals()
        {
            if (Run.RunManager.Instance == null) return;
            int currentIndex = Run.RunManager.Instance.CurrentNodeIndex;

            foreach (var entry in _nodeEntries)
            {
                if (entry.SignalLabel == null) continue;

                if (entry.Index < currentIndex)
                {
                    // Completed: flatline
                    entry.SignalLabel.text = new string('\u2500', SignalWidth); // ─ flatline
                    entry.SignalLabel.RemoveFromClassList("map-screen__node-signal--active");
                    entry.SignalLabel.RemoveFromClassList("map-screen__node-signal--noisy");
                    entry.SignalLabel.RemoveFromClassList("map-screen__node-signal--boss");
                    entry.SignalLabel.AddToClassList("map-screen__node-signal--flat");
                }
                else
                {
                    float noise = GetSignalNoise(entry);
                    entry.SignalLabel.text = GenerateWaveform(noise, _signalTick, entry.Index);

                    entry.SignalLabel.RemoveFromClassList("map-screen__node-signal--flat");
                    if (entry.Type == NodeType.Boss)
                    {
                        entry.SignalLabel.AddToClassList("map-screen__node-signal--boss");
                    }
                    else if (noise > 0.5f)
                    {
                        entry.SignalLabel.RemoveFromClassList("map-screen__node-signal--active");
                        entry.SignalLabel.AddToClassList("map-screen__node-signal--noisy");
                    }
                    else
                    {
                        entry.SignalLabel.RemoveFromClassList("map-screen__node-signal--noisy");
                        entry.SignalLabel.AddToClassList("map-screen__node-signal--active");
                    }
                }
            }
        }

        private float GetSignalNoise(DungeonNodeEntry entry)
        {
            if (entry.Type == NodeType.Boss) return 0.9f;

            var gm = Run.GameManager.Instance;
            if (gm == null) return 0.3f;

            var npc = gm.PreviewNPCForNode(entry.Index, entry.Type);
            float diff = npc != null ? npc.difficultyModifier : 1f;

            // Map difficulty (0.5 easy .. 2.0 boss) to noise (0.1 .. 0.8)
            return Mathf.Clamp01((diff - 0.3f) / 1.7f);
        }

        private static string GenerateWaveform(float noise, int tick, int seed)
        {
            var chars = new char[SignalWidth];
            var rng = new System.Random(tick * 31 + seed * 7);

            for (int i = 0; i < SignalWidth; i++)
            {
                // Base sine wave phase
                float phase = (tick * 0.5f + i * 0.8f + seed * 2.1f);
                float sine = Mathf.Sin(phase);

                // Add noise
                float noiseVal = (float)(rng.NextDouble() * 2 - 1) * noise;
                float combined = sine + noiseVal;

                if (noise > 0.7f && rng.NextDouble() < 0.15f)
                {
                    // Glitch: scanline tear for high noise
                    chars[i] = WaveCharsNoisy[rng.Next(WaveCharsNoisy.Length)];
                }
                else if (combined > 0.3f)
                {
                    chars[i] = WaveCharsClean[rng.Next(WaveCharsClean.Length)];
                }
                else if (combined < -0.3f)
                {
                    chars[i] = noise > 0.4f ? WaveCharsNoisy[rng.Next(WaveCharsNoisy.Length)] : '_';
                }
                else
                {
                    chars[i] = noise > 0.5f ? '\u2248' : '~'; // ≈ or ~
                }
            }
            return new string(chars);
        }

        // ── Dead Drop ─────────────────────────────────────────

        private void OnDeadDropClicked(DungeonNodeEntry entry)
        {
            if (Run.RunManager.Instance == null) return;
            int currentIndex = Run.RunManager.Instance.CurrentNodeIndex;

            // Only interact with current dead drop node
            if (entry.Index != currentIndex) return;

            var rm = Run.RunManager.Instance;
            var outcome = rm.RevealDeadDrop(entry.Index);

            // Show outcome in dossier
            entry.Dossier.Clear();
            var header = new Label($"DEAD DROP #{entry.Index:X4}");
            header.AddToClassList("map-screen__dossier-line");
            header.AddToClassList("map-screen__dead-drop-header");
            entry.Dossier.Add(header);

            // Type-out animation for the message
            var msgLabel = new Label("");
            msgLabel.AddToClassList("map-screen__dossier-line");
            msgLabel.AddToClassList(outcome.Type == Run.RunManager.DeadDropType.Trap
                ? "map-screen__dead-drop-trap"
                : "map-screen__dead-drop-reward");
            entry.Dossier.Add(msgLabel);

            entry.Dossier.AddToClassList("map-screen__dossier--visible");
            entry.DossierExpanded = true;

            // Character typewriter effect
            string fullText = outcome.Message;
            int charIdx = 0;
            _root?.schedule.Execute(() =>
            {
                if (charIdx < fullText.Length)
                {
                    charIdx = Mathf.Min(charIdx + 2, fullText.Length);
                    msgLabel.text = fullText.Substring(0, charIdx);
                }
            }).Every(30).ForDuration((long)(fullText.Length * 15 + 200));

            // Handle free tome outcome
            if (outcome.Type == Run.RunManager.DeadDropType.FreeTome)
            {
                var tomeManager = Tomes.TomeManager.Instance;
                if (tomeManager != null && tomeManager.TomeSystem != null && tomeManager.TomeSystem.HasFreeSlot)
                {
                    // Give a random equipped-able tome (reuse shop logic concept)
                    var gm = Run.GameManager.Instance;
                    if (gm != null)
                        Debug.Log("[MapController] Dead drop: Free tome awarded (handled by TomeManager)");
                }
            }

            // Update display
            UpdateStats();
            entry.Indicator.text = "[\u2713]";
            entry.OutcomeLabel.text = "OPENED";
        }

        // ── Intercept Transmission ───────────────────────────

        private void CheckAndShowIntercept()
        {
            var rm = Run.RunManager.Instance;
            if (rm == null || !rm.InterceptPending) return;
            if (_interceptOverlay == null) return;

            rm.ConsumeIntercept();

            // Generate garbled intel
            var gm = Run.GameManager.Instance;
            int currentIdx = rm.CurrentNodeIndex;
            int futureIdx = Mathf.Min(currentIdx + 1, rm.NodeSequence.Count - 1);
            string category = gm?.PreviewCategoryForNode(futureIdx) ?? "???";
            var npc = gm?.PreviewNPCForNode(futureIdx, rm.NodeSequence[futureIdx]);
            string npcName = npc?.displayName ?? "???";

            var rng = new System.Random();
            string[] fragments =
            {
                $"\"...the w\u2588rd is about {GarbleText(category, 0.4f)}...\"",
                $"\"...be\u2588\u2588re of the {GarbleText(npcName, 0.5f)}...\"",
                $"\"...diffic\u2588lty level is {(npc != null ? npc.difficultyModifier > 1.2f ? "HIGH" : "moderate" : "\u2588\u2588\u2588")}...\"",
                $"\"...\u2588\u2588\u2588 category \u2588\u2588 {GarbleText(category, 0.3f)} \u2588\u2588\u2588...\""
            };

            // Some transmissions are garbage (unreliable)
            bool isGarbage = rng.NextDouble() < 0.35;
            string payload = isGarbage
                ? $"\"...\u2588\u2588\u2588 {GarbleText("signal corrupted beyond recovery", 0.6f)} \u2588\u2588\u2588...\""
                : fragments[rng.Next(fragments.Length)];

            if (_interceptPayload != null)
                _interceptPayload.text = $"payload: {payload}";

            _interceptOverlay.AddToClassList("map-screen__intercept-overlay--visible");
            _interceptOverlay.pickingMode = PickingMode.Position;

            // Auto-dismiss after 5 seconds
            _root?.schedule.Execute(DismissIntercept).ExecuteLater(5000);
        }

        private void DismissIntercept()
        {
            if (_interceptOverlay == null) return;
            _interceptOverlay.RemoveFromClassList("map-screen__intercept-overlay--visible");
            _interceptOverlay.pickingMode = PickingMode.Ignore;
        }

        private void OnInterceptOverlayClicked(ClickEvent evt)
        {
            if (evt.target == _interceptOverlay)
                DismissIntercept();
        }

        // ── Ghost Input Echo ──────────────────────────────────

        private void StartGhostEcho()
        {
            StopGhostEcho();
            if (_root == null) return;

            var ghosts = Run.RunManager.Instance?.GhostLetters;
            if (ghosts == null || ghosts.Count == 0) return;

            // Create ghost label pool
            for (int i = 0; i < GhostPoolSize; i++)
            {
                var label = new Label("");
                label.AddToClassList("map-screen__ghost-letter");
                label.style.position = Position.Absolute;
                label.style.display = DisplayStyle.None;
                _root.Add(label);
                _ghostPool.Add(label);
            }

            _ghostNextIndex = 0;

            // Spawn a ghost every 600-1000ms
            _ghostSchedule = _root.schedule.Execute(SpawnGhostLetter).Every(800);
        }

        private void StopGhostEcho()
        {
            _ghostSchedule?.Pause();
            _ghostSchedule = null;

            foreach (var label in _ghostPool)
                label.RemoveFromHierarchy();
            _ghostPool.Clear();
        }

        private void SpawnGhostLetter()
        {
            // Don't spawn during shop overlay or dossier expansion
            if (_shopOpen || _expandedDossierIndex >= 0) return;

            var ghosts = Run.RunManager.Instance?.GhostLetters;
            if (ghosts == null || ghosts.Count == 0 || _ghostPool.Count == 0) return;

            var rng = new System.Random();
            var ghost = ghosts[rng.Next(ghosts.Count)];
            var label = _ghostPool[_ghostNextIndex % _ghostPool.Count];
            _ghostNextIndex++;

            label.text = ghost.Letter.ToString();

            // Position randomly within the root bounds
            float x = rng.Next(5, 90);
            float y = rng.Next(5, 85);
            label.style.left = new StyleLength(new Length(x, LengthUnit.Percent));
            label.style.top = new StyleLength(new Length(y, LengthUnit.Percent));

            // Style based on correctness
            label.RemoveFromClassList("map-screen__ghost-letter--correct");
            label.RemoveFromClassList("map-screen__ghost-letter--wrong");
            label.AddToClassList(ghost.Correct
                ? "map-screen__ghost-letter--correct"
                : "map-screen__ghost-letter--wrong");

            // Show and fade
            label.style.display = DisplayStyle.Flex;
            label.style.opacity = ghost.Correct ? 0.25f : 0.15f;

            // Fade out after delay
            long fadeDelay = ghost.Correct ? 1500 : 600;
            _root.schedule.Execute(() =>
            {
                label.style.opacity = 0f;
                _root.schedule.Execute(() =>
                {
                    label.style.display = DisplayStyle.None;
                }).ExecuteLater(400);
            }).ExecuteLater(fadeDelay);
        }

        // ── Shop Overlay ─────────────────────────────────────

        /// <summary>Flags the shop to auto-open on next OnEnable (called by GameManager).</summary>
        public void RequestShopAutoOpen() => _shopAutoOpen = true;

        private void OnShopToggleClicked()
        {
            if (_shopOpen)
                CloseShop();
            else
                OpenShop();
        }

        private void OnShopOverlayClicked(ClickEvent evt)
        {
            // Close if clicking the backdrop (not the popup itself)
            if (evt.target == _shopOverlay)
                CloseShop();
        }

        private void OpenShop()
        {
            if (_shopOverlay == null || shopManager == null) return;

            shopManager.GenerateInventory();
            RefreshShopUI();

            _shopOverlay.AddToClassList("map-screen__shop-overlay--visible");
            _shopOverlay.pickingMode = PickingMode.Position;
            _shopOpen = true;
        }

        private void CloseShop()
        {
            if (_shopOverlay == null) return;

            _shopOverlay.RemoveFromClassList("map-screen__shop-overlay--visible");
            _shopOverlay.pickingMode = PickingMode.Ignore;
            _shopOpen = false;

            // Refresh map stats (gold may have changed)
            UpdateStats();
            RefreshRightPane();
        }

        private void RefreshShopUI()
        {
            if (shopManager == null) return;

            // Build buy items
            _shopBuyItems?.Clear();
            for (int i = 0; i < shopManager.Inventory.Count; i++)
            {
                var item = shopManager.Inventory[i];
                if (!item.IsHealItem)
                    CreateShopBuyItem(i, item);
            }

            // Build sell items + heal in services section
            _shopServices?.Clear();
            BuildShopHealButton();
            BuildShopSellItems();

            UpdateShopGold();
            if (_shopFeedbackLabel != null)
                _shopFeedbackLabel.text = "";
        }

        private void CreateShopBuyItem(int index, ShopItem item)
        {
            if (_shopBuyItems == null) return;

            var row = new VisualElement();
            row.AddToClassList("map-screen__shop-item");
            if (item.IsSold)
                row.AddToClassList("map-screen__shop-item--sold");

            var nameLabel = new Label();
            nameLabel.AddToClassList("map-screen__shop-item-name");
            if (item.IsSold)
            {
                nameLabel.text = $"[SOLD] {item.TomeData?.displayName ?? "Unknown"}";
                nameLabel.AddToClassList("map-screen__shop-item-name--sold");
            }
            else
            {
                nameLabel.text = item.TomeData?.displayName ?? "Unknown";
                nameLabel.AddToClassList(GetShopRarityClass(item.TomeData));
            }
            row.Add(nameLabel);

            if (!item.IsSold)
            {
                var priceLabel = new Label($"{item.Price}g");
                priceLabel.AddToClassList("map-screen__shop-item-price");
                row.Add(priceLabel);

                var actionLabel = new Label("[ BUY ]");
                actionLabel.AddToClassList("map-screen__shop-item-action");
                row.Add(actionLabel);

                int capturedIndex = index;
                row.RegisterCallback<ClickEvent>(_ => OnShopBuyClicked(capturedIndex));
            }

            _shopBuyItems.Add(row);
        }

        private void BuildShopHealButton()
        {
            if (_shopServices == null || shopManager == null) return;

            int healCost = 0;
            foreach (var item in shopManager.Inventory)
            {
                if (item.IsHealItem) { healCost = item.Price; break; }
            }

            int healAmount = shopManager.HealAmountValue;
            bool canHeal = Run.RunManager.Instance != null
                && Run.RunManager.Instance.Gold >= healCost
                && Run.RunManager.Instance.CurrentHP < Run.RunManager.Instance.MaxHP;

            var btn = new Button();
            btn.text = $"[ HEAL +{healAmount}HP ({healCost}g) ]";
            btn.AddToClassList("map-screen__shop-heal-btn");
            if (!canHeal)
                btn.AddToClassList("map-screen__shop-heal-btn--disabled");
            btn.clicked += OnShopHealClicked;
            _shopServices.Add(btn);
        }

        private void BuildShopSellItems()
        {
            if (_shopServices == null) return;

            var tomeManager = TomeManager.Instance;
            if (tomeManager?.TomeSystem == null) return;

            var equipped = tomeManager.TomeSystem.GetEquippedTomes();
            if (equipped.Count == 0) return;

            var sellHeader = new Label("> SELL TOMES");
            sellHeader.AddToClassList("map-screen__shop-section-header");
            _shopServices.Add(sellHeader);

            foreach (var tome in equipped)
            {
                var row = new VisualElement();
                row.AddToClassList("map-screen__shop-sell-item");

                var nameLabel = new Label(tome.TomeName);
                nameLabel.AddToClassList("map-screen__shop-sell-name");
                row.Add(nameLabel);

                var priceLabel = new Label($"+{ShopManager.MinSellPrice}g");
                priceLabel.AddToClassList("map-screen__shop-sell-price");
                row.Add(priceLabel);

                var actionLabel = new Label("[ SELL ]");
                actionLabel.AddToClassList("map-screen__shop-item-action");
                row.Add(actionLabel);

                string capturedId = tome.TomeId;
                row.RegisterCallback<ClickEvent>(_ => OnShopSellClicked(capturedId));
                _shopServices.Add(row);
            }
        }

        private void OnShopBuyClicked(int index)
        {
            if (shopManager == null) return;
            var result = shopManager.BuyTome(index);
            if (_shopFeedbackLabel != null) _shopFeedbackLabel.text = result.Message;
            if (result.Success) RefreshShopUI();
            UpdateShopGold();
            UpdateStats();
        }

        private void OnShopSellClicked(string tomeId)
        {
            if (shopManager == null) return;
            var result = shopManager.SellTome(tomeId);
            if (_shopFeedbackLabel != null) _shopFeedbackLabel.text = result.Message;
            if (result.Success) RefreshShopUI();
            UpdateShopGold();
            UpdateStats();
        }

        private void OnShopHealClicked()
        {
            if (shopManager == null) return;
            var result = shopManager.BuyHeal();
            if (_shopFeedbackLabel != null) _shopFeedbackLabel.text = result.Message;
            RefreshShopUI();
            UpdateShopGold();
            UpdateStats();
        }

        private void UpdateShopGold()
        {
            if (_shopGoldLabel != null && Run.RunManager.Instance != null)
                _shopGoldLabel.text = $"GOLD: {Run.RunManager.Instance.Gold}g";
        }

        private static string GetShopRarityClass(TomeDataSO tome)
        {
            if (tome == null) return "map-screen__shop-item-name--common";
            return tome.rarity switch
            {
                TomeRarity.Common => "map-screen__shop-item-name--common",
                TomeRarity.Uncommon => "map-screen__shop-item-name--uncommon",
                TomeRarity.Rare => "map-screen__shop-item-name--rare",
                TomeRarity.Legendary => "map-screen__shop-item-name--legendary",
                _ => "map-screen__shop-item-name--common"
            };
        }

        private class DungeonNodeEntry
        {
            public VisualElement Row;
            public VisualElement Node;
            public Label Indicator;
            public Label RoomLabel;
            public Label PermsLabel;
            public Label SignalLabel;
            public Label OutcomeLabel;
            public VisualElement Dossier;
            public int Index;
            public NodeType Type;
            public bool DossierExpanded;
        }
    }
}
