using System.Text;
using TMPro;
using UnityEngine;

namespace Spellwright.UI
{
    /// <summary>
    /// Matrix-style falling character columns with color variety.
    /// More visible than before, with occasional amber/cyan accent characters
    /// mixed in with the primary green. Creates a living, atmospheric background.
    /// </summary>
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

        private TextMeshProUGUI[] _columns;
        private float[] _scrollOffsets;
        private float[] _columnSpeeds;
        private Color[] _columnBaseColors;

        private static readonly string GlyphSet =
            "0123456789ABCDEF\u2588\u2591\u2592\u2593\u2502\u2500\u253c\u251c\u2524\u2534\u252c\u256c\u2561\u2562\u25cf\u25cb\u25a0\u25a1";

        private static readonly string AccentGlyphs = "#$@&*~^!?";

        private void Start()
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

                        // Occasionally shift alpha slightly for organic feel
                        float alpha = Random.Range(minAlpha, maxAlpha);
                        var baseColor = _columnBaseColors[i];
                        _columns[i].color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                    }
                }
            }
        }

        private void CreateColumns()
        {
            _columns = new TextMeshProUGUI[columnCount];
            _scrollOffsets = new float[columnCount];
            _columnSpeeds = new float[columnCount];
            _columnBaseColors = new Color[columnCount];

            var parentRT = GetComponent<RectTransform>();
            if (parentRT == null) return;

            Color greenBase = theme != null ? theme.phosphorGreen : new Color(0.12f, 1f, 0.45f);
            Color amberBase = theme != null ? theme.amberBright : new Color(1f, 0.78f, 0.15f);
            Color cyanBase = theme != null ? theme.cyanInfo : new Color(0.15f, 0.9f, 0.95f);
            int fontSize = theme != null ? theme.smallSize : 13;

            float colWidth = 1f / columnCount;

            for (int i = 0; i < columnCount; i++)
            {
                var go = new GameObject($"DataStream_{i}");
                go.transform.SetParent(transform, false);

                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(colWidth * i, 0f);
                rt.anchorMax = new Vector2(colWidth * (i + 1), 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = GenerateColumn();
                if (theme != null && theme.primaryFont != null)
                    tmp.font = theme.primaryFont;
                tmp.fontSize = fontSize;

                // Most columns green, occasional amber or cyan column
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
                // Edge columns slightly dimmer for vignette feel
                float edgeFade = 1f;
                float normalizedX = (float)i / (columnCount - 1);
                float distFromCenter = Mathf.Abs(normalizedX - 0.5f) * 2f; // 0 at center, 1 at edges
                edgeFade = Mathf.Lerp(1f, 0.4f, distFromCenter * distFromCenter);

                tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha * edgeFade);
                tmp.alignment = TextAlignmentOptions.Top;
                tmp.enableWordWrapping = false;
                tmp.overflowMode = TextOverflowModes.Overflow;
                tmp.raycastTarget = false;

                _columns[i] = tmp;
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
            if (_columns != null)
            {
                foreach (var col in _columns)
                    if (col != null) Destroy(col.gameObject);
            }
            _columns = null;
        }
    }
}
