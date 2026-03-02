using System.Collections.Generic;
using Spellwright.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Displays an A-Z alphabet row showing which letters have been guessed.
    /// Dim = unused, green = hit, red = miss.
    /// </summary>
    public class GuessedLettersUI : MonoBehaviour
    {
        [SerializeField] private TerminalThemeSO theme;

        private readonly Dictionary<char, TextMeshProUGUI> _letterTexts = new Dictionary<char, TextMeshProUGUI>();
        private RectTransform _container;

        private Color UnusedColor => theme != null ? theme.phosphorDim : new Color(0f, 0.5f, 0.18f);
        private Color HitColor => theme != null ? theme.successColor : new Color(0.1f, 0.9f, 0.3f);
        private Color MissColor => theme != null ? theme.damageColor : new Color(1f, 0.15f, 0.1f);

        private void Awake()
        {
            _container = GetComponent<RectTransform>();
            BuildAlphabet();
        }

        private void BuildAlphabet()
        {
            // Add horizontal layout group
            var hlg = gameObject.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
            {
                hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 2;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;
                hlg.childAlignment = TextAnchor.MiddleCenter;
            }

            for (char c = 'A'; c <= 'Z'; c++)
            {
                var go = new GameObject($"Letter_{c}");
                go.transform.SetParent(transform, false);

                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth = 22;
                le.preferredHeight = 24;

                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = c.ToString();
                if (theme != null && theme.primaryFont != null)
                    tmp.font = theme.primaryFont;
                tmp.fontSize = theme != null ? theme.smallSize : 13;
                tmp.color = UnusedColor;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;

                _letterTexts[char.ToLowerInvariant(c)] = tmp;
            }
        }

        /// <summary>
        /// Marks a letter as guessed, coloring it green (hit) or red (miss).
        /// </summary>
        public void MarkLetterGuessed(char letter, bool wasInPhrase)
        {
            char lower = char.ToLowerInvariant(letter);
            if (_letterTexts.TryGetValue(lower, out var tmp))
            {
                tmp.color = wasInPhrase ? HitColor : MissColor;
            }
        }

        /// <summary>
        /// Resets all letters to the dim unused color.
        /// </summary>
        public void Reset()
        {
            foreach (var kvp in _letterTexts)
                kvp.Value.color = UnusedColor;
        }
    }
}
