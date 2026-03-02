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

        [Header("Font Sizes")]
        public int titleSize = 48;
        public int headerSize = 28;
        public int bodySize = 18;
        public int labelSize = 14;
        public int smallSize = 13;
        public int blanksSize = 36;

        [Header("Terminal Palette")]
        public Color phosphorGreen = new Color(0f, 1f, 0.33f);
        public Color phosphorBright = new Color(0.2f, 1f, 0.5f);
        public Color phosphorDim = new Color(0f, 0.5f, 0.18f);
        public Color terminalBg = new Color(0.02f, 0.05f, 0.02f, 1f);
        public Color panelBg = new Color(0.03f, 0.08f, 0.03f, 0.95f);
        public Color borderColor = new Color(0f, 0.6f, 0.2f, 0.8f);

        [Header("Accent Colors")]
        public Color amberBright = new Color(1f, 0.75f, 0f);
        public Color cyanInfo = new Color(0f, 0.85f, 0.85f);
        public Color magentaMagic = new Color(0.85f, 0.2f, 0.85f);

        [Header("State Colors")]
        public Color successColor = new Color(0.1f, 0.9f, 0.3f);
        public Color damageColor = new Color(1f, 0.15f, 0.1f);
        public Color warningColor = new Color(1f, 0.6f, 0f);
        public Color inactiveColor = new Color(0.3f, 0.5f, 0.3f);
        public Color bossAccent = new Color(0.85f, 0.1f, 0.1f);

        [Header("Component Colors")]
        public Color buttonBg = new Color(0.05f, 0.2f, 0.05f, 0.9f);
        public Color buttonBgDanger = new Color(0.3f, 0.05f, 0.05f, 0.9f);
        public Color buttonText = new Color(0f, 1f, 0.33f);
        public Color inputFieldBg = new Color(0.02f, 0.08f, 0.02f, 0.9f);
        public Color inputFieldText = new Color(0f, 1f, 0.33f);
        public Color inputPlaceholder = new Color(0f, 0.35f, 0.12f, 0.5f);

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
        public Color nodeCompleted = new Color(0f, 0.5f, 0.18f);
        public Color nodeCurrent = new Color(0f, 1f, 0.33f);
        public Color nodeFuture = new Color(0.2f, 0.35f, 0.2f);
        public Color nodeBoss = new Color(0.85f, 0.1f, 0.1f);

        [Header("Shop Rarity Colors")]
        public Color rarityCommon = new Color(0f, 0.6f, 0.2f);
        public Color rarityUncommon = new Color(0f, 0.85f, 0.85f);
        public Color rarityRare = new Color(0.4f, 0.5f, 1f);
        public Color rarityLegendary = new Color(1f, 0.75f, 0f);

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
