using System.Collections.Generic;
using DG.Tweening;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Run;
using Spellwright.Tomes;
using Spellwright.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

            // Force layout rebuild so cards have correct positions before animation reads them
            if (itemContainer is RectTransform itemRT)
                LayoutRebuilder.ForceRebuildLayoutImmediate(itemRT);
            if (equippedContainer is RectTransform equippedRT)
                LayoutRebuilder.ForceRebuildLayoutImmediate(equippedRT);

            // Staggered entrance animations for cards
            AnimateCardEntrance(_itemEntries, 0f);
            AnimateCardEntrance(_equippedEntries, _itemEntries.Count * 0.06f);

            UpdateStatusTexts();
            if (feedbackText != null) feedbackText.text = "";
        }

        private void AnimateCardEntrance(List<GameObject> entries, float baseDelay)
        {
            float stagger = theme != null ? theme.staggerDelay : 0.08f;
            for (int i = 0; i < entries.Count; i++)
            {
                var card = entries[i];
                if (card == null) continue;

                var cg = card.GetComponent<CanvasGroup>();
                if (cg == null) cg = card.AddComponent<CanvasGroup>();
                cg.alpha = 0f;

                var rt = card.GetComponent<RectTransform>();
                var originalPos = rt.anchoredPosition;
                rt.anchoredPosition = originalPos + new Vector2(-30f, 0f);

                float delay = baseDelay + i * stagger;
                cg.DOFade(1f, 0.25f).SetDelay(delay).SetUpdate(true);
                rt.DOAnchorPos(originalPos, 0.25f).SetDelay(delay).SetEase(Ease.OutCubic).SetUpdate(true);
            }
        }

        private void CreateBuyEntry(int index, ShopItem item)
        {
            // Determine rarity color
            Color rarityColor;
            if (item.IsHealItem)
                rarityColor = theme != null ? theme.successColor : new Color(0.1f, 0.9f, 0.3f);
            else if (item.TomeData != null)
                rarityColor = theme != null ? theme.GetRarityColor(item.TomeData.rarity) : Color.white;
            else
                rarityColor = theme != null ? theme.phosphorGreen : Color.white;

            var (card, content, right) = TerminalUIHelper.CreateItemCard(
                itemContainer, $"ShopItem_{index}", theme, rarityColor, 78);

            // Dim sold items
            if (item.IsSold)
            {
                var cardBg = card.GetComponent<Image>();
                if (cardBg != null)
                    cardBg.color = theme != null ? theme.cardBgSold : new Color(0.02f, 0.04f, 0.02f, 0.4f);
            }

            // ── Content area: name + description ──
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(content.transform, false);
            nameGO.AddComponent<RectTransform>();
            var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
            if (theme != null && theme.primaryFont != null)
                nameTmp.font = theme.primaryFont;
            nameTmp.fontSize = theme != null ? theme.labelSize + 2 : 24;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.raycastTarget = false;
            var nameLe = nameGO.AddComponent<LayoutElement>();
            nameLe.preferredHeight = 28;
            nameLe.minHeight = 24;

            if (item.IsHealItem)
            {
                int healAmt = shopManager != null ? shopManager.HealAmountValue : 8;
                nameTmp.text = $"Heal Potion (+{healAmt} HP)";
                nameTmp.color = theme != null ? theme.successColor : new Color(0.1f, 0.9f, 0.3f);
            }
            else if (item.TomeData != null)
            {
                nameTmp.text = item.IsSold ? $"[SOLD] {item.TomeData.displayName}" : item.TomeData.displayName;
                nameTmp.color = item.IsSold
                    ? (theme != null ? theme.inactiveColor : new Color(0.3f, 0.5f, 0.3f))
                    : rarityColor;
            }
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Description line
            var descGO = new GameObject("Desc");
            descGO.transform.SetParent(content.transform, false);
            descGO.AddComponent<RectTransform>();
            var descTmp = descGO.AddComponent<TextMeshProUGUI>();
            if (theme != null && theme.primaryFont != null)
                descTmp.font = theme.primaryFont;
            descTmp.fontSize = theme != null ? theme.labelSize : 22;
            Color descColor = theme != null
                ? new Color(theme.phosphorGreen.r, theme.phosphorGreen.g, theme.phosphorGreen.b, 0.65f)
                : new Color(0.08f, 0.65f, 0.30f);
            descTmp.color = descColor;
            descTmp.raycastTarget = false;
            descTmp.alignment = TextAlignmentOptions.MidlineLeft;
            var descLe = descGO.AddComponent<LayoutElement>();
            descLe.preferredHeight = 20;
            descLe.minHeight = 16;

            if (item.IsHealItem)
            {
                int curHP = RunManager.Instance != null ? RunManager.Instance.CurrentHP : 0;
                int maxHP = RunManager.Instance != null ? RunManager.Instance.MaxHP : 30;
                int healAmt = shopManager != null ? shopManager.HealAmountValue : 8;
                int newHP = Mathf.Min(curHP + healAmt, maxHP);
                descTmp.text = $"{curHP} \u2192 {newHP} HP";
                descTmp.color = theme != null ? theme.successColor : new Color(0.1f, 0.9f, 0.3f);
            }
            else if (item.TomeData != null)
            {
                descTmp.text = item.IsSold ? "" : item.TomeData.description;
            }

            // ── Right area: price + buy label ──
            if (!item.IsSold)
            {
                // Price tag
                var priceGO = new GameObject("Price");
                priceGO.transform.SetParent(right.transform, false);
                priceGO.AddComponent<RectTransform>();
                var priceTmp = priceGO.AddComponent<TextMeshProUGUI>();
                priceTmp.text = $"{item.Price}g";
                if (theme != null && theme.primaryFont != null)
                    priceTmp.font = theme.primaryFont;
                priceTmp.fontSize = theme != null ? theme.bodySize : 26;
                priceTmp.fontStyle = FontStyles.Bold;
                priceTmp.color = theme != null ? theme.amberBright : new Color(1f, 0.75f, 0f);
                priceTmp.alignment = TextAlignmentOptions.Center;
                priceTmp.raycastTarget = false;

                // Buy label (visual indicator only)
                var labelGO = new GameObject("BuyLabel");
                labelGO.transform.SetParent(right.transform, false);
                labelGO.AddComponent<RectTransform>();
                var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
                labelTmp.text = "[ BUY ]";
                if (theme != null && theme.primaryFont != null)
                    labelTmp.font = theme.primaryFont;
                labelTmp.fontSize = theme != null ? theme.labelSize : 22;
                labelTmp.color = theme != null ? theme.buttonText : new Color(0f, 1f, 0.33f);
                labelTmp.alignment = TextAlignmentOptions.Center;
                labelTmp.raycastTarget = false;
                var labelLe = labelGO.AddComponent<LayoutElement>();
                labelLe.preferredHeight = 24;

                // Make entire card clickable
                var cardBtn = card.AddComponent<Button>();
                cardBtn.targetGraphic = card.GetComponent<Image>();
                int capturedIndex = index;
                cardBtn.onClick.AddListener(() => OnBuyClicked(capturedIndex));
            }

            // Add hover glow to card
            if (!item.IsSold)
                AddCardHoverGlow(card);

            _itemEntries.Add(card);
        }

        private void CreateSellEntry(TomeInstance tome)
        {
            Color rarityColor = theme != null ? theme.GetRarityColor(tome.Rarity) : Color.white;

            var (card, content, right) = TerminalUIHelper.CreateItemCard(
                equippedContainer, $"Equipped_{tome.TomeId}", theme, rarityColor, 62);

            // ── Content: name ──
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(content.transform, false);
            nameGO.AddComponent<RectTransform>();
            var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
            nameTmp.text = tome.TomeName;
            if (theme != null && theme.primaryFont != null)
                nameTmp.font = theme.primaryFont;
            nameTmp.fontSize = theme != null ? theme.labelSize + 2 : 16;
            nameTmp.color = rarityColor;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.raycastTarget = false;
            var sellNameLe = nameGO.AddComponent<LayoutElement>();
            sellNameLe.preferredHeight = 24;
            sellNameLe.minHeight = 20;

            // ── Right: sell button ──

            var btnGO = new GameObject("SellBtn");
            btnGO.transform.SetParent(right.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(0, 24);

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
            btnLabel.text = "[ SELL ]";
            if (theme != null && theme.primaryFont != null)
                btnLabel.font = theme.primaryFont;
            btnLabel.fontSize = theme != null ? theme.labelSize : 22;
            btnLabel.color = theme != null ? theme.buttonText : new Color(0f, 1f, 0.33f);
            btnLabel.alignment = TextAlignmentOptions.Center;
            btnLabel.raycastTarget = false;

            string capturedId = tome.TomeId;
            btn.onClick.AddListener(() => OnSellClicked(capturedId));

            var btnLe = btnGO.AddComponent<LayoutElement>();
            btnLe.preferredHeight = 24;

            AddCardHoverGlow(card);
            _equippedEntries.Add(card);
        }

        private static void AddCardHoverGlow(GameObject card)
        {
            if (card.GetComponent<ShopCardHover>() == null)
                card.AddComponent<ShopCardHover>();
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
                goldText.text = $"$ {RunManager.Instance.Gold}g";

            if (hpText != null && RunManager.Instance != null)
                hpText.text = $"HP {RunManager.Instance.CurrentHP}/{RunManager.Instance.MaxHP}";
        }

        private void ClearEntries(List<GameObject> entries)
        {
            foreach (var e in entries)
            {
                if (e != null)
                {
                    DOTween.Kill(e.GetComponent<Image>());
                    DOTween.Kill(e.GetComponent<CanvasGroup>());
                    DOTween.Kill(e.GetComponent<RectTransform>());
                    Destroy(e);
                }
            }
            entries.Clear();
        }
    }

    /// <summary>
    /// DOTween card hover glow: card background + outline transitions on pointer enter/exit.
    /// Provides satisfying hover feedback with subtle scale + brightness shift.
    /// </summary>
    public class ShopCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Image _bg;
        private Color _originalColor;
        private Outline _outline;
        private Color _originalOutlineColor;
        private Vector3 _originalScale;

        private void Awake()
        {
            _bg = GetComponent<Image>();
            if (_bg != null)
                _originalColor = _bg.color;
            _outline = GetComponent<Outline>();
            if (_outline != null)
                _originalOutlineColor = _outline.effectColor;
            _originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_bg == null) return;
            DOTween.Kill(_bg);
            DOTween.Kill(transform);
            Color hover = new Color(
                Mathf.Min(_originalColor.r + 0.04f, 1f),
                Mathf.Min(_originalColor.g + 0.10f, 1f),
                Mathf.Min(_originalColor.b + 0.04f, 1f),
                Mathf.Min(_originalColor.a + 0.05f, 1f));
            _bg.DOColor(hover, 0.12f).SetUpdate(true);
            transform.DOScale(_originalScale * 1.02f, 0.12f).SetEase(Ease.OutCubic).SetUpdate(true);

            if (_outline != null)
            {
                _outline.effectColor = new Color(
                    Mathf.Min(_originalOutlineColor.r + 0.1f, 1f),
                    Mathf.Min(_originalOutlineColor.g + 0.2f, 1f),
                    Mathf.Min(_originalOutlineColor.b + 0.1f, 1f),
                    Mathf.Min(_originalOutlineColor.a + 0.3f, 1f));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_bg == null) return;
            DOTween.Kill(_bg);
            DOTween.Kill(transform);
            _bg.DOColor(_originalColor, 0.12f).SetUpdate(true);
            transform.DOScale(_originalScale, 0.12f).SetEase(Ease.OutCubic).SetUpdate(true);

            if (_outline != null)
                _outline.effectColor = _originalOutlineColor;
        }

        private void OnDisable()
        {
            if (_bg != null)
            {
                DOTween.Kill(_bg);
                _bg.color = _originalColor;
            }
            DOTween.Kill(transform);
            transform.localScale = _originalScale;
            if (_outline != null)
                _outline.effectColor = _originalOutlineColor;
        }
    }
}
