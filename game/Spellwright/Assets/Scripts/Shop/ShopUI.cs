using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Run;
using Spellwright.Tomes;
using Spellwright.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.Shop
{
    /// <summary>
    /// UI for the shop screen. Shows buyable Tomes, heal item,
    /// equipped Tomes for selling, and a Leave button.
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ShopManager shopManager;
        [SerializeField] private Transform itemContainer;
        [SerializeField] private Transform equippedContainer;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private Button leaveButton;

        [Header("Theme")]
        [SerializeField] private TerminalThemeSO theme;

        private readonly List<GameObject> _itemEntries = new List<GameObject>();
        private readonly List<GameObject> _equippedEntries = new List<GameObject>();

        private void OnEnable()
        {
            if (leaveButton != null)
                leaveButton.onClick.AddListener(OnLeaveClicked);

            EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);

            // Generate inventory once on enable (GameStateChangedEvent handler defers to avoid double-gen)
            if (shopManager != null)
            {
                shopManager.GenerateInventory();
                RefreshUI();
            }
        }

        private void OnDisable()
        {
            if (leaveButton != null)
                leaveButton.onClick.RemoveListener(OnLeaveClicked);

            EventBus.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            // Inventory is already generated in OnEnable when the panel activates.
            // Only refresh display here in case state changed while already visible.
            if (evt.NewState == GameState.Shop && shopManager != null)
            {
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            ClearEntries(_itemEntries);
            ClearEntries(_equippedEntries);

            if (shopManager == null) return;

            // Shop items
            for (int i = 0; i < shopManager.Inventory.Count; i++)
            {
                var item = shopManager.Inventory[i];
                CreateBuyEntry(i, item);
            }

            // Equipped tomes (for selling)
            if (TomeManager.Instance?.TomeSystem != null)
            {
                foreach (var tome in TomeManager.Instance.TomeSystem.GetEquippedTomes())
                {
                    CreateSellEntry(tome);
                }
            }

            UpdateStatusTexts();
            if (feedbackText != null) feedbackText.text = "";
        }

        private void CreateBuyEntry(int index, ShopItem item)
        {
            var entryGO = new GameObject($"ShopItem_{index}");
            entryGO.transform.SetParent(itemContainer, false);

            var rt = entryGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 50);

            var bg = entryGO.AddComponent<Image>();
            bg.color = item.IsSold
                ? new Color(0.02f, 0.04f, 0.02f, 0.4f)
                : (theme != null ? theme.panelBg : new Color(0.03f, 0.08f, 0.03f, 0.95f));

            // Layout
            var layout = entryGO.AddComponent<HorizontalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 5;
            layout.padding = new RectOffset(8, 8, 4, 4);

            // Info label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(entryGO.transform, false);
            labelGO.AddComponent<RectTransform>();

            var label = labelGO.AddComponent<TextMeshProUGUI>();
            if (theme != null && theme.primaryFont != null)
                label.font = theme.primaryFont;
            label.fontSize = theme != null ? theme.labelSize : 14;

            if (item.IsHealItem)
            {
                int healAmt = shopManager != null ? shopManager.HealAmountValue : 8;
                label.text = $"Heal Potion (+{healAmt} HP) — {item.Price}g";
                label.color = theme != null ? theme.successColor : new Color(0.1f, 0.9f, 0.3f);
            }
            else if (item.TomeData != null)
            {
                label.text = item.IsSold
                    ? $"[SOLD] {item.TomeData.displayName}"
                    : $"{item.TomeData.displayName} ({item.TomeData.rarity}) — {item.Price}g\n  {item.TomeData.description}";
                label.color = item.IsSold
                    ? (theme != null ? theme.inactiveColor : new Color(0.3f, 0.5f, 0.3f))
                    : (theme != null ? theme.GetRarityColor(item.TomeData.rarity) : Color.white);
            }

            label.alignment = TextAlignmentOptions.MidlineLeft;

            // Buy button
            if (!item.IsSold)
            {
                var btnGO = new GameObject("BuyBtn");
                btnGO.transform.SetParent(entryGO.transform, false);
                var btnRT = btnGO.AddComponent<RectTransform>();
                btnRT.sizeDelta = new Vector2(60, 0);

                var btnBg = btnGO.AddComponent<Image>();
                btnBg.color = theme != null ? theme.buttonBg : new Color(0.05f, 0.2f, 0.05f, 0.9f);

                var btn = btnGO.AddComponent<Button>();
                btn.targetGraphic = btnBg;

                var btnLabelGO = new GameObject("BtnLabel");
                btnLabelGO.transform.SetParent(btnGO.transform, false);
                var btnLabelRT = btnLabelGO.AddComponent<RectTransform>();
                btnLabelRT.anchorMin = Vector2.zero;
                btnLabelRT.anchorMax = Vector2.one;
                btnLabelRT.offsetMin = Vector2.zero;
                btnLabelRT.offsetMax = Vector2.zero;

                var btnLabel = btnLabelGO.AddComponent<TextMeshProUGUI>();
                btnLabel.text = "BUY";
                if (theme != null && theme.primaryFont != null)
                    btnLabel.font = theme.primaryFont;
                btnLabel.fontSize = theme != null ? theme.labelSize : 14;
                btnLabel.color = theme != null ? theme.buttonText : new Color(0f, 1f, 0.33f);
                btnLabel.alignment = TextAlignmentOptions.Center;

                int capturedIndex = index;
                btn.onClick.AddListener(() => OnBuyClicked(capturedIndex));

                var le = btnGO.AddComponent<LayoutElement>();
                le.minWidth = 60;
                le.preferredWidth = 60;
                le.flexibleWidth = 0;
            }

            _itemEntries.Add(entryGO);
        }

        private void CreateSellEntry(TomeInstance tome)
        {
            var entryGO = new GameObject($"Equipped_{tome.TomeId}");
            entryGO.transform.SetParent(equippedContainer, false);

            var rt = entryGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 40);

            var bg = entryGO.AddComponent<Image>();
            bg.color = theme != null
                ? new Color(theme.panelBg.r, theme.panelBg.g, theme.panelBg.b, 0.8f)
                : new Color(0.03f, 0.08f, 0.03f, 0.8f);

            var layout = entryGO.AddComponent<HorizontalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 5;
            layout.padding = new RectOffset(8, 8, 4, 4);

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(entryGO.transform, false);
            labelGO.AddComponent<RectTransform>();

            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = $"{tome.TomeName} ({tome.Rarity})";
            if (theme != null && theme.primaryFont != null)
                label.font = theme.primaryFont;
            label.fontSize = theme != null ? theme.labelSize : 14;
            label.color = theme != null ? theme.GetRarityColor(tome.Rarity) : Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            // Sell button
            var btnGO = new GameObject("SellBtn");
            btnGO.transform.SetParent(entryGO.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(70, 0);

            var btnBg = btnGO.AddComponent<Image>();
            btnBg.color = theme != null ? theme.buttonBgDanger : new Color(0.3f, 0.05f, 0.05f, 0.9f);

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnBg;

            var btnLabelGO = new GameObject("BtnLabel");
            btnLabelGO.transform.SetParent(btnGO.transform, false);
            var btnLabelRT = btnLabelGO.AddComponent<RectTransform>();
            btnLabelRT.anchorMin = Vector2.zero;
            btnLabelRT.anchorMax = Vector2.one;
            btnLabelRT.offsetMin = Vector2.zero;
            btnLabelRT.offsetMax = Vector2.zero;

            var btnLabel = btnLabelGO.AddComponent<TextMeshProUGUI>();
            btnLabel.text = "SELL";
            if (theme != null && theme.primaryFont != null)
                btnLabel.font = theme.primaryFont;
            btnLabel.fontSize = theme != null ? theme.labelSize : 14;
            btnLabel.color = theme != null ? theme.buttonText : new Color(0f, 1f, 0.33f);
            btnLabel.alignment = TextAlignmentOptions.Center;

            string capturedId = tome.TomeId;
            btn.onClick.AddListener(() => OnSellClicked(capturedId));

            var le = btnGO.AddComponent<LayoutElement>();
            le.minWidth = 70;
            le.preferredWidth = 70;
            le.flexibleWidth = 0;

            _equippedEntries.Add(entryGO);
        }

        private void OnBuyClicked(int index)
        {
            if (shopManager == null) return;

            var result = shopManager.BuyTome(index);
            if (feedbackText != null) feedbackText.text = result.Message;
            if (result.Success) RefreshUI();

            UpdateStatusTexts();
        }

        private void OnSellClicked(string tomeId)
        {
            if (shopManager == null) return;

            var result = shopManager.SellTome(tomeId);
            if (feedbackText != null) feedbackText.text = result.Message;
            if (result.Success) RefreshUI();

            UpdateStatusTexts();
        }

        private void OnLeaveClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMap();
        }

        private void UpdateStatusTexts()
        {
            if (goldText != null && RunManager.Instance != null)
                goldText.text = $"Gold: {RunManager.Instance.Gold}";

            if (hpText != null && RunManager.Instance != null)
                hpText.text = $"HP: {RunManager.Instance.CurrentHP}/{RunManager.Instance.MaxHP}";
        }

        private void ClearEntries(List<GameObject> entries)
        {
            foreach (var e in entries)
                if (e != null) Destroy(e);
            entries.Clear();
        }
    }
}
