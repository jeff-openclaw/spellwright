using UnityEngine;
using UnityEngine.UIElements;

namespace Spellwright.UI
{
    /// <summary>
    /// Procedural scanline + vignette overlay for atmospheric CRT depth.
    /// Uses UI Toolkit VisualElements with runtime-generated textures.
    /// Scanlines: 1x2 tiled texture creating subtle horizontal lines.
    /// Vignette: radial gradient darkening screen edges.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ScreenEffectsOverlay : MonoBehaviour
    {
        [SerializeField] private TerminalThemeSO theme;

        private Texture2D _scanlineTex;
        private Texture2D _vignetteTex;

        private void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null || doc.rootVisualElement == null) return;

            var root = doc.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;

            float scanlineAlpha = theme != null ? theme.scanlineAlpha : 0.04f;
            float vignetteStrength = theme != null ? theme.vignetteStrength : 0.45f;

            CreateScanlines(root, scanlineAlpha);
            CreateVignette(root, vignetteStrength);
        }

        private void CreateScanlines(VisualElement root, float alpha)
        {
            _scanlineTex = new Texture2D(1, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            _scanlineTex.SetPixel(0, 0, Color.clear);
            _scanlineTex.SetPixel(0, 1, new Color(0f, 0f, 0f, alpha));
            _scanlineTex.Apply();

            var scanlines = new VisualElement();
            scanlines.name = "scanlines";
            scanlines.pickingMode = PickingMode.Ignore;
            scanlines.style.position = Position.Absolute;
            scanlines.style.left = 0;
            scanlines.style.top = 0;
            scanlines.style.right = 0;
            scanlines.style.bottom = 0;
            scanlines.style.backgroundImage = new StyleBackground(_scanlineTex);
            scanlines.style.unityBackgroundImageTintColor = Color.white;
            // Tiling is handled by background-repeat + background-size
            // Set background-size to 1px wide x 2px tall to tile naturally
            scanlines.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            scanlines.style.backgroundSize = new BackgroundSize(new Length(1, LengthUnit.Pixel), new Length(2, LengthUnit.Pixel));
            root.Add(scanlines);
        }

        private void CreateVignette(VisualElement root, float strength)
        {
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
                    float a = Mathf.SmoothStep(0f, strength, dist - 0.3f);
                    _vignetteTex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
                }
            }
            _vignetteTex.Apply();

            var vignette = new VisualElement();
            vignette.name = "vignette";
            vignette.pickingMode = PickingMode.Ignore;
            vignette.style.position = Position.Absolute;
            vignette.style.left = 0;
            vignette.style.top = 0;
            vignette.style.right = 0;
            vignette.style.bottom = 0;
            vignette.style.backgroundImage = new StyleBackground(_vignetteTex);
            vignette.style.unityBackgroundImageTintColor = Color.white;
            // Stretch vignette to fill the screen
            vignette.style.backgroundSize = new BackgroundSize(new Length(100, LengthUnit.Percent), new Length(100, LengthUnit.Percent));
            root.Add(vignette);
        }

        private void OnDestroy()
        {
            if (_scanlineTex != null) Destroy(_scanlineTex);
            if (_vignetteTex != null) Destroy(_vignetteTex);
        }
    }
}
