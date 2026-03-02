using Spellwright.Data;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Static utility for evaluating player guesses against the target word/phrase.
    /// Supports both single-letter guesses and full phrase solve attempts.
    /// </summary>
    public static class GuessProcessor
    {
        /// <summary>
        /// Determines whether the input is a single-letter guess or a phrase solve attempt.
        /// </summary>
        public static GuessType DetermineGuessType(string input)
        {
            var trimmed = input?.Trim() ?? "";
            if (trimmed.Length == 1 && char.IsLetter(trimmed[0]))
                return GuessType.Letter;
            return GuessType.Phrase;
        }

        /// <summary>
        /// Top-level entry point. Delegates to letter or phrase processing based on input.
        /// Backwards-compatible: still returns a GuessResult with all fields populated.
        /// </summary>
        public static GuessResult Process(string guess, string targetWord, GameLanguage language = GameLanguage.English)
        {
            var guessType = DetermineGuessType(guess);
            if (guessType == GuessType.Letter)
                return ProcessLetterGuess(guess.Trim().ToLowerInvariant()[0], targetWord, language);
            return ProcessPhraseGuess(guess, targetWord, language);
        }

        /// <summary>
        /// Evaluates a single-letter guess. Does NOT mutate the board — that's EncounterManager's job.
        /// </summary>
        public static GuessResult ProcessLetterGuess(char letter, string targetPhrase, GameLanguage language)
        {
            bool ro = language == GameLanguage.Romanian;
            char lower = char.ToLowerInvariant(letter);
            var target = targetPhrase?.Trim().ToLowerInvariant() ?? "";

            var result = new GuessResult
            {
                GuessedWord = lower.ToString(),
                GuessType = GuessType.Letter,
                GuessedLetter = lower,
                IsCorrect = false,
                IsValidWord = true,
                Feedback = "",
                LettersCorrect = 0,
                IsLetterAlreadyGuessed = false
            };

            if (!char.IsLetter(lower))
            {
                result.IsValidWord = false;
                result.Feedback = ro ? "Introdu o litera." : "Please enter a letter.";
                return result;
            }

            // Count occurrences in target
            int count = 0;
            foreach (char c in target)
            {
                if (c == lower) count++;
            }

            result.IsLetterInPhrase = count > 0;
            result.LetterOccurrences = count;

            if (count > 0)
            {
                result.Feedback = ro
                    ? $"Da! '{char.ToUpperInvariant(lower)}' apare de {count} ori!"
                    : $"Yes! '{char.ToUpperInvariant(lower)}' appears {count} time{(count > 1 ? "s" : "")}!";
            }
            else
            {
                result.Feedback = ro
                    ? $"'{char.ToUpperInvariant(lower)}' nu e in cuvant."
                    : $"'{char.ToUpperInvariant(lower)}' is not in the word.";
            }

            return result;
        }

        /// <summary>
        /// Evaluates a full phrase/word solve attempt.
        /// For single-word attempts, validates against dictionary. Skips validation for multi-word.
        /// </summary>
        public static GuessResult ProcessPhraseGuess(string guess, string targetPhrase, GameLanguage language)
        {
            bool ro = language == GameLanguage.Romanian;
            var normalized = guess?.Trim().ToLowerInvariant() ?? "";
            var target = targetPhrase?.Trim().ToLowerInvariant() ?? "";

            var result = new GuessResult
            {
                GuessedWord = normalized,
                GuessType = GuessType.Phrase,
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

            // For single-word attempts, validate against dictionary
            bool isMultiWord = normalized.Contains(' ');
            if (!isMultiWord && WordValidator.Instance != null && WordValidator.Instance.IsLoaded)
            {
                if (!WordValidator.Instance.IsValidWord(normalized))
                {
                    result.IsValidWord = false;
                    result.Feedback = ro ? "Cuvant nerecunoscut" : "Not a recognized word";
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

            // Wrong guess
            result.LettersCorrect = CountMatchingPositions(normalized, target);
            if (normalized.Length != target.Length)
            {
                result.Feedback = ro
                    ? "Nu e raspunsul corect."
                    : "That's not the answer.";
            }
            else
            {
                result.Feedback = ro
                    ? "Nu e raspunsul corect, dar aceeasi lungime!"
                    : "Not the right answer, but same length!";
            }
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
