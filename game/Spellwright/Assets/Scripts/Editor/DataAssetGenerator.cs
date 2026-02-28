using UnityEngine;
using UnityEditor;
using Spellwright.Data;
using Spellwright.ScriptableObjects;

namespace Spellwright.Editor
{
    public static class DataAssetGenerator
    {
        [MenuItem("Spellwright/Generate All Data Assets")]
        public static void GenerateAll()
        {
            GenerateTomes();
            GenerateNPCs();
            GenerateWordPools();
            GenerateGameConfig();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Spellwright] All data assets generated.");
        }

        [MenuItem("Spellwright/Generate Tome Assets")]
        public static void GenerateTomes()
        {
            CreateTome("vowel_lens", "Vowel Lens",
                "Reveals all vowels in the hidden word after first wrong guess.",
                TomeRarity.Common, TomeCategory.Insight, "VowelLensEffect", 50);

            CreateTome("first_light", "First Light",
                "The first letter of the word is always revealed.",
                TomeRarity.Common, TomeCategory.Insight, "FirstLightEffect", 50);

            CreateTome("echo_chamber", "Echo Chamber",
                "After a wrong guess, learn if any of its letters appear in the answer.",
                TomeRarity.Uncommon, TomeCategory.Insight, "EchoChamberEffect", 80);

            CreateTome("thick_skin", "Thick Skin",
                "+10 max HP. Simple but effective.",
                TomeRarity.Common, TomeCategory.Defense, "ThickSkinEffect", 40);

            CreateTome("second_wind", "Second Wind",
                "Once per encounter, a wrong guess costs 0 HP.",
                TomeRarity.Uncommon, TomeCategory.Defense, "SecondWindEffect", 75);

            Debug.Log("[Spellwright] 5 Tome assets generated.");
        }

        [MenuItem("Spellwright/Generate NPC Assets")]
        public static void GenerateNPCs()
        {
            CreateNPC("riddlemaster", "Riddlemaster", NPCArchetype.Riddlemaster,
                "Assets/Data/NPCs/riddlemaster.md", 1.0f, false, "");

            CreateNPC("merchant", "Trickster Merchant", NPCArchetype.TricksterMerchant,
                "Assets/Data/NPCs/merchant.md", 0.9f, false, "");

            CreateNPC("librarian", "Silent Librarian", NPCArchetype.SilentLibrarian,
                "Assets/Data/NPCs/librarian.md", 1.1f, false, "");

            // Boss NPC - The Whisperer is not in the archetype enum yet,
            // using Riddlemaster as base archetype
            CreateNPC("boss_whisperer", "The Whisperer", NPCArchetype.Riddlemaster,
                "Assets/Data/NPCs/boss_whisperer.md", 1.5f, true,
                "Your clues must be exactly 3 words.");

            Debug.Log("[Spellwright] 4 NPC assets generated (3 regular + 1 boss).");
        }

        [MenuItem("Spellwright/Generate Word Pool Assets")]
        public static void GenerateWordPools()
        {
            string[] categories = { "animals", "emotions", "everyday", "food",
                                    "mythology", "nature", "science", "tools" };

            foreach (var cat in categories)
            {
                CreateWordPool(cat, "Assets/Data/Words/" + cat + ".txt");
            }

            Debug.Log("[Spellwright] 8 Word Pool assets generated.");
        }

        [MenuItem("Spellwright/Generate Game Config")]
        public static void GenerateGameConfig()
        {
            var path = "Assets/Data/Config/GameConfig.asset";
            var existing = AssetDatabase.LoadAssetAtPath<GameConfigSO>(path);
            if (existing != null)
            {
                Debug.Log("[Spellwright] GameConfig already exists, skipping.");
                return;
            }

            var config = ScriptableObject.CreateInstance<GameConfigSO>();
            AssetDatabase.CreateAsset(config, path);
            Debug.Log("[Spellwright] GameConfig asset generated.");
        }

        private static void CreateTome(string id, string displayName, string description,
            TomeRarity rarity, TomeCategory category, string effectClass, int cost)
        {
            var path = $"Assets/Data/Tomes/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TomeDataSO>(path);
            if (existing != null) return;

            var tome = ScriptableObject.CreateInstance<TomeDataSO>();
            tome.tomeId = id;
            tome.displayName = displayName;
            tome.description = description;
            tome.rarity = rarity;
            tome.category = category;
            tome.effectClassName = effectClass;
            tome.shopCost = cost;

            AssetDatabase.CreateAsset(tome, path);
        }

        private static void CreateNPC(string id, string displayName, NPCArchetype archetype,
            string promptAssetPath, float difficulty, bool isBoss, string bossConstraint)
        {
            var path = $"Assets/Data/NPCs/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<NPCDataSO>(path);
            if (existing != null) return;

            var npc = ScriptableObject.CreateInstance<NPCDataSO>();
            npc.displayName = displayName;
            npc.archetype = archetype;
            npc.systemPromptAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(promptAssetPath);
            npc.difficultyModifier = difficulty;
            npc.isBoss = isBoss;
            npc.bossConstraint = bossConstraint;

            AssetDatabase.CreateAsset(npc, path);
        }

        private static void CreateWordPool(string category, string sourceFilePath)
        {
            var path = $"Assets/Data/Words/{category}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<WordPoolSO>(path);
            if (existing != null) return;

            var pool = ScriptableObject.CreateInstance<WordPoolSO>();
            pool.category = category;
            pool.sourceFile = AssetDatabase.LoadAssetAtPath<TextAsset>(sourceFilePath);

            AssetDatabase.CreateAsset(pool, path);
        }
    }
}
