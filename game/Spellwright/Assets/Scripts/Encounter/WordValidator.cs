using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spellwright.Data;
using Spellwright.ScriptableObjects;
using UnityEngine;
using WeCantSpell.Hunspell;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Singleton MonoBehaviour that wraps WeCantSpell.Hunspell for validating
    /// player guesses against a real English dictionary.
    /// Loads dictionary files from StreamingAssets/Dictionaries/ on Awake.
    /// </summary>
    public class WordValidator : MonoBehaviour
    {
        public static WordValidator Instance { get; set; }

        [SerializeField] private GameConfigSO gameConfig;

        private WordList _dictionary;
        private bool _isLoaded;
        private string _loadedLangCode;

        public bool IsLoaded => _isLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadDictionary();
        }

        private void LoadDictionary()
        {
            var langCode = gameConfig != null ? gameConfig.HunspellLanguageCode : "en_US";

            // Skip reload if already loaded for this language
            if (_isLoaded && _loadedLangCode == langCode)
                return;

            var dictDir = Path.Combine(Application.streamingAssetsPath, "Dictionaries");
            var dicPath = Path.Combine(dictDir, $"{langCode}.dic");
            var affPath = Path.Combine(dictDir, $"{langCode}.aff");

            if (!File.Exists(dicPath))
            {
                Debug.LogError($"[WordValidator] Dictionary file not found: {dicPath}");
                return;
            }
            if (!File.Exists(affPath))
            {
                Debug.LogError($"[WordValidator] Affix file not found: {affPath}");
                return;
            }

            _dictionary = WordList.CreateFromFiles(dicPath, affPath);
            _isLoaded = true;
            _loadedLangCode = langCode;
            Debug.Log($"[WordValidator] Dictionary loaded: {langCode}");
        }

        /// <summary>
        /// Reloads the dictionary if the configured language has changed.
        /// </summary>
        private void EnsureCorrectDictionary()
        {
            var langCode = gameConfig != null ? gameConfig.HunspellLanguageCode : "en_US";
            if (_loadedLangCode != langCode)
                LoadDictionary();
        }

        /// <summary>
        /// Checks whether a word is valid in the loaded dictionary.
        /// For Romanian, also accepts words without diacritics by checking suggestions.
        /// </summary>
        public bool IsValidWord(string word)
        {
            EnsureCorrectDictionary();
            if (!_isLoaded || string.IsNullOrWhiteSpace(word))
                return false;

            var clean = word.Trim().ToLowerInvariant();
            if (_dictionary.Check(clean))
                return true;

            // For Romanian, accept words without diacritics if Hunspell suggests
            // the diacritics-bearing variant (e.g. "pisica" → "pisică")
            if (gameConfig != null && gameConfig.language == GameLanguage.Romanian)
            {
                var suggestions = _dictionary.Suggest(clean);
                return suggestions.Any(s => RemoveDiacritics(s) == clean);
            }

            return false;
        }

        /// <summary>
        /// Checks whether a word is a valid English word.
        /// Kept for backwards compatibility.
        /// </summary>
        public bool IsValidEnglishWord(string word)
        {
            if (!_isLoaded || string.IsNullOrWhiteSpace(word))
                return false;

            return _dictionary.Check(word.Trim().ToLowerInvariant());
        }

        private static string RemoveDiacritics(string text)
        {
            return text
                .Replace('ă', 'a').Replace('â', 'a').Replace('î', 'i')
                .Replace('ș', 's').Replace('ş', 's')
                .Replace('ț', 't').Replace('ţ', 't');
        }

        /// <summary>
        /// Returns spelling suggestions for a misspelled word.
        /// </summary>
        public List<string> GetSuggestions(string word)
        {
            EnsureCorrectDictionary();
            if (!_isLoaded || string.IsNullOrWhiteSpace(word))
                return new List<string>();

            return _dictionary.Suggest(word.Trim().ToLowerInvariant()).ToList();
        }
    }
}
