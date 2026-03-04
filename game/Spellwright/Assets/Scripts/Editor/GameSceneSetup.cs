using System.Collections.Generic;
using Spellwright.Audio;
using Spellwright.Data;
using Spellwright.Encounter;
using Spellwright.Rendering;
using Spellwright.Run;
using Spellwright.ScriptableObjects;
using Spellwright.Shop;
using Spellwright.Tomes;
using Spellwright.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Spellwright.Editor
{
    /// <summary>
    /// Creates a fully wired GameScene for Spellwright with all UI Toolkit panels, managers, and references.
    /// Menu: Spellwright/Setup Game Scene
    /// </summary>
    public static class GameSceneSetup
    {
        private static TerminalThemeSO _theme;

        [MenuItem("Spellwright/Setup Game Scene")]
        public static void SetupScene()
        {
            // Load theme asset
            _theme = LoadAsset<TerminalThemeSO>("Assets/Data/Config/TerminalTheme.asset");
            if (_theme == null)
            {
                Debug.LogWarning("[GameSceneSetup] TerminalTheme.asset not found — using fallback colors.");
            }

            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "GameScene";

            // ── Core Objects ────────────────────────────────
            CreateCamera();
            CreateDirectionalLight();

            // ── Background Effects (UI Toolkit UIDocuments) ──
            CreateAmbientDataStream();
            CreateScreenEffectsOverlay();

            // ── UI Toolkit Panels ────────────────────────────
            var mainMenuPanel = CreateMainMenuPanel();
            var mapPanel = CreateMapPanel();
            var encounterPanel = CreateEncounterPanel();
            var shopPanel = CreateShopPanel();
            var runEndPanel = CreateRunEndPanel();

            // Start with all panels inactive except main menu
            mapPanel.SetActive(false);
            encounterPanel.SetActive(false);
            shopPanel.SetActive(false);
            runEndPanel.SetActive(false);

            // ── Managers (root-level for DontDestroyOnLoad) ──

            // RunManager
            var runMgrGO = new GameObject("RunManager");
            var runMgr = runMgrGO.AddComponent<RunManager>();
            SetSerializedField(runMgr, "gameConfig", LoadAsset<GameConfigSO>("Assets/Data/Config/GameConfig.asset"));

            // WordValidator
            var wordValGO = new GameObject("WordValidator");
            var wordVal = wordValGO.AddComponent<WordValidator>();
            SetSerializedField(wordVal, "gameConfig", LoadAsset<GameConfigSO>("Assets/Data/Config/GameConfig.asset"));

            // LLMManager
            var llmGO = new GameObject("LLMManager");
            var llmMgr = llmGO.AddComponent<LLM.LLMManager>();
            SetSerializedField(llmMgr, "gameConfig", LoadAsset<GameConfigSO>("Assets/Data/Config/GameConfig.asset"));
            SetSerializedField(llmMgr, "fallbackCluesAsset", LoadAsset<TextAsset>("Assets/Data/fallback_clues.json"));
            SetSerializedField(llmMgr, "fallbackCluesAssetRo", LoadAsset<TextAsset>("Assets/Data/fallback_clues_ro.json"));

            // TomeManager
            var tomeGO = new GameObject("TomeManager");
            tomeGO.AddComponent<TomeManager>();

            // EncounterManager
            var encMgrGO = new GameObject("EncounterManager");
            var encMgr = encMgrGO.AddComponent<EncounterManager>();

            // AdaptiveDifficultyMod (reads NPC mood, adjusts difficulty)
            var adaptiveGO = new GameObject("AdaptiveDifficultyMod");
            adaptiveGO.AddComponent<AdaptiveDifficultyMod>();

            // UltimatumSystem (final-guess dramatic moment)
            var ultimatumGO = new GameObject("UltimatumSystem");
            ultimatumGO.AddComponent<UltimatumSystem>();

            // RivalSystem (persistent antagonist arc)
            var rivalGO = new GameObject("RivalSystem");
            rivalGO.AddComponent<RivalSystem>();

            // ShopManager (on shopPanel for convenience)
            var shopMgr = shopPanel.AddComponent<ShopManager>();
            WireShopManager(shopMgr);

            // Wire ShopController → ShopManager
            var shopController = shopPanel.GetComponent<ShopController>();
            if (shopController != null)
                SetSerializedField(shopController, "shopManager", shopMgr);

            // CRTSettings
            var crtGO = new GameObject("CRTSettings");
            crtGO.AddComponent<CRTSettings>();

            // AudioManager
            var audioGO = new GameObject("AudioManager");
            audioGO.AddComponent<AudioManager>();

            // GameManager (root-level for DontDestroyOnLoad)
            var gmGO = new GameObject("GameManager");
            var gm = gmGO.AddComponent<GameManager>();
            WireGameManager(gm, mainMenuPanel, mapPanel, encounterPanel, shopPanel, runEndPanel, encMgr);

            // ── Save Scene ──────────────────────────────────
            string scenePath = "Assets/Scenes/GameScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            // Add to build settings
            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            foreach (var s in buildScenes)
            {
                if (s.path == scenePath) { found = true; break; }
            }
            if (!found)
            {
                buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            Debug.Log($"[GameSceneSetup] Scene saved to {scenePath} and added to build settings.");

            // Add CRT Render Feature to PC_Renderer
            AddCRTToRenderer();
        }

        [MenuItem("Spellwright/Add CRT to Renderer")]
        public static void AddCRTToRenderer()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            if (rendererData == null)
            {
                Debug.LogError("[GameSceneSetup] Could not find PC_Renderer.asset");
                return;
            }

            // Check if CRT feature already exists
            var so = new SerializedObject(rendererData);
            var featuresProp = so.FindProperty("m_RendererFeatures");
            for (int i = 0; i < featuresProp.arraySize; i++)
            {
                var existing = featuresProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (existing != null && existing is CRTRenderFeature)
                {
                    Debug.Log("[GameSceneSetup] CRTRenderFeature already present on PC_Renderer.");
                    return;
                }
            }

            // Create the feature as a sub-asset of the renderer data
            var feature = ScriptableObject.CreateInstance<CRTRenderFeature>();
            feature.name = "CRTRenderFeature";
            AssetDatabase.AddObjectToAsset(feature, rendererData);

            // Add to the renderer features list
            featuresProp.arraySize++;
            featuresProp.GetArrayElementAtIndex(featuresProp.arraySize - 1).objectReferenceValue = feature;

            // Also add to the map list (m_RendererFeatureMap)
            var mapProp = so.FindProperty("m_RendererFeatureMap");
            if (mapProp != null)
            {
                mapProp.arraySize++;
                mapProp.GetArrayElementAtIndex(mapProp.arraySize - 1).longValue = feature.GetInstanceID();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();

            rendererData.SetDirty();

            Debug.Log("[GameSceneSetup] CRTRenderFeature added to PC_Renderer.asset.");
        }

        // ── Theme Color Helpers ─────────────────────────────

        private static Color ThemeColor(System.Func<TerminalThemeSO, Color> getter, Color fallback)
        {
            return _theme != null ? getter(_theme) : fallback;
        }

        private static Color TerminalBg => ThemeColor(t => t.terminalBg, new Color(0.01f, 0.02f, 0.02f, 1f));

        // ── Background Effects ─────────────────────────────

        private static void CreateAmbientDataStream()
        {
            var go = new GameObject("AmbientDataStream");
            var panelSettings = EnsurePanelSettings();

            var uiDoc = go.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings;
            uiDoc.sortingOrder = -100; // Behind everything

            var dataStream = go.AddComponent<AmbientDataStream>();
            SetSerializedField(dataStream, "theme", _theme);
        }

        private static void CreateScreenEffectsOverlay()
        {
            var go = new GameObject("ScreenEffectsOverlay");
            var panelSettings = EnsurePanelSettings();

            var uiDoc = go.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings;
            uiDoc.sortingOrder = 100; // Above everything

            var overlay = go.AddComponent<ScreenEffectsOverlay>();
            SetSerializedField(overlay, "theme", _theme);
        }

        // ── Panel Creators ──────────────────────────────────

        private static GameObject CreateMainMenuPanel()
        {
            var panelGO = new GameObject("MainMenuPanel");
            var panelSettings = EnsurePanelSettings();

            var uiDoc = panelGO.AddComponent<UIDocument>();
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/MainMenu.uxml");
            if (uxml == null)
                Debug.LogWarning("[GameSceneSetup] MainMenu.uxml not found at Assets/UI/Screens/MainMenu.uxml");

            uiDoc.panelSettings = panelSettings;
            uiDoc.visualTreeAsset = uxml;

            var controller = panelGO.AddComponent<MainMenuController>();
            SetSerializedField(controller, "uiDocument", uiDoc);

            return panelGO;
        }

        /// <summary>Creates or loads the shared PanelSettings asset for UI Toolkit screens.</summary>
        private static PanelSettings EnsurePanelSettings()
        {
            const string path = "Assets/UI/SpellwrightPanelSettings.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (existing != null) return existing;

            var ps = ScriptableObject.CreateInstance<PanelSettings>();

            var tss = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/Themes/terminal-theme.tss");
            if (tss != null)
                ps.themeStyleSheet = tss;
            else
                Debug.LogWarning("[GameSceneSetup] terminal-theme.tss not found — PanelSettings will have no theme.");

            if (!AssetDatabase.IsValidFolder("Assets/UI"))
                AssetDatabase.CreateFolder("Assets", "UI");

            AssetDatabase.CreateAsset(ps, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GameSceneSetup] Created PanelSettings at {path}");
            return ps;
        }

        private static GameObject CreateMapPanel()
        {
            var panelGO = new GameObject("MapPanel");
            var panelSettings = EnsurePanelSettings();

            var uiDoc = panelGO.AddComponent<UIDocument>();
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/Map.uxml");
            if (uxml == null)
                Debug.LogWarning("[GameSceneSetup] Map.uxml not found at Assets/UI/Screens/Map.uxml");

            uiDoc.panelSettings = panelSettings;
            uiDoc.visualTreeAsset = uxml;

            var controller = panelGO.AddComponent<MapController>();
            SetSerializedField(controller, "uiDocument", uiDoc);
            SetSerializedField(controller, "gameConfig", LoadAsset<GameConfigSO>("Assets/Data/Config/GameConfig.asset"));

            return panelGO;
        }

        private static GameObject CreateEncounterPanel()
        {
            var panelGO = new GameObject("EncounterPanel");
            var panelSettings = EnsurePanelSettings();

            var uiDoc = panelGO.AddComponent<UIDocument>();
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/Encounter.uxml");
            if (uxml == null)
                Debug.LogWarning("[GameSceneSetup] Encounter.uxml not found at Assets/UI/Screens/Encounter.uxml");

            uiDoc.panelSettings = panelSettings;
            uiDoc.visualTreeAsset = uxml;

            var controller = panelGO.AddComponent<EncounterController>();
            SetSerializedField(controller, "uiDocument", uiDoc);

            return panelGO;
        }

        private static GameObject CreateShopPanel()
        {
            var panelGO = new GameObject("ShopPanel");
            var panelSettings = EnsurePanelSettings();

            var uiDoc = panelGO.AddComponent<UIDocument>();
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/Shop.uxml");
            if (uxml == null)
                Debug.LogWarning("[GameSceneSetup] Shop.uxml not found at Assets/UI/Screens/Shop.uxml");

            uiDoc.panelSettings = panelSettings;
            uiDoc.visualTreeAsset = uxml;

            var controller = panelGO.AddComponent<ShopController>();
            SetSerializedField(controller, "uiDocument", uiDoc);

            return panelGO;
        }

        private static GameObject CreateRunEndPanel()
        {
            var panelGO = new GameObject("RunEndPanel");
            var panelSettings = EnsurePanelSettings();

            var uiDoc = panelGO.AddComponent<UIDocument>();
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/Result.uxml");
            if (uxml == null)
                Debug.LogWarning("[GameSceneSetup] Result.uxml not found at Assets/UI/Screens/Result.uxml");

            uiDoc.panelSettings = panelSettings;
            uiDoc.visualTreeAsset = uxml;

            var controller = panelGO.AddComponent<ResultController>();
            SetSerializedField(controller, "uiDocument", uiDoc);

            return panelGO;
        }

        // ── Wiring Methods ──────────────────────────────────

        private static void WireGameManager(GameManager gm, GameObject mainMenu, GameObject map,
            GameObject encounter, GameObject shop, GameObject runEnd, EncounterManager encMgr)
        {
            SetSerializedField(gm, "mainMenuPanel", mainMenu);
            SetSerializedField(gm, "mapPanel", map);
            SetSerializedField(gm, "encounterPanel", encounter);
            SetSerializedField(gm, "shopPanel", shop);
            SetSerializedField(gm, "runEndPanel", runEnd);
            SetSerializedField(gm, "encounterManager", encMgr);
            SetSerializedField(gm, "gameConfig", LoadAsset<GameConfigSO>("Assets/Data/Config/GameConfig.asset"));
            SetSerializedField(gm, "bossNPC", LoadAsset<NPCDataSO>("Assets/Data/NPCs/boss_whisperer.asset"));

            EnsureGuideNPC();
            EnsurePhrasePools();

            // Regular NPCs ordered easy → hard (Inkwell is the tutorial guide)
            var guide = LoadAsset<NPCDataSO>("Assets/Data/NPCs/guide.asset");
            var riddlemaster = LoadAsset<NPCDataSO>("Assets/Data/NPCs/riddlemaster.asset");
            var merchant = LoadAsset<NPCDataSO>("Assets/Data/NPCs/merchant.asset");
            var librarian = LoadAsset<NPCDataSO>("Assets/Data/NPCs/librarian.asset");

            var so = new SerializedObject(gm);
            var npcsProp = so.FindProperty("regularNPCs");
            npcsProp.arraySize = 4;
            npcsProp.GetArrayElementAtIndex(0).objectReferenceValue = guide;
            npcsProp.GetArrayElementAtIndex(1).objectReferenceValue = riddlemaster;
            npcsProp.GetArrayElementAtIndex(2).objectReferenceValue = merchant;
            npcsProp.GetArrayElementAtIndex(3).objectReferenceValue = librarian;

            var poolsProp = so.FindProperty("wordPools");
            var poolPaths = new[]
            {
                "Assets/Data/Words/animals.asset",
                "Assets/Data/Words/emotions.asset",
                "Assets/Data/Words/everyday.asset",
                "Assets/Data/Words/food.asset",
                "Assets/Data/Words/mythology.asset",
                "Assets/Data/Words/nature.asset",
                "Assets/Data/Words/science.asset",
                "Assets/Data/Words/tools.asset",
                "Assets/Data/Words/phrases.asset"
            };
            poolsProp.arraySize = poolPaths.Length;
            for (int i = 0; i < poolPaths.Length; i++)
                poolsProp.GetArrayElementAtIndex(i).objectReferenceValue = LoadAsset<WordPoolSO>(poolPaths[i]);

            // Romanian word pools
            var poolsRoProp = so.FindProperty("wordPoolsRo");
            var roPoolPaths = new[]
            {
                "Assets/Data/Words/ro/animale.asset",
                "Assets/Data/Words/ro/emotii.asset",
                "Assets/Data/Words/ro/cotidian.asset",
                "Assets/Data/Words/ro/mancare.asset",
                "Assets/Data/Words/ro/mitologie.asset",
                "Assets/Data/Words/ro/natura.asset",
                "Assets/Data/Words/ro/stiinta.asset",
                "Assets/Data/Words/ro/unelte.asset",
                "Assets/Data/Words/ro/expresii.asset"
            };
            poolsRoProp.arraySize = roPoolPaths.Length;
            for (int i = 0; i < roPoolPaths.Length; i++)
                poolsRoProp.GetArrayElementAtIndex(i).objectReferenceValue = LoadAsset<WordPoolSO>(roPoolPaths[i]);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireShopManager(ShopManager shopMgr)
        {
            SetSerializedField(shopMgr, "gameConfig", LoadAsset<GameConfigSO>("Assets/Data/Config/GameConfig.asset"));

            var so = new SerializedObject(shopMgr);
            var tomesProp = so.FindProperty("allTomes");
            var tomePaths = new[]
            {
                "Assets/Data/Tomes/echo_chamber.asset",
                "Assets/Data/Tomes/first_light.asset",
                "Assets/Data/Tomes/second_wind.asset",
                "Assets/Data/Tomes/thick_skin.asset",
                "Assets/Data/Tomes/vowel_lens.asset"
            };
            tomesProp.arraySize = tomePaths.Length;
            for (int i = 0; i < tomePaths.Length; i++)
                tomesProp.GetArrayElementAtIndex(i).objectReferenceValue = LoadAsset<TomeDataSO>(tomePaths[i]);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Core Scene Objects ──────────────────────────────

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = TerminalBg;
            cam.orthographic = false;
            go.AddComponent<AudioListener>();
        }

        private static void CreateDirectionalLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            go.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        // ── Utility ─────────────────────────────────────────

        private static void SetSerializedField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[GameSceneSetup] Could not find field '{fieldName}' on {target.GetType().Name}");
            }
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogWarning($"[GameSceneSetup] Could not load asset at '{path}'");
            return asset;
        }

        // ── Asset Creation Helpers ─────────────────────────────

        private static void EnsureGuideNPC()
        {
            const string path = "Assets/Data/NPCs/guide.asset";
            if (AssetDatabase.LoadAssetAtPath<NPCDataSO>(path) != null) return;

            var npc = ScriptableObject.CreateInstance<NPCDataSO>();
            npc.displayName = "Inkwell";
            npc.archetype = NPCArchetype.Guide;
            npc.systemPromptAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/NPCs/guide.md");
            npc.difficultyModifier = 0.5f;
            npc.isBoss = false;
            npc.bossConstraint = "";

            AssetDatabase.CreateAsset(npc, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GameSceneSetup] Created Inkwell guide NPC at {path}");
        }

        private static void EnsurePhrasePools()
        {
            const string enPath = "Assets/Data/Words/phrases.asset";
            if (AssetDatabase.LoadAssetAtPath<WordPoolSO>(enPath) == null)
            {
                var pool = ScriptableObject.CreateInstance<WordPoolSO>();
                pool.category = "Phrases";
                pool.minDifficulty = 1;
                pool.maxDifficulty = 5;
                pool.sourceFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/Words/phrases.txt");

                AssetDatabase.CreateAsset(pool, enPath);
                Debug.Log($"[GameSceneSetup] Created phrases word pool at {enPath}");
            }

            const string roPath = "Assets/Data/Words/ro/expresii.asset";
            if (AssetDatabase.LoadAssetAtPath<WordPoolSO>(roPath) == null)
            {
                var pool = ScriptableObject.CreateInstance<WordPoolSO>();
                pool.category = "Expresii";
                pool.minDifficulty = 1;
                pool.maxDifficulty = 5;
                pool.sourceFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/Words/ro/expresii.txt");

                AssetDatabase.CreateAsset(pool, roPath);
                Debug.Log($"[GameSceneSetup] Created Romanian expressions word pool at {roPath}");
            }

            AssetDatabase.SaveAssets();
        }
    }
}
