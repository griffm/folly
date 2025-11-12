using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Folly.Core.Hyphenation
{
    /// <summary>
    /// Implements Frank Liang's TeX hyphenation algorithm.
    /// Uses pre-generated pattern data from source generators.
    /// Zero runtime dependencies - all pattern data is embedded at compile time.
    /// </summary>
    public class HyphenationEngine
    {
        private readonly Dictionary<string, int[]>? _patterns;
        private readonly string _languageCode;
        private readonly int _minWordLength;
        private readonly int _minLeftChars;
        private readonly int _minRightChars;

        /// <summary>
        /// Creates a new hyphenation engine for the specified language.
        /// </summary>
        /// <param name="languageCode">Language code (e.g., "en-US", "de-DE", "fr-FR", "es-ES")</param>
        /// <param name="minWordLength">Minimum word length to hyphenate (default: 5)</param>
        /// <param name="minLeftChars">Minimum characters before first hyphen (default: 2)</param>
        /// <param name="minRightChars">Minimum characters after last hyphen (default: 3)</param>
        public HyphenationEngine(
            string languageCode,
            int minWordLength = 5,
            int minLeftChars = 2,
            int minRightChars = 3)
        {
            _languageCode = languageCode ?? "en-US";
            _minWordLength = minWordLength;
            _minLeftChars = minLeftChars;
            _minRightChars = minRightChars;

            // Load patterns from generated code
            _patterns = HyphenationPatterns.GetPatterns(_languageCode);

            if (_patterns == null)
            {
                // Fallback to en-US if language not found
                _patterns = HyphenationPatterns.GetPatterns("en-US");
            }
        }

        /// <summary>
        /// Gets all supported language codes.
        /// </summary>
        public static string[] GetSupportedLanguages()
        {
            return HyphenationPatterns.GetSupportedLanguages();
        }

        /// <summary>
        /// Finds all valid hyphenation points in a word using Liang's algorithm.
        /// </summary>
        /// <param name="word">The word to hyphenate (should be lowercase)</param>
        /// <returns>
        /// Array of indices where hyphens can be inserted.
        /// For example, "hyphenation" might return [2, 6] for "hy-phen-ation"
        /// </returns>
        public int[] FindHyphenationPoints(string word)
        {
            if (_patterns == null || string.IsNullOrWhiteSpace(word))
                return Array.Empty<int>();

            // Don't hyphenate short words
            if (word.Length < _minWordLength)
                return Array.Empty<int>();

            // Remove any existing hyphens or special characters
            var cleanWord = CleanWord(word);
            if (cleanWord.Length < _minWordLength)
                return Array.Empty<int>();

            // Add word boundaries (periods)
            var wordWithBoundaries = "." + cleanWord.ToLowerInvariant() + ".";

            // Initialize priority array (0 = no hyphen, odd = allow, even = disallow)
            var priorities = new int[wordWithBoundaries.Length + 1];

            // Apply all matching patterns
            foreach (var pattern in _patterns)
            {
                ApplyPattern(wordWithBoundaries, pattern.Key, pattern.Value, priorities);
            }

            // Extract valid hyphenation points
            var hyphenPoints = new List<int>();

            // Check each position (excluding boundaries and enforcing min left/right)
            for (int i = _minLeftChars; i <= cleanWord.Length - _minRightChars; i++)
            {
                // Position i in cleanWord corresponds to position i+1 in wordWithBoundaries
                // (because of the leading period)
                var priority = priorities[i + 1];

                // Odd priority = allow hyphenation
                if (priority % 2 == 1)
                {
                    hyphenPoints.Add(i);
                }
            }

            return hyphenPoints.ToArray();
        }

        /// <summary>
        /// Hyphenates a word by inserting the hyphenation character at all valid break points.
        /// </summary>
        /// <param name="word">The word to hyphenate</param>
        /// <param name="hyphenChar">The character to insert (default: U+00AD soft hyphen)</param>
        /// <returns>The word with hyphenation characters inserted</returns>
        public string Hyphenate(string word, char hyphenChar = '\u00AD')
        {
            var points = FindHyphenationPoints(word);
            if (points.Length == 0)
                return word;

            var sb = new StringBuilder(word.Length + points.Length);
            int lastIndex = 0;

            foreach (var point in points)
            {
                sb.Append(word.Substring(lastIndex, point - lastIndex));
                sb.Append(hyphenChar);
                lastIndex = point;
            }

            sb.Append(word.Substring(lastIndex));
            return sb.ToString();
        }

        /// <summary>
        /// Applies a single hyphenation pattern to the priority array.
        /// This implements the core of Liang's algorithm.
        /// </summary>
        private void ApplyPattern(
            string word,
            string pattern,
            int[] patternPriorities,
            int[] priorities)
        {
            // Find all positions where the pattern matches
            int pos = 0;
            while (pos <= word.Length - pattern.Length)
            {
                int matchPos = word.IndexOf(pattern, pos, StringComparison.Ordinal);
                if (matchPos < 0)
                    break;

                // Apply pattern priorities at this position
                for (int i = 0; i < patternPriorities.Length; i++)
                {
                    var patternValue = patternPriorities[i];
                    var targetIndex = matchPos + i;

                    if (targetIndex < priorities.Length)
                    {
                        // Keep the maximum priority value
                        if (patternValue > priorities[targetIndex])
                        {
                            priorities[targetIndex] = patternValue;
                        }
                    }
                }

                // Continue searching for more matches
                pos = matchPos + 1;
            }
        }

        /// <summary>
        /// Removes non-alphabetic characters from a word.
        /// </summary>
        private string CleanWord(string word)
        {
            var sb = new StringBuilder(word.Length);
            foreach (var ch in word)
            {
                if (char.IsLetter(ch))
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Helper method to get a description of where a word can be hyphenated.
        /// Useful for debugging and testing.
        /// </summary>
        /// <param name="word">The word to analyze</param>
        /// <returns>String showing hyphenation points with '-' characters</returns>
        public string ShowHyphenationPoints(string word)
        {
            var points = FindHyphenationPoints(word);
            if (points.Length == 0)
                return word;

            var sb = new StringBuilder(word.Length + points.Length);
            int lastIndex = 0;

            foreach (var point in points)
            {
                sb.Append(word.Substring(lastIndex, point - lastIndex));
                sb.Append('-');
                lastIndex = point;
            }

            sb.Append(word.Substring(lastIndex));
            return sb.ToString();
        }
    }
}
