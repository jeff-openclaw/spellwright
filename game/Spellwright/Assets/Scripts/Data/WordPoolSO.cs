using System.Collections.Generic;
using UnityEngine;
using Spellwright.Data;

namespace Spellwright.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewWordPool", menuName = "Spellwright/Word Pool")]
    public class WordPoolSO : ScriptableObject
    {
        [Header("Pool Settings")]
        public string category;
        [Range(1, 5)]
        public int minDifficulty = 1;
        [Range(1, 5)]
        public int maxDifficulty = 5;

        [Header("Source")]
        [Tooltip("Tab-separated text file: word<TAB>difficulty")]
        public TextAsset sourceFile;

        private List<WordEntry> _cachedEntries;

        public List<WordEntry> GetWords()
        {
            if (_cachedEntries != null)
                return _cachedEntries;

            _cachedEntries = new List<WordEntry>();

            if (sourceFile == null)
                return _cachedEntries;

            var lines = sourceFile.text.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                var parts = trimmed.Split('\t');
                if (parts.Length < 2)
                    continue;

                var word = parts[0].Trim().ToLowerInvariant();
                if (!int.TryParse(parts[1].Trim(), out int difficulty))
                    continue;

                _cachedEntries.Add(new WordEntry
                {
                    Word = word,
                    Category = category,
                    Difficulty = difficulty,
                    LetterCount = word.Length
                });
            }

            return _cachedEntries;
        }

        public List<WordEntry> GetWordsByDifficulty(int difficulty)
        {
            return GetWords().FindAll(w => w.Difficulty == difficulty);
        }

        public int WordCount => GetWords().Count;
    }
}
