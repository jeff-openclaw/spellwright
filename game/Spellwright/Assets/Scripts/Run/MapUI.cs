using System.Collections.Generic;
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
    /// Renders the run map as a vertical node list.
    /// Current node is highlighted, completed nodes are checked.
    /// Player clicks "Proceed" to enter the current node.
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform nodeContainer;
        [SerializeField] private Button proceedButton;
        [SerializeField] private TextMeshProUGUI mapTitleText;
        [SerializeField] private TextMeshProUGUI statsText;

        [Header("Language")]
        [SerializeField] private Button languageButton;
        [SerializeField] private TextMeshProUGUI languageButtonText;
        [SerializeField] private GameConfigSO gameConfig;

        [Header("Theme")]
        [SerializeField] private TerminalThemeSO theme;

        [Header("Prefab (optional)")]
        [SerializeField] private GameObject nodeEntryPrefab;

        private readonly List<MapNodeEntry> _nodeEntries = new List<MapNodeEntry>();

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
            ClearNodes();

            if (RunManager.Instance == null || !RunManager.Instance.IsRunActive)
            {
                if (mapTitleText != null)
                    mapTitleText.text = "No active run";
                return;
            }

            if (mapTitleText != null)
                mapTitleText.text = "- YOUR JOURNEY -";

            var sequence = RunManager.Instance.NodeSequence;
            for (int i = 0; i < sequence.Count; i++)
            {
                CreateNodeEntry(i, sequence[i]);
            }

            UpdateNodeStates();
            UpdateStats();
            UpdateLanguageButtonVisibility();
        }

        private void CreateNodeEntry(int index, NodeType nodeType)
        {
            GameObject entryGO;

            if (nodeEntryPrefab != null)
            {
                entryGO = Instantiate(nodeEntryPrefab, nodeContainer);
            }
            else
            {
                entryGO = new GameObject($"Node_{index}");
                entryGO.transform.SetParent(nodeContainer, false);

                var rt = entryGO.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 36);

                var bg = entryGO.AddComponent<Image>();
                bg.color = theme != null
                    ? new Color(theme.panelBg.r, theme.panelBg.g, theme.panelBg.b, 0.6f)
                    : new Color(0.03f, 0.08f, 0.03f, 0.6f);

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(entryGO.transform, false);
                var labelRT = labelGO.AddComponent<RectTransform>();
                labelRT.anchorMin = Vector2.zero;
                labelRT.anchorMax = Vector2.one;
                labelRT.offsetMin = new Vector2(10, 0);
                labelRT.offsetMax = new Vector2(-10, 0);

                var label = labelGO.AddComponent<TextMeshProUGUI>();
                if (theme != null && theme.primaryFont != null)
                    label.font = theme.primaryFont;
                label.fontSize = theme != null ? theme.labelSize : 14;
                label.color = theme != null ? theme.phosphorGreen : Color.white;
                label.alignment = TextAlignmentOptions.MidlineLeft;
            }

            var entry = new MapNodeEntry
            {
                Root = entryGO,
                Background = entryGO.GetComponent<Image>(),
                Label = entryGO.GetComponentInChildren<TextMeshProUGUI>(),
                Index = index,
                Type = nodeType
            };

            _nodeEntries.Add(entry);
        }

        private void UpdateNodeStates()
        {
            if (RunManager.Instance == null) return;

            int currentIndex = RunManager.Instance.CurrentNodeIndex;

            Color completedColor = theme != null ? theme.nodeCompleted : new Color(0f, 0.5f, 0.18f);
            Color currentColor = theme != null ? theme.nodeCurrent : new Color(0f, 1f, 0.33f);
            Color futureColor = theme != null ? theme.nodeFuture : new Color(0.2f, 0.35f, 0.2f);
            Color bossColor = theme != null ? theme.nodeBoss : new Color(0.85f, 0.1f, 0.1f);

            foreach (var entry in _nodeEntries)
            {
                string icon = GetNodeIcon(entry.Type);
                string typeName = GetNodeTypeName(entry.Type);
                string prefix;
                Color color;

                if (entry.Index < currentIndex)
                {
                    prefix = "[DONE]";
                    color = completedColor;
                }
                else if (entry.Index == currentIndex)
                {
                    prefix = ">>>";
                    color = entry.Type == NodeType.Boss ? bossColor : currentColor;
                }
                else
                {
                    prefix = "   ";
                    color = futureColor;
                }

                if (entry.Label != null)
                    entry.Label.text = $" {prefix}  {icon} {typeName}";

                if (entry.Background != null)
                {
                    var bgColor = entry.Index == currentIndex
                        ? new Color(color.r, color.g, color.b, 0.25f)
                        : (theme != null
                            ? new Color(theme.panelBg.r, theme.panelBg.g, theme.panelBg.b, 0.6f)
                            : new Color(0.03f, 0.08f, 0.03f, 0.6f));
                    entry.Background.color = bgColor;
                }

                if (entry.Label != null)
                    entry.Label.color = color;
            }
        }

        private void UpdateStats()
        {
            if (statsText == null || RunManager.Instance == null) return;

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

        private static string GetNodeIcon(NodeType type)
        {
            return type switch
            {
                NodeType.Encounter => "[E]",
                NodeType.Shop => "[S]",
                NodeType.Boss => "[B]",
                NodeType.Rest => "[R]",
                _ => "[?]"
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
            public TextMeshProUGUI Label;
            public int Index;
            public NodeType Type;
        }
    }
}
