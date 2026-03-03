using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Spellwright.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Renders a Wheel-of-Fortune-style tile board with a fixed grid of slots.
    /// Active tiles show hidden/revealed letters. Spare tiles fill the remaining
    /// grid positions as dim empty boxes, giving the classic WoF look.
    /// </summary>
    public class TileBoardUI : MonoBehaviour
    {
        [SerializeField] private TerminalThemeSO theme;

        [Header("Tile Settings")]
        [SerializeField] private float tileWidth = 52f;
        [SerializeField] private float tileHeight = 60f;
        [SerializeField] private float tileSpacing = 5f;
        [SerializeField] private float rowSpacing = 10f;
        [SerializeField] private float revealFlashDuration = 0.3f;
        [SerializeField] private int minRows = 4;

        private readonly List<GameObject> _tileObjects = new List<GameObject>();
        private readonly List<Image> _tileBgs = new List<Image>();
        private readonly List<TextMeshProUGUI> _tileTexts = new List<TextMeshProUGUI>();
        private readonly List<TextMeshProUGUI> _tilePatterns = new List<TextMeshProUGUI>();
        private readonly List<GameObject> _emptyTiles = new List<GameObject>();
        private RectTransform _container;

        // WoF-style colors
        private Color HiddenBg => new Color(0.04f, 0.28f, 0.12f);
        private Color HiddenPatternColor => new Color(0.06f, 0.42f, 0.18f, 0.7f);
        private Color RevealedBg => new Color(0.93f, 0.96f, 0.91f);
        private Color RevealedText => new Color(0.08f, 0.12f, 0.08f);
        private Color TileBorder => theme != null
            ? new Color(theme.borderColor.r, theme.borderColor.g, theme.borderColor.b, 0.5f)
            : new Color(0f, 0.6f, 0.2f, 0.5f);
        private Color RevealedBorder => new Color(0.15f, 0.70f, 0.35f, 0.8f);
        private Color EmptyBg => new Color(0.02f, 0.06f, 0.03f, 0.6f);
        private Color EmptyBorder => new Color(0f, 0.15f, 0.06f, 0.3f);

        private void Awake()
        {
            _container = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Creates a WoF-style grid: 4 rows with edge rows (top/bottom) narrower
        /// than middle rows. Content is vertically centered. Grid is horizontally
        /// centered within the container with natural padding.
        /// </summary>
        public void InitializeBoard(BoardState board)
        {
            ClearBoard();

            if (board == null || board.Tiles.Length == 0) return;

            var tiles = board.Tiles;
            float containerWidth = _container.rect.width;
            if (containerWidth <= 0) containerWidth = 800f;

            // WoF grid: cap middle rows at 14 columns, edge rows get 2 fewer
            int dynamicMax = Mathf.FloorToInt((containerWidth + tileSpacing) / (tileWidth + tileSpacing));
            int middleCols = Mathf.Clamp(dynamicMax, 8, 14);
            int edgeCols = middleCols - 2;

            // Pack words into content rows (using middle row width)
            var words = SplitIntoWords(tiles);
            const int GAP = -1;
            var contentRows = new List<List<int>>();
            var currentRow = new List<int>();
            int currentCols = 0;

            foreach (var word in words)
            {
                int gap = currentCols > 0 ? 1 : 0;
                if (currentCols > 0 && currentCols + gap + word.Count > middleCols)
                {
                    contentRows.Add(currentRow);
                    currentRow = new List<int>();
                    currentCols = 0;
                    gap = 0;
                }
                if (gap > 0) { currentRow.Add(GAP); currentCols++; }
                foreach (int idx in word) { currentRow.Add(idx); currentCols++; }
            }
            if (currentRow.Count > 0) contentRows.Add(currentRow);

            // Build row structure: edge/middle/middle/edge (expandable)
            int totalRows = Mathf.Max(minRows, contentRows.Count);
            int[] rowColCounts = new int[totalRows];
            for (int i = 0; i < totalRows; i++)
                rowColCounts[i] = (i == 0 || i == totalRows - 1) ? edgeCols : middleCols;

            // Vertically center content rows within the grid
            int startRow = Mathf.Max(0, (totalRows - contentRows.Count) / 2);

            // Dynamically scale tiles to fit container with padding
            float containerHeight = _container.rect.height;
            if (containerHeight <= 0) containerHeight = 300f;
            float pad = 16f;
            float usableHeight = containerHeight - pad * 2;
            float neededHeight = totalRows * (tileHeight + rowSpacing) - rowSpacing;
            float scale = (neededHeight > usableHeight) ? usableHeight / neededHeight : 1f;

            float tw = tileWidth * scale;
            float th = tileHeight * scale;
            float ts = tileSpacing * scale;
            float rs = rowSpacing * scale;

            // Layout dimensions
            float totalHeight = totalRows * (th + rs) - rs;
            float startY = totalHeight / 2f - th / 2f;

            // Pre-fill tile lists
            for (int i = 0; i < tiles.Length; i++)
            {
                _tileObjects.Add(null);
                _tileBgs.Add(null);
                _tileTexts.Add(null);
                _tilePatterns.Add(null);
            }

            for (int r = 0; r < totalRows; r++)
            {
                int cols = rowColCounts[r];
                float rowGridWidth = cols * (tw + ts) - ts;
                float yPos = startY - r * (th + rs);

                int contentRowIdx = r - startRow;
                bool hasContent = contentRowIdx >= 0 && contentRowIdx < contentRows.Count;

                if (hasContent)
                {
                    var rowContent = contentRows[contentRowIdx];
                    int contentWidth = rowContent.Count;
                    int effectiveCols = Mathf.Max(cols, contentWidth);
                    float effectiveWidth = effectiveCols * (tw + ts) - ts;
                    int leftPad = (effectiveCols - contentWidth) / 2;

                    for (int c = 0; c < effectiveCols; c++)
                    {
                        float xPos = -effectiveWidth / 2f + c * (tw + ts) + tw / 2f;
                        int ci = c - leftPad;

                        if (ci >= 0 && ci < rowContent.Count)
                        {
                            int item = rowContent[ci];
                            if (item == GAP)
                            {
                                PlaceEmptyTile(xPos, yPos, tw, th);
                            }
                            else
                            {
                                var tileGO = CreateTileObject(tiles[item], item, tw, th);
                                var rt = tileGO.GetComponent<RectTransform>();
                                rt.anchoredPosition = new Vector2(xPos, yPos);

                                _tileObjects[item] = tileGO;
                                _tileBgs[item] = tileGO.GetComponent<Image>();
                                _tileTexts[item] = tileGO.transform.Find("Letter")?.GetComponent<TextMeshProUGUI>();
                                _tilePatterns[item] = tileGO.transform.Find("Pattern")?.GetComponent<TextMeshProUGUI>();
                                UpdateTileVisual(item, tiles[item].State == TileState.Revealed);
                            }
                        }
                        else
                        {
                            PlaceEmptyTile(xPos, yPos, tw, th);
                        }
                    }
                }
                else
                {
                    for (int c = 0; c < cols; c++)
                    {
                        float xPos = -rowGridWidth / 2f + c * (tw + ts) + tw / 2f;
                        PlaceEmptyTile(xPos, yPos, tw, th);
                    }
                }
            }
        }

        /// <summary>
        /// Animates reveal of specific tile positions with a flash + scale punch.
        /// </summary>
        public void RevealTilesAnimated(List<int> positions)
        {
            if (positions == null) return;
            foreach (int idx in positions)
            {
                if (idx >= 0 && idx < _tileObjects.Count && _tileObjects[idx] != null)
                {
                    UpdateTileVisual(idx, true);
                    StartCoroutine(TileRevealPunch(_tileObjects[idx], _tileBgs[idx], _tilePatterns[idx]));
                }
            }
        }

        /// <summary>
        /// Cascade reveals all tiles for encounter end.
        /// </summary>
        public void RevealAllAnimated()
        {
            StartCoroutine(CascadeReveal());
        }

        public void ClearBoard()
        {
            foreach (var go in _tileObjects)
            {
                if (go != null)
                {
                    DOTween.Kill(go);
                    Destroy(go);
                }
            }
            foreach (var go in _emptyTiles)
            {
                if (go != null)
                    Destroy(go);
            }
            _tileObjects.Clear();
            _tileBgs.Clear();
            _tileTexts.Clear();
            _tilePatterns.Clear();
            _emptyTiles.Clear();
        }

        // ── Internal ────────────────────────────────────────

        private void PlaceEmptyTile(float x, float y, float w = 0, float h = 0)
        {
            if (w <= 0) w = tileWidth;
            if (h <= 0) h = tileHeight;
            var go = new GameObject("EmptyTile");
            go.transform.SetParent(_container, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);

            var img = go.AddComponent<Image>();
            img.color = EmptyBg;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = EmptyBorder;
            outline.effectDistance = new Vector2(2, -2);

            _emptyTiles.Add(go);
        }

        private GameObject CreateTileObject(Tile tile, int index, float w = 0, float h = 0)
        {
            if (w <= 0) w = tileWidth;
            if (h <= 0) h = tileHeight;
            var go = new GameObject($"Tile_{index}");
            go.transform.SetParent(_container, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);

            var img = go.AddComponent<Image>();
            img.color = HiddenBg;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = TileBorder;
            outline.effectDistance = new Vector2(2, -2);

            // Pattern overlay (diamond) for hidden tiles
            var patternGO = new GameObject("Pattern");
            patternGO.transform.SetParent(go.transform, false);
            var patternRT = patternGO.AddComponent<RectTransform>();
            patternRT.anchorMin = Vector2.zero;
            patternRT.anchorMax = Vector2.one;
            patternRT.offsetMin = Vector2.zero;
            patternRT.offsetMax = Vector2.zero;

            var patternTmp = patternGO.AddComponent<TextMeshProUGUI>();
            patternTmp.text = "\u25C6";
            if (theme != null && theme.primaryFont != null)
                patternTmp.font = theme.primaryFont;
            patternTmp.fontSize = theme != null ? theme.labelSize : 22;
            patternTmp.color = HiddenPatternColor;
            patternTmp.alignment = TextAlignmentOptions.Center;
            patternTmp.raycastTarget = false;

            // Letter text
            var textGO = new GameObject("Letter");
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            if (theme != null && theme.secondaryBoldFont != null)
                tmp.font = theme.secondaryBoldFont;
            else if (theme != null && theme.primaryFont != null)
                tmp.font = theme.primaryFont;
            tmp.fontSize = theme != null ? theme.headerSize : 28;
            tmp.color = RevealedText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return go;
        }

        private void UpdateTileVisual(int index, bool revealed)
        {
            if (index < 0 || index >= _tileBgs.Count) return;
            var bg = _tileBgs[index];
            var text = _tileTexts[index];
            var pattern = index < _tilePatterns.Count ? _tilePatterns[index] : null;
            if (bg == null || text == null) return;

            if (revealed)
            {
                bg.color = RevealedBg;
                text.text = _tileObjects[index] != null ? GetTileChar(index) : "";
                text.color = RevealedText;
                if (pattern != null) pattern.alpha = 0f;

                var outline = _tileObjects[index]?.GetComponent<Outline>();
                if (outline != null) outline.effectColor = RevealedBorder;
            }
            else
            {
                bg.color = HiddenBg;
                text.text = "";
                if (pattern != null) pattern.alpha = 1f;

                var outline = _tileObjects[index]?.GetComponent<Outline>();
                if (outline != null) outline.effectColor = TileBorder;
            }
        }

        private string GetTileChar(int index)
        {
            var encMgr = FindAnyObjectByType<EncounterManager>();
            if (encMgr?.Board?.Tiles != null && index < encMgr.Board.Tiles.Length)
                return encMgr.Board.Tiles[index].Character.ToString().ToUpperInvariant();
            return "?";
        }

        private IEnumerator TileRevealPunch(GameObject tileGO, Image bg, TextMeshProUGUI pattern)
        {
            if (tileGO == null) yield break;
            var rt = tileGO.GetComponent<RectTransform>();

            // Flash from green through warm-white to cream
            Color flashColor = new Color(1f, 1f, 0.85f);
            float elapsed = 0f;
            float half = revealFlashDuration / 2f;

            // Scale up + flash bright
            while (elapsed < half)
            {
                float t = elapsed / half;
                rt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.3f, t);
                if (bg != null)
                    bg.color = Color.Lerp(HiddenBg, flashColor, t);
                if (pattern != null)
                    pattern.alpha = 1f - t;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Scale back + settle to cream
            elapsed = 0f;
            while (elapsed < half)
            {
                float t = elapsed / half;
                rt.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, t);
                if (bg != null)
                    bg.color = Color.Lerp(flashColor, RevealedBg, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            rt.localScale = Vector3.one;
            if (bg != null)
                bg.color = RevealedBg;

            // Update outline for revealed style
            var outline = tileGO.GetComponent<Outline>();
            if (outline != null) outline.effectColor = RevealedBorder;

            // Apply breathing glow after reveal
            TileGlowEffect.ApplyBreathingGlow(tileGO);
        }

        private IEnumerator CascadeReveal()
        {
            for (int i = 0; i < _tileObjects.Count; i++)
            {
                if (_tileObjects[i] != null && _tileBgs[i] != null)
                {
                    UpdateTileVisual(i, true);
                    var pattern = i < _tilePatterns.Count ? _tilePatterns[i] : null;
                    StartCoroutine(TileRevealPunch(_tileObjects[i], _tileBgs[i], pattern));
                    yield return new WaitForSeconds(0.05f);
                }
            }
        }

        /// <summary>
        /// Splits tile indices into word groups (split on Space tiles).
        /// </summary>
        private static List<List<int>> SplitIntoWords(Tile[] tiles)
        {
            var words = new List<List<int>>();
            var current = new List<int>();

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].Type == TileType.Space)
                {
                    if (current.Count > 0)
                    {
                        words.Add(current);
                        current = new List<int>();
                    }
                }
                else
                {
                    current.Add(i);
                }
            }
            if (current.Count > 0) words.Add(current);

            return words;
        }
    }
}
