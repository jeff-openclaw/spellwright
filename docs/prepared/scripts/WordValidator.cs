using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WeCantSpell.Hunspell;

namespace Spellwright.Data
{
    /// <summary>
    /// Thread-safe singleton that wraps WeCantSpell.Hunspell for validating
    /// player guesses against a real English dictionary.
    /// </summary>
    public sealed class WordValidator : IDisposable
    {
        private static readonly object _lock = new object();
        private static WordValidator _instance;

        private WordList _dictionary;
        private bool _isLoaded;

        /// <summary>Whether the dictionary has been successfully loaded.</summary>
        public bool IsLoaded => _isLoaded;

        private WordValidator() { }

        /// <summary>Gets the singleton instance.</summary>
        public static WordValidator Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new WordValidator();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Loads the Hunspell dictionary from the given directory.
        /// Expects {language}.dic and {language}.aff files (e.g. en_US.dic, en_US.aff).
        /// </summary>
        /// <param name="dictionaryDir">
        /// Path to the dictionary directory.
        /// In Unity, typically: Application.streamingAssetsPath + "/Dictionaries"
        /// </param>
        /// <param name="language">Language code, e.g. "en_US".</param>
        // TODO: In Unity, call this from a MonoBehaviour.Start() or a loading screen.
        // Use Application.streamingAssetsPath for the directory path.
        public void LoadDictionary(string dictionaryDir, string language = "en_US")
        {
            lock (_lock)
            {
                if (_isLoaded) return;

                var dicPath = Path.Combine(dictionaryDir, $"{language}.dic");
                var affPath = Path.Combine(dictionaryDir, $"{language}.aff");

                if (!File.Exists(dicPath))
                    throw new FileNotFoundException($"Dictionary file not found: {dicPath}");
                if (!File.Exists(affPath))
                    throw new FileNotFoundException($"Affix file not found: {affPath}");

                _dictionary = WordList.CreateFromFiles(dicPath, affPath);
                _isLoaded = true;
            }
        }

        /// <summary>
        /// Loads from streams (useful for Unity's StreamingAssets on Android/WebGL
        /// where direct file paths may not work).
        /// </summary>
        public void LoadDictionary(Stream dicStream, Stream affStream)
        {
            lock (_lock)
            {
                if (_isLoaded) return;

                _dictionary = WordList.CreateFromStreams(dicStream, affStream);
                _isLoaded = true;
            }
        }

        /// <summary>
        /// Checks whether a word is a valid English word according to the loaded dictionary.
        /// </summary>
        /// <param name="word">The word to validate (case-insensitive).</param>
        /// <returns>True if the word is valid; false if invalid or dictionary not loaded.</returns>
        public bool IsValidWord(string word)
        {
            if (!_isLoaded || string.IsNullOrWhiteSpace(word))
                return false;

            return _dictionary.Check(word.Trim().ToLowerInvariant());
        }

        /// <summary>
        /// Returns spelling suggestions for a misspelled word.
        /// Useful for "did you mean?" UX feedback.
        /// </summary>
        /// <param name="word">The misspelled word.</param>
        /// <returns>A list of suggested corrections, or an empty list.</returns>
        public List<string> GetSuggestions(string word)
        {
            if (!_isLoaded || string.IsNullOrWhiteSpace(word))
                return new List<string>();

            return _dictionary.Suggest(word.Trim().ToLowerInvariant()).ToList();
        }

        public void Dispose()
        {
            // WordList does not implement IDisposable, but we clear our reference.
            _dictionary = null;
            _isLoaded = false;
        }
    }
}
