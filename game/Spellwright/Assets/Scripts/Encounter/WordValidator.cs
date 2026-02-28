using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static WordValidator Instance { get; private set; }

        [SerializeField] private string language = "en_US";

        private WordList _dictionary;
        private bool _isLoaded;

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
            var dictDir = Path.Combine(Application.streamingAssetsPath, "Dictionaries");
            var dicPath = Path.Combine(dictDir, $"{language}.dic");
            var affPath = Path.Combine(dictDir, $"{language}.aff");

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
            Debug.Log($"[WordValidator] Dictionary loaded: {language}");
        }

        /// <summary>
        /// Checks whether a word is a valid English word.
        /// </summary>
        public bool IsValidEnglishWord(string word)
        {
            if (!_isLoaded || string.IsNullOrWhiteSpace(word))
                return false;

            return _dictionary.Check(word.Trim().ToLowerInvariant());
        }

        /// <summary>
        /// Returns spelling suggestions for a misspelled word.
        /// </summary>
        public List<string> GetSuggestions(string word)
        {
            if (!_isLoaded || string.IsNullOrWhiteSpace(word))
                return new List<string>();

            return _dictionary.Suggest(word.Trim().ToLowerInvariant()).ToList();
        }
    }
}
