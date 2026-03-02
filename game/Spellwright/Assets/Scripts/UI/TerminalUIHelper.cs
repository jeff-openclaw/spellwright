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

            // Border — use dedicated button border color for more presence
            var outline = go.AddComponent<Outline>();
            outline.effectColor = theme != null ? theme.buttonBorder : new Color(0.12f, 0.7f, 0.35f, 0.9f);
            outline.effectDistance = new Vector2(2, -2);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Disable built-in color tinting (we handle hover ourselves)
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colors;

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
            tmp.color = theme != null ? theme.buttonText : new Color(0.12f, 1f, 0.45f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
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

        /// <summary>
        /// Creates a TextMeshProUGUI using the decorative (VT323) font with optional glow.
        /// </summary>
        public static TextMeshProUGUI CreateDecorativeText(Transform parent, string name, string text,
            TerminalThemeSO theme, int fontSize, Color color, TextAlignmentOptions alignment,
            Vector2 anchorMin, Vector2 anchorMax, bool applyGlow = false)
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
            if (theme != null && theme.decorativeFont != null)
                tmp.font = theme.decorativeFont;
            else if (theme != null && theme.primaryFont != null)
                tmp.font = theme.primaryFont;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.raycastTarget = false;

            if (applyGlow && theme != null)
                ApplyGlow(tmp, color, theme.titleGlowOffset, theme.titleGlowPower);

            return tmp;
        }

        /// <summary>
        /// Creates a section header with ASCII decorators: ══ TITLE ══
        /// </summary>
        public static TextMeshProUGUI CreateSectionHeader(Transform parent, string name, string title,
            TerminalThemeSO theme, Color color, Vector2 anchorMin, Vector2 anchorMax, bool applyGlow = false)
        {
            string decorated = $"\u2550\u2550 {title} \u2550\u2550";
            int fontSize = theme != null ? theme.decorativeHeaderSize : 36;
            return CreateDecorativeText(parent, name, decorated, theme, fontSize, color,
                TextAlignmentOptions.Center, anchorMin, anchorMax, applyGlow);
        }

        /// <summary>
        /// Creates a separator line: ─────────────
        /// </summary>
        public static TextMeshProUGUI CreateSeparator(Transform parent, string name,
            TerminalThemeSO theme, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            string line = new string('\u2500', 40);
            int fontSize = theme != null ? theme.smallSize : 13;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = line;
            if (theme != null && theme.primaryFont != null)
                tmp.font = theme.primaryFont;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.raycastTarget = false;

            return tmp;
        }

        /// <summary>
        /// Creates a card container with a left-side rarity color stripe,
        /// a content area, and a right area (for price/button).
        /// Returns (cardRoot, contentArea, rightArea).
        /// </summary>
        public static (GameObject card, GameObject content, GameObject right) CreateItemCard(
            Transform parent, string name, TerminalThemeSO theme, Color rarityColor, float height)
        {
            var card = new GameObject(name);
            card.transform.SetParent(parent, false);

            var cardRT = card.AddComponent<RectTransform>();
            cardRT.sizeDelta = new Vector2(0, height);

            var cardBg = card.AddComponent<Image>();
            cardBg.color = theme != null ? theme.cardBg : new Color(0.04f, 0.10f, 0.04f, 0.9f);

            var cardLayout = card.AddComponent<HorizontalLayoutGroup>();
            cardLayout.spacing = 0;
            cardLayout.padding = new RectOffset(0, 0, 0, 0);
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = true;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;

            // Card border for visual separation
            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = theme != null
                ? new Color(theme.borderColor.r, theme.borderColor.g, theme.borderColor.b, 0.4f)
                : new Color(0.05f, 0.55f, 0.25f, 0.4f);
            cardOutline.effectDistance = new Vector2(1, -1);

            // Left color stripe (rarity indicator)
            var stripe = new GameObject("Stripe");
            stripe.transform.SetParent(card.transform, false);
            stripe.AddComponent<RectTransform>();
            var stripeImg = stripe.AddComponent<Image>();
            stripeImg.color = rarityColor;
            var stripeLe = stripe.AddComponent<LayoutElement>();
            stripeLe.minWidth = 4;
            stripeLe.preferredWidth = 4;
            stripeLe.flexibleWidth = 0;

            // Content area (left side, flexible)
            var content = new GameObject("Content");
            content.transform.SetParent(card.transform, false);
            content.AddComponent<RectTransform>();
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 2;
            contentLayout.padding = new RectOffset(10, 4, 4, 4);
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            var contentLe = content.AddComponent<LayoutElement>();
            contentLe.flexibleWidth = 1;

            // Right area (for price + button)
            var right = new GameObject("Right");
            right.transform.SetParent(card.transform, false);
            right.AddComponent<RectTransform>();
            var rightLayout = right.AddComponent<VerticalLayoutGroup>();
            rightLayout.spacing = 2;
            rightLayout.padding = new RectOffset(4, 8, 4, 4);
            rightLayout.childForceExpandWidth = true;
            rightLayout.childForceExpandHeight = false;
            rightLayout.childControlWidth = true;
            rightLayout.childControlHeight = true;
            rightLayout.childAlignment = TextAnchor.MiddleCenter;
            var rightLe = right.AddComponent<LayoutElement>();
            rightLe.minWidth = 80;
            rightLe.preferredWidth = 80;
            rightLe.flexibleWidth = 0;

            return (card, content, right);
        }

        /// <summary>
        /// Applies TMP material glow effect.
        /// </summary>
        public static void ApplyGlow(TextMeshProUGUI tmp, Color color, float offset, float power)
        {
            if (tmp == null || tmp.fontMaterial == null) return;

            var mat = new Material(tmp.fontMaterial);
            mat.EnableKeyword("GLOW_ON");
            mat.SetFloat(ShaderUtilities.ID_GlowOffset, offset);
            mat.SetFloat(ShaderUtilities.ID_GlowPower, power);
            mat.SetColor(ShaderUtilities.ID_GlowColor, color);
            tmp.fontMaterial = mat;
        }

        /// <summary>
        /// Applies a vertical color gradient (top to bottom) on TMP text.
        /// </summary>
        public static void ApplyVerticalGradient(TextMeshProUGUI tmp, Color topColor, Color bottomColor)
        {
            if (tmp == null) return;
            tmp.enableVertexGradient = true;
            tmp.colorGradient = new VertexGradient(topColor, topColor, bottomColor, bottomColor);
        }
    }
}
