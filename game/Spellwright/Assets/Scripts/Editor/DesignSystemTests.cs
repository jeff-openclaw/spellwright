using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Linq;

namespace Spellwright.Editor
{
    public static class DesignSystemTests
    {
        private const string USSPath = "Assets/UI/Styles/";
        private const string TSSPath = "Assets/UI/Themes/terminal-theme.tss";
        private const string TestUXMLPath = "Assets/UI/Screens/theme-test.uxml";

        [MenuItem("Spellwright/Tests/Verify Design System")]
        public static void VerifyDesignSystem()
        {
            int passed = 0;
            int failed = 0;

            // Check all USS files exist
            string[] requiredUSS = {
                "variables.uss", "reset.uss", "typography.uss",
                "components.uss", "layout.uss", "animations.uss"
            };

            foreach (var file in requiredUSS)
            {
                var path = USSPath + file;
                var asset = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (asset != null)
                {
                    Debug.Log($"[PASS] USS file loaded: {path}");
                    passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] USS file not found or has errors: {path}");
                    failed++;
                }
            }

            // Check TSS exists
            var tss = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(TSSPath);
            if (tss != null)
            {
                Debug.Log($"[PASS] TSS theme loaded: {TSSPath}");
                passed++;
            }
            else
            {
                Debug.LogError($"[FAIL] TSS theme not found: {TSSPath}");
                failed++;
            }

            // Check test UXML exists
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TestUXMLPath);
            if (uxml != null)
            {
                Debug.Log($"[PASS] Test UXML loaded: {TestUXMLPath}");
                passed++;
            }
            else
            {
                Debug.LogError($"[FAIL] Test UXML not found: {TestUXMLPath}");
                failed++;
            }

            // Check screen USS stubs
            string[] screenUSS = {
                "main-menu.uss", "map.uss", "encounter.uss",
                "shop.uss", "result.uss"
            };

            foreach (var file in screenUSS)
            {
                var path = "Assets/UI/Screens/" + file;
                var asset = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (asset != null)
                {
                    Debug.Log($"[PASS] Screen USS loaded: {path}");
                    passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] Screen USS not found: {path}");
                    failed++;
                }
            }

            // Check font assets referenced in variables.uss exist
            string[] fontPaths = {
                "Assets/Fonts/VT323-Regular SDF.asset",
                "Assets/Fonts/IBMPlexMono-Regular SDF.asset",
                "Assets/Fonts/IBMPlexMono-Bold SDF.asset"
            };

            foreach (var path in fontPaths)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    Debug.Log($"[PASS] Font asset exists: {path}");
                    passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] Font asset missing: {path}");
                    failed++;
                }
            }

            // Try to instantiate the test UXML with theme
            if (uxml != null && tss != null)
            {
                var root = new VisualElement();
                uxml.CloneTree(root);

                int elementCount = root.Query<VisualElement>().ToList().Count;
                if (elementCount > 0)
                {
                    Debug.Log($"[PASS] Test UXML instantiated with {elementCount} elements");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] Test UXML produced no elements");
                    failed++;
                }
            }

            Debug.Log($"\n=== Design System Verification: {passed} passed, {failed} failed ===");

            if (failed == 0)
                Debug.Log("All design system checks passed!");
            else
                Debug.LogWarning($"{failed} check(s) failed. Review errors above.");
        }

        [MenuItem("Spellwright/Tests/Verify MainMenu UI Toolkit")]
        public static void VerifyMainMenuUIToolkit()
        {
            int passed = 0;
            int failed = 0;

            // Check MainMenu.uxml exists and loads
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/MainMenu.uxml");
            if (uxml != null)
            {
                Debug.Log("[PASS] MainMenu.uxml loaded");
                passed++;
            }
            else
            {
                Debug.LogError("[FAIL] MainMenu.uxml not found at Assets/UI/Screens/MainMenu.uxml");
                failed++;
            }

            // Check main-menu.uss loads
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI/Screens/main-menu.uss");
            if (uss != null)
            {
                Debug.Log("[PASS] main-menu.uss loaded");
                passed++;
            }
            else
            {
                Debug.LogError("[FAIL] main-menu.uss not found");
                failed++;
            }

            // Instantiate UXML and verify expected elements exist
            if (uxml != null)
            {
                var root = new VisualElement();
                uxml.CloneTree(root);

                string[] expectedNames = { "title", "subtitle", "cursor", "start-button", "version", "hint" };
                foreach (var name in expectedNames)
                {
                    var el = root.Q(name);
                    if (el != null)
                    {
                        Debug.Log($"[PASS] Element '{name}' found in MainMenu.uxml");
                        passed++;
                    }
                    else
                    {
                        Debug.LogError($"[FAIL] Element '{name}' NOT found in MainMenu.uxml");
                        failed++;
                    }
                }

                // Verify start-button is a Button
                var btn = root.Q<Button>("start-button");
                if (btn != null)
                {
                    Debug.Log("[PASS] start-button is a Button element");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] start-button is not a Button element");
                    failed++;
                }

                // Verify CSS classes are applied
                var menuRoot = root.Q(className: "main-menu");
                if (menuRoot != null)
                {
                    Debug.Log("[PASS] .main-menu class found on root element");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] .main-menu class not found");
                    failed++;
                }
            }

            // Check PanelSettings asset
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/SpellwrightPanelSettings.asset");
            if (ps != null)
            {
                Debug.Log("[PASS] SpellwrightPanelSettings.asset found");
                passed++;

                if (ps.themeStyleSheet != null)
                {
                    Debug.Log("[PASS] PanelSettings has theme assigned");
                    passed++;
                }
                else
                {
                    Debug.LogWarning("[WARN] PanelSettings has no theme — run Setup Game Scene first");
                }
            }
            else
            {
                Debug.LogWarning("[WARN] SpellwrightPanelSettings.asset not found — will be created on Setup Game Scene");
            }

            Debug.Log($"\n=== MainMenu UI Toolkit Verification: {passed} passed, {failed} failed ===");
            if (failed == 0)
                Debug.Log("All MainMenu UI Toolkit checks passed!");
            else
                Debug.LogWarning($"{failed} check(s) failed. Review errors above.");
        }

        [MenuItem("Spellwright/Tests/Verify Map UI Toolkit")]
        public static void VerifyMapUIToolkit()
        {
            int passed = 0;
            int failed = 0;

            // Check Map.uxml exists and loads
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/Map.uxml");
            if (uxml != null)
            {
                Debug.Log("[PASS] Map.uxml loaded");
                passed++;
            }
            else
            {
                Debug.LogError("[FAIL] Map.uxml not found at Assets/UI/Screens/Map.uxml");
                failed++;
            }

            // Check map.uss loads
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI/Screens/map.uss");
            if (uss != null)
            {
                Debug.Log("[PASS] map.uss loaded");
                passed++;
            }
            else
            {
                Debug.LogError("[FAIL] map.uss not found");
                failed++;
            }

            // Instantiate UXML and verify expected elements exist
            if (uxml != null)
            {
                var root = new VisualElement();
                uxml.CloneTree(root);

                string[] expectedNames = { "title", "wave", "hp", "gold", "score", "node-container", "proceed", "lang-toggle" };
                foreach (var name in expectedNames)
                {
                    var el = root.Q(name);
                    if (el != null)
                    {
                        Debug.Log($"[PASS] Element '{name}' found in Map.uxml");
                        passed++;
                    }
                    else
                    {
                        Debug.LogError($"[FAIL] Element '{name}' NOT found in Map.uxml");
                        failed++;
                    }
                }

                // Verify proceed is a Button
                var btn = root.Q<Button>("proceed");
                if (btn != null)
                {
                    Debug.Log("[PASS] proceed is a Button element");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] proceed is not a Button element");
                    failed++;
                }

                // Verify lang-toggle is a Button
                var langBtn = root.Q<Button>("lang-toggle");
                if (langBtn != null)
                {
                    Debug.Log("[PASS] lang-toggle is a Button element");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] lang-toggle is not a Button element");
                    failed++;
                }

                // Verify node-container is a ScrollView
                var nodeContainer = root.Q<ScrollView>("node-container");
                if (nodeContainer != null)
                {
                    Debug.Log("[PASS] node-container is a ScrollView element");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] node-container is not a ScrollView element");
                    failed++;
                }

                // Verify CSS classes are applied
                var mapRoot = root.Q(className: "map-screen");
                if (mapRoot != null)
                {
                    Debug.Log("[PASS] .map-screen class found on root element");
                    passed++;
                }
                else
                {
                    Debug.LogError("[FAIL] .map-screen class not found");
                    failed++;
                }
            }

            Debug.Log($"\n=== Map UI Toolkit Verification: {passed} passed, {failed} failed ===");
            if (failed == 0)
                Debug.Log("All Map UI Toolkit checks passed!");
            else
                Debug.LogWarning($"{failed} check(s) failed. Review errors above.");
        }

        [MenuItem("Spellwright/Tests/Preview Theme Test")]
        public static void PreviewThemeTest()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TestUXMLPath);
            if (uxml == null)
            {
                Debug.LogError($"Test UXML not found at {TestUXMLPath}");
                return;
            }

            // Open the UXML in UI Builder for visual preview
            EditorGUIUtility.PingObject(uxml);
            AssetDatabase.OpenAsset(uxml);
            Debug.Log("Opened theme-test.uxml in UI Builder. Assign terminal-theme.tss to PanelSettings to see themed preview.");
        }
    }
}
