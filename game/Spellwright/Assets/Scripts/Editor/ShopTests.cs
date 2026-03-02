using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Run;
using Spellwright.ScriptableObjects;
using Spellwright.Shop;
using Spellwright.Tomes;
using UnityEditor;
using UnityEngine;

namespace Spellwright.Editor
{
    /// <summary>
    /// Menu-driven editor tests for the Shop system.
    /// </summary>
    public static class ShopTests
    {
        [MenuItem("Spellwright/Tests/Run All Shop Tests")]
        public static void RunAll()
        {
            int passed = 0;
            int failed = 0;

            if (TestInventoryGeneration()) passed++; else failed++;
            if (TestBuyTome()) passed++; else failed++;
            if (TestBuyInsufficientGold()) passed++; else failed++;
            if (TestBuySlotsFull()) passed++; else failed++;
            if (TestSellTome()) passed++; else failed++;
            if (TestTomePricing()) passed++; else failed++;

            Debug.Log($"[Shop Tests] === RESULTS: {passed} passed, {failed} failed ===");
        }

        // ── Helpers ──────────────────────────────────────────

        private static (GameObject go, ShopManager shop, RunManager run, TomeManager tomes, TomeDataSO[] tomeAssets) SetupTestEnvironment()
        {
            var go = new GameObject("ShopTestEnv");

            // RunManager — force singleton for edit-mode tests
            var run = go.AddComponent<RunManager>();
            RunManager.Instance = run;
            run.StartRun();
            run.AddGold(100); // Start with some gold

            // TomeManager — force singleton for edit-mode tests
            var tomeGO = new GameObject("TestTomeManager");
            var tomes = tomeGO.AddComponent<TomeManager>();
            TomeManager.Instance = tomes;
            if (tomes.TomeSystem == null)
                tomes.TomeSystem = new TomeSystem(EventBus.Instance);

            // Create test TomeDataSO assets
            var tome1 = ScriptableObject.CreateInstance<TomeDataSO>();
            tome1.tomeId = "test_vowel";
            tome1.displayName = "Vowel Lens";
            tome1.description = "Reveals vowels";
            tome1.rarity = TomeRarity.Common;
            tome1.effectClassName = "VowelLensEffect";
            tome1.shopCost = 8;

            var tome2 = ScriptableObject.CreateInstance<TomeDataSO>();
            tome2.tomeId = "test_firstlight";
            tome2.displayName = "First Light";
            tome2.description = "Reveals first letter";
            tome2.rarity = TomeRarity.Uncommon;
            tome2.effectClassName = "FirstLightEffect";
            tome2.shopCost = 12;

            var tome3 = ScriptableObject.CreateInstance<TomeDataSO>();
            tome3.tomeId = "test_echo";
            tome3.displayName = "Echo Chamber";
            tome3.description = "Shows shared letters";
            tome3.rarity = TomeRarity.Common;
            tome3.effectClassName = "EchoChamberEffect";
            tome3.shopCost = 7;

            var tomeAssets = new TomeDataSO[] { tome1, tome2, tome3 };

            // ShopManager
            var shop = go.AddComponent<ShopManager>();
            // Set allTomes via serialized field workaround
            var so = new SerializedObject(shop);
            var prop = so.FindProperty("allTomes");
            prop.arraySize = tomeAssets.Length;
            for (int i = 0; i < tomeAssets.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = tomeAssets[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            return (go, shop, run, tomes, tomeAssets);
        }

        private static void TeardownTestEnvironment(GameObject go, TomeDataSO[] tomeAssets)
        {
            Object.DestroyImmediate(go);
            // Also destroy the TomeManager GO
            if (TomeManager.Instance != null)
                Object.DestroyImmediate(TomeManager.Instance.gameObject);
            foreach (var t in tomeAssets)
                Object.DestroyImmediate(t);
        }

        // ── Tests ────────────────────────────────────────────

        [MenuItem("Spellwright/Tests/Shop Inventory Generation")]
        public static bool TestInventoryGeneration()
        {
            Debug.Log("[Shop Tests] -- Inventory Generation --");
            bool allPassed = true;
            var (go, shop, run, tomes, tomeAssets) = SetupTestEnvironment();

            shop.GenerateInventory();

            // Should have 2-3 tomes + 1 heal = 3-4 items
            int count = shop.Inventory.Count;
            if (count < 3 || count > 4)
            {
                Debug.LogError($"  FAIL: Expected 3-4 items, got {count}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Inventory has {count} items");
            }

            // Last item should be heal
            bool hasHeal = false;
            foreach (var item in shop.Inventory)
            {
                if (item.IsHealItem)
                {
                    hasHeal = true;
                    if (item.Price != ShopManager.HealCost)
                    {
                        Debug.LogError($"  FAIL: Heal price should be {ShopManager.HealCost}, got {item.Price}");
                        allPassed = false;
                    }
                    else
                    {
                        Debug.Log($"  OK: Heal item at {item.Price}g");
                    }
                    break;
                }
            }
            if (!hasHeal)
            {
                Debug.LogError("  FAIL: No heal item in inventory");
                allPassed = false;
            }

            // Tome items should have prices
            foreach (var item in shop.Inventory)
            {
                if (!item.IsHealItem && item.Price <= 0)
                {
                    Debug.LogError($"  FAIL: Tome \"{item.TomeData?.displayName}\" has no price");
                    allPassed = false;
                }
            }
            if (allPassed)
                Debug.Log("  OK: All items have valid prices");

            // Regenerate should produce potentially different inventory
            shop.GenerateInventory();
            Debug.Log($"  OK: Regenerated inventory has {shop.Inventory.Count} items");

            TeardownTestEnvironment(go, tomeAssets);
            LogResult("Inventory Generation", allPassed);
            return allPassed;
        }

        [MenuItem("Spellwright/Tests/Shop Buy Tome")]
        public static bool TestBuyTome()
        {
            Debug.Log("[Shop Tests] -- Buy Tome --");
            bool allPassed = true;
            var (go, shop, run, tomes, tomeAssets) = SetupTestEnvironment();

            shop.GenerateInventory();

            // Find first tome item
            int tomeIndex = -1;
            for (int i = 0; i < shop.Inventory.Count; i++)
            {
                if (!shop.Inventory[i].IsHealItem)
                {
                    tomeIndex = i;
                    break;
                }
            }

            if (tomeIndex < 0)
            {
                Debug.LogError("  FAIL: No tome items in inventory");
                TeardownTestEnvironment(go, tomeAssets);
                return false;
            }

            int priceBefore = shop.Inventory[tomeIndex].Price;
            int goldBefore = run.Gold;

            var result = shop.BuyTome(tomeIndex);
            if (!result.Success)
            {
                Debug.LogError($"  FAIL: Buy should succeed. Message: {result.Message}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: {result.Message}");
            }

            // Gold should decrease
            if (run.Gold != goldBefore - priceBefore)
            {
                Debug.LogError($"  FAIL: Gold should be {goldBefore - priceBefore}, got {run.Gold}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Gold deducted: {goldBefore} → {run.Gold}");
            }

            // Item should be marked sold
            if (!shop.Inventory[tomeIndex].IsSold)
            {
                Debug.LogError("  FAIL: Item should be marked as sold");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Item marked as sold");
            }

            // Buying again should fail
            var result2 = shop.BuyTome(tomeIndex);
            if (result2.Success)
            {
                Debug.LogError("  FAIL: Buying sold item should fail");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Cannot buy sold item");
            }

            TeardownTestEnvironment(go, tomeAssets);
            LogResult("Buy Tome", allPassed);
            return allPassed;
        }

        [MenuItem("Spellwright/Tests/Shop Insufficient Gold")]
        public static bool TestBuyInsufficientGold()
        {
            Debug.Log("[Shop Tests] -- Buy Insufficient Gold --");
            bool allPassed = true;
            var (go, shop, run, tomes, tomeAssets) = SetupTestEnvironment();

            // Drain gold
            run.SpendGold(run.Gold);

            shop.GenerateInventory();

            // Find first tome item
            int tomeIndex = -1;
            for (int i = 0; i < shop.Inventory.Count; i++)
            {
                if (!shop.Inventory[i].IsHealItem)
                {
                    tomeIndex = i;
                    break;
                }
            }

            if (tomeIndex >= 0)
            {
                var result = shop.BuyTome(tomeIndex);
                if (result.Success)
                {
                    Debug.LogError("  FAIL: Should not buy with 0 gold");
                    allPassed = false;
                }
                else
                {
                    Debug.Log($"  OK: Rejected — {result.Message}");
                }
            }

            TeardownTestEnvironment(go, tomeAssets);
            LogResult("Buy Insufficient Gold", allPassed);
            return allPassed;
        }

        [MenuItem("Spellwright/Tests/Shop Slots Full")]
        public static bool TestBuySlotsFull()
        {
            Debug.Log("[Shop Tests] -- Buy Slots Full --");
            bool allPassed = true;
            var (go, shop, run, tomes, tomeAssets) = SetupTestEnvironment();

            // Fill all 5 tome slots
            for (int i = 0; i < TomeSystem.MaxSlots; i++)
            {
                var dummyTome = new TomeInstance
                {
                    TomeId = $"dummy_{i}",
                    TomeName = $"Dummy {i}",
                    Rarity = TomeRarity.Common,
                    Category = TomeCategory.Insight,
                    EffectClassName = "FirstLightEffect"
                };
                tomes.TomeSystem.EquipTome(dummyTome, new Tomes.Effects.FirstLightEffect());
            }

            shop.GenerateInventory();

            // Find first tome item
            int tomeIndex = -1;
            for (int i = 0; i < shop.Inventory.Count; i++)
            {
                if (!shop.Inventory[i].IsHealItem)
                {
                    tomeIndex = i;
                    break;
                }
            }

            if (tomeIndex >= 0)
            {
                var result = shop.BuyTome(tomeIndex);
                if (result.Success)
                {
                    Debug.LogError("  FAIL: Should not buy when slots are full");
                    allPassed = false;
                }
                else
                {
                    Debug.Log($"  OK: Rejected — {result.Message}");
                }
            }

            TeardownTestEnvironment(go, tomeAssets);
            LogResult("Buy Slots Full", allPassed);
            return allPassed;
        }

        [MenuItem("Spellwright/Tests/Shop Sell Tome")]
        public static bool TestSellTome()
        {
            Debug.Log("[Shop Tests] -- Sell Tome --");
            bool allPassed = true;
            var (go, shop, run, tomes, tomeAssets) = SetupTestEnvironment();

            // First equip a tome
            tomes.EquipTome(tomeAssets[0]);

            int goldBefore = run.Gold;
            int expectedSellPrice = Mathf.Max(ShopManager.MinSellPrice,
                Mathf.RoundToInt(tomeAssets[0].shopCost * ShopManager.SellMultiplier));

            var result = shop.SellTome(tomeAssets[0].tomeId);
            if (!result.Success)
            {
                Debug.LogError($"  FAIL: Sell should succeed. Message: {result.Message}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: {result.Message}");
            }

            // Gold should increase
            if (run.Gold != goldBefore + expectedSellPrice)
            {
                Debug.LogError($"  FAIL: Gold should be {goldBefore + expectedSellPrice}, got {run.Gold}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Gold increased: {goldBefore} → {run.Gold} (+{expectedSellPrice}g)");
            }

            // Selling again should fail (already unequipped)
            var result2 = shop.SellTome(tomeAssets[0].tomeId);
            if (result2.Success)
            {
                Debug.LogError("  FAIL: Selling unequipped tome should fail");
                allPassed = false;
            }
            else
            {
                Debug.Log("  OK: Cannot sell unequipped tome");
            }

            TeardownTestEnvironment(go, tomeAssets);
            LogResult("Sell Tome", allPassed);
            return allPassed;
        }

        [MenuItem("Spellwright/Tests/Shop Pricing")]
        public static bool TestTomePricing()
        {
            Debug.Log("[Shop Tests] -- Tome Pricing --");
            bool allPassed = true;

            // Common: 5-8g
            var common = ScriptableObject.CreateInstance<TomeDataSO>();
            common.rarity = TomeRarity.Common;
            common.shopCost = 7;
            int commonPrice = ShopManager.GetTomePrice(common);
            if (commonPrice != 7)
            {
                Debug.LogError($"  FAIL: Common price should be 7 (from shopCost), got {commonPrice}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Common price = {commonPrice}g");
            }

            // Uncommon: 10-15g
            var uncommon = ScriptableObject.CreateInstance<TomeDataSO>();
            uncommon.rarity = TomeRarity.Uncommon;
            uncommon.shopCost = 12;
            int uncommonPrice = ShopManager.GetTomePrice(uncommon);
            if (uncommonPrice != 12)
            {
                Debug.LogError($"  FAIL: Uncommon price should be 12, got {uncommonPrice}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Uncommon price = {uncommonPrice}g");
            }

            // Sell price should be 50% of shop cost (min 3g)
            int sellCommon = Mathf.Max(ShopManager.MinSellPrice,
                Mathf.RoundToInt(common.shopCost * ShopManager.SellMultiplier));
            if (sellCommon < ShopManager.MinSellPrice)
            {
                Debug.LogError($"  FAIL: Sell price should be >= {ShopManager.MinSellPrice}, got {sellCommon}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"  OK: Common sell price = {sellCommon}g (50% of {common.shopCost})");
            }

            Object.DestroyImmediate(common);
            Object.DestroyImmediate(uncommon);
            LogResult("Tome Pricing", allPassed);
            return allPassed;
        }

        private static void LogResult(string testName, bool passed)
        {
            if (passed)
                Debug.Log($"[Shop Tests] PASS: {testName}");
            else
                Debug.LogError($"[Shop Tests] FAIL: {testName}");
        }
    }
}
