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
