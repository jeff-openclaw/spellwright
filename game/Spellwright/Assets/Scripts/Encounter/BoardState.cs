using System;
using System.Collections.Generic;
using System.Linq;

namespace Spellwright.Encounter
{
    public enum TileType { Letter, Space }
    public enum TileState { Hidden, Revealed }

    public class Tile
    {
        public char Character { get; set; }
        public TileType Type { get; set; }
        public TileState State { get; set; }
        public int PositionInPhrase { get; set; }
    }

    /// <summary>
    /// Tracks the state of all letter tiles on the Wheel-of-Fortune-style board.
    /// Spaces are always Revealed. Letters start Hidden.
    /// </summary>
    public class BoardState
    {
        private readonly Tile[] _tiles;
        private readonly HashSet<char> _guessedLetters = new HashSet<char>();
        private readonly string _phrase;

        private static readonly HashSet<char> VowelSet = new HashSet<char> { 'a', 'e', 'i', 'o', 'u' };
        private static readonly Random _rng = new Random();

        public Tile[] Tiles => _tiles;
        public IReadOnlyCollection<char> GuessedLetters => _guessedLetters;
        public string Phrase => _phrase;

        public int TotalLetters { get; }
        public int RevealedLetterCount => _tiles.Count(t => t.Type == TileType.Letter && t.State == TileState.Revealed);
        public int HiddenLetterCount => TotalLetters - RevealedLetterCount;

        public BoardState(string phrase)
        {
            _phrase = phrase?.ToLowerInvariant() ?? "";
            _tiles = new Tile[_phrase.Length];

            int letterCount = 0;
            for (int i = 0; i < _phrase.Length; i++)
            {
                char c = _phrase[i];
                bool isSpace = c == ' ';
                _tiles[i] = new Tile
                {
                    Character = c,
                    Type = isSpace ? TileType.Space : TileType.Letter,
                    State = isSpace ? TileState.Revealed : TileState.Hidden,
                    PositionInPhrase = i
                };
                if (!isSpace) letterCount++;
            }
            TotalLetters = letterCount;
        }

        /// <summary>
        /// Reveals all tiles matching the given letter. Returns the count revealed.
        /// </summary>
        public int RevealAllOfLetter(char letter)
        {
            char lower = char.ToLowerInvariant(letter);
            _guessedLetters.Add(lower);
            int count = 0;
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].Type == TileType.Letter && _tiles[i].Character == lower
                    && _tiles[i].State == TileState.Hidden)
                {
                    _tiles[i].State = TileState.Revealed;
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Reveals a single random hidden letter tile. Returns its index, or -1 if none left.
        /// </summary>
        public int RevealRandomHidden()
        {
            var hidden = new List<int>();
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].Type == TileType.Letter && _tiles[i].State == TileState.Hidden)
                    hidden.Add(i);
            }
            if (hidden.Count == 0) return -1;

            int idx = hidden[_rng.Next(hidden.Count)];
            _tiles[idx].State = TileState.Revealed;
            return idx;
        }

        /// <summary>
        /// Reveals the first hidden letter tile. Returns its index, or -1 if none left.
        /// </summary>
        public int RevealFirstLetter()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].Type == TileType.Letter && _tiles[i].State == TileState.Hidden)
                {
                    _tiles[i].State = TileState.Revealed;
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Checks whether the phrase contains the given letter.
        /// </summary>
        public bool HasLetter(char letter)
        {
            char lower = char.ToLowerInvariant(letter);
            return _tiles.Any(t => t.Type == TileType.Letter && t.Character == lower);
        }

        /// <summary>
        /// Checks whether all letter tiles have been revealed.
        /// </summary>
        public bool IsFullyRevealed()
        {
            return _tiles.All(t => t.State == TileState.Revealed);
        }

        /// <summary>
        /// Returns indices of all vowel tiles (hidden or revealed).
        /// </summary>
        public List<int> GetVowelPositions()
        {
            var positions = new List<int>();
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].Type == TileType.Letter && VowelSet.Contains(_tiles[i].Character))
                    positions.Add(i);
            }
            return positions;
        }

        /// <summary>
        /// Returns indices of all tiles matching the given letter (hidden or revealed).
        /// </summary>
        public List<int> GetPositionsOfLetter(char letter)
        {
            char lower = char.ToLowerInvariant(letter);
            var positions = new List<int>();
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].Type == TileType.Letter && _tiles[i].Character == lower)
                    positions.Add(i);
            }
            return positions;
        }

        /// <summary>
        /// Reveals all hidden vowel tiles. Returns the list of indices revealed.
        /// </summary>
        public List<int> RevealAllVowels()
        {
            var revealed = new List<int>();
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].Type == TileType.Letter && _tiles[i].State == TileState.Hidden
                    && VowelSet.Contains(_tiles[i].Character))
                {
                    _tiles[i].State = TileState.Revealed;
                    revealed.Add(i);
                }
            }
            return revealed;
        }

        /// <summary>
        /// Reveals all hidden tiles matching the given letters. Returns the list of indices revealed.
        /// </summary>
        public List<int> RevealSpecificLetters(IEnumerable<char> letters)
        {
            var set = new HashSet<char>(letters.Select(char.ToLowerInvariant));
            var revealed = new List<int>();
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].Type == TileType.Letter && _tiles[i].State == TileState.Hidden
                    && set.Contains(_tiles[i].Character))
                {
                    _tiles[i].State = TileState.Revealed;
                    revealed.Add(i);
                }
            }
            return revealed;
        }

        /// <summary>
        /// Reveals all remaining hidden tiles. Returns the list of indices revealed.
        /// </summary>
        public List<int> RevealAll()
        {
            var revealed = new List<int>();
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].Type == TileType.Letter && _tiles[i].State == TileState.Hidden)
                {
                    _tiles[i].State = TileState.Revealed;
                    revealed.Add(i);
                }
            }
            return revealed;
        }

        /// <summary>
        /// Checks whether the given letter has already been guessed.
        /// </summary>
        public bool IsLetterAlreadyGuessed(char letter)
        {
            return _guessedLetters.Contains(char.ToLowerInvariant(letter));
        }

        /// <summary>
        /// Marks a letter as guessed without revealing it (for tracking miss history).
        /// </summary>
        public void MarkLetterGuessed(char letter)
        {
            _guessedLetters.Add(char.ToLowerInvariant(letter));
        }
    }
}
