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
    /// Renders a Wheel-of-Fortune-style tile board with fixed-grid layout.
    /// Hidden = patterned dark-green bg with diamond overlay. Revealed = white/cream bg, dark letter.
    /// Spaces = one tile-width gap to maintain grid structure.
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

        private readonly List<GameObject> _tileObjects = new List<GameObject>();
        private readonly List<Image> _tileBgs = new List<Image>();
        private readonly List<TextMeshProUGUI> _tileTexts = new List<TextMeshProUGUI>();
        private readonly List<TextMeshProUGUI> _tilePatterns = new List<TextMeshProUGUI>();
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

        private void Awake()
        {
            _container = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Creates tile GameObjects from a BoardState. Lays out tiles in a fixed-grid style
        /// with word-wrapping at spaces. Space gaps are one tile wide for grid consistency.
        /// </summary>
        public void InitializeBoard(BoardState board)
        {
            ClearBoard();

            if (board == null || board.Tiles.Length == 0) return;

            var tiles = board.Tiles;
            float containerWidth = _container.rect.width;
            if (containerWidth <= 0) containerWidth = 800f;

            // Space gap = one tile width for grid-like structure
            float wordSpacing = tileWidth + tileSpacing;

            var words = SplitIntoWords(tiles);

            const int GAP_SENTINEL = -1;
            var rows = new List<List<int>>();
            var row = new List<int>();
            float rowWidth = 0f;

            foreach (var word in words)
            {
                float wordWidth = word.Count * (tileWidth + tileSpacing) - tileSpacing;

                if (rowWidth > 0 && rowWidth + wordSpacing + wordWidth > containerWidth)
                {
                    rows.Add(row);
                    row = new List<int>();
                    rowWidth = 0f;
                }

                if (rowWidth > 0)
                {
                    row.Add(GAP_SENTINEL);
                    rowWidth += wordSpacing;
                }

                foreach (int idx in word)
                {
                    row.Add(idx);
                    rowWidth += tileWidth + tileSpacing;
                }
                rowWidth -= tileSpacing;
            }
            if (row.Count > 0) rows.Add(row);

            float totalHeight = rows.Count * (tileHeight + rowSpacing) - rowSpacing;
            float startY = totalHeight / 2f - tileHeight / 2f;

            // Pre-fill lists with nulls
            for (int i = 0; i < tiles.Length; i++)
            {
                _tileObjects.Add(null);
                _tileBgs.Add(null);
                _tileTexts.Add(null);
                _tilePatterns.Add(null);
            }

            for (int r = 0; r < rows.Count; r++)
            {
                var rowItems = rows[r];
                float totalRowWidth = 0f;
                foreach (int item in rowItems)
                {
                    if (item == GAP_SENTINEL)
                        totalRowWidth += wordSpacing;
                    else
                        totalRowWidth += tileWidth + tileSpacing;
                }
                totalRowWidth -= tileSpacing;

                float xOffset = -totalRowWidth / 2f;
                float yPos = startY - r * (tileHeight + rowSpacing);

                foreach (int item in rowItems)
                {
                    if (item == GAP_SENTINEL)
                    {
                        xOffset += wordSpacing;
                        continue;
                    }

                    var tileGO = CreateTileObject(tiles[item], item);
                    var rt = tileGO.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(xOffset + tileWidth / 2f, yPos);
                    rt.sizeDelta = new Vector2(tileWidth, tileHeight);

                    _tileObjects[item] = tileGO;
                    _tileBgs[item] = tileGO.GetComponent<Image>();
                    _tileTexts[item] = tileGO.transform.Find("Letter")?.GetComponent<TextMeshProUGUI>();
                    _tilePatterns[item] = tileGO.transform.Find("Pattern")?.GetComponent<TextMeshProUGUI>();

                    UpdateTileVisual(item, tiles[item].State == TileState.Revealed);

                    xOffset += tileWidth + tileSpacing;
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
            _tileObjects.Clear();
            _tileBgs.Clear();
            _tileTexts.Clear();
            _tilePatterns.Clear();
        }

        // ── Internal ────────────────────────────────────────

        private GameObject CreateTileObject(Tile tile, int index)
        {
            var go = new GameObject($"Tile_{index}");
            go.transform.SetParent(_container, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(tileWidth, tileHeight);

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
