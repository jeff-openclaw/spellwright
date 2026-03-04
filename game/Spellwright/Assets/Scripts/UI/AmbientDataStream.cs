using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellwright.UI
{
    /// <summary>
    /// Matrix-style falling character columns with color variety.
    /// Uses UI Toolkit Labels instead of TextMeshProUGUI.
    /// Creates a living, atmospheric background behind game panels.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class AmbientDataStream : MonoBehaviour
    {
        [SerializeField] private TerminalThemeSO theme;

        [Header("Settings")]
        [SerializeField] private int columnCount = 12;
        [SerializeField] private int charsPerColumn = 25;
        [SerializeField] private float scrollSpeed = 0.18f;
        [SerializeField] private float minAlpha = 0.08f;
        [SerializeField] private float maxAlpha = 0.28f;

        [Header("Color Variety")]
        [SerializeField] [Range(0f, 0.3f)] private float accentChance = 0.08f;

        private Label[] _columns;
        private float[] _scrollOffsets;
        private float[] _columnSpeeds;
        private Color[] _columnBaseColors;

        private static readonly string GlyphSet =
            "0123456789ABCDEF\u2588\u2591\u2592\u2593\u2502\u2500\u253c\u251c\u2524\u2534\u252c\u256c\u2561\u2562\u25cf\u25cb\u25a0\u25a1";

        private static readonly string AccentGlyphs = "#$@&*~^!?";

        private void OnEnable()
        {
            if (theme != null)
            {
                columnCount = theme.dataStreamColumns;
                minAlpha = theme.dataStreamMinAlpha;
                maxAlpha = theme.dataStreamMaxAlpha;
            }
            CreateColumns();
        }

        private void Update()
        {
            if (_columns == null) return;

            for (int i = 0; i < _columns.Length; i++)
            {
                _scrollOffsets[i] += _columnSpeeds[i] * Time.deltaTime;

                if (_scrollOffsets[i] > 1f)
                {
                    _scrollOffsets[i] = 0f;
                    if (_columns[i] != null)
                    {
                        _columns[i].text = GenerateColumn();

                        float alpha = Random.Range(minAlpha, maxAlpha);
                        var baseColor = _columnBaseColors[i];
                        _columns[i].style.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                    }
                }
            }
        }

        private void CreateColumns()
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
            root.style.flexDirection = FlexDirection.Row;
            root.style.overflow = Overflow.Hidden;

            _columns = new Label[columnCount];
            _scrollOffsets = new float[columnCount];
            _columnSpeeds = new float[columnCount];
            _columnBaseColors = new Color[columnCount];

            Color greenBase = theme != null ? theme.phosphorGreen : new Color(0.12f, 1f, 0.45f);
            Color amberBase = theme != null ? theme.amberBright : new Color(1f, 0.78f, 0.15f);
            Color cyanBase = theme != null ? theme.cyanInfo : new Color(0.15f, 0.9f, 0.95f);
            int fontSize = theme != null ? theme.smallSize : 13;

            for (int i = 0; i < columnCount; i++)
            {
                var label = new Label(GenerateColumn());
                label.name = $"DataStream_{i}";
                label.pickingMode = PickingMode.Ignore;
                label.style.flexGrow = 1;
                label.style.fontSize = fontSize;
                label.style.whiteSpace = WhiteSpace.PreWrap;
                label.style.overflow = Overflow.Hidden;
                label.style.unityTextAlign = TextAnchor.UpperCenter;

                // Most columns green, occasional amber or cyan
                float roll = Random.value;
                Color baseColor;
                if (roll < 0.08f)
                    baseColor = amberBase;
                else if (roll < 0.14f)
                    baseColor = cyanBase;
                else
                    baseColor = greenBase;

                _columnBaseColors[i] = baseColor;

                float alpha = Random.Range(minAlpha, maxAlpha);
                // Edge columns dimmer for vignette feel
                float normalizedX = columnCount > 1 ? (float)i / (columnCount - 1) : 0.5f;
                float distFromCenter = Mathf.Abs(normalizedX - 0.5f) * 2f;
                float edgeFade = Mathf.Lerp(1f, 0.4f, distFromCenter * distFromCenter);

                label.style.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha * edgeFade);

                root.Add(label);
                _columns[i] = label;
                _scrollOffsets[i] = Random.value;
                _columnSpeeds[i] = scrollSpeed * Random.Range(0.4f, 1.6f);
            }
        }

        private string GenerateColumn()
        {
            var sb = new StringBuilder();
            for (int j = 0; j < charsPerColumn; j++)
            {
                if (Random.value < accentChance)
                    sb.Append(AccentGlyphs[Random.Range(0, AccentGlyphs.Length)]);
                else
                    sb.Append(GlyphSet[Random.Range(0, GlyphSet.Length)]);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        private void OnDisable()
        {
            var doc = GetComponent<UIDocument>();
            if (doc != null && doc.rootVisualElement != null)
                doc.rootVisualElement.Clear();
            _columns = null;
        }
    }
}
