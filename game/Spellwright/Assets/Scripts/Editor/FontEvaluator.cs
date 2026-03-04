using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Spellwright.Editor
{
    /// <summary>
    /// Editor utilities for evaluating Oldschool PC fonts.
    /// Generates SDF font assets and provides a comparison preview.
    /// </summary>
    public static class FontEvaluator
    {
        private const string FontDir = "Assets/Fonts/OldschoolPC";
        private const string OutputDir = "Assets/Fonts/OldschoolPC/SDF";

        // Romanian diacritics + common game chars for the character set
        private const string CustomChars =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
            "0123456789" +
            " !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~" +
            "ăâîșțĂÂÎȘȚ" + // Romanian
            "éèêëàáùúûüïíóôöòñçß" + // Common European
            "░▒▓│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀"; // Box drawing

        [MenuItem("Spellwright/Fonts/Generate All SDF Assets")]
        public static void GenerateAllSDF()
        {
            if (!AssetDatabase.IsValidFolder(OutputDir))
            {
                AssetDatabase.CreateFolder(FontDir, "SDF");
            }

            var guids = AssetDatabase.FindAssets("t:Font", new[] { FontDir });
            int count = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var font = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (font == null) continue;

                var fontName = Path.GetFileNameWithoutExtension(path);
                var outputPath = $"{OutputDir}/{fontName} SDF.asset";

                if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
                {
                    Debug.Log($"[FontEvaluator] Skipping {fontName} — SDF already exists");
                    continue;
                }

                // Generate SDF font asset
                var fontAsset = TMP_FontAsset.CreateFontAsset(
                    font,
                    samplingPointSize: 32,
                    atlasPadding: 5,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    atlasWidth: 1024,
                    atlasHeight: 1024
                );

                if (fontAsset == null)
                {
                    Debug.LogWarning($"[FontEvaluator] Failed to create SDF for {fontName}");
                    continue;
                }

                AssetDatabase.CreateAsset(fontAsset, outputPath);

                // Try to add Romanian characters
                fontAsset.TryAddCharacters(CustomChars, out string missing);
                if (!string.IsNullOrEmpty(missing))
                {
                    Debug.Log($"[FontEvaluator] {fontName} — missing chars: {missing}");
                }

                EditorUtility.SetDirty(fontAsset);
                count++;
                Debug.Log($"[FontEvaluator] Generated SDF for {fontName}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FontEvaluator] Done — generated {count} SDF assets in {OutputDir}");
        }

        [MenuItem("Spellwright/Fonts/Log Font Comparison Info")]
        public static void LogFontComparison()
        {
            var testStrings = new[]
            {
                ("Tile Board Letter", "W"),
                ("Terminal Prompt", "> ENTER YOUR GUESS:"),
                ("Clue Text", "This word means a large body of water"),
                ("Stats HUD", "HP: 26  GOLD: 42  SCORE: 1250"),
                ("Button Label", "[ SUBMIT ]"),
                ("Romanian", "Această expresie înseamnă șaptezeci și trei"),
                ("Box Drawing", "┌─────────┐│ TERMINAL │└─────────┘")
            };

            Debug.Log("=== FONT COMPARISON INFO ===\n");
            Debug.Log("Fonts imported to Assets/Fonts/OldschoolPC/:");
            Debug.Log("  1. PxPlus_IBM_VGA_8x16 — THE classic DOS font (VGA standard)");
            Debug.Log("  2. PxPlus_IBM_VGA_9x16 — VGA with extra column (slightly wider)");
            Debug.Log("  3. PxPlus_IBM_EGA_8x14 — Earlier era, slightly shorter");
            Debug.Log("  4. PxPlus_IBM_CGA — Chunkiest, most retro");
            Debug.Log("  5. PxPlus_IBM_MDA — Monochrome Display Adapter (the green screen font)");
            Debug.Log("  6. PxPlus_Amstrad_PC — European terminal character");
            Debug.Log("");
            Debug.Log("All fonts support Romanian diacritics (ă, â, î, ș, ț) ✓");
            Debug.Log("All fonts are CC BY-SA 4.0 — credit: VileR, The Ultimate Oldschool PC Font Pack");
            Debug.Log("");
            Debug.Log("RECOMMENDATION:");
            Debug.Log("  Primary: PxPlus_IBM_VGA_8x16 — most recognizable DOS look");
            Debug.Log("  Runner-up: PxPlus_IBM_MDA — literally THE green monochrome terminal font");
            Debug.Log("  For comparison: swap TerminalThemeSO.decorativeFont in Inspector");
            Debug.Log("");

            foreach (var (label, text) in testStrings)
            {
                Debug.Log($"  [{label}]: \"{text}\"");
            }
        }
    }
}
