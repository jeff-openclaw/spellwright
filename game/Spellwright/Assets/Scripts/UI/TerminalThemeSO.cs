using Spellwright.Data;
using TMPro;
using UnityEngine;

namespace Spellwright.UI
{
    [CreateAssetMenu(fileName = "TerminalTheme", menuName = "Spellwright/Terminal Theme")]
    public class TerminalThemeSO : ScriptableObject
    {
        [Header("Fonts")]
        public TMP_FontAsset primaryFont;
        public TMP_FontAsset secondaryFont;
        public TMP_FontAsset secondaryBoldFont;

        [Header("Decorative Font")]
        public TMP_FontAsset decorativeFont;

        [Header("Font Sizes")]
        public int titleSize = 56;
        public int headerSize = 30;
        public int bodySize = 26;
        public int labelSize = 22;
        public int smallSize = 18;
        public int blanksSize = 36;

        [Header("Decorative Font Sizes")]
        public int decorativeTitleSize = 72;
        public int decorativeHeaderSize = 42;
        public int decorativeSubheaderSize = 28;

        [Header("Glow Settings")]
        public float titleGlowOffset = 0.6f;
        public float titleGlowPower = 1.0f;
        public float subtleGlowOffset = 0.4f;
        public float subtleGlowPower = 0.5f;

        [Header("Card Colors")]
        public Color cardBg = new Color(0.03f, 0.08f, 0.04f, 0.92f);
        public Color cardBgHover = new Color(0.05f, 0.14f, 0.07f, 0.98f);
        public Color cardBgSold = new Color(0.02f, 0.03f, 0.02f, 0.4f);

        [Header("Animation")]
        public float panelFadeInDuration = 0.35f;
        public float panelSlideDistance = 50f;
        public float buttonHoverScale = 1.06f;
        public float buttonHoverDuration = 0.12f;
        public float staggerDelay = 0.08f;

        [Header("Terminal Palette")]
        public Color phosphorGreen = new Color(0.12f, 1f, 0.45f);
        public Color phosphorBright = new Color(0.3f, 1f, 0.6f);
        public Color phosphorDim = new Color(0.05f, 0.45f, 0.2f);
        public Color terminalBg = new Color(0.01f, 0.02f, 0.02f, 1f);
        public Color panelBg = new Color(0.02f, 0.06f, 0.04f, 0.95f);
        public Color borderColor = new Color(0.05f, 0.55f, 0.25f, 0.7f);
        public Color panelBgLight = new Color(0.04f, 0.10f, 0.06f, 0.85f);

        [Header("Accent Colors")]
        public Color amberBright = new Color(1f, 0.78f, 0.15f);
        public Color amberDim = new Color(0.6f, 0.45f, 0.1f);
        public Color cyanInfo = new Color(0.15f, 0.9f, 0.95f);
        public Color cyanDim = new Color(0.05f, 0.4f, 0.45f);
        public Color magentaMagic = new Color(0.9f, 0.25f, 0.9f);

        [Header("State Colors")]
        public Color successColor = new Color(0.1f, 0.9f, 0.3f);
        public Color damageColor = new Color(1f, 0.15f, 0.1f);
        public Color warningColor = new Color(1f, 0.6f, 0f);
        public Color inactiveColor = new Color(0.3f, 0.5f, 0.3f);
        public Color bossAccent = new Color(0.85f, 0.1f, 0.1f);

        [Header("Component Colors")]
        public Color buttonBg = new Color(0.03f, 0.18f, 0.08f, 0.95f);
        public Color buttonBgHover = new Color(0.06f, 0.28f, 0.12f, 1f);
        public Color buttonBgDanger = new Color(0.35f, 0.06f, 0.06f, 0.95f);
        public Color buttonText = new Color(0.12f, 1f, 0.45f);
        public Color buttonBorder = new Color(0.12f, 0.7f, 0.35f, 0.9f);
        public Color inputFieldBg = new Color(0.01f, 0.05f, 0.03f, 0.95f);
        public Color inputFieldText = new Color(0.12f, 1f, 0.45f);
        public Color inputPlaceholder = new Color(0.05f, 0.3f, 0.15f, 0.5f);

        [Header("HP Bar")]
        public Color hpBarBg = new Color(0.1f, 0.15f, 0.1f, 1f);
        public Color hpBarFill = new Color(0f, 0.8f, 0.25f, 1f);
        public Color hpBarLow = new Color(1f, 0.6f, 0f, 1f);
        public Color hpBarCritical = new Color(1f, 0.15f, 0.1f, 1f);

        [Header("Flash Overlays")]
        public Color damageFlash = new Color(0.8f, 0.1f, 0.1f, 0.4f);
        public Color successFlash = new Color(0.1f, 0.8f, 0.2f, 0.4f);
        public Color bossIntroFlash = new Color(0.5f, 0f, 0f, 0.3f);

        [Header("Map Node Colors")]
        public Color nodeCompleted = new Color(0.05f, 0.5f, 0.22f);
        public Color nodeCurrent = new Color(0.12f, 1f, 0.45f);
        public Color nodeFuture = new Color(0.2f, 0.45f, 0.3f);
        public Color nodeBoss = new Color(0.9f, 0.12f, 0.12f);

        [Header("Shop Rarity Colors")]
        public Color rarityCommon = new Color(0.1f, 0.65f, 0.3f);
        public Color rarityUncommon = new Color(0.15f, 0.9f, 0.95f);
        public Color rarityRare = new Color(0.45f, 0.55f, 1f);
        public Color rarityLegendary = new Color(1f, 0.78f, 0.15f);

        [Header("Tile Glow")]
        public float tileGlowMinAlpha = 0.3f;
        public float tileGlowMaxAlpha = 1.0f;

        [Header("Data Stream")]
        public float dataStreamMinAlpha = 0.12f;
        public float dataStreamMaxAlpha = 0.35f;
        public int dataStreamColumns = 16;

        [Header("Screen Effects")]
        public float scanlineAlpha = 0.04f;
        public float vignetteStrength = 0.45f;

        [Header("Suspense Timing")]
        public float cluePulseSpeed = 2f;
        public float heartbeatThreshold = 0.25f;
        public float heartbeatScale = 1.06f;
        public float dramaDuration = 1.2f;
        public float guessCountdownThreshold = 2;

        public Color GetRarityColor(TomeRarity rarity)
        {
            return rarity switch
            {
                TomeRarity.Common => rarityCommon,
                TomeRarity.Uncommon => rarityUncommon,
                TomeRarity.Rare => rarityRare,
                TomeRarity.Legendary => rarityLegendary,
                _ => phosphorGreen
            };
        }

        public Color GetHPBarColor(float fillPercent)
        {
            if (fillPercent <= 0.2f) return hpBarCritical;
            if (fillPercent <= 0.4f) return hpBarLow;
            return hpBarFill;
        }
    }
}
