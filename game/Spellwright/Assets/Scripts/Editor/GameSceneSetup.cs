using System.Collections.Generic;
using Spellwright.Encounter;
using Spellwright.Rendering;
using Spellwright.Run;
using Spellwright.ScriptableObjects;
using Spellwright.Shop;
using Spellwright.Tomes;
using Spellwright.UI;
using TMPro;
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
            var camera = CreateCamera();
            CreateDirectionalLight();
            CreateEventSystem();

            // ── Canvas ──────────────────────────────────────
            var canvasGO = CreateCanvas();
            var canvas = canvasGO.GetComponent<Canvas>();

            // Background
            Color bgColor = _theme != null ? _theme.terminalBg : new Color(0.02f, 0.05f, 0.02f, 1f);
            var bg = CreateFullscreenImage(canvasGO.transform, "Background", bgColor);

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
            var wordVal = wordValGO.AddComponent<Encounter.WordValidator>();
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

        // ── Theme Color Helpers ─────────────────────────────

        private static Color ThemeColor(System.Func<TerminalThemeSO, Color> getter, Color fallback)
        {
            return _theme != null ? getter(_theme) : fallback;
        }

        private static Color PhosphorGreen => ThemeColor(t => t.phosphorGreen, new Color(0f, 1f, 0.33f));
        private static Color PhosphorBright => ThemeColor(t => t.phosphorBright, new Color(0.2f, 1f, 0.5f));
        private static Color PhosphorDim => ThemeColor(t => t.phosphorDim, new Color(0f, 0.5f, 0.18f));
        private static Color TerminalBg => ThemeColor(t => t.terminalBg, new Color(0.02f, 0.05f, 0.02f, 1f));
        private static Color PanelBg => ThemeColor(t => t.panelBg, new Color(0.03f, 0.08f, 0.03f, 0.95f));
        private static Color BorderColor => ThemeColor(t => t.borderColor, new Color(0f, 0.6f, 0.2f, 0.8f));
        private static Color AmberBright => ThemeColor(t => t.amberBright, new Color(1f, 0.75f, 0f));
        private static Color CyanInfo => ThemeColor(t => t.cyanInfo, new Color(0f, 0.85f, 0.85f));
        private static Color MagentaMagic => ThemeColor(t => t.magentaMagic, new Color(0.85f, 0.2f, 0.85f));
        private static Color ButtonBg => ThemeColor(t => t.buttonBg, new Color(0.05f, 0.2f, 0.05f, 0.9f));
        private static Color ButtonBgDanger => ThemeColor(t => t.buttonBgDanger, new Color(0.3f, 0.05f, 0.05f, 0.9f));
        private static Color ButtonText => ThemeColor(t => t.buttonText, new Color(0f, 1f, 0.33f));
        private static Color HpBarBg => ThemeColor(t => t.hpBarBg, new Color(0.1f, 0.15f, 0.1f, 1f));
        private static Color HpBarFill => ThemeColor(t => t.hpBarFill, new Color(0f, 0.8f, 0.25f, 1f));

        // ── Panel Creators ──────────────────────────────────

        private static GameObject CreateMainMenuPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "MainMenuPanel");
            AddPanelBorder(panel);
            var menuUI = panel.AddComponent<MainMenuUI>();

            // Title
            var title = CreateText(panel.transform, "TitleText", "SPELLWRIGHT",
                _theme != null ? _theme.titleSize : 48, PhosphorBright,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.85f));

            // Subtitle
            CreateText(panel.transform, "SubtitleText", "A Word-Guessing Roguelike",
                _theme != null ? _theme.bodySize : 18, PhosphorDim,
                TextAlignmentOptions.Center, new Vector2(0.2f, 0.42f), new Vector2(0.8f, 0.52f));

            // Start button
            var startBtn = CreateButton(panel.transform, "StartButton", "START RUN", ButtonBg,
                new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.32f));

            SetSerializedField(menuUI, "titleText", title);
            SetSerializedField(menuUI, "startButton", startBtn.GetComponent<Button>());

            return panel;
        }

        private static GameObject CreateMapPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "MapPanel");
            AddPanelBorder(panel);
            var mapUI = panel.AddComponent<MapUI>();

            // Title
            var title = CreateText(panel.transform, "MapTitleText", "- YOUR JOURNEY -",
                _theme != null ? _theme.headerSize : 28, PhosphorBright,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.97f));

            // Stats
            var stats = CreateText(panel.transform, "StatsText", "HP: --/-- | Gold: -- | Score: --",
                _theme != null ? _theme.labelSize : 14, PhosphorGreen,
                TextAlignmentOptions.Center, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.89f));

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
            var proceedBtn = CreateButton(panel.transform, "ProceedButton", "PROCEED", ButtonBg,
                new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.12f));

            // Language toggle button (small, bottom-left)
            var langBtn = CreateButton(panel.transform, "LanguageButton", "EN", ButtonBg,
                new Vector2(0.02f, 0.03f), new Vector2(0.15f, 0.10f));
            var langLabel = langBtn.GetComponentInChildren<TMP_Text>();

            SetSerializedField(mapUI, "mapTitleText", title);
            SetSerializedField(mapUI, "statsText", stats);
            SetSerializedField(mapUI, "nodeContainer", container.transform);
            SetSerializedField(mapUI, "proceedButton", proceedBtn.GetComponent<Button>());
            SetSerializedField(mapUI, "languageButton", langBtn.GetComponent<Button>());
            SetSerializedField(mapUI, "languageButtonText", langLabel);
            SetSerializedField(mapUI, "gameConfig", LoadAsset<GameConfigSO>("Assets/Data/Config/GameConfig.asset"));
            SetSerializedField(mapUI, "theme", _theme);

            return panel;
        }

        private static GameObject CreateEncounterPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "EncounterPanel");
            AddPanelBorder(panel);

            // ── NPC Info (top) ──
            var npcName = CreateText(panel.transform, "NpcNameText", "NPC Name",
                _theme != null ? _theme.headerSize - 4 : 24, PhosphorBright,
                TextAlignmentOptions.Center, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.98f));
            var npcArchetype = CreateText(panel.transform, "NpcArchetypeText", "Archetype",
                _theme != null ? _theme.labelSize : 14, PhosphorDim,
                TextAlignmentOptions.Center, new Vector2(0.2f, 0.88f), new Vector2(0.8f, 0.93f));

            // ── Word Display ──
            var blanks = CreateText(panel.transform, "BlanksText", "_ _ _ _ _",
                _theme != null ? _theme.blanksSize : 36, PhosphorGreen,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.88f));
            var category = CreateText(panel.transform, "CategoryText", "Category: ...",
                _theme != null ? _theme.labelSize : 14, PhosphorDim,
                TextAlignmentOptions.Center, new Vector2(0.2f, 0.74f), new Vector2(0.8f, 0.79f));

            // ── Clue Area ──
            var clueNum = CreateText(panel.transform, "ClueNumberText", "Clue 1/6",
                _theme != null ? _theme.labelSize : 14, AmberBright,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.05f, 0.68f), new Vector2(0.3f, 0.74f));
            var clue = CreateText(panel.transform, "ClueText", "Waiting for clue...",
                _theme != null ? _theme.bodySize : 18, PhosphorGreen,
                TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.68f));

            // ── Status Bar ──
            var hpTextGO = CreateText(panel.transform, "HPText", "HP: 30/30",
                _theme != null ? _theme.labelSize : 14, PhosphorGreen,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.05f, 0.46f), new Vector2(0.25f, 0.52f));

            // HP Bar
            var hpBarBg = CreateFullscreenImage(panel.transform, "HPBarBg", HpBarBg);
            SetAnchors(hpBarBg, new Vector2(0.25f, 0.47f), new Vector2(0.55f, 0.51f));
            var hpBarFill = CreateFullscreenImage(hpBarBg.transform, "HPBarFill", HpBarFill);
            var hpFillRT = hpBarFill.GetComponent<RectTransform>();
            hpFillRT.anchorMin = Vector2.zero;
            hpFillRT.anchorMax = Vector2.one;
            hpFillRT.offsetMin = Vector2.zero;
            hpFillRT.offsetMax = Vector2.zero;

            var goldText = CreateText(panel.transform, "GoldText", "Gold: 0",
                _theme != null ? _theme.labelSize : 14, AmberBright,
                TextAlignmentOptions.Center, new Vector2(0.58f, 0.46f), new Vector2(0.73f, 0.52f));
            var guessesText = CreateText(panel.transform, "GuessesText", "Guesses: 6",
                _theme != null ? _theme.labelSize : 14, PhosphorGreen,
                TextAlignmentOptions.Center, new Vector2(0.73f, 0.46f), new Vector2(0.88f, 0.52f));
            var scoreText = CreateText(panel.transform, "ScoreText", "Score: 0",
                _theme != null ? _theme.labelSize : 14, PhosphorGreen,
                TextAlignmentOptions.MidlineRight, new Vector2(0.85f, 0.46f), new Vector2(0.95f, 0.52f));

            // ── Input Area ──
            var inputGO = CreateInputField(panel.transform, "GuessInput",
                new Vector2(0.05f, 0.38f), new Vector2(0.75f, 0.45f));
            var submitBtn = CreateButton(panel.transform, "SubmitButton", "GUESS", ButtonBg,
                new Vector2(0.77f, 0.38f), new Vector2(0.95f, 0.45f));

            // ── Guess History ──
            var history = CreateText(panel.transform, "HistoryText", "",
                _theme != null ? _theme.smallSize : 13, PhosphorDim,
                TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.1f), new Vector2(0.65f, 0.36f));

            // ── Tome Info ──
            var tomeInfo = CreateText(panel.transform, "TomeInfoText", "",
                _theme != null ? _theme.smallSize : 13, MagentaMagic,
                TextAlignmentOptions.TopLeft, new Vector2(0.67f, 0.1f), new Vector2(0.95f, 0.36f));

            // ── Result Panel (overlay) ──
            var resultPanel = CreateFullscreenImage(panel.transform, "ResultPanel",
                new Color(TerminalBg.r, TerminalBg.g, TerminalBg.b, 0.9f));
            var resultTitle = CreateText(resultPanel.transform, "ResultTitleText", "VICTORY!",
                _theme != null ? _theme.blanksSize : 36, PhosphorBright,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f));
            var resultDetails = CreateText(resultPanel.transform, "ResultDetailsText", "",
                _theme != null ? _theme.bodySize : 18, PhosphorGreen,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.55f));
            var continueBtn = CreateButton(resultPanel.transform, "ContinueButton", "CONTINUE", ButtonBg,
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
            AddPanelBorder(panel);

            // Title
            CreateText(panel.transform, "ShopTitle", "- ARCANE SHOP -",
                _theme != null ? _theme.headerSize : 28, PhosphorBright,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.9f), new Vector2(0.9f, 0.98f));

            // Status
            var goldText = CreateText(panel.transform, "GoldText", "Gold: 0",
                _theme != null ? _theme.bodySize : 18, AmberBright,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.05f, 0.84f), new Vector2(0.45f, 0.9f));
            var hpText = CreateText(panel.transform, "HPText", "HP: 30/30",
                _theme != null ? _theme.bodySize : 18, PhosphorGreen,
                TextAlignmentOptions.MidlineRight, new Vector2(0.55f, 0.84f), new Vector2(0.95f, 0.9f));

            // Buy section header
            CreateText(panel.transform, "BuyHeader", "Available Items:",
                _theme != null ? _theme.labelSize : 16, PhosphorDim,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.84f));

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
            CreateText(panel.transform, "SellHeader", "Your Tomes (sell):",
                _theme != null ? _theme.labelSize : 16, PhosphorDim,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.42f));

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
            var feedbackText = CreateText(panel.transform, "FeedbackText", "",
                _theme != null ? _theme.labelSize : 16, AmberBright,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.12f));

            // Leave button
            var leaveBtn = CreateButton(panel.transform, "LeaveButton", "LEAVE SHOP", ButtonBgDanger,
                new Vector2(0.3f, 0.01f), new Vector2(0.7f, 0.06f));

            // Wire ShopUI (will be added by caller who also has shopManager ref)
            var shopUI = panel.AddComponent<ShopUI>();
            SetSerializedField(shopUI, "itemContainer", itemContainer.transform);
            SetSerializedField(shopUI, "equippedContainer", equippedContainer.transform);
            SetSerializedField(shopUI, "goldText", goldText);
            SetSerializedField(shopUI, "hpText", hpText);
            SetSerializedField(shopUI, "feedbackText", feedbackText);
            SetSerializedField(shopUI, "leaveButton", leaveBtn.GetComponent<Button>());
            SetSerializedField(shopUI, "theme", _theme);

            return panel;
        }

        private static GameObject CreateRunEndPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "RunEndPanel");
            AddPanelBorder(panel);
            var runEndUI = panel.AddComponent<RunEndUI>();

            var title = CreateText(panel.transform, "TitleText", "RUN OVER",
                _theme != null ? _theme.titleSize - 6 : 42, PhosphorBright,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.85f));
            var stats = CreateText(panel.transform, "StatsText", "Score: 0\nEncounters: 0",
                _theme != null ? _theme.bodySize + 2 : 20, PhosphorGreen,
                TextAlignmentOptions.Center, new Vector2(0.15f, 0.3f), new Vector2(0.85f, 0.6f));
            var playAgainBtn = CreateButton(panel.transform, "PlayAgainButton", "PLAY AGAIN", ButtonBg,
                new Vector2(0.3f, 0.12f), new Vector2(0.7f, 0.25f));

            SetSerializedField(runEndUI, "titleText", title);
            SetSerializedField(runEndUI, "statsText", stats);
            SetSerializedField(runEndUI, "playAgainButton", playAgainBtn.GetComponent<Button>());
            SetSerializedField(runEndUI, "theme", _theme);

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
                "Assets/Data/Words/ro/unelte.asset"
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

        private static void WireEncounterUI(EncounterUI encUI, GameObject panel)
        {
            // Find all the child elements by name
            var so = new SerializedObject(encUI);
            so.FindProperty("npcNameText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "NpcNameText");
            so.FindProperty("npcArchetypeText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "NpcArchetypeText");
            so.FindProperty("blanksText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "BlanksText");
            so.FindProperty("categoryText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "CategoryText");
            so.FindProperty("clueText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "ClueText");
            so.FindProperty("clueNumberText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "ClueNumberText");
            so.FindProperty("guessInput").objectReferenceValue = FindChild<TMP_InputField>(panel, "GuessInput");
            so.FindProperty("submitButton").objectReferenceValue = FindChild<Button>(panel, "SubmitButton");
            so.FindProperty("hpText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "HPText");
            so.FindProperty("hpBarFill").objectReferenceValue = FindChild<Image>(panel, "HPBarFill");
            so.FindProperty("goldText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "GoldText");
            so.FindProperty("guessesText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "GuessesText");
            so.FindProperty("scoreText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "ScoreText");
            so.FindProperty("historyText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "HistoryText");
            so.FindProperty("tomeInfoText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "TomeInfoText");
            so.FindProperty("resultPanel").objectReferenceValue = FindChildGO(panel, "ResultPanel");
            so.FindProperty("resultTitleText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "ResultTitleText");
            so.FindProperty("resultDetailsText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "ResultDetailsText");
            so.FindProperty("continueButton").objectReferenceValue = FindChild<Button>(panel, "ContinueButton");
            so.FindProperty("flashOverlay").objectReferenceValue = FindChild<Image>(panel, "FlashOverlay");
            so.FindProperty("theme").objectReferenceValue = _theme;
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
            cam.backgroundColor = TerminalBg;
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

        private static void AddPanelBorder(GameObject panel)
        {
            // Add a background Image for the panel
            var img = panel.GetComponent<Image>();
            if (img == null)
                img = panel.AddComponent<Image>();
            img.color = PanelBg;

            // Add terminal border via Outline
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(2, -2);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize,
            Color color, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            if (_theme != null && _theme.primaryFont != null)
                tmp.font = _theme.primaryFont;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.raycastTarget = false;

            return tmp;
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

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            // Terminal border
            var outline = go.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(1, -1);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // TMP Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            if (_theme != null && _theme.primaryFont != null)
                tmp.font = _theme.primaryFont;
            tmp.fontSize = _theme != null ? _theme.bodySize : 18;
            tmp.color = ButtonText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

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

            var bg = go.AddComponent<Image>();
            Color inputBg = _theme != null ? _theme.inputFieldBg : new Color(0.02f, 0.08f, 0.02f, 0.9f);
            bg.color = inputBg;

            // Terminal border
            var outline = go.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(1, -1);

            // Text area (required by TMP_InputField)
            var textAreaGO = new GameObject("Text Area");
            textAreaGO.transform.SetParent(go.transform, false);
            var textAreaRT = textAreaGO.AddComponent<RectTransform>();
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.offsetMin = new Vector2(10, 0);
            textAreaRT.offsetMax = new Vector2(-10, 0);
            textAreaGO.AddComponent<RectMask2D>();

            // Placeholder text
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(textAreaGO.transform, false);
            var phRT = placeholderGO.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = Vector2.zero;
            phRT.offsetMax = Vector2.zero;
            var phTMP = placeholderGO.AddComponent<TextMeshProUGUI>();
            phTMP.text = "Type your guess...";
            if (_theme != null && _theme.primaryFont != null)
                phTMP.font = _theme.primaryFont;
            phTMP.fontSize = _theme != null ? _theme.bodySize : 18;
            Color placeholder = _theme != null ? _theme.inputPlaceholder : new Color(0f, 0.35f, 0.12f, 0.5f);
            phTMP.color = placeholder;
            phTMP.fontStyle = FontStyles.Italic;
            phTMP.alignment = TextAlignmentOptions.MidlineLeft;
            phTMP.raycastTarget = false;

            // Text component
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(textAreaGO.transform, false);
            var tRT = textGO.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.offsetMin = Vector2.zero;
            tRT.offsetMax = Vector2.zero;
            var inputTMP = textGO.AddComponent<TextMeshProUGUI>();
            inputTMP.text = "";
            if (_theme != null && _theme.primaryFont != null)
                inputTMP.font = _theme.primaryFont;
            inputTMP.fontSize = _theme != null ? _theme.bodySize : 18;
            Color inputColor = _theme != null ? _theme.inputFieldText : new Color(0f, 1f, 0.33f);
            inputTMP.color = inputColor;
            inputTMP.alignment = TextAlignmentOptions.MidlineLeft;
            inputTMP.richText = false;

            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = textAreaRT;
            input.textComponent = inputTMP;
            input.placeholder = phTMP;
            input.characterLimit = 25;
            if (_theme != null)
                input.fontAsset = _theme.primaryFont;

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
