using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.UI
{
    /// <summary>
    /// After tile reveal, applies a breathing glow: outline alpha oscillates
    /// 0.3 to 1.0 over a randomized 2-3.5s cycle. Creates organic "living board" effect.
    /// </summary>
    public static class TileGlowEffect
    {
        /// <summary>
        /// Apply a breathing glow to a revealed tile's Outline component.
        /// </summary>
        public static void ApplyBreathingGlow(GameObject tileGO)
        {
            if (tileGO == null) return;

            var outline = tileGO.GetComponent<Outline>();
            if (outline == null) return;

            float cycleDuration = Random.Range(2f, 3.5f);
            Color baseColor = outline.effectColor;
            Color dimColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.3f);
            Color brightColor = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);

            // Start from dim and pulse to bright
            outline.effectColor = dimColor;

            DOTween.To(
                () => outline.effectColor,
                c => { if (outline != null) outline.effectColor = c; },
                brightColor,
                cycleDuration / 2f
            )
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetTarget(tileGO); // Use tileGO as target so DOTween.Kill(tileGO) cleans up
        }
    }
}
