using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
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
        [SerializeField] private Text mapTitleText;
        [SerializeField] private Text statsText;

        [Header("Prefab (optional)")]
        [SerializeField] private GameObject nodeEntryPrefab;

        private readonly List<MapNodeEntry> _nodeEntries = new List<MapNodeEntry>();

        private static readonly Color CompletedColor = new Color(0.3f, 0.6f, 0.3f, 1f);
        private static readonly Color CurrentColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color FutureColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color BossColor = new Color(0.8f, 0.2f, 0.2f, 1f);

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<RunStartedEvent>(OnRunStarted);
            EventBus.Instance.Subscribe<RunStateChangedEvent>(OnRunStateChanged);

            if (proceedButton != null)
                proceedButton.onClick.AddListener(OnProceedClicked);

            RefreshMap();
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<RunStartedEvent>(OnRunStarted);
            EventBus.Instance.Unsubscribe<RunStateChangedEvent>(OnRunStateChanged);

            if (proceedButton != null)
                proceedButton.onClick.RemoveListener(OnProceedClicked);
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
                bg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(entryGO.transform, false);
                var labelRT = labelGO.AddComponent<RectTransform>();
                labelRT.anchorMin = Vector2.zero;
                labelRT.anchorMax = Vector2.one;
                labelRT.offsetMin = new Vector2(10, 0);
                labelRT.offsetMax = new Vector2(-10, 0);

                var label = labelGO.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 16;
                label.color = Color.white;
                label.alignment = TextAnchor.MiddleLeft;
            }

            var entry = new MapNodeEntry
            {
                Root = entryGO,
                Background = entryGO.GetComponent<Image>(),
                Label = entryGO.GetComponentInChildren<Text>(),
                Index = index,
                Type = nodeType
            };

            _nodeEntries.Add(entry);
        }

        private void UpdateNodeStates()
        {
            if (RunManager.Instance == null) return;

            int currentIndex = RunManager.Instance.CurrentNodeIndex;

            foreach (var entry in _nodeEntries)
            {
                string icon = GetNodeIcon(entry.Type);
                string typeName = GetNodeTypeName(entry.Type);
                string prefix;
                Color color;

                if (entry.Index < currentIndex)
                {
                    prefix = "[DONE]";
                    color = CompletedColor;
                }
                else if (entry.Index == currentIndex)
                {
                    prefix = ">>>";
                    color = entry.Type == NodeType.Boss ? BossColor : CurrentColor;
                }
                else
                {
                    prefix = "   ";
                    color = FutureColor;
                }

                if (entry.Label != null)
                    entry.Label.text = $" {prefix}  {icon} {typeName}";

                if (entry.Background != null)
                {
                    var bgColor = entry.Index == currentIndex
                        ? new Color(color.r, color.g, color.b, 0.25f)
                        : new Color(0.15f, 0.15f, 0.15f, 0.6f);
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

        private class MapNodeEntry
        {
            public GameObject Root;
            public Image Background;
            public Text Label;
            public int Index;
            public NodeType Type;
        }
    }
}
