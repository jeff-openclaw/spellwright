using System.Collections.Generic;
using Spellwright.Encounter;
using Spellwright.Rendering;
using Spellwright.Run;
using Spellwright.ScriptableObjects;
using Spellwright.Shop;
using Spellwright.Tomes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Spellwright.Editor
{
    /// <summary>
    /// Creates a fully wired GameScene for Spellwright with all panels, managers, and references.
    /// Menu: Spellwright/Setup Game Scene
    /// </summary>
    public static class GameSceneSetup
    {
        private static readonly Color BgDark = new Color(0.08f, 0.08f, 0.12f, 1f);
        private static readonly Color PanelBg = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        private static readonly Color AccentGold = new Color(1f, 0.85f, 0.2f);
        private static readonly Color TextWhite = new Color(0.9f, 0.9f, 0.9f);
        private static readonly Color TextGray = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color BtnGreen = new Color(0.15f, 0.4f, 0.15f, 0.9f);
        private static readonly Color BtnRed = new Color(0.5f, 0.15f, 0.15f, 0.9f);
        private static readonly Color BarBg = new Color(0.2f, 0.2f, 0.2f, 1f);
        private static readonly Color BarFill = new Color(0.2f, 0.7f, 0.2f, 1f);

        [MenuItem("Spellwright/Setup Game Scene")]
        public static void SetupScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "GameScene";

            // ── Core Objects ────────────────────────────────
            var camera = CreateCamera();
            CreateDirectionalLight();
            CreateEventSystem();

            // ── Canvas ──────────────────────────────────────
            var canvasGO = CreateCanvas();
            var canvas = canvasGO.GetComponent<Canvas>();

            // Background
            var bg = CreateFullscreenImage(canvasGO.transform, "Background", BgDark);

            // ── Panels ──────────────────────────────────────
            var mainMenuPanel = CreateMainMenuPanel(canvasGO.transform);
            var mapPanel = CreateMapPanel(canvasGO.transform);
            var encounterPanel = CreateEncounterPanel(canvasGO.transform);
            var shopPanel = CreateShopPanel(canvasGO.transform);
            var runEndPanel = CreateRunEndPanel(canvasGO.transform);

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
            wordValGO.AddComponent<Encounter.WordValidator>();

            // LLMManager
            var llmGO = new GameObject("LLMManager");
            var llmMgr = llmGO.AddComponent<LLM.LLMManager>();
            SetSerializedField(llmMgr, "fallbackCluesAsset", LoadAsset<TextAsset>("Assets/Data/fallback_clues.json"));

            // TomeManager
            var tomeGO = new GameObject("TomeManager");
            tomeGO.AddComponent<TomeManager>();

            // EncounterManager
            var encMgrGO = new GameObject("EncounterManager");
            var encMgr = encMgrGO.AddComponent<EncounterManager>();

            // ShopManager (on shopPanel for convenience)
            var shopMgr = shopPanel.AddComponent<ShopManager>();
            WireShopManager(shopMgr);

            // Wire ShopUI → ShopManager
            var shopUI = shopPanel.GetComponent<ShopUI>();
            if (shopUI != null)
                SetSerializedField(shopUI, "shopManager", shopMgr);

            // CRTSettings
            var crtGO = new GameObject("CRTSettings");
            crtGO.AddComponent<CRTSettings>();

            // EncounterUI (on encounterPanel)
            var encUI = encounterPanel.AddComponent<EncounterUI>();
            WireEncounterUI(encUI, encounterPanel);

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
                // Generate a unique ID for the feature map entry
                mapProp.GetArrayElementAtIndex(mapProp.arraySize - 1).longValue = feature.GetInstanceID();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();

            // Force Create() to be called
            rendererData.SetDirty();

            Debug.Log("[GameSceneSetup] CRTRenderFeature added to PC_Renderer.asset.");
        }

        // ── Panel Creators ──────────────────────────────────

        private static GameObject CreateMainMenuPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "MainMenuPanel");
            var menuUI = panel.AddComponent<MainMenuUI>();

            // Title
            var title = CreateText(panel.transform, "TitleText", "SPELLWRIGHT", 48, AccentGold,
                TextAnchor.MiddleCenter, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.85f));

            // Subtitle
            CreateText(panel.transform, "SubtitleText", "A Word-Guessing Roguelike", 18, TextGray,
                TextAnchor.MiddleCenter, new Vector2(0.2f, 0.42f), new Vector2(0.8f, 0.52f));

            // Start button
            var startBtn = CreateButton(panel.transform, "StartButton", "START RUN", BtnGreen,
                new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.32f));

            SetSerializedField(menuUI, "titleText", title.GetComponent<Text>());
            SetSerializedField(menuUI, "startButton", startBtn.GetComponent<Button>());

            return panel;
        }

        private static GameObject CreateMapPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "MapPanel");
            var mapUI = panel.AddComponent<MapUI>();

            // Title
            var title = CreateText(panel.transform, "MapTitleText", "- YOUR JOURNEY -", 28, AccentGold,
                TextAnchor.MiddleCenter, new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.97f));

            // Stats
            var stats = CreateText(panel.transform, "StatsText", "HP: --/-- | Gold: -- | Score: --", 16, TextWhite,
                TextAnchor.MiddleCenter, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.89f));

            // Node container with vertical layout
            var container = CreateContainer(panel.transform, "NodeContainer",
                new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.8f));
            var vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Proceed button
            var proceedBtn = CreateButton(panel.transform, "ProceedButton", "PROCEED", BtnGreen,
                new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.12f));

            SetSerializedField(mapUI, "mapTitleText", title.GetComponent<Text>());
            SetSerializedField(mapUI, "statsText", stats.GetComponent<Text>());
            SetSerializedField(mapUI, "nodeContainer", container.transform);
            SetSerializedField(mapUI, "proceedButton", proceedBtn.GetComponent<Button>());

            return panel;
        }

        private static GameObject CreateEncounterPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "EncounterPanel");

            // ── NPC Info (top) ──
            var npcName = CreateText(panel.transform, "NpcNameText", "NPC Name", 24, AccentGold,
                TextAnchor.MiddleCenter, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.98f));
            var npcArchetype = CreateText(panel.transform, "NpcArchetypeText", "Archetype", 14, TextGray,
                TextAnchor.MiddleCenter, new Vector2(0.2f, 0.88f), new Vector2(0.8f, 0.93f));

            // ── Word Display ──
            var blanks = CreateText(panel.transform, "BlanksText", "_ _ _ _ _", 36, TextWhite,
                TextAnchor.MiddleCenter, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.88f));
            var category = CreateText(panel.transform, "CategoryText", "Category: ...", 14, TextGray,
                TextAnchor.MiddleCenter, new Vector2(0.2f, 0.74f), new Vector2(0.8f, 0.79f));

            // ── Clue Area ──
            var clueNum = CreateText(panel.transform, "ClueNumberText", "Clue 1/6", 14, AccentGold,
                TextAnchor.MiddleLeft, new Vector2(0.05f, 0.68f), new Vector2(0.3f, 0.74f));
            var clue = CreateText(panel.transform, "ClueText", "Waiting for clue...", 18, TextWhite,
                TextAnchor.UpperLeft, new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.68f));

            // ── Status Bar ──
            var hpTextGO = CreateText(panel.transform, "HPText", "HP: 30/30", 14, TextWhite,
                TextAnchor.MiddleLeft, new Vector2(0.05f, 0.46f), new Vector2(0.25f, 0.52f));

            // HP Bar
            var hpBarBg = CreateFullscreenImage(panel.transform, "HPBarBg", BarBg);
            SetAnchors(hpBarBg, new Vector2(0.25f, 0.47f), new Vector2(0.55f, 0.51f));
            var hpBarFill = CreateFullscreenImage(hpBarBg.transform, "HPBarFill", BarFill);
            var hpFillRT = hpBarFill.GetComponent<RectTransform>();
            hpFillRT.anchorMin = Vector2.zero;
            hpFillRT.anchorMax = Vector2.one;
            hpFillRT.offsetMin = Vector2.zero;
            hpFillRT.offsetMax = Vector2.zero;

            var goldText = CreateText(panel.transform, "GoldText", "Gold: 0", 14, AccentGold,
                TextAnchor.MiddleCenter, new Vector2(0.58f, 0.46f), new Vector2(0.73f, 0.52f));
            var guessesText = CreateText(panel.transform, "GuessesText", "Guesses: 6", 14, TextWhite,
                TextAnchor.MiddleCenter, new Vector2(0.73f, 0.46f), new Vector2(0.88f, 0.52f));
            var scoreText = CreateText(panel.transform, "ScoreText", "Score: 0", 14, TextWhite,
                TextAnchor.MiddleRight, new Vector2(0.85f, 0.46f), new Vector2(0.95f, 0.52f));

            // ── Input Area ──
            var inputGO = CreateInputField(panel.transform, "GuessInput",
                new Vector2(0.05f, 0.38f), new Vector2(0.75f, 0.45f));
            var submitBtn = CreateButton(panel.transform, "SubmitButton", "GUESS", BtnGreen,
                new Vector2(0.77f, 0.38f), new Vector2(0.95f, 0.45f));

            // ── Guess History ──
            var history = CreateText(panel.transform, "HistoryText", "", 13, TextGray,
                TextAnchor.UpperLeft, new Vector2(0.05f, 0.1f), new Vector2(0.65f, 0.36f));

            // ── Tome Info ──
            var tomeInfo = CreateText(panel.transform, "TomeInfoText", "", 13, new Color(0.6f, 0.4f, 1f),
                TextAnchor.UpperLeft, new Vector2(0.67f, 0.1f), new Vector2(0.95f, 0.36f));

            // ── Result Panel (overlay) ──
            var resultPanel = CreateFullscreenImage(panel.transform, "ResultPanel", new Color(0f, 0f, 0f, 0.85f));
            var resultTitle = CreateText(resultPanel.transform, "ResultTitleText", "VICTORY!", 36, AccentGold,
                TextAnchor.MiddleCenter, new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f));
            var resultDetails = CreateText(resultPanel.transform, "ResultDetailsText", "", 18, TextWhite,
                TextAnchor.MiddleCenter, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.55f));
            var continueBtn = CreateButton(resultPanel.transform, "ContinueButton", "CONTINUE", BtnGreen,
                new Vector2(0.3f, 0.15f), new Vector2(0.7f, 0.28f));
            resultPanel.SetActive(false);

            // ── Flash Overlay ──
            var flash = CreateFullscreenImage(panel.transform, "FlashOverlay", new Color(1f, 1f, 1f, 0f));
            flash.GetComponent<Image>().raycastTarget = false;

            return panel;
        }

        private static GameObject CreateShopPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "ShopPanel");

            // Title
            CreateText(panel.transform, "ShopTitle", "- ARCANE SHOP -", 28, AccentGold,
                TextAnchor.MiddleCenter, new Vector2(0.1f, 0.9f), new Vector2(0.9f, 0.98f));

            // Status
            var goldText = CreateText(panel.transform, "GoldText", "Gold: 0", 18, AccentGold,
                TextAnchor.MiddleLeft, new Vector2(0.05f, 0.84f), new Vector2(0.45f, 0.9f));
            var hpText = CreateText(panel.transform, "HPText", "HP: 30/30", 18, TextWhite,
                TextAnchor.MiddleRight, new Vector2(0.55f, 0.84f), new Vector2(0.95f, 0.9f));

            // Buy section header
            CreateText(panel.transform, "BuyHeader", "Available Items:", 16, TextGray,
                TextAnchor.MiddleLeft, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.84f));

            // Item container
            var itemContainer = CreateContainer(panel.transform, "ItemContainer",
                new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.78f));
            var vlg1 = itemContainer.AddComponent<VerticalLayoutGroup>();
            vlg1.spacing = 4;
            vlg1.childForceExpandWidth = true;
            vlg1.childForceExpandHeight = false;
            vlg1.childControlWidth = true;
            vlg1.childControlHeight = false;

            // Sell section header
            CreateText(panel.transform, "SellHeader", "Your Tomes (sell):", 16, TextGray,
                TextAnchor.MiddleLeft, new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.42f));

            // Equipped container
            var equippedContainer = CreateContainer(panel.transform, "EquippedContainer",
                new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.36f));
            var vlg2 = equippedContainer.AddComponent<VerticalLayoutGroup>();
            vlg2.spacing = 4;
            vlg2.childForceExpandWidth = true;
            vlg2.childForceExpandHeight = false;
            vlg2.childControlWidth = true;
            vlg2.childControlHeight = false;

            // Feedback text
            var feedbackText = CreateText(panel.transform, "FeedbackText", "", 16, AccentGold,
                TextAnchor.MiddleCenter, new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.12f));

            // Leave button
            var leaveBtn = CreateButton(panel.transform, "LeaveButton", "LEAVE SHOP", BtnRed,
                new Vector2(0.3f, 0.01f), new Vector2(0.7f, 0.06f));

            // Wire ShopUI (will be added by caller who also has shopManager ref)
            var shopUI = panel.AddComponent<ShopUI>();
            SetSerializedField(shopUI, "itemContainer", itemContainer.transform);
            SetSerializedField(shopUI, "equippedContainer", equippedContainer.transform);
            SetSerializedField(shopUI, "goldText", goldText.GetComponent<Text>());
            SetSerializedField(shopUI, "hpText", hpText.GetComponent<Text>());
            SetSerializedField(shopUI, "feedbackText", feedbackText.GetComponent<Text>());
            SetSerializedField(shopUI, "leaveButton", leaveBtn.GetComponent<Button>());

            return panel;
        }

        private static GameObject CreateRunEndPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "RunEndPanel");
            var runEndUI = panel.AddComponent<RunEndUI>();

            var title = CreateText(panel.transform, "TitleText", "RUN OVER", 42, AccentGold,
                TextAnchor.MiddleCenter, new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.85f));
            var stats = CreateText(panel.transform, "StatsText", "Score: 0\nEncounters: 0", 20, TextWhite,
                TextAnchor.MiddleCenter, new Vector2(0.15f, 0.3f), new Vector2(0.85f, 0.6f));
            var playAgainBtn = CreateButton(panel.transform, "PlayAgainButton", "PLAY AGAIN", BtnGreen,
                new Vector2(0.3f, 0.12f), new Vector2(0.7f, 0.25f));

            SetSerializedField(runEndUI, "titleText", title.GetComponent<Text>());
            SetSerializedField(runEndUI, "statsText", stats.GetComponent<Text>());
            SetSerializedField(runEndUI, "playAgainButton", playAgainBtn.GetComponent<Button>());

            return panel;
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

            // Regular NPCs ordered easy → hard
            var riddlemaster = LoadAsset<NPCDataSO>("Assets/Data/NPCs/riddlemaster.asset");
            var merchant = LoadAsset<NPCDataSO>("Assets/Data/NPCs/merchant.asset");
            var librarian = LoadAsset<NPCDataSO>("Assets/Data/NPCs/librarian.asset");

            var so = new SerializedObject(gm);
            var npcsProp = so.FindProperty("regularNPCs");
            npcsProp.arraySize = 3;
            npcsProp.GetArrayElementAtIndex(0).objectReferenceValue = riddlemaster;
            npcsProp.GetArrayElementAtIndex(1).objectReferenceValue = merchant;
            npcsProp.GetArrayElementAtIndex(2).objectReferenceValue = librarian;

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
                "Assets/Data/Words/tools.asset"
            };
            poolsProp.arraySize = poolPaths.Length;
            for (int i = 0; i < poolPaths.Length; i++)
                poolsProp.GetArrayElementAtIndex(i).objectReferenceValue = LoadAsset<WordPoolSO>(poolPaths[i]);

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

        private static void WireEncounterUI(EncounterUI encUI, GameObject panel)
        {
            // Find all the child elements by name
            var so = new SerializedObject(encUI);
            so.FindProperty("npcNameText").objectReferenceValue = FindChild<Text>(panel, "NpcNameText");
            so.FindProperty("npcArchetypeText").objectReferenceValue = FindChild<Text>(panel, "NpcArchetypeText");
            so.FindProperty("blanksText").objectReferenceValue = FindChild<Text>(panel, "BlanksText");
            so.FindProperty("categoryText").objectReferenceValue = FindChild<Text>(panel, "CategoryText");
            so.FindProperty("clueText").objectReferenceValue = FindChild<Text>(panel, "ClueText");
            so.FindProperty("clueNumberText").objectReferenceValue = FindChild<Text>(panel, "ClueNumberText");
            so.FindProperty("guessInput").objectReferenceValue = FindChild<InputField>(panel, "GuessInput");
            so.FindProperty("submitButton").objectReferenceValue = FindChild<Button>(panel, "SubmitButton");
            so.FindProperty("hpText").objectReferenceValue = FindChild<Text>(panel, "HPText");
            so.FindProperty("hpBarFill").objectReferenceValue = FindChild<Image>(panel, "HPBarFill");
            so.FindProperty("goldText").objectReferenceValue = FindChild<Text>(panel, "GoldText");
            so.FindProperty("guessesText").objectReferenceValue = FindChild<Text>(panel, "GuessesText");
            so.FindProperty("scoreText").objectReferenceValue = FindChild<Text>(panel, "ScoreText");
            so.FindProperty("historyText").objectReferenceValue = FindChild<Text>(panel, "HistoryText");
            so.FindProperty("tomeInfoText").objectReferenceValue = FindChild<Text>(panel, "TomeInfoText");
            so.FindProperty("resultPanel").objectReferenceValue = FindChildGO(panel, "ResultPanel");
            so.FindProperty("resultTitleText").objectReferenceValue = FindChild<Text>(panel, "ResultTitleText");
            so.FindProperty("resultDetailsText").objectReferenceValue = FindChild<Text>(panel, "ResultDetailsText");
            so.FindProperty("continueButton").objectReferenceValue = FindChild<Button>(panel, "ContinueButton");
            so.FindProperty("flashOverlay").objectReferenceValue = FindChild<Image>(panel, "FlashOverlay");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── ShopUI shopManager wiring (called after ShopManager is added) ──

        public static void WireShopUIToManager(ShopUI shopUI, ShopManager shopMgr)
        {
            SetSerializedField(shopUI, "shopManager", shopMgr);
        }

        // ── UI Factory Methods ──────────────────────────────

        private static GameObject CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BgDark;
            cam.orthographic = false;
            go.AddComponent<AudioListener>();
            // URP camera data is auto-added
            return go;
        }

        private static void CreateDirectionalLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            go.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static GameObject CreateCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return go;
        }

        private static GameObject CreateText(Transform parent, string name, string text, int fontSize,
            Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            go.AddComponent<CanvasRenderer>();
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            txt.raycastTarget = false;

            return go;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Color bgColor,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            labelGO.AddComponent<CanvasRenderer>();
            var txt = labelGO.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 18;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            return go;
        }

        private static GameObject CreateInputField(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            // Placeholder text
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(go.transform, false);
            var phRT = placeholderGO.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(10, 0);
            phRT.offsetMax = new Vector2(-10, 0);
            placeholderGO.AddComponent<CanvasRenderer>();
            var phText = placeholderGO.AddComponent<Text>();
            phText.text = "Type your guess...";
            phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phText.fontSize = 16;
            phText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            phText.fontStyle = FontStyle.Italic;
            phText.alignment = TextAnchor.MiddleLeft;

            // Text component
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var tRT = textGO.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.offsetMin = new Vector2(10, 0);
            tRT.offsetMax = new Vector2(-10, 0);
            textGO.AddComponent<CanvasRenderer>();
            var inputText = textGO.AddComponent<Text>();
            inputText.text = "";
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputText.fontSize = 16;
            inputText.color = TextWhite;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.supportRichText = false;

            var input = go.AddComponent<InputField>();
            input.textComponent = inputText;
            input.placeholder = phText;
            input.characterLimit = 25;

            return go;
        }

        private static GameObject CreateFullscreenImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.color = color;

            return go;
        }

        private static GameObject CreateContainer(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return go;
        }

        private static void SetAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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

        private static T FindChild<T>(GameObject root, string name) where T : Component
        {
            foreach (var c in root.GetComponentsInChildren<T>(true))
            {
                if (c.gameObject.name == name)
                    return c;
            }
            Debug.LogWarning($"[GameSceneSetup] Could not find child '{name}' with component {typeof(T).Name}");
            return null;
        }

        private static GameObject FindChildGO(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject.name == name)
                    return t.gameObject;
            }
            Debug.LogWarning($"[GameSceneSetup] Could not find child GameObject '{name}'");
            return null;
        }
    }
}
