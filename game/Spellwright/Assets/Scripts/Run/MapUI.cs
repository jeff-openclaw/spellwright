using System.Collections.Generic;
using DG.Tweening;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.ScriptableObjects;
using Spellwright.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.Run
{
    /// <summary>
    /// Renders the run map as a vertical node list with rich card-style entries,
    /// DOTween entrance animations, and a breathing glow on the current node.
    /// Balatro-inspired: colored stripes, icon badges, stat chips, staggered reveal.
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform nodeContainer;
        [SerializeField] private Button proceedButton;
        [SerializeField] private TextMeshProUGUI mapTitleText;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI hpStatText;
        [SerializeField] private TextMeshProUGUI goldStatText;
        [SerializeField] private TextMeshProUGUI scoreStatText;

        [Header("Language")]
        [SerializeField] private Button languageButton;
        [SerializeField] private TextMeshProUGUI languageButtonText;
        [SerializeField] private GameConfigSO gameConfig;

        [Header("Theme")]
        [SerializeField] private TerminalThemeSO theme;

        // Keep legacy field for backward compat with old scenes
        [SerializeField] private TextMeshProUGUI statsText;

        [Header("Prefab (optional)")]
        [SerializeField] private GameObject nodeEntryPrefab;

        [Header("Animation")]
        [SerializeField] private float staggerDelay = 0.06f;
        [SerializeField] private float nodeEntranceDuration = 0.35f;
        [SerializeField] private float currentNodeGlowSpeed = 1.8f;

        private readonly List<MapNodeEntry> _nodeEntries = new List<MapNodeEntry>();
        private Tweener _currentNodeGlow;

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<RunStartedEvent>(OnRunStarted);
            EventBus.Instance.Subscribe<RunStateChangedEvent>(OnRunStateChanged);

            if (proceedButton != null)
                proceedButton.onClick.AddListener(OnProceedClicked);
            if (languageButton != null)
                languageButton.onClick.AddListener(OnLanguageClicked);

            UpdateLanguageLabel();
            RefreshMap();
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<RunStartedEvent>(OnRunStarted);
            EventBus.Instance.Unsubscribe<RunStateChangedEvent>(OnRunStateChanged);

            if (proceedButton != null)
                proceedButton.onClick.RemoveListener(OnProceedClicked);
            if (languageButton != null)
                languageButton.onClick.RemoveListener(OnLanguageClicked);

            KillAnimations();
        }

        private void OnRunStarted(RunStartedEvent evt)
        {
            RefreshMap();
        }

        private void OnRunStateChanged(RunStateChangedEvent evt)
        {
            UpdateNodeStates();
            UpdateStats();
        }

        /// <summary>Clears and rebuilds the node list from RunManager.</summary>
        public void RefreshMap()
        {
            KillAnimations();
            ClearNodes();

            if (RunManager.Instance == null || !RunManager.Instance.IsRunActive)
            {
                if (mapTitleText != null)
                    mapTitleText.text = "No active run";
                return;
            }

            if (mapTitleText != null)
                mapTitleText.text = "\u2550\u2550 YOUR JOURNEY \u2550\u2550";

            var sequence = RunManager.Instance.NodeSequence;
            for (int i = 0; i < sequence.Count; i++)
            {
                CreateNodeEntry(i, sequence[i]);
            }

            UpdateNodeStates();
            UpdateStats();
            UpdateLanguageButtonVisibility();

            // Force layout computation so PlayEntranceAnimation captures correct positions
            Canvas.ForceUpdateCanvases();
            PlayEntranceAnimation();
        }

        private void CreateNodeEntry(int index, NodeType nodeType)
        {
            if (nodeEntryPrefab != null)
            {
                var prefabGO = Instantiate(nodeEntryPrefab, nodeContainer);
                var entry = new MapNodeEntry
                {
                    Root = prefabGO,
                    Background = prefabGO.GetComponent<Image>(),
                    Label = prefabGO.GetComponentInChildren<TextMeshProUGUI>(),
                    Index = index,
                    Type = nodeType
                };
                _nodeEntries.Add(entry);
                return;
            }

            bool isBoss = nodeType == NodeType.Boss;
            float nodeHeight = isBoss ? 64 : 50;

            // ── Card root ──
            var entryGO = new GameObject($"Node_{index}");
            entryGO.transform.SetParent(nodeContainer, false);

            var rt = entryGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, nodeHeight);

            // Card has a horizontal layout: stripe | connector | icon | label | status
            var hlg = entryGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 0;
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var bg = entryGO.AddComponent<Image>();
            bg.color = theme != null
                ? new Color(theme.panelBg.r, theme.panelBg.g, theme.panelBg.b, 0.7f)
                : new Color(0.03f, 0.08f, 0.03f, 0.7f);

            // Border outline
            var outline = entryGO.AddComponent<Outline>();
            outline.effectColor = theme != null
                ? new Color(theme.borderColor.r, theme.borderColor.g, theme.borderColor.b, 0.3f)
                : new Color(0.05f, 0.55f, 0.25f, 0.3f);
            outline.effectDistance = new Vector2(1, -1);

            // ── Left color stripe (4px, type-colored) ──
            var stripe = new GameObject("Stripe");
            stripe.transform.SetParent(entryGO.transform, false);
            stripe.AddComponent<RectTransform>();
            var stripeImg = stripe.AddComponent<Image>();
            stripeImg.color = GetNodeTypeColor(nodeType);
            var stripeLe = stripe.AddComponent<LayoutElement>();
            stripeLe.minWidth = 4;
            stripeLe.preferredWidth = 4;
            stripeLe.flexibleWidth = 0;

            // ── Connector line (tree connector between nodes) ──
            var connGO = new GameObject("Connector");
            connGO.transform.SetParent(entryGO.transform, false);
            connGO.AddComponent<RectTransform>();
            var connTMP = connGO.AddComponent<TextMeshProUGUI>();
            if (theme != null && theme.primaryFont != null)
                connTMP.font = theme.primaryFont;
            connTMP.fontSize = theme != null ? theme.labelSize : 14;
            connTMP.color = theme != null ? theme.phosphorDim : new Color(0.05f, 0.45f, 0.2f);
            connTMP.alignment = TextAlignmentOptions.Center;
            connTMP.raycastTarget = false;
            var connLe = connGO.AddComponent<LayoutElement>();
            connLe.minWidth = 36;
            connLe.preferredWidth = 36;
            connLe.flexibleWidth = 0;

            // ── Icon badge ──
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(entryGO.transform, false);
            iconGO.AddComponent<RectTransform>();
            var iconTMP = iconGO.AddComponent<TextMeshProUGUI>();
            if (theme != null && theme.primaryFont != null)
                iconTMP.font = theme.primaryFont;
            iconTMP.fontSize = isBoss ? (theme != null ? theme.bodySize + 2 : 20) : (theme != null ? theme.bodySize : 18);
            iconTMP.color = GetNodeTypeColor(nodeType);
            iconTMP.text = GetNodeIcon(nodeType);
            iconTMP.alignment = TextAlignmentOptions.Center;
            iconTMP.raycastTarget = false;
            var iconLe = iconGO.AddComponent<LayoutElement>();
            iconLe.minWidth = 32;
            iconLe.preferredWidth = 32;
            iconLe.flexibleWidth = 0;

            // ── Label (fills remaining space) ──
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(entryGO.transform, false);
            labelGO.AddComponent<RectTransform>();
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            if (theme != null && theme.primaryFont != null)
                label.font = theme.primaryFont;
            label.fontSize = isBoss ? (theme != null ? theme.bodySize : 18) : (theme != null ? theme.labelSize : 14);
            label.color = theme != null ? theme.phosphorGreen : Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            if (isBoss)
                label.fontStyle = FontStyles.Bold;
            var labelLe = labelGO.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1;

            // ── Status indicator (right side) ──
            var statusGO = new GameObject("Status");
            statusGO.transform.SetParent(entryGO.transform, false);
            statusGO.AddComponent<RectTransform>();
            var statusTMP = statusGO.AddComponent<TextMeshProUGUI>();
            if (theme != null && theme.primaryFont != null)
                statusTMP.font = theme.primaryFont;
            statusTMP.fontSize = theme != null ? theme.smallSize : 13;
            statusTMP.color = theme != null ? theme.phosphorDim : new Color(0.05f, 0.45f, 0.2f);
            statusTMP.alignment = TextAlignmentOptions.Center;
            statusTMP.raycastTarget = false;
            var statusLe = statusGO.AddComponent<LayoutElement>();
            statusLe.minWidth = 50;
            statusLe.preferredWidth = 50;
            statusLe.flexibleWidth = 0;

            // Add a CanvasGroup for fade animation
            var cg = entryGO.AddComponent<CanvasGroup>();
            cg.alpha = 0f; // Start invisible for entrance animation

            var entry2 = new MapNodeEntry
            {
                Root = entryGO,
                Background = bg,
                Outline = outline,
                Stripe = stripeImg,
                Label = label,
                IconText = iconTMP,
                ConnectorText = connTMP,
                StatusText = statusTMP,
                CanvasGroup = cg,
                Index = index,
                Type = nodeType
            };

            _nodeEntries.Add(entry2);
        }

        private void PlayEntranceAnimation()
        {
            for (int i = 0; i < _nodeEntries.Count; i++)
            {
                var entry = _nodeEntries[i];
                if (entry.CanvasGroup == null) continue;

                float delay = i * staggerDelay;

                // Fade in only — avoid DOAnchorPos which conflicts with the VerticalLayoutGroup
                entry.CanvasGroup.alpha = 0f;
                DOTween.Sequence()
                    .SetDelay(delay)
                    .Append(entry.CanvasGroup.DOFade(1f, nodeEntranceDuration).SetEase(Ease.OutCubic))
                    .SetUpdate(true);
            }
        }

        private void UpdateNodeStates()
        {
            if (RunManager.Instance == null) return;

            // Kill previous glow
            if (_currentNodeGlow != null)
            {
                _currentNodeGlow.Kill();
                _currentNodeGlow = null;
            }

            int currentIndex = RunManager.Instance.CurrentNodeIndex;
            int totalNodes = _nodeEntries.Count;

            Color completedColor = theme != null ? theme.nodeCompleted : new Color(0f, 0.5f, 0.18f);
            Color currentColor = theme != null ? theme.nodeCurrent : new Color(0f, 1f, 0.33f);
            Color futureColor = theme != null ? theme.nodeFuture : new Color(0.2f, 0.35f, 0.2f);
            Color bossColor = theme != null ? theme.nodeBoss : new Color(0.85f, 0.1f, 0.1f);

            foreach (var entry in _nodeEntries)
            {
                string typeName = GetNodeTypeName(entry.Type);
                bool isLast = entry.Index == totalNodes - 1;
                string connector = isLast ? "\u2514\u2500\u2500" : "\u251C\u2500\u2500";

                Color nodeColor;
                string statusIcon;
                float bgAlpha;

                if (entry.Index < currentIndex)
                {
                    // Completed
                    nodeColor = completedColor;
                    statusIcon = "+"; // checkmark
                    bgAlpha = 0.6f;
                }
                else if (entry.Index == currentIndex)
                {
                    // Current — highlighted
                    nodeColor = entry.Type == NodeType.Boss ? bossColor : currentColor;
                    statusIcon = ">"; // play arrow
                    bgAlpha = 0.9f;
                }
                else
                {
                    // Future — visible but subdued
                    nodeColor = entry.Type == NodeType.Boss ? bossColor : futureColor;
                    statusIcon = "-";
                    bgAlpha = 0.65f;
                }

                // Update connector
                if (entry.ConnectorText != null)
                {
                    entry.ConnectorText.text = connector;
                    entry.ConnectorText.color = entry.Index <= currentIndex ? nodeColor
                        : (theme != null ? theme.phosphorDim : futureColor);
                }

                // Update icon color
                if (entry.IconText != null)
                    entry.IconText.color = nodeColor;

                // Update label
                if (entry.Label != null)
                {
                    entry.Label.text = entry.Type == NodeType.Boss
                        ? $"<< {typeName} >>"
                        : typeName;
                    entry.Label.color = nodeColor;
                }

                // Update status indicator
                if (entry.StatusText != null)
                {
                    entry.StatusText.text = statusIcon;
                    entry.StatusText.color = nodeColor;
                }

                // Update stripe color
                if (entry.Stripe != null)
                    entry.Stripe.color = nodeColor;

                // Update background
                if (entry.Background != null)
                {
                    Color baseBg = theme != null
                        ? new Color(theme.panelBg.r, theme.panelBg.g, theme.panelBg.b)
                        : new Color(0.03f, 0.08f, 0.03f);

                    if (entry.Index == currentIndex)
                    {
                        // Current node gets tinted background
                        entry.Background.color = new Color(
                            baseBg.r + nodeColor.r * 0.06f,
                            baseBg.g + nodeColor.g * 0.06f,
                            baseBg.b + nodeColor.b * 0.06f,
                            bgAlpha);
                    }
                    else
                    {
                        entry.Background.color = new Color(baseBg.r, baseBg.g, baseBg.b, bgAlpha);
                    }
                }

                // Update outline
                if (entry.Outline != null)
                {
                    if (entry.Index == currentIndex)
                    {
                        entry.Outline.effectColor = new Color(nodeColor.r, nodeColor.g, nodeColor.b, 0.7f);
                        entry.Outline.effectDistance = new Vector2(2, -2);
                    }
                    else if (entry.Index < currentIndex)
                    {
                        entry.Outline.effectColor = new Color(nodeColor.r, nodeColor.g, nodeColor.b, 0.2f);
                        entry.Outline.effectDistance = new Vector2(1, -1);
                    }
                    else
                    {
                        entry.Outline.effectColor = new Color(0, 0, 0, 0);
                    }
                }

                // Breathing glow on current node via CanvasGroup alpha pulsing
                if (entry.Index == currentIndex && entry.CanvasGroup != null)
                {
                    entry.CanvasGroup.alpha = 1f;
                    _currentNodeGlow = entry.CanvasGroup
                        .DOFade(0.75f, 1f / currentNodeGlowSpeed)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetUpdate(true);
                }
                else if (entry.CanvasGroup != null)
                {
                    entry.CanvasGroup.alpha = 1f;
                }
            }
        }

        private void UpdateStats()
        {
            if (RunManager.Instance == null) return;

            if (waveText != null)
                waveText.text = $"# Wave {RunManager.Instance.WaveNumber}";

            if (hpStatText != null)
                hpStatText.text = $"HP {RunManager.Instance.CurrentHP}/{RunManager.Instance.MaxHP}";

            if (goldStatText != null)
                goldStatText.text = $"$ {RunManager.Instance.Gold}g";

            if (scoreStatText != null)
                scoreStatText.text = $"* {RunManager.Instance.Score}";

            // Legacy single stats field fallback
            if (statsText != null)
                statsText.text = $"HP: {RunManager.Instance.CurrentHP}/{RunManager.Instance.MaxHP}  |  "
                    + $"Gold: {RunManager.Instance.Gold}  |  "
                    + $"Score: {RunManager.Instance.Score}";
        }

        private void OnProceedClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ProceedToCurrentNode();
        }

        private void ClearNodes()
        {
            foreach (var entry in _nodeEntries)
            {
                if (entry.Root != null)
                    Destroy(entry.Root);
            }
            _nodeEntries.Clear();
        }

        private void KillAnimations()
        {
            if (_currentNodeGlow != null)
            {
                _currentNodeGlow.Kill();
                _currentNodeGlow = null;
            }

            foreach (var entry in _nodeEntries)
            {
                if (entry.Root != null)
                {
                    DOTween.Kill(entry.CanvasGroup);
                    DOTween.Kill(entry.Root.GetComponent<RectTransform>());
                }
            }
        }

        private Color GetNodeTypeColor(NodeType type)
        {
            return type switch
            {
                NodeType.Encounter => theme != null ? theme.phosphorGreen : new Color(0.12f, 1f, 0.45f),
                NodeType.Shop => theme != null ? theme.amberBright : new Color(1f, 0.78f, 0.15f),
                NodeType.Boss => theme != null ? theme.nodeBoss : new Color(0.9f, 0.12f, 0.12f),
                NodeType.Rest => theme != null ? theme.cyanInfo : new Color(0.15f, 0.9f, 0.95f),
                _ => theme != null ? theme.phosphorDim : new Color(0.05f, 0.45f, 0.2f)
            };
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

        private void OnLanguageClicked()
        {
            if (gameConfig == null) return;

            gameConfig.language = gameConfig.language == GameLanguage.English
                ? GameLanguage.Romanian
                : GameLanguage.English;

            UpdateLanguageLabel();
            Debug.Log($"[MapUI] Language set to {gameConfig.language}");
        }

        private void UpdateLanguageLabel()
        {
            if (languageButtonText == null) return;
            languageButtonText.text = gameConfig != null && gameConfig.language == GameLanguage.Romanian
                ? "RO" : "EN";
        }

        private void UpdateLanguageButtonVisibility()
        {
            if (languageButton == null) return;

            // Only show language picker before the first encounter starts
            bool show = RunManager.Instance != null && RunManager.Instance.CurrentNodeIndex == 0;
            languageButton.gameObject.SetActive(show);
        }

        private class MapNodeEntry
        {
            public GameObject Root;
            public Image Background;
            public Outline Outline;
            public Image Stripe;
            public TextMeshProUGUI Label;
            public TextMeshProUGUI IconText;
            public TextMeshProUGUI ConnectorText;
            public TextMeshProUGUI StatusText;
            public CanvasGroup CanvasGroup;
            public int Index;
            public NodeType Type;
        }
    }
}
