using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Run;
using Spellwright.ScriptableObjects;
using Spellwright.Tomes;
using UnityEngine;

namespace Spellwright.Shop
{
    /// <summary>
    /// Generates shop inventory and handles buy/sell/heal transactions.
    /// Inventory is 2-3 random Tomes + 1 heal item.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        [SerializeField] private TomeDataSO[] allTomes;
        [SerializeField] private GameConfigSO gameConfig;

        public const float SellMultiplier = 0.5f;
        public const int MinSellPrice = 2;

        private int HealCost => gameConfig != null ? gameConfig.healCost : 8;
        private int HealAmount => gameConfig != null ? gameConfig.healAmount : 8;
        private float TomePriceMultiplier => gameConfig != null ? gameConfig.tomePriceMultiplier : 0.3f;

        /// <summary>Publicly readable heal amount for UI display.</summary>
        public int HealAmountValue => HealAmount;

        private readonly List<ShopItem> _inventory = new List<ShopItem>();
        public IReadOnlyList<ShopItem> Inventory => _inventory;

        private RunManager RunMgr => RunManager.Instance;
        private TomeManager TomeMgr => TomeManager.Instance;

        /// <summary>
        /// Generates a new shop inventory with 2-3 random Tomes + 1 heal item.
        /// Avoids offering Tomes the player already has equipped.
        /// </summary>
        public void GenerateInventory()
        {
            _inventory.Clear();

            if (allTomes == null || allTomes.Length == 0)
            {
                Debug.LogWarning("[ShopManager] No Tome assets assigned.");
                return;
            }

            // Get equipped tome IDs to avoid duplicates
            var equippedIds = new HashSet<string>();
            if (TomeManager.Instance?.TomeSystem != null)
            {
                foreach (var tome in TomeManager.Instance.TomeSystem.GetEquippedTomes())
                    equippedIds.Add(tome.TomeId);
            }

            // Filter available tomes
            var available = new List<TomeDataSO>();
            foreach (var tome in allTomes)
            {
                if (tome != null && !equippedIds.Contains(tome.tomeId))
                    available.Add(tome);
            }

            // Pick 2-3 random tomes
            int count = Mathf.Min(Random.Range(2, 4), available.Count);
            var shuffled = new List<TomeDataSO>(available);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            for (int i = 0; i < count; i++)
            {
                var tome = shuffled[i];
                int price = Mathf.Max(3, Mathf.RoundToInt(GetTomePrice(tome) * TomePriceMultiplier));
                _inventory.Add(new ShopItem
                {
                    TomeData = tome,
                    Price = price,
                    IsHealItem = false,
                    IsSold = false
                });
            }

            // Add heal item
            _inventory.Add(new ShopItem
            {
                TomeData = null,
                Price = HealCost,
                IsHealItem = true,
                IsSold = false
            });

            Debug.Log($"[ShopManager] Generated {_inventory.Count} shop items ({count} tomes + heal).");
        }

        /// <summary>
        /// Attempts to buy a Tome from the shop.
        /// Returns a result describing success/failure.
        /// </summary>
        public ShopResult BuyTome(int inventoryIndex)
        {
            if (inventoryIndex < 0 || inventoryIndex >= _inventory.Count)
                return new ShopResult { Success = false, Message = "Invalid item." };

            var item = _inventory[inventoryIndex];
            if (item.IsSold)
                return new ShopResult { Success = false, Message = "Already sold." };
            if (item.IsHealItem)
                return BuyHeal();

            if (RunManager.Instance == null)
                return new ShopResult { Success = false, Message = "No active run." };

            // Check gold
            if (RunManager.Instance.Gold < item.Price)
                return new ShopResult { Success = false, Message = $"Not enough gold! Need {item.Price}g, have {RunManager.Instance.Gold}g." };

            // Check tome slots
            if (TomeManager.Instance == null || !TomeManager.Instance.TomeSystem.HasFreeSlot)
                return new ShopResult { Success = false, Message = "Tome slots full!" };

            // Execute purchase
            RunManager.Instance.SpendGold(item.Price);
            TomeManager.Instance.EquipTome(item.TomeData);
            item.IsSold = true;

            Debug.Log($"[ShopManager] Bought \"{item.TomeData.displayName}\" for {item.Price}g.");
            return new ShopResult
            {
                Success = true,
                Message = $"Bought {item.TomeData.displayName}!"
            };
        }

        /// <summary>Buys a heal potion.</summary>
        public ShopResult BuyHeal()
        {
            if (RunManager.Instance == null)
                return new ShopResult { Success = false, Message = "No active run." };

            if (RunManager.Instance.Gold < HealCost)
                return new ShopResult { Success = false, Message = $"Not enough gold! Need {HealCost}g." };

            if (RunManager.Instance.CurrentHP >= RunManager.Instance.MaxHP)
                return new ShopResult { Success = false, Message = "Already at full HP!" };

            RunManager.Instance.SpendGold(HealCost);
            RunManager.Instance.Heal(HealAmount);

            Debug.Log($"[ShopManager] Bought heal for {HealCost}g. HP: {RunManager.Instance.CurrentHP}/{RunManager.Instance.MaxHP}");
            return new ShopResult
            {
                Success = true,
                Message = $"Healed {HealAmount} HP!"
            };
        }

        /// <summary>
        /// Sells an equipped Tome for 50% of its shop cost (minimum 3g).
        /// </summary>
        public ShopResult SellTome(string tomeId)
        {
            if (RunManager.Instance == null || TomeManager.Instance == null)
                return new ShopResult { Success = false, Message = "Cannot sell right now." };

            // Find the tome data to get its price
            TomeDataSO tomeData = null;
            if (allTomes != null)
            {
                foreach (var t in allTomes)
                {
                    if (t != null && t.tomeId == tomeId)
                    {
                        tomeData = t;
                        break;
                    }
                }
            }

            // Sell price is based on the actual buy price (with multiplier), not the raw shopCost
            int buyPrice = tomeData != null
                ? Mathf.Max(3, Mathf.RoundToInt(GetTomePrice(tomeData) * TomePriceMultiplier))
                : MinSellPrice;
            int sellPrice = Mathf.Max(MinSellPrice, Mathf.RoundToInt(buyPrice * SellMultiplier));

            bool removed = TomeManager.Instance.UnequipTome(tomeId);
            if (!removed)
                return new ShopResult { Success = false, Message = "Tome not found in inventory." };

            RunManager.Instance.AddGold(sellPrice);

            string tomeName = tomeData != null ? tomeData.displayName : tomeId;
            Debug.Log($"[ShopManager] Sold \"{tomeName}\" for {sellPrice}g.");
            return new ShopResult
            {
                Success = true,
                Message = $"Sold {tomeName} for {sellPrice}g!"
            };
        }

        /// <summary>Gets the shop price for a Tome based on rarity.</summary>
        public static int GetTomePrice(TomeDataSO tome)
        {
            if (tome == null) return 0;

            // Use shopCost if set, otherwise derive from rarity
            if (tome.shopCost > 0) return tome.shopCost;

            return tome.rarity switch
            {
                TomeRarity.Common => Random.Range(5, 9),
                TomeRarity.Uncommon => Random.Range(10, 16),
                TomeRarity.Rare => Random.Range(20, 31),
                TomeRarity.Legendary => Random.Range(40, 61),
                _ => 10
            };
        }
    }

    /// <summary>An item available in the shop.</summary>
    public class ShopItem
    {
        public TomeDataSO TomeData;
        public int Price;
        public bool IsHealItem;
        public bool IsSold;
    }

    /// <summary>Result of a shop transaction.</summary>
    public class ShopResult
    {
        public bool Success;
        public string Message;
    }
}
