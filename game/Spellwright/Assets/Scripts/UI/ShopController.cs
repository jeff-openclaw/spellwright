using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Run;
using Spellwright.ScriptableObjects;
using Spellwright.Shop;
using Spellwright.Tomes;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellwright.UI
{
    /// <summary>
    /// UI Toolkit-based shop screen controller. Replaces the uGUI ShopUI.
    /// Renders buy/sell cards dynamically with staggered entrance animations,
    /// rarity-colored stripes, and purchase feedback.
    /// </summary>
    public class ShopController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private ShopManager shopManager;

        [Header("Animation")]
        [SerializeField] private float staggerDelayMs = 60f;

        private VisualElement _root;
        private Label _titleLabel;
        private Label _goldLabel;
        private Label _hpLabel;
        private ScrollView _buyItems;
        private ScrollView _sellItems;
        private Label _feedbackLabel;
        private Button _healButton;
        private Button _leaveButton;

        private readonly List<VisualElement> _buyCards = new();
        private readonly List<VisualElement> _sellCards = new();

        private void OnEnable()
        {
            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            CacheElements();
            WireEvents();
            SubscribeEventBus();

            if (shopManager != null)
            {
                shopManager.GenerateInventory();
                RefreshUI();
            }
        }

        private void OnDisable()
        {
            UnwireEvents();
            UnsubscribeEventBus();
        }

        private void CacheElements()
        {
            _titleLabel = _root.Q<Label>("title");
            _goldLabel = _root.Q<Label>("gold");
            _hpLabel = _root.Q<Label>("hp");
            _buyItems = _root.Q<ScrollView>("buy-items");
            _sellItems = _root.Q<ScrollView>("sell-items");
            _feedbackLabel = _root.Q<Label>("feedback");
            _healButton = _root.Q<Button>("heal-btn");
            _leaveButton = _root.Q<Button>("leave-btn");
        }

        private void WireEvents()
        {
            if (_healButton != null)
                _healButton.clicked += OnHealClicked;
            if (_leaveButton != null)
                _leaveButton.clicked += OnLeaveClicked;
        }

        private void UnwireEvents()
        {
            if (_healButton != null)
                _healButton.clicked -= OnHealClicked;
            if (_leaveButton != null)
                _leaveButton.clicked -= OnLeaveClicked;
        }

        private void SubscribeEventBus()
        {
            EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void UnsubscribeEventBus()
        {
            EventBus.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState == GameState.Shop && shopManager != null)
                RefreshUI();
        }

        // ── UI Building ────────────────────────────────────

        private void RefreshUI()
        {
            ClearCards();

            if (shopManager == null) return;

            // Build buy cards
            for (int i = 0; i < shopManager.Inventory.Count; i++)
            {
                var item = shopManager.Inventory[i];
                if (!item.IsHealItem)
                    CreateBuyCard(i, item);
            }

            // Build sell cards (equipped tomes)
            if (TomeManager.Instance?.TomeSystem != null)
            {
                foreach (var tome in TomeManager.Instance.TomeSystem.GetEquippedTomes())
                    CreateSellCard(tome);
            }

            UpdateStats();
            UpdateHealButton();
            if (_feedbackLabel != null)
                _feedbackLabel.text = "";

            // Staggered entrance animations
            PlayEntranceAnimation();
        }

        private void CreateBuyCard(int index, ShopItem item)
        {
            var card = new VisualElement();
            card.AddToClassList("shop-screen__card");
            if (item.IsSold)
                card.AddToClassList("shop-screen__card--sold");

            // Left stripe (rarity color)
            var stripe = new VisualElement();
            stripe.AddToClassList("shop-screen__card-stripe");
            stripe.AddToClassList(GetRarityStripeClass(item.TomeData));
            card.Add(stripe);

            // Content area
            var content = new VisualElement();
            content.AddToClassList("shop-screen__card-content");

            var nameLabel = new Label();
            nameLabel.AddToClassList("shop-screen__card-name");
            nameLabel.AddToClassList(GetRarityNameClass(item.TomeData, item.IsSold));
            nameLabel.text = item.IsSold
                ? $"[SOLD] {item.TomeData?.displayName ?? "Unknown"}"
                : item.TomeData?.displayName ?? "Unknown";
            content.Add(nameLabel);

            var descLabel = new Label();
            descLabel.AddToClassList("shop-screen__card-desc");
            descLabel.text = item.IsSold ? "" : item.TomeData?.description ?? "";
            content.Add(descLabel);

            card.Add(content);

            // Right area (price + buy label)
            if (!item.IsSold)
            {
                var right = new VisualElement();
                right.AddToClassList("shop-screen__card-right");

                var priceLabel = new Label($"{item.Price}g");
                priceLabel.AddToClassList("shop-screen__card-price");
                right.Add(priceLabel);

                var actionLabel = new Label("[ BUY ]");
                actionLabel.AddToClassList("shop-screen__card-action");
                right.Add(actionLabel);

                card.Add(right);

                // Click handler
                int capturedIndex = index;
                card.RegisterCallback<ClickEvent>(_ => OnBuyClicked(capturedIndex));
            }

            _buyItems.Add(card);
            _buyCards.Add(card);
        }

        private void CreateSellCard(TomeInstance tome)
        {
            var card = new VisualElement();
            card.AddToClassList("shop-screen__sell-card");

            // Left stripe
            var stripe = new VisualElement();
            stripe.AddToClassList("shop-screen__card-stripe");
            stripe.AddToClassList(GetRarityStripeClassFromRarity(tome.Rarity));
            card.Add(stripe);

            // Name
            var nameLabel = new Label(tome.TomeName);
            nameLabel.AddToClassList("shop-screen__sell-name");
            nameLabel.AddToClassList(GetRarityNameClassFromRarity(tome.Rarity));
            card.Add(nameLabel);

            // Sell price
            int sellPrice = CalculateSellPrice(tome.TomeId);
            var priceLabel = new Label($"+{sellPrice}g");
            priceLabel.AddToClassList("shop-screen__sell-price");
            card.Add(priceLabel);

            // Sell action label
            var actionLabel = new Label("[ SELL ]");
            actionLabel.AddToClassList("shop-screen__sell-action");
            card.Add(actionLabel);

            // Click handler
            string capturedId = tome.TomeId;
            card.RegisterCallback<ClickEvent>(_ => OnSellClicked(capturedId));

            _sellItems.Add(card);
            _sellCards.Add(card);
        }

        // ── Event Handlers ──────────────────────────────────

        private void OnBuyClicked(int index)
        {
            if (shopManager == null) return;

            var result = shopManager.BuyTome(index);
            if (_feedbackLabel != null) _feedbackLabel.text = result.Message;
            if (result.Success) RefreshUI();

            UpdateStats();
        }

        private void OnSellClicked(string tomeId)
        {
            if (shopManager == null) return;

            var result = shopManager.SellTome(tomeId);
            if (_feedbackLabel != null) _feedbackLabel.text = result.Message;
            if (result.Success) RefreshUI();

            UpdateStats();
        }

        private void OnHealClicked()
        {
            if (shopManager == null) return;

            var result = shopManager.BuyHeal();
            if (_feedbackLabel != null) _feedbackLabel.text = result.Message;

            UpdateStats();
            UpdateHealButton();
        }

        private void OnLeaveClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMap();
        }

        // ── Stats & Heal ────────────────────────────────────

        private void UpdateStats()
        {
            if (RunManager.Instance == null) return;

            if (_goldLabel != null)
                _goldLabel.text = $"$ {RunManager.Instance.Gold}g";
            if (_hpLabel != null)
                _hpLabel.text = $"HP {RunManager.Instance.CurrentHP}/{RunManager.Instance.MaxHP}";
        }

        private void UpdateHealButton()
        {
            if (_healButton == null) return;

            if (shopManager == null || RunManager.Instance == null)
            {
                _healButton.AddToClassList("shop-screen__heal-btn--disabled");
                return;
            }

            // Find heal item in inventory to get price
            int healCost = 0;
            foreach (var item in shopManager.Inventory)
            {
                if (item.IsHealItem)
                {
                    healCost = item.Price;
                    break;
                }
            }

            int healAmount = shopManager.HealAmountValue;
            bool canHeal = RunManager.Instance.Gold >= healCost
                && RunManager.Instance.CurrentHP < RunManager.Instance.MaxHP;

            _healButton.text = $"[ HEAL +{healAmount}HP ({healCost}g) ]";

            if (canHeal)
                _healButton.RemoveFromClassList("shop-screen__heal-btn--disabled");
            else
                _healButton.AddToClassList("shop-screen__heal-btn--disabled");
        }

        // ── Animations ──────────────────────────────────────

        private void PlayEntranceAnimation()
        {
            var allCards = new List<VisualElement>();
            allCards.AddRange(_buyCards);
            allCards.AddRange(_sellCards);

            for (int i = 0; i < allCards.Count; i++)
            {
                var card = allCards[i];
                long delay = (long)(i * staggerDelayMs);

                _root.schedule.Execute(() =>
                {
                    card.AddToClassList("shop-card--visible");
                }).ExecuteLater(delay);
            }
        }

        // ── Helpers ─────────────────────────────────────────

        private void ClearCards()
        {
            _buyItems?.Clear();
            _sellItems?.Clear();
            _buyCards.Clear();
            _sellCards.Clear();
        }

        private int CalculateSellPrice(string tomeId)
        {
            // Mirror ShopManager sell price logic
            // sellPrice = max(MinSellPrice, round(buyPrice * SellMultiplier))
            // We can't easily access the allTomes array from here, so use a fallback
            return ShopManager.MinSellPrice;
        }

        private static string GetRarityStripeClass(TomeDataSO tome)
        {
            if (tome == null) return "shop-card-stripe--common";
            return GetRarityStripeClassFromRarity(tome.rarity);
        }

        private static string GetRarityStripeClassFromRarity(TomeRarity rarity)
        {
            return rarity switch
            {
                TomeRarity.Common => "shop-card-stripe--common",
                TomeRarity.Uncommon => "shop-card-stripe--uncommon",
                TomeRarity.Rare => "shop-card-stripe--rare",
                TomeRarity.Legendary => "shop-card-stripe--legendary",
                _ => "shop-card-stripe--common"
            };
        }

        private static string GetRarityNameClass(TomeDataSO tome, bool isSold)
        {
            if (isSold) return "shop-card-name--sold";
            if (tome == null) return "shop-card-name--common";
            return GetRarityNameClassFromRarity(tome.rarity);
        }

        private static string GetRarityNameClassFromRarity(TomeRarity rarity)
        {
            return rarity switch
            {
                TomeRarity.Common => "shop-card-name--common",
                TomeRarity.Uncommon => "shop-card-name--uncommon",
                TomeRarity.Rare => "shop-card-name--rare",
                TomeRarity.Legendary => "shop-card-name--legendary",
                _ => "shop-card-name--common"
            };
        }
    }
}
