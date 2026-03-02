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

            // Ambient data stream (Matrix-style, behind everything)
            var dataStreamGO = new GameObject("AmbientDataStream");
            dataStreamGO.transform.SetParent(canvasGO.transform, false);
            var dsRT = dataStreamGO.AddComponent<RectTransform>();
            dsRT.anchorMin = Vector2.zero;
            dsRT.anchorMax = Vector2.one;
            dsRT.offsetMin = Vector2.zero;
            dsRT.offsetMax = Vector2.zero;
            dataStreamGO.transform.SetSiblingIndex(1); // Right after background
            var dataStream = dataStreamGO.AddComponent<AmbientDataStream>();
            SetSerializedField(dataStream, "theme", _theme);

            // ── Panels ──────────────────────────────────────
            var mainMenuPanel = CreateMainMenuPanel(canvasGO.transform);
            var mapPanel = CreateMapPanel(canvasGO.transform);
            var encounterPanel = CreateEncounterPanel(canvasGO.transform);
            var shopPanel = CreateShopPanel(canvasGO.transform);
            var runEndPanel = CreateRunEndPanel(canvasGO.transform);

            // Add CanvasGroup + UIAnimator to each panel for entrance animations
            AddPanelAnimator(mainMenuPanel);
            AddPanelAnimator(mapPanel);
            AddPanelAnimator(encounterPanel);
            AddPanelAnimator(shopPanel);
            AddPanelAnimator(runEndPanel);

            // ── Screen Effects Overlay (scanlines + vignette, above panels) ──
            var overlayGO = new GameObject("ScreenEffectsOverlay");
            overlayGO.transform.SetParent(canvasGO.transform, false);
            var overlayRT = overlayGO.AddComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;

            // Scanlines RawImage
            var scanlinesGO = new GameObject("Scanlines");
            scanlinesGO.transform.SetParent(overlayGO.transform, false);
            var scanRT = scanlinesGO.AddComponent<RectTransform>();
            scanRT.anchorMin = Vector2.zero;
            scanRT.anchorMax = Vector2.one;
            scanRT.offsetMin = Vector2.zero;
            scanRT.offsetMax = Vector2.zero;
            var scanRaw = scanlinesGO.AddComponent<RawImage>();
            scanRaw.raycastTarget = false;
            scanRaw.enabled = false; // Enabled by ScreenEffectsOverlay after texture setup

            // Vignette RawImage
            var vignetteGO = new GameObject("Vignette");
            vignetteGO.transform.SetParent(overlayGO.transform, false);
            var vigRT = vignetteGO.AddComponent<RectTransform>();
            vigRT.anchorMin = Vector2.zero;
            vigRT.anchorMax = Vector2.one;
            vigRT.offsetMin = Vector2.zero;
            vigRT.offsetMax = Vector2.zero;
            var vigRaw = vignetteGO.AddComponent<RawImage>();
            vigRaw.raycastTarget = false;
            vigRaw.enabled = false; // Enabled by ScreenEffectsOverlay after texture setup

            // ScreenEffectsOverlay component
            var screenFX = overlayGO.AddComponent<ScreenEffectsOverlay>();
            SetSerializedField(screenFX, "theme", _theme);
            SetSerializedField(screenFX, "scanlineImage", scanRaw);
            SetSerializedField(screenFX, "vignetteImage", vigRaw);

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

        private static Color PhosphorGreen => ThemeColor(t => t.phosphorGreen, new Color(0.12f, 1f, 0.45f));
        private static Color PhosphorBright => ThemeColor(t => t.phosphorBright, new Color(0.3f, 1f, 0.6f));
        private static Color PhosphorDim => ThemeColor(t => t.phosphorDim, new Color(0.05f, 0.45f, 0.2f));
        private static Color TerminalBg => ThemeColor(t => t.terminalBg, new Color(0.01f, 0.02f, 0.02f, 1f));
        private static Color PanelBg => ThemeColor(t => t.panelBg, new Color(0.02f, 0.06f, 0.04f, 0.95f));
        private static Color BorderColor => ThemeColor(t => t.borderColor, new Color(0.05f, 0.55f, 0.25f, 0.7f));
        private static Color ButtonBorderColor => ThemeColor(t => t.buttonBorder, new Color(0.12f, 0.7f, 0.35f, 0.9f));
        private static Color AmberBright => ThemeColor(t => t.amberBright, new Color(1f, 0.78f, 0.15f));
        private static Color CyanInfo => ThemeColor(t => t.cyanInfo, new Color(0.15f, 0.9f, 0.95f));
        private static Color MagentaMagic => ThemeColor(t => t.magentaMagic, new Color(0.9f, 0.25f, 0.9f));
        private static Color ButtonBg => ThemeColor(t => t.buttonBg, new Color(0.03f, 0.18f, 0.08f, 0.95f));
        private static Color ButtonBgDanger => ThemeColor(t => t.buttonBgDanger, new Color(0.35f, 0.06f, 0.06f, 0.95f));
        private static Color ButtonText => ThemeColor(t => t.buttonText, new Color(0.12f, 1f, 0.45f));
        private static Color HpBarBg => ThemeColor(t => t.hpBarBg, new Color(0.1f, 0.15f, 0.1f, 1f));
        private static Color HpBarFill => ThemeColor(t => t.hpBarFill, new Color(0f, 0.8f, 0.25f, 1f));

        // ── Panel Creators ──────────────────────────────────

        private static GameObject CreateMainMenuPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "MainMenuPanel");
            AddPanelBorder(panel);
            var menuUI = panel.AddComponent<MainMenuUI>();

            // ── Corner brackets (decorative frame — larger, brighter) ──
            Color frameColor = Color.Lerp(PhosphorDim, PhosphorGreen, 0.35f);
            int frameSize = _theme != null ? _theme.headerSize + 8 : 38;
            CreateText(panel.transform, "CornerTL", "\u250C",
                frameSize, frameColor,
                TextAlignmentOptions.TopLeft, new Vector2(0.04f, 0.88f), new Vector2(0.12f, 0.97f));
            CreateText(panel.transform, "CornerTR", "\u2510",
                frameSize, frameColor,
                TextAlignmentOptions.TopRight, new Vector2(0.88f, 0.88f), new Vector2(0.96f, 0.97f));
            CreateText(panel.transform, "CornerBL", "\u2514",
                frameSize, frameColor,
                TextAlignmentOptions.BottomLeft, new Vector2(0.04f, 0.11f), new Vector2(0.12f, 0.20f));
            CreateText(panel.transform, "CornerBR", "\u2518",
                frameSize, frameColor,
                TextAlignmentOptions.BottomRight, new Vector2(0.88f, 0.11f), new Vector2(0.96f, 0.20f));

            // ── Top decorative border (between corners, brighter) ──
            CreateText(panel.transform, "TopBorder", new string('\u2500', 50),
                _theme != null ? _theme.smallSize + 1 : 14, frameColor,
                TextAlignmentOptions.Center, new Vector2(0.10f, 0.92f), new Vector2(0.90f, 0.96f));

            // ── Bottom decorative border (between corners, brighter) ──
            CreateText(panel.transform, "BottomBorder", new string('\u2500', 50),
                _theme != null ? _theme.smallSize + 1 : 14, frameColor,
                TextAlignmentOptions.Center, new Vector2(0.10f, 0.14f), new Vector2(0.90f, 0.18f));

            // ── Title (VT323, large, with glow + amber-green gradient) ──
            Color titleTop = new Color(1f, 0.9f, 0.5f); // warm amber-white
            var title = TerminalUIHelper.CreateDecorativeText(panel.transform, "TitleText", "SPELLWRIGHT",
                _theme, _theme != null ? _theme.decorativeTitleSize : 64, titleTop,
                TextAlignmentOptions.Center, new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.86f), applyGlow: true);
            TerminalUIHelper.ApplyVerticalGradient(title, titleTop, PhosphorGreen);

            // ── Title underline accent (amber, more visible) ──
            Color underlineColor = new Color(AmberBright.r, AmberBright.g, AmberBright.b, 0.55f);
            CreateText(panel.transform, "TitleUnderline", new string('\u2500', 22),
                _theme != null ? _theme.smallSize + 3 : 16, underlineColor,
                TextAlignmentOptions.Center, new Vector2(0.28f, 0.58f), new Vector2(0.72f, 0.62f));

            // ── Subtitle (typewriter-revealed by MainMenuUI, larger and brighter) ──
            Color subtitleColor = Color.Lerp(PhosphorDim, PhosphorGreen, 0.65f);
            var subtitle = CreateText(panel.transform, "SubtitleText", "",
                _theme != null ? _theme.bodySize + 2 : 20, subtitleColor,
                TextAlignmentOptions.Center, new Vector2(0.15f, 0.51f), new Vector2(0.85f, 0.58f));

            // ── Blinking cursor ──
            var cursor = CreateText(panel.transform, "CursorBlink", "_",
                _theme != null ? _theme.bodySize : 18, PhosphorGreen,
                TextAlignmentOptions.Center, new Vector2(0.48f, 0.47f), new Vector2(0.52f, 0.52f));

            // ── Separator ──
            TerminalUIHelper.CreateSeparator(panel.transform, "MenuSeparator",
                _theme, PhosphorDim, new Vector2(0.30f, 0.43f), new Vector2(0.70f, 0.47f));

            // ── Start button (amber-accented primary action) ──
            var startBtn = CreatePrimaryButton(panel.transform, "StartButton", "[ START RUN ]",
                new Vector2(0.34f, 0.28f), new Vector2(0.66f, 0.40f));

            // ── Hint text below button ──
            Color hintColor = Color.Lerp(PhosphorDim, PhosphorGreen, 0.25f);
            CreateText(panel.transform, "HintText", "Press Enter or click to begin",
                _theme != null ? _theme.smallSize + 1 : 14,
                new Color(hintColor.r, hintColor.g, hintColor.b, 0.75f),
                TextAlignmentOptions.Center, new Vector2(0.25f, 0.22f), new Vector2(0.75f, 0.28f));

            // ── Version text ──
            var versionText = CreateText(panel.transform, "VersionText", "v0.1.0",
                _theme != null ? _theme.smallSize : 13, PhosphorDim,
                TextAlignmentOptions.BottomRight, new Vector2(0.7f, 0.02f), new Vector2(0.96f, 0.08f));

            SetSerializedField(menuUI, "titleText", title);
            SetSerializedField(menuUI, "subtitleText", subtitle);
            SetSerializedField(menuUI, "startButton", startBtn.GetComponent<Button>());
            SetSerializedField(menuUI, "cursorBlink", cursor);
            SetSerializedField(menuUI, "versionText", versionText);
            SetSerializedField(menuUI, "theme", _theme);

            return panel;
        }

        private static GameObject CreateMapPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "MapPanel");
            AddPanelBorder(panel);
            var mapUI = panel.AddComponent<MapUI>();

            // ── Title (decorative, with glow) ──
            var title = TerminalUIHelper.CreateSectionHeader(panel.transform, "MapTitleText", "YOUR JOURNEY",
                _theme, PhosphorBright, new Vector2(0.1f, 0.92f), new Vector2(0.9f, 0.99f), applyGlow: true);

            // ── Stats bar container (horizontal chip layout) ──
            var statsBar = CreateContainer(panel.transform, "StatsBar",
                new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.92f));
            var statsHlg = statsBar.AddComponent<HorizontalLayoutGroup>();
            statsHlg.spacing = 10;
            statsHlg.padding = new RectOffset(4, 4, 2, 2);
            statsHlg.childForceExpandWidth = false;
            statsHlg.childForceExpandHeight = true;
            statsHlg.childControlWidth = true;
            statsHlg.childControlHeight = true;
            statsHlg.childAlignment = TextAnchor.MiddleCenter;

            // Wave chip
            var waveChip = CreateStatChip(statsBar.transform, "WaveChip", "# Wave 1",
                CyanInfo, new Color(CyanInfo.r * 0.08f, CyanInfo.g * 0.08f, CyanInfo.b * 0.08f, 0.7f));
            var waveText = waveChip.GetComponentInChildren<TextMeshProUGUI>();

            // HP chip
            var hpChip = CreateStatChip(statsBar.transform, "HPChip", "HP --/--",
                PhosphorGreen, new Color(PhosphorGreen.r * 0.06f, PhosphorGreen.g * 0.06f, PhosphorGreen.b * 0.06f, 0.7f));
            var hpStatText = hpChip.GetComponentInChildren<TextMeshProUGUI>();

            // Gold chip
            var goldChip = CreateStatChip(statsBar.transform, "GoldChip", "$ --g",
                AmberBright, new Color(AmberBright.r * 0.08f, AmberBright.g * 0.08f, AmberBright.b * 0.08f, 0.7f));
            var goldStatText = goldChip.GetComponentInChildren<TextMeshProUGUI>();

            // Score chip
            var scoreChip = CreateStatChip(statsBar.transform, "ScoreChip", "* --",
                CyanInfo, new Color(CyanInfo.r * 0.08f, CyanInfo.g * 0.08f, CyanInfo.b * 0.08f, 0.7f));
            var scoreStatText = scoreChip.GetComponentInChildren<TextMeshProUGUI>();

            // ── Separator ──
            TerminalUIHelper.CreateSeparator(panel.transform, "MapSeparator",
                _theme, PhosphorDim, new Vector2(0.05f, 0.81f), new Vector2(0.95f, 0.84f));

            // ── Node container ──
            var container = CreateContainer(panel.transform, "NodeContainer",
                new Vector2(0.08f, 0.15f), new Vector2(0.92f, 0.81f));
            var vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.padding = new RectOffset(0, 0, 6, 6);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            var csf = container.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ── Proceed button (amber-accented primary action) ──
            var proceedBtn = CreatePrimaryButton(panel.transform, "ProceedButton", "[ PROCEED ]",
                new Vector2(0.3f, 0.04f), new Vector2(0.7f, 0.12f));

            // ── Language toggle (small, bottom-left) ──
            var langBtn = CreateButton(panel.transform, "LanguageButton", "EN", ButtonBg,
                new Vector2(0.02f, 0.04f), new Vector2(0.15f, 0.10f));
            var langLabel = langBtn.GetComponentInChildren<TMP_Text>();

            SetSerializedField(mapUI, "mapTitleText", title);
            SetSerializedField(mapUI, "waveText", waveText);
            SetSerializedField(mapUI, "hpStatText", hpStatText);
            SetSerializedField(mapUI, "goldStatText", goldStatText);
            SetSerializedField(mapUI, "scoreStatText", scoreStatText);
            SetSerializedField(mapUI, "nodeContainer", container.transform);
            SetSerializedField(mapUI, "proceedButton", proceedBtn.GetComponent<Button>());
            SetSerializedField(mapUI, "languageButton", langBtn.GetComponent<Button>());
            SetSerializedField(mapUI, "languageButtonText", langLabel);
            SetSerializedField(mapUI, "gameConfig", LoadAsset<GameConfigSO>("Assets/Data/Config/GameConfig.asset"));
            SetSerializedField(mapUI, "theme", _theme);

            return panel;
        }

        /// <summary>
        /// Creates a stat "chip" — a small rounded-feel container with colored bg, border, and label.
        /// Balatro-inspired: compact, color-coded, with icon prefix.
        /// </summary>
        private static GameObject CreateStatChip(Transform parent, string name, string text,
            Color textColor, Color bgColor)
        {
            var chip = new GameObject(name);
            chip.transform.SetParent(parent, false);
            chip.AddComponent<RectTransform>();

            var chipBg = chip.AddComponent<Image>();
            chipBg.color = bgColor;

            var chipOutline = chip.AddComponent<Outline>();
            chipOutline.effectColor = new Color(textColor.r, textColor.g, textColor.b, 0.35f);
            chipOutline.effectDistance = new Vector2(1, -1);

            var chipLe = chip.AddComponent<LayoutElement>();
            chipLe.flexibleWidth = 1;
            chipLe.minHeight = 28;
            chipLe.preferredHeight = 28;

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(chip.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(8, 0);
            labelRT.offsetMax = new Vector2(-8, 0);

            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = text;
            if (_theme != null && _theme.primaryFont != null)
                label.font = _theme.primaryFont;
            label.fontSize = _theme != null ? _theme.labelSize + 2 : 17;
            label.color = textColor;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;

            return chip;
        }

        /// <summary>
        /// Creates a stat chip for the encounter screen. Like CreateStatChip but with a custom
        /// label name so FindChild can locate it by the expected encounter field name.
        /// </summary>
        private static GameObject CreateEncounterStatChip(Transform parent, string chipName, string labelName,
            string text, Color textColor, Color bgColor, float flexWidth = 1f)
        {
            var chip = new GameObject(chipName);
            chip.transform.SetParent(parent, false);
            chip.AddComponent<RectTransform>();

            var chipBg = chip.AddComponent<Image>();
            chipBg.color = bgColor;

            var chipOutline = chip.AddComponent<Outline>();
            chipOutline.effectColor = new Color(textColor.r, textColor.g, textColor.b, 0.35f);
            chipOutline.effectDistance = new Vector2(1, -1);

            var chipLe = chip.AddComponent<LayoutElement>();
            chipLe.flexibleWidth = flexWidth;
            chipLe.minHeight = 34;
            chipLe.preferredHeight = 34;

            var labelGO = new GameObject(labelName);
            labelGO.transform.SetParent(chip.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 0.25f);
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(8, 0);
            labelRT.offsetMax = new Vector2(-8, 0);

            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = text;
            if (_theme != null && _theme.primaryFont != null)
                label.font = _theme.primaryFont;
            label.fontSize = _theme != null ? _theme.labelSize + 2 : 17;
            label.color = textColor;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;

            return chip;
        }

        private static GameObject CreateEncounterPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "EncounterPanel");
            AddPanelBorder(panel);

            // ── Board Frame (WoF-style bordered frame) ──
            var boardFrame = CreateFullscreenImage(panel.transform, "BoardFrame",
                new Color(PanelBg.r, PanelBg.g, PanelBg.b, 0.7f));
            SetAnchors(boardFrame, new Vector2(0.04f, 0.73f), new Vector2(0.96f, 0.97f));
            var boardFrameOutline = boardFrame.AddComponent<Outline>();
            boardFrameOutline.effectColor = new Color(PhosphorGreen.r, PhosphorGreen.g, PhosphorGreen.b, 0.6f);
            boardFrameOutline.effectDistance = new Vector2(2, -2);

            // ── Tile Board (inside frame, doubled height) ──
            var boardContainer = CreateContainer(panel.transform, "BoardContainer",
                new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.96f));
            boardContainer.AddComponent<TileBoardUI>();
            SetSerializedField(boardContainer.GetComponent<TileBoardUI>(), "theme", _theme);

            // ── Guessed Letters (hidden, kept for wiring) ──
            var guessedContainer = CreateContainer(panel.transform, "GuessedLettersContainer",
                new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.64f));
            guessedContainer.AddComponent<GuessedLettersUI>();
            SetSerializedField(guessedContainer.GetComponent<GuessedLettersUI>(), "theme", _theme);
            guessedContainer.SetActive(false);

            // ── NPC Dialog Card (portrait + name + category + clue grouped) ──
            var dialogBg = CreateFullscreenImage(panel.transform, "DialogBg",
                new Color(PanelBg.r, PanelBg.g, PanelBg.b, 0.7f));
            SetAnchors(dialogBg, new Vector2(0.04f, 0.40f), new Vector2(0.96f, 0.64f));
            var dialogOutline = dialogBg.AddComponent<Outline>();
            dialogOutline.effectColor = new Color(BorderColor.r, BorderColor.g, BorderColor.b, 0.35f);
            dialogOutline.effectDistance = new Vector2(1, -1);

            // ── NPC Portrait (inside dialog card, left side) ──
            var portraitContainer = CreateContainer(panel.transform, "PortraitContainer",
                new Vector2(0.05f, 0.55f), new Vector2(0.15f, 0.64f));
            var portraitUI = portraitContainer.AddComponent<NPCPortraitUI>();
            var portraitText = CreateText(portraitContainer.transform, "PortraitText", "",
                _theme != null ? _theme.smallSize - 2 : 11, PhosphorGreen,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 0f), new Vector2(1f, 1f));
            portraitText.enableWordWrapping = false;
            portraitText.overflowMode = TextOverflowModes.Overflow;
            SetSerializedField(portraitUI, "portraitText", portraitText);
            SetSerializedField(portraitUI, "theme", _theme);

            // ── NPC Name (inside dialog card, right of portrait) ──
            var npcName = TerminalUIHelper.CreateDecorativeText(panel.transform, "NpcNameText", "NPC Name",
                _theme, _theme != null ? _theme.decorativeSubheaderSize + 6 : 32, PhosphorBright,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.16f, 0.58f), new Vector2(0.60f, 0.64f));

            // ── Archetype (hidden, kept for wiring) ──
            var npcArchetype = CreateText(panel.transform, "NpcArchetypeText", "",
                _theme != null ? _theme.labelSize : 14, PhosphorDim,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.16f, 0.57f), new Vector2(0.60f, 0.58f));

            // ── Category (right of portrait, under name) ──
            var category = CreateText(panel.transform, "CategoryText", "Category: ...",
                _theme != null ? _theme.bodySize + 2 : 20, AmberBright,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.16f, 0.52f), new Vector2(0.80f, 0.57f));
            if (_theme != null)
                TerminalUIHelper.ApplyGlow(category, AmberBright, _theme.subtleGlowOffset, _theme.subtleGlowPower);

            // ── NPC separator (inside dialog card) ──
            TerminalUIHelper.CreateSeparator(panel.transform, "NpcSeparator",
                _theme, PhosphorDim, new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.51f));

            // ── Clue Number + Text (inside dialog card, below separator) ──
            var clueNum = CreateText(panel.transform, "ClueNumberText", "[Clue #1]",
                _theme != null ? _theme.bodySize : 18, AmberBright,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.06f, 0.46f), new Vector2(0.30f, 0.50f));
            if (_theme != null)
                TerminalUIHelper.ApplyGlow(clueNum, AmberBright, _theme.subtleGlowOffset, _theme.subtleGlowPower);

            var clue = CreateText(panel.transform, "ClueText", "Waiting for clue...",
                _theme != null ? _theme.bodySize + 4 : 22, PhosphorGreen,
                TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.41f), new Vector2(0.94f, 0.46f));

            // ── Status Bar (chip layout, equal-width chips, same width as frame/dialog) ──
            var statsBar = CreateContainer(panel.transform, "EncounterStatsBar",
                new Vector2(0.04f, 0.34f), new Vector2(0.96f, 0.40f));
            var statsHlg = statsBar.AddComponent<HorizontalLayoutGroup>();
            statsHlg.spacing = 6;
            statsHlg.padding = new RectOffset(0, 0, 0, 0);
            statsHlg.childForceExpandWidth = false;
            statsHlg.childForceExpandHeight = true;
            statsHlg.childControlWidth = true;
            statsHlg.childControlHeight = true;
            statsHlg.childAlignment = TextAnchor.MiddleCenter;

            // HP chip (equal width now)
            var hpChip = CreateEncounterStatChip(statsBar.transform, "HPChip", "HPText",
                "HP 30/30", PhosphorGreen,
                new Color(PhosphorGreen.r * 0.06f, PhosphorGreen.g * 0.06f, PhosphorGreen.b * 0.06f, 0.7f),
                flexWidth: 1f);
            // HP bar embedded at bottom of HP chip
            var hpBarBg = new GameObject("HPBarBg");
            hpBarBg.transform.SetParent(hpChip.transform, false);
            var hpBarBgRT = hpBarBg.AddComponent<RectTransform>();
            hpBarBgRT.anchorMin = new Vector2(0.03f, 0.08f);
            hpBarBgRT.anchorMax = new Vector2(0.97f, 0.28f);
            hpBarBgRT.offsetMin = Vector2.zero;
            hpBarBgRT.offsetMax = Vector2.zero;
            var hpBarBgImg = hpBarBg.AddComponent<Image>();
            hpBarBgImg.color = HpBarBg;
            var hpBarFill = CreateFullscreenImage(hpBarBg.transform, "HPBarFill", HpBarFill);
            var hpFillRT = hpBarFill.GetComponent<RectTransform>();
            hpFillRT.anchorMin = Vector2.zero;
            hpFillRT.anchorMax = Vector2.one;
            hpFillRT.offsetMin = Vector2.zero;
            hpFillRT.offsetMax = Vector2.zero;

            // Gold chip
            CreateEncounterStatChip(statsBar.transform, "GoldChip", "GoldText",
                "$ 0g", AmberBright,
                new Color(AmberBright.r * 0.08f, AmberBright.g * 0.08f, AmberBright.b * 0.08f, 0.7f));

            // Guesses chip
            CreateEncounterStatChip(statsBar.transform, "GuessesChip", "GuessesText",
                "Guesses: 6", CyanInfo,
                new Color(CyanInfo.r * 0.08f, CyanInfo.g * 0.08f, CyanInfo.b * 0.08f, 0.7f));

            // Score chip
            CreateEncounterStatChip(statsBar.transform, "ScoreChip", "ScoreText",
                "* 0", CyanInfo,
                new Color(CyanInfo.r * 0.08f, CyanInfo.g * 0.08f, CyanInfo.b * 0.08f, 0.7f));

            // ── Input Area (with background container) ──
            var inputAreaBg = CreateFullscreenImage(panel.transform, "InputAreaBg",
                new Color(PanelBg.r * 0.7f, PanelBg.g * 0.7f, PanelBg.b * 0.7f, 0.5f));
            SetAnchors(inputAreaBg, new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.34f));
            var inputAreaOutline = inputAreaBg.AddComponent<Outline>();
            inputAreaOutline.effectColor = new Color(BorderColor.r, BorderColor.g, BorderColor.b, 0.25f);
            inputAreaOutline.effectDistance = new Vector2(1, -1);

            var terminalPrompt = CreateText(panel.transform, "TerminalPrompt", ">_",
                _theme != null ? _theme.bodySize + 2 : 20, PhosphorGreen,
                TextAlignmentOptions.MidlineRight, new Vector2(0.06f, 0.26f), new Vector2(0.09f, 0.32f));

            var inputGO = CreateInputField(panel.transform, "GuessInput",
                new Vector2(0.09f, 0.26f), new Vector2(0.58f, 0.32f));
            var submitBtn = CreateButton(panel.transform, "SubmitButton", "[ GUESS ]", ButtonBg,
                new Vector2(0.60f, 0.26f), new Vector2(0.76f, 0.32f));
            var solveBtn = CreateButton(panel.transform, "SolveButton", "[ SOLVE ]", ButtonBg,
                new Vector2(0.78f, 0.26f), new Vector2(0.94f, 0.32f));

            // Input mode text (shows LETTER MODE / SOLVE MODE) — below input area
            var inputModeText = CreateText(panel.transform, "InputModeText", "LETTER MODE",
                _theme != null ? _theme.smallSize + 1 : 14, PhosphorDim,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.09f, 0.21f), new Vector2(0.40f, 0.24f));

            // ── History section header (centered, true 50% left) ──
            CreateText(panel.transform, "HistoryHeader", "\u2500\u2500 History \u2500\u2500",
                _theme != null ? _theme.smallSize + 1 : 14, PhosphorDim,
                TextAlignmentOptions.Midline, new Vector2(0.05f, 0.20f), new Vector2(0.48f, 0.24f));

            // ── Tome section header (centered, true 50% right) ──
            CreateText(panel.transform, "TomeHeader", "\u2500\u2500 Tomes \u2500\u2500",
                _theme != null ? _theme.smallSize + 1 : 14, MagentaMagic,
                TextAlignmentOptions.Midline, new Vector2(0.52f, 0.20f), new Vector2(0.95f, 0.24f));

            // ── Guess History (true 50% left column) ──
            var history = CreateText(panel.transform, "HistoryText", "",
                _theme != null ? _theme.smallSize + 1 : 14, PhosphorDim,
                TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.02f), new Vector2(0.48f, 0.20f));

            // ── Tome Info (true 50% right column) ──
            var tomeInfo = CreateText(panel.transform, "TomeInfoText", "",
                _theme != null ? _theme.smallSize + 1 : 14, MagentaMagic,
                TextAlignmentOptions.TopLeft, new Vector2(0.52f, 0.02f), new Vector2(0.95f, 0.20f));

            // ── Result Panel (overlay) ──
            var resultPanel = CreateFullscreenImage(panel.transform, "ResultPanel",
                new Color(TerminalBg.r, TerminalBg.g, TerminalBg.b, 0.9f));

            // Result banner (ASCII art)
            var resultBanner = CreateText(resultPanel.transform, "ResultBannerText", "",
                _theme != null ? _theme.smallSize - 1 : 12, PhosphorGreen,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.88f));
            resultBanner.enableWordWrapping = false;
            resultBanner.overflowMode = TextOverflowModes.Overflow;

            var resultTitle = TerminalUIHelper.CreateDecorativeText(resultPanel.transform, "ResultTitleText", "VICTORY!",
                _theme, _theme != null ? _theme.decorativeHeaderSize : 36, PhosphorBright,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.72f), applyGlow: true);

            // Result separator bottom
            CreateText(resultPanel.transform, "ResultBottomBorder", new string('\u2550', 25),
                _theme != null ? _theme.smallSize : 13, PhosphorDim,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.56f));

            var resultDetails = CreateText(resultPanel.transform, "ResultDetailsText", "",
                _theme != null ? _theme.bodySize : 18, PhosphorGreen,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.52f));
            var continueBtn = CreatePrimaryButton(resultPanel.transform, "ContinueButton", "[ CONTINUE ]",
                new Vector2(0.3f, 0.15f), new Vector2(0.7f, 0.28f));
            resultPanel.SetActive(false);

            // ── Boss Entrance Overlay ──
            var bossOverlay = CreateFullscreenImage(panel.transform, "BossEntranceOverlay",
                new Color(0, 0, 0, 0));
            var bossEntUI = bossOverlay.AddComponent<BossEntranceUI>();
            var bossBanner = CreateText(bossOverlay.transform, "BossBannerText", "",
                _theme != null ? _theme.smallSize - 1 : 12, PhosphorGreen,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.75f));
            bossBanner.enableWordWrapping = false;
            bossBanner.overflowMode = TextOverflowModes.Overflow;
            var bossNameLabel = TerminalUIHelper.CreateDecorativeText(bossOverlay.transform, "BossNameText", "",
                _theme, _theme != null ? _theme.decorativeHeaderSize : 36, PhosphorBright,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.50f), applyGlow: true);
            SetSerializedField(bossEntUI, "overlayPanel", bossOverlay);
            SetSerializedField(bossEntUI, "overlayBg", bossOverlay.GetComponent<Image>());
            SetSerializedField(bossEntUI, "bannerText", bossBanner);
            SetSerializedField(bossEntUI, "bossNameText", bossNameLabel);
            SetSerializedField(bossEntUI, "theme", _theme);
            bossOverlay.SetActive(false);

            // ── Flash Overlay ──
            var flash = CreateFullscreenImage(panel.transform, "FlashOverlay", new Color(1f, 1f, 1f, 0f));
            flash.GetComponent<Image>().raycastTarget = false;

            // ── TextSpinner + AnimatedCounter ──
            var spinner = panel.AddComponent<TextSpinner>();
            SetSerializedField(spinner, "theme", _theme);
            var counter = panel.AddComponent<AnimatedCounter>();
            SetSerializedField(counter, "theme", _theme);

            // ── Suspense Effects ──
            var suspense = panel.AddComponent<SuspenseEffects>();
            SetSerializedField(suspense, "clueText", FindChild<TextMeshProUGUI>(panel, "ClueText"));
            SetSerializedField(suspense, "hpBarFill", FindChild<Image>(panel, "HPBarFill"));
            SetSerializedField(suspense, "guessesText", FindChild<TextMeshProUGUI>(panel, "GuessesText"));
            SetSerializedField(suspense, "flashOverlay", FindChild<Image>(panel, "FlashOverlay"));
            SetSerializedField(suspense, "resultPanel", FindChildGO(panel, "ResultPanel"));
            SetSerializedField(suspense, "theme", _theme);

            return panel;
        }

        private static GameObject CreateShopPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "ShopPanel");
            AddPanelBorder(panel);

            // ── Title (decorative, large, amber-green gradient) ──
            var shopTitle = TerminalUIHelper.CreateDecorativeText(panel.transform, "ShopTitle", "ARCANE SHOP",
                _theme, _theme != null ? _theme.decorativeHeaderSize + 6 : 42, AmberBright,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.91f), new Vector2(0.9f, 0.99f), applyGlow: true);
            TerminalUIHelper.ApplyVerticalGradient(shopTitle, AmberBright,
                new Color(AmberBright.r * 0.7f, AmberBright.g * 0.5f, AmberBright.b * 0.2f));

            // ── Stats bar (chip layout matching map/encounter) ──
            var statsBar = CreateContainer(panel.transform, "ShopStatsBar",
                new Vector2(0.06f, 0.85f), new Vector2(0.94f, 0.92f));
            var statsHlg = statsBar.AddComponent<HorizontalLayoutGroup>();
            statsHlg.spacing = 10;
            statsHlg.padding = new RectOffset(4, 4, 2, 2);
            statsHlg.childForceExpandWidth = false;
            statsHlg.childForceExpandHeight = true;
            statsHlg.childControlWidth = true;
            statsHlg.childControlHeight = true;
            statsHlg.childAlignment = TextAnchor.MiddleCenter;

            // Gold chip (amber)
            var goldChip = CreateStatChip(statsBar.transform, "GoldChip", "$ 0g",
                AmberBright, new Color(AmberBright.r * 0.08f, AmberBright.g * 0.08f, AmberBright.b * 0.08f, 0.7f));
            var goldText = goldChip.GetComponentInChildren<TextMeshProUGUI>();

            // HP chip (green, wider with embedded HP bar)
            var hpChip = CreateEncounterStatChip(statsBar.transform, "HPChip", "HPLabel",
                "HP 30/30", PhosphorGreen,
                new Color(PhosphorGreen.r * 0.06f, PhosphorGreen.g * 0.06f, PhosphorGreen.b * 0.06f, 0.7f),
                flexWidth: 1.5f);
            var hpText = hpChip.transform.Find("HPLabel")?.GetComponent<TextMeshProUGUI>();
            // HP bar at bottom of chip
            var shopHpBarBg = new GameObject("HPBarBg");
            shopHpBarBg.transform.SetParent(hpChip.transform, false);
            var shopHpBarBgRT = shopHpBarBg.AddComponent<RectTransform>();
            shopHpBarBgRT.anchorMin = new Vector2(0.03f, 0.08f);
            shopHpBarBgRT.anchorMax = new Vector2(0.97f, 0.28f);
            shopHpBarBgRT.offsetMin = Vector2.zero;
            shopHpBarBgRT.offsetMax = Vector2.zero;
            shopHpBarBg.AddComponent<Image>().color = HpBarBg;
            var shopHpFill = CreateFullscreenImage(shopHpBarBg.transform, "HPBarFill", HpBarFill);
            var shopHpFillRT = shopHpFill.GetComponent<RectTransform>();
            shopHpFillRT.anchorMin = Vector2.zero;
            shopHpFillRT.anchorMax = Vector2.one;
            shopHpFillRT.offsetMin = Vector2.zero;
            shopHpFillRT.offsetMax = Vector2.zero;

            // ── Separator ──
            TerminalUIHelper.CreateSeparator(panel.transform, "TopSeparator",
                _theme, PhosphorDim, new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.85f));

            // ── Buy section header (amber accent) ──
            CreateText(panel.transform, "BuyHeader", "\u2500\u2500 FOR SALE \u2500\u2500",
                _theme != null ? _theme.labelSize + 2 : 16, AmberBright,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.05f, 0.78f), new Vector2(0.50f, 0.83f));

            // ── Item container ──
            var itemContainer = CreateContainer(panel.transform, "ItemContainer",
                new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.78f));
            var vlg1 = itemContainer.AddComponent<VerticalLayoutGroup>();
            vlg1.spacing = 6;
            vlg1.padding = new RectOffset(0, 0, 2, 2);
            vlg1.childAlignment = TextAnchor.UpperLeft;
            vlg1.childForceExpandWidth = true;
            vlg1.childForceExpandHeight = false;
            vlg1.childControlWidth = true;
            vlg1.childControlHeight = true;

            // ── Separator ──
            TerminalUIHelper.CreateSeparator(panel.transform, "MidSeparator",
                _theme, PhosphorDim, new Vector2(0.05f, 0.39f), new Vector2(0.95f, 0.42f));

            // ── Sell section header (magenta accent) ──
            CreateText(panel.transform, "SellHeader", "\u2500\u2500 YOUR TOMES \u2500\u2500",
                _theme != null ? _theme.labelSize + 2 : 16, MagentaMagic,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.05f, 0.35f), new Vector2(0.50f, 0.40f));

            // ── Equipped container ──
            var equippedContainer = CreateContainer(panel.transform, "EquippedContainer",
                new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.35f));
            var vlg2 = equippedContainer.AddComponent<VerticalLayoutGroup>();
            vlg2.spacing = 6;
            vlg2.padding = new RectOffset(0, 0, 2, 2);
            vlg2.childAlignment = TextAnchor.UpperLeft;
            vlg2.childForceExpandWidth = true;
            vlg2.childForceExpandHeight = false;
            vlg2.childControlWidth = true;
            vlg2.childControlHeight = true;

            // ── Feedback text ──
            var feedbackText = CreateText(panel.transform, "FeedbackText", "",
                _theme != null ? _theme.labelSize : 16, AmberBright,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.13f));

            // ── Leave button ──
            var leaveBtn = CreateButton(panel.transform, "LeaveButton", "[ LEAVE SHOP ]", ButtonBg,
                new Vector2(0.32f, 0.02f), new Vector2(0.68f, 0.08f));

            // ── Wire ShopUI ──
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

            // ── Corner brackets (decorative frame, matching main menu) ──
            Color frameColor = Color.Lerp(PhosphorDim, PhosphorGreen, 0.35f);
            int frameSize = _theme != null ? _theme.headerSize + 8 : 38;
            CreateText(panel.transform, "CornerTL", "\u250C",
                frameSize, frameColor,
                TextAlignmentOptions.TopLeft, new Vector2(0.04f, 0.88f), new Vector2(0.12f, 0.97f));
            CreateText(panel.transform, "CornerTR", "\u2510",
                frameSize, frameColor,
                TextAlignmentOptions.TopRight, new Vector2(0.88f, 0.88f), new Vector2(0.96f, 0.97f));
            CreateText(panel.transform, "CornerBL", "\u2514",
                frameSize, frameColor,
                TextAlignmentOptions.BottomLeft, new Vector2(0.04f, 0.05f), new Vector2(0.12f, 0.14f));
            CreateText(panel.transform, "CornerBR", "\u2518",
                frameSize, frameColor,
                TextAlignmentOptions.BottomRight, new Vector2(0.88f, 0.05f), new Vector2(0.96f, 0.14f));

            // ── Top decorative border ──
            CreateText(panel.transform, "TopBorder", new string('\u2500', 50),
                _theme != null ? _theme.smallSize + 1 : 14, frameColor,
                TextAlignmentOptions.Center, new Vector2(0.10f, 0.92f), new Vector2(0.90f, 0.96f));

            // ── ASCII Banner (above title) ──
            var banner = CreateText(panel.transform, "BannerText", "",
                _theme != null ? _theme.smallSize - 1 : 12, PhosphorGreen,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.92f));
            banner.enableWordWrapping = false;
            banner.overflowMode = TextOverflowModes.Overflow;

            // ── Title (VT323 with glow) ──
            var title = TerminalUIHelper.CreateDecorativeText(panel.transform, "TitleText", "RUN OVER",
                _theme, _theme != null ? _theme.decorativeTitleSize - 12 : 52, PhosphorBright,
                TextAlignmentOptions.Center, new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.78f), applyGlow: true);

            // ── Separator above stats (amber for visual hierarchy) ──
            Color sepAmber = new Color(AmberBright.r, AmberBright.g, AmberBright.b, 0.35f);
            TerminalUIHelper.CreateSeparator(panel.transform, "StatsSepTop",
                _theme, sepAmber, new Vector2(0.20f, 0.59f), new Vector2(0.80f, 0.62f));

            // ── Stats ──
            var stats = CreateText(panel.transform, "StatsText", "Score: 0\nEncounters: 0",
                _theme != null ? _theme.bodySize + 2 : 20, PhosphorGreen,
                TextAlignmentOptions.Center, new Vector2(0.15f, 0.30f), new Vector2(0.85f, 0.59f));

            // ── Separator below stats ──
            TerminalUIHelper.CreateSeparator(panel.transform, "StatsSepBottom",
                _theme, sepAmber, new Vector2(0.20f, 0.27f), new Vector2(0.80f, 0.30f));

            // ── Play again button (amber-accented primary action) ──
            var playAgainBtn = CreatePrimaryButton(panel.transform, "PlayAgainButton", "[ PLAY AGAIN ]",
                new Vector2(0.3f, 0.14f), new Vector2(0.7f, 0.25f));

            // ── Bottom decorative border ──
            CreateText(panel.transform, "BottomBorder", new string('\u2500', 50),
                _theme != null ? _theme.smallSize + 1 : 14, frameColor,
                TextAlignmentOptions.Center, new Vector2(0.10f, 0.08f), new Vector2(0.90f, 0.12f));

            SetSerializedField(runEndUI, "bannerText", banner);
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
            var so = new SerializedObject(encUI);
            so.FindProperty("npcNameText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "NpcNameText");
            so.FindProperty("npcArchetypeText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "NpcArchetypeText");
            so.FindProperty("npcPortraitUI").objectReferenceValue = FindChild<NPCPortraitUI>(panel, "PortraitContainer");
            so.FindProperty("tileBoardUI").objectReferenceValue = FindChild<TileBoardUI>(panel, "BoardContainer");
            so.FindProperty("guessedLettersUI").objectReferenceValue = FindChild<GuessedLettersUI>(panel, "GuessedLettersContainer");
            so.FindProperty("categoryText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "CategoryText");
            so.FindProperty("clueText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "ClueText");
            so.FindProperty("clueNumberText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "ClueNumberText");
            so.FindProperty("guessInput").objectReferenceValue = FindChild<TMP_InputField>(panel, "GuessInput");
            so.FindProperty("submitButton").objectReferenceValue = FindChild<Button>(panel, "SubmitButton");
            so.FindProperty("solveButton").objectReferenceValue = FindChild<Button>(panel, "SolveButton");
            so.FindProperty("inputModeText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "InputModeText");
            so.FindProperty("hpText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "HPText");
            so.FindProperty("hpBarFill").objectReferenceValue = FindChild<Image>(panel, "HPBarFill");
            so.FindProperty("goldText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "GoldText");
            so.FindProperty("guessesText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "GuessesText");
            so.FindProperty("scoreText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "ScoreText");
            so.FindProperty("historyText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "HistoryText");
            so.FindProperty("tomeInfoText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "TomeInfoText");
            so.FindProperty("resultPanel").objectReferenceValue = FindChildGO(panel, "ResultPanel");
            so.FindProperty("resultBannerText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "ResultBannerText");
            so.FindProperty("resultTitleText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "ResultTitleText");
            so.FindProperty("resultDetailsText").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "ResultDetailsText");
            so.FindProperty("continueButton").objectReferenceValue = FindChild<Button>(panel, "ContinueButton");
            so.FindProperty("flashOverlay").objectReferenceValue = FindChild<Image>(panel, "FlashOverlay");
            so.FindProperty("terminalPrompt").objectReferenceValue = FindChild<TextMeshProUGUI>(panel, "TerminalPrompt");
            so.FindProperty("suspenseEffects").objectReferenceValue = panel.GetComponent<SuspenseEffects>();
            so.FindProperty("textSpinner").objectReferenceValue = panel.GetComponent<TextSpinner>();
            so.FindProperty("animatedCounter").objectReferenceValue = panel.GetComponent<AnimatedCounter>();
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

        private static void AddPanelAnimator(GameObject panel)
        {
            if (panel.GetComponent<CanvasGroup>() == null)
                panel.AddComponent<CanvasGroup>();
            if (panel.GetComponent<UIAnimator>() == null)
                panel.AddComponent<UIAnimator>();
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

            // Button border — brighter than panel borders for visual presence
            var outline = go.AddComponent<Outline>();
            outline.effectColor = ButtonBorderColor;
            outline.effectDistance = new Vector2(2, -2);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Disable built-in color tinting (we handle hover via ButtonHoverEffect)
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colors;

            // Enhanced hover effect
            go.AddComponent<ButtonHoverEffect>();

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
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return go;
        }

        /// <summary>
        /// Creates an amber-accented primary action button with warmer bg and amber border.
        /// Used for main calls-to-action: START RUN, PROCEED, PLAY AGAIN, CONTINUE.
        /// </summary>
        private static GameObject CreatePrimaryButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            // Bold amber-tinted background - unmistakably distinct from dark green panel
            Color primaryBg = new Color(0.30f, 0.22f, 0.06f, 0.92f);
            Color amberBorder = new Color(AmberBright.r, AmberBright.g, AmberBright.b, 0.9f);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = primaryBg;

            // Amber border for visual distinction
            var outline = go.AddComponent<Outline>();
            outline.effectColor = amberBorder;
            outline.effectDistance = new Vector2(2, -2);

            // Outer amber glow (second shadow component)
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(AmberBright.r, AmberBright.g, AmberBright.b, 0.25f);
            shadow.effectDistance = new Vector2(3, -3);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Disable built-in color tinting
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colors;

            go.AddComponent<ButtonHoverEffect>();

            // Label in warm amber
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            Color labelColor = Color.Lerp(AmberBright, new Color(1f, 0.95f, 0.7f), 0.3f);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            if (_theme != null && _theme.primaryFont != null)
                tmp.font = _theme.primaryFont;
            tmp.fontSize = _theme != null ? _theme.bodySize + 2 : 20;
            tmp.color = labelColor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            // Apply glow to label for warmth
            if (_theme != null)
                TerminalUIHelper.ApplyGlow(tmp, AmberBright, _theme.subtleGlowOffset, _theme.subtleGlowPower);

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
