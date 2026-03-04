using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.ScriptableObjects;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellwright.UI
{
    /// <summary>
    /// UI Toolkit-based map screen controller. Replaces the uGUI MapUI.
    /// Renders the run map as a vertical node list with staggered entrance
    /// animations, a breathing glow on the current node, stat chips, and
    /// language toggle.
    /// </summary>
    public class MapController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private GameConfigSO gameConfig;

        [Header("Animation")]
        [SerializeField] private float staggerDelayMs = 60f;
        [SerializeField] private float glowIntervalMs = 550f;

        private VisualElement _root;
        private Label _titleLabel;
        private Label _waveLabel;
        private Label _hpLabel;
        private Label _goldLabel;
        private Label _scoreLabel;
        private Label _rivalInfoLabel;
        private ScrollView _nodeContainer;
        private Button _proceedButton;
        private Button _langToggleButton;

        private readonly List<NodeEntry> _nodeEntries = new();
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
            _titleLabel = _root.Q<Label>("title");
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
            {
                if (_titleLabel != null)
                    _titleLabel.text = "No active run";
                return;
            }

            if (_titleLabel != null)
                _titleLabel.text = "\u2550\u2550 YOUR JOURNEY \u2550\u2550";

            var sequence = Run.RunManager.Instance.NodeSequence;
            for (int i = 0; i < sequence.Count; i++)
                CreateNodeEntry(i, sequence[i], i == sequence.Count - 1);

            UpdateNodeStates();
            UpdateStats();
            UpdateLanguageButtonVisibility();
            PlayEntranceAnimation();
        }

        private void CreateNodeEntry(int index, NodeType nodeType, bool isLast)
        {
            bool isBoss = nodeType == NodeType.Boss;

            // Card root
            var card = new VisualElement();
            card.AddToClassList("map-screen__node");
            if (isBoss)
                card.AddToClassList("map-screen__node--boss");

            // Left color stripe
            var stripe = new VisualElement();
            stripe.AddToClassList("map-screen__node-stripe");
            card.Add(stripe);

            // Tree connector
            var connector = new Label(isLast ? "\u2514\u2500\u2500" : "\u251C\u2500\u2500");
            connector.AddToClassList("map-screen__node-connector");
            card.Add(connector);

            // Icon badge
            var icon = new Label(GetNodeIcon(nodeType));
            icon.AddToClassList("map-screen__node-icon");
            card.Add(icon);

            // Label
            var label = new Label(GetNodeTypeName(nodeType));
            label.AddToClassList("map-screen__node-label");
            if (isBoss)
                label.AddToClassList("map-screen__node-label--boss");
            card.Add(label);

            // Status indicator
            var status = new Label("-");
            status.AddToClassList("map-screen__node-status");
            card.Add(status);

            // Start invisible for staggered entrance
            card.AddToClassList("stagger-item");

            _nodeContainer.Add(card);

            _nodeEntries.Add(new NodeEntry
            {
                Card = card,
                Stripe = stripe,
                Connector = connector,
                Icon = icon,
                Label = label,
                Status = status,
                Index = index,
                Type = nodeType
            });
        }

        // ── Node States ─────────────────────────────────────

        private void UpdateNodeStates()
        {
            if (Run.RunManager.Instance == null) return;

            StopGlow();
            int currentIndex = Run.RunManager.Instance.CurrentNodeIndex;

            foreach (var entry in _nodeEntries)
            {
                bool isBoss = entry.Type == NodeType.Boss;

                // Remove all state classes
                RemoveStateClasses(entry);

                string colorClass;
                string stripeClass;
                string stateClass;
                string statusIcon;

                if (entry.Index < currentIndex)
                {
                    // Completed
                    colorClass = "node-color--completed";
                    stripeClass = "node-stripe--completed";
                    stateClass = "map-screen__node--completed";
                    statusIcon = "+";
                }
                else if (entry.Index == currentIndex)
                {
                    // Current
                    colorClass = isBoss ? "node-color--boss" : "node-color--current";
                    stripeClass = isBoss ? "node-stripe--boss" : "node-stripe--current";
                    stateClass = isBoss ? "map-screen__node--current-boss" : "map-screen__node--current";
                    statusIcon = ">";
                }
                else
                {
                    // Future
                    colorClass = isBoss ? "node-color--boss" : "node-color--future";
                    stripeClass = isBoss ? "node-stripe--boss" : "node-stripe--future";
                    stateClass = isBoss ? "map-screen__node--future-boss" : "map-screen__node--future";
                    statusIcon = "-";
                }

                entry.Card.AddToClassList(stateClass);
                entry.Stripe.AddToClassList(stripeClass);
                entry.Connector.AddToClassList(entry.Index <= currentIndex ? colorClass : "node-color--future");
                entry.Icon.AddToClassList(colorClass);
                entry.Label.AddToClassList(colorClass);
                entry.Status.AddToClassList(colorClass);

                entry.Label.text = isBoss ? $"<< {GetNodeTypeName(entry.Type)} >>" : GetNodeTypeName(entry.Type);
                entry.Status.text = statusIcon;
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
                _waveLabel.text = $"# Wave {Run.RunManager.Instance.WaveNumber}";
            if (_hpLabel != null)
                _hpLabel.text = $"HP {Run.RunManager.Instance.CurrentHP}/{Run.RunManager.Instance.MaxHP}";
            if (_goldLabel != null)
                _goldLabel.text = $"$ {Run.RunManager.Instance.Gold}g";
            if (_scoreLabel != null)
                _scoreLabel.text = $"* {Run.RunManager.Instance.Score}";

            // Rival info
            if (_rivalInfoLabel != null)
            {
                var rival = Run.RivalSystem.Instance;
                _rivalInfoLabel.text = rival != null && rival.HasRival
                    ? $"RIVAL: {rival.RivalDisplayName}"
                    : "";
            }
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
                    entry.Card.AddToClassList("stagger-visible");
                }).ExecuteLater(delay);
            }
        }

        private void StartGlow()
        {
            if (_currentGlowNodeIndex < 0 || _currentGlowNodeIndex >= _nodeEntries.Count)
                return;

            var card = _nodeEntries[_currentGlowNodeIndex].Card;
            card.AddToClassList("map-screen__node--glow-bright");

            _glowSchedule = _root.schedule.Execute(() =>
            {
                _glowBright = !_glowBright;
                if (_glowBright)
                {
                    card.RemoveFromClassList("map-screen__node--glow-dim");
                    card.AddToClassList("map-screen__node--glow-bright");
                }
                else
                {
                    card.RemoveFromClassList("map-screen__node--glow-bright");
                    card.AddToClassList("map-screen__node--glow-dim");
                }
            }).Every((long)glowIntervalMs);
        }

        private void StopGlow()
        {
            _glowSchedule?.Pause();
            _glowSchedule = null;

            if (_currentGlowNodeIndex >= 0 && _currentGlowNodeIndex < _nodeEntries.Count)
            {
                var card = _nodeEntries[_currentGlowNodeIndex].Card;
                card.RemoveFromClassList("map-screen__node--glow-bright");
                card.RemoveFromClassList("map-screen__node--glow-dim");
            }
            _currentGlowNodeIndex = -1;
        }

        // ── Helpers ─────────────────────────────────────────

        private void ClearNodes()
        {
            _nodeContainer?.Clear();
            _nodeEntries.Clear();
        }

        private static void RemoveStateClasses(NodeEntry entry)
        {
            // Card state
            entry.Card.RemoveFromClassList("map-screen__node--completed");
            entry.Card.RemoveFromClassList("map-screen__node--current");
            entry.Card.RemoveFromClassList("map-screen__node--current-boss");
            entry.Card.RemoveFromClassList("map-screen__node--future");
            entry.Card.RemoveFromClassList("map-screen__node--future-boss");

            // Color classes
            string[] colorClasses = { "node-color--completed", "node-color--current", "node-color--future", "node-color--boss" };
            foreach (var cls in colorClasses)
            {
                entry.Connector.RemoveFromClassList(cls);
                entry.Icon.RemoveFromClassList(cls);
                entry.Label.RemoveFromClassList(cls);
                entry.Status.RemoveFromClassList(cls);
            }

            // Stripe classes
            string[] stripeClasses = { "node-stripe--completed", "node-stripe--current", "node-stripe--future", "node-stripe--boss" };
            foreach (var cls in stripeClasses)
                entry.Stripe.RemoveFromClassList(cls);
        }

        private static string GetNodeIcon(NodeType type)
        {
            return type switch
            {
                NodeType.Encounter => ">",
                NodeType.Shop => "$",
                NodeType.Boss => "!",
                NodeType.Rest => "~",
                _ => "-"
            };
        }

        private static string GetNodeTypeName(NodeType type)
        {
            return type switch
            {
                NodeType.Encounter => "Encounter",
                NodeType.Shop => "Shop",
                NodeType.Boss => "BOSS",
                NodeType.Rest => "Rest",
                _ => "Unknown"
            };
        }

        private class NodeEntry
        {
            public VisualElement Card;
            public VisualElement Stripe;
            public Label Connector;
            public Label Icon;
            public Label Label;
            public Label Status;
            public int Index;
            public NodeType Type;
        }
    }
}
