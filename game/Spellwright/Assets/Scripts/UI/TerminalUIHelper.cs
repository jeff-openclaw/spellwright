using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.UI
{
    public static class TerminalUIHelper
    {
        public static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text,
            TerminalThemeSO theme, int fontSize, Color color, TextAlignmentOptions alignment,
            Vector2 anchorMin, Vector2 anchorMax)
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
            if (theme != null && theme.primaryFont != null)
                tmp.font = theme.primaryFont;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.raycastTarget = false;

            return tmp;
        }

        public static TMP_InputField CreateTMPInputField(Transform parent, string name,
            TerminalThemeSO theme, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = theme != null ? theme.inputFieldBg : new Color(0.02f, 0.08f, 0.02f, 0.9f);

            // Add border
            var outline = go.AddComponent<Outline>();
            outline.effectColor = theme != null ? theme.borderColor : new Color(0f, 0.6f, 0.2f, 0.8f);
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

            // Placeholder
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(textAreaGO.transform, false);
            var phRT = placeholderGO.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = Vector2.zero;
            phRT.offsetMax = Vector2.zero;
            var phTMP = placeholderGO.AddComponent<TextMeshProUGUI>();
            phTMP.text = "Type your guess...";
            if (theme != null && theme.primaryFont != null)
                phTMP.font = theme.primaryFont;
            phTMP.fontSize = theme != null ? theme.bodySize : 18;
            phTMP.color = theme != null ? theme.inputPlaceholder : new Color(0f, 0.35f, 0.12f, 0.5f);
            phTMP.fontStyle = FontStyles.Italic;
            phTMP.alignment = TextAlignmentOptions.MidlineLeft;
            phTMP.raycastTarget = false;

            // Input text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(textAreaGO.transform, false);
            var tRT = textGO.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.offsetMin = Vector2.zero;
            tRT.offsetMax = Vector2.zero;
            var inputTMP = textGO.AddComponent<TextMeshProUGUI>();
            inputTMP.text = "";
            if (theme != null && theme.primaryFont != null)
                inputTMP.font = theme.primaryFont;
            inputTMP.fontSize = theme != null ? theme.bodySize : 18;
            inputTMP.color = theme != null ? theme.inputFieldText : new Color(0f, 1f, 0.33f);
            inputTMP.alignment = TextAlignmentOptions.MidlineLeft;
            inputTMP.richText = false;

            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = textAreaRT;
            input.textComponent = inputTMP;
            input.placeholder = phTMP;
            input.characterLimit = 25;
            input.fontAsset = theme != null ? theme.primaryFont : null;

            return input;
        }

        public static Button CreateTerminalButton(Transform parent, string name, string label,
            TerminalThemeSO theme, Color bgColor, Vector2 anchorMin, Vector2 anchorMax)
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

            // Border
            var outline = go.AddComponent<Outline>();
            outline.effectColor = theme != null ? theme.borderColor : new Color(0f, 0.6f, 0.2f, 0.8f);
            outline.effectDistance = new Vector2(1, -1);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // TMP label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            if (theme != null && theme.primaryFont != null)
                tmp.font = theme.primaryFont;
            tmp.fontSize = theme != null ? theme.bodySize : 18;
            tmp.color = theme != null ? theme.buttonText : new Color(0f, 1f, 0.33f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return btn;
        }

        public static void ApplyPhosphorGlow(TextMeshProUGUI tmp, Color glowColor)
        {
            if (tmp == null || tmp.fontMaterial == null) return;

            // Use material property block to avoid modifying shared material
            var mat = new Material(tmp.fontMaterial);
            mat.EnableKeyword("GLOW_ON");
            mat.SetFloat(ShaderUtilities.ID_GlowOffset, 0.5f);
            mat.SetFloat(ShaderUtilities.ID_GlowPower, 0.6f);
            mat.SetColor(ShaderUtilities.ID_GlowColor, glowColor);
            tmp.fontMaterial = mat;
        }

        public static Outline CreateBorderedPanel(GameObject panel, Color borderColor)
        {
            var outline = panel.GetComponent<Outline>();
            if (outline == null)
                outline = panel.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2, -2);
            return outline;
        }
    }
}
