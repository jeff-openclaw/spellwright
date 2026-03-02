using Spellwright.Data;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Static utility for evaluating player guesses against the target word.
    /// Normalizes input, validates against the dictionary, and provides feedback.
    /// </summary>
    public static class GuessProcessor
    {
        /// <summary>
        /// Evaluates a player's guess against the target word.
        /// </summary>
        /// <param name="guess">Raw player input.</param>
        /// <param name="targetWord">The secret target word (lowercase).</param>
        /// <param name="language">Game language for feedback strings.</param>
        /// <returns>A <see cref="GuessResult"/> with correctness, validity, and feedback.</returns>
        public static GuessResult Process(string guess, string targetWord, GameLanguage language = GameLanguage.English)
        {
            bool ro = language == GameLanguage.Romanian;
            var normalized = guess?.Trim().ToLowerInvariant() ?? "";
            var target = targetWord?.Trim().ToLowerInvariant() ?? "";

            var result = new GuessResult
            {
                GuessedWord = normalized,
                IsCorrect = false,
                IsValidWord = true,
                Feedback = "",
                LettersCorrect = 0
            };

            if (string.IsNullOrEmpty(normalized))
            {
                result.IsValidWord = false;
                result.Feedback = ro ? "Introdu un cuvant." : "Please enter a word.";
                return result;
            }

            // Validate against the dictionary
            if (WordValidator.Instance != null && WordValidator.Instance.IsLoaded)
            {
                if (!WordValidator.Instance.IsValidWord(normalized))
                {
                    result.IsValidWord = false;
                    result.Feedback = ro ? "Cuvant nerecunoscut" : "Not a recognized English word";
                    return result;
                }
            }

            // Exact match
            if (normalized == target)
            {
                result.IsCorrect = true;
                result.LettersCorrect = target.Length;
                result.Feedback = ro ? "Corect!" : "Correct!";
                return result;
            }

            // Wrong length
            if (normalized.Length != target.Length)
            {
                result.Feedback = ro
                    ? $"Numar gresit de litere ({normalized.Length} vs {target.Length})"
                    : $"Wrong number of letters ({normalized.Length} vs {target.Length})";
                result.LettersCorrect = CountMatchingPositions(normalized, target);
                return result;
            }

            // Right length, wrong word
            result.LettersCorrect = CountMatchingPositions(normalized, target);
            result.Feedback = ro ? "Nu e cuvantul corect, dar aceeasi lungime!" : "Not the right word, but same length!";
            return result;
        }

        /// <summary>
        /// Counts the number of letter positions where guess and target match.
        /// </summary>
        private static int CountMatchingPositions(string guess, string target)
        {
            int count = 0;
            int minLen = guess.Length < target.Length ? guess.Length : target.Length;
            for (int i = 0; i < minLen; i++)
            {
                if (guess[i] == target[i])
                    count++;
            }
            return count;
        }
    }
}
