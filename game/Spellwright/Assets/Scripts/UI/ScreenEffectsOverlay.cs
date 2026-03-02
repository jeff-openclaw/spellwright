using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.UI
{
    /// <summary>
    /// Procedural scanline + vignette overlay for atmospheric CRT depth.
    /// Scanlines: 1x2 tiled texture creating subtle horizontal lines.
    /// Vignette: radial gradient darkening screen edges.
    /// Both are non-interactive fullscreen overlays.
    /// </summary>
    public class ScreenEffectsOverlay : MonoBehaviour
    {
        [SerializeField] private TerminalThemeSO theme;
        [SerializeField] private RawImage scanlineImage;
        [SerializeField] private RawImage vignetteImage;

        private Texture2D _scanlineTex;
        private Texture2D _vignetteTex;

        private void Start()
        {
            float scanlineAlpha = theme != null ? theme.scanlineAlpha : 0.04f;
            float vignetteStrength = theme != null ? theme.vignetteStrength : 0.45f;

            CreateScanlines(scanlineAlpha);
            CreateVignette(vignetteStrength);
        }

        private void CreateScanlines(float alpha)
        {
            if (scanlineImage == null) return;

            _scanlineTex = new Texture2D(1, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            _scanlineTex.SetPixel(0, 0, Color.clear);
            _scanlineTex.SetPixel(0, 1, new Color(0f, 0f, 0f, alpha));
            _scanlineTex.Apply();

            scanlineImage.texture = _scanlineTex;
            // Tile vertically: each scanline pair = 2 pixels, cover screen height
            float tileCount = Screen.height / 2f;
            scanlineImage.uvRect = new Rect(0, 0, 1, tileCount);
            scanlineImage.color = Color.white;
            scanlineImage.enabled = true;
        }

        private void CreateVignette(float strength)
        {
            if (vignetteImage == null) return;

            const int size = 128;
            _vignetteTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float halfSize = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - halfSize) / halfSize;
                    float dy = (y - halfSize) / halfSize;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    // Smooth falloff: transparent center, dark edges
                    float alpha = Mathf.SmoothStep(0f, strength, dist - 0.3f);
                    _vignetteTex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }
            _vignetteTex.Apply();

            vignetteImage.texture = _vignetteTex;
            vignetteImage.color = Color.white;
            vignetteImage.enabled = true;
        }

        private void OnDestroy()
        {
            if (_scanlineTex != null) Destroy(_scanlineTex);
            if (_vignetteTex != null) Destroy(_vignetteTex);
        }
    }
}
