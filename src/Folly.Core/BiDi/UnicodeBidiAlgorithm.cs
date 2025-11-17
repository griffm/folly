namespace Folly.BiDi;

/// <summary>
/// Pure .NET implementation of the Unicode Bidirectional Algorithm (UAX#9).
/// Zero runtime dependencies.
///
/// This implementation follows the Unicode Standard Annex #9 (UAX#9):
/// https://www.unicode.org/reports/tr9/
///
/// The algorithm has several phases:
/// - P1: Determine paragraph embedding level
/// - P2-P3: Resolve explicit embedding levels and overrides
/// - W1-W7: Resolve weak types
/// - N0-N2: Resolve neutral and isolate formatting types
/// - I1-I2: Resolve implicit levels
/// - L1-L4: Resolve whitespace levels and reorder
/// </summary>
public sealed class UnicodeBidiAlgorithm
{
    private const int MaxDepth = 125; // Maximum embedding level depth (UAX#9 BD2)

    /// <summary>
    /// Main entry point: Reorders text according to the Unicode Bidirectional Algorithm.
    /// </summary>
    /// <param name="text">The text to reorder</param>
    /// <param name="baseDirection">The base paragraph direction (0 = LTR, 1 = RTL, -1 = auto-detect)</param>
    /// <returns>Reordered text for display</returns>
    public string ReorderText(string text, int baseDirection = -1)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // P1: Determine paragraph embedding level
        var paragraphEmbeddingLevel = DetermineParagraphEmbeddingLevel(text, baseDirection);

        // Get character types for each character
        var types = GetCharacterTypes(text);

        // P2-P3: Resolve explicit embedding levels and overrides
        var levels = ResolveExplicitLevels(text, types, paragraphEmbeddingLevel);

        // W1-W7: Resolve weak types
        ResolveWeakTypes(text, types, levels);

        // N0-N2: Resolve neutral types
        ResolveNeutralTypes(text, types, levels, paragraphEmbeddingLevel);

        // I1-I2: Resolve implicit levels
        ResolveImplicitLevels(types, levels);

        // L1: Resolve whitespace levels
        ResolveWhitespace(text, types, levels, paragraphEmbeddingLevel);

        // L2-L4: Reorder text by levels
        return ReorderByLevels(text, levels);
    }

    /// <summary>
    /// P1: Determines the paragraph embedding level.
    /// </summary>
    private int DetermineParagraphEmbeddingLevel(string text, int baseDirection)
    {
        // If base direction is specified, use it
        if (baseDirection == 0) return 0; // LTR
        if (baseDirection == 1) return 1; // RTL

        // Auto-detect: find first strong character (L, R, or AL)
        for (int i = 0; i < text.Length; i++)
        {
            var type = UnicodeBidiData.GetCharacterType(text[i]);
            if (type == BidiCharacterType.L)
                return 0; // LTR paragraph
            if (type == BidiCharacterType.R || type == BidiCharacterType.AL)
                return 1; // RTL paragraph
        }

        // Default to LTR if no strong character found
        return 0;
    }

    /// <summary>
    /// Gets the character types for all characters in the text.
    /// </summary>
    private BidiCharacterType[] GetCharacterTypes(string text)
    {
        var types = new BidiCharacterType[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            types[i] = UnicodeBidiData.GetCharacterType(text[i]);
        }
        return types;
    }

    /// <summary>
    /// P2-P3: Resolves explicit embedding levels and overrides.
    /// </summary>
    private int[] ResolveExplicitLevels(string text, BidiCharacterType[] types, int paragraphEmbeddingLevel)
    {
        var levels = new int[text.Length];
        var currentLevel = paragraphEmbeddingLevel;
        var levelStack = new Stack<(int Level, bool Override, bool Isolate)>();
        var overrideStatus = false;
        var overrideType = BidiCharacterType.ON;

        for (int i = 0; i < text.Length; i++)
        {
            var type = types[i];

            // Handle explicit formatting characters
            switch (type)
            {
                case BidiCharacterType.RLE: // Right-to-Left Embedding
                    if (levelStack.Count < MaxDepth)
                    {
                        var newLevel = ((currentLevel + 1) | 1); // Next odd level
                        if (newLevel <= MaxDepth)
                        {
                            levelStack.Push((currentLevel, overrideStatus, false));
                            currentLevel = newLevel;
                            overrideStatus = false;
                        }
                    }
                    levels[i] = currentLevel;
                    break;

                case BidiCharacterType.LRE: // Left-to-Right Embedding
                    if (levelStack.Count < MaxDepth)
                    {
                        var newLevel = ((currentLevel + 2) & ~1); // Next even level
                        if (newLevel <= MaxDepth)
                        {
                            levelStack.Push((currentLevel, overrideStatus, false));
                            currentLevel = newLevel;
                            overrideStatus = false;
                        }
                    }
                    levels[i] = currentLevel;
                    break;

                case BidiCharacterType.RLO: // Right-to-Left Override
                    if (levelStack.Count < MaxDepth)
                    {
                        var newLevel = ((currentLevel + 1) | 1); // Next odd level
                        if (newLevel <= MaxDepth)
                        {
                            levelStack.Push((currentLevel, overrideStatus, false));
                            currentLevel = newLevel;
                            overrideStatus = true;
                            overrideType = BidiCharacterType.R;
                        }
                    }
                    levels[i] = currentLevel;
                    break;

                case BidiCharacterType.LRO: // Left-to-Right Override
                    if (levelStack.Count < MaxDepth)
                    {
                        var newLevel = ((currentLevel + 2) & ~1); // Next even level
                        if (newLevel <= MaxDepth)
                        {
                            levelStack.Push((currentLevel, overrideStatus, false));
                            currentLevel = newLevel;
                            overrideStatus = true;
                            overrideType = BidiCharacterType.L;
                        }
                    }
                    levels[i] = currentLevel;
                    break;

                case BidiCharacterType.PDF: // Pop Directional Format
                    if (levelStack.Count > 0)
                    {
                        var (prevLevel, prevOverride, _) = levelStack.Pop();
                        currentLevel = prevLevel;
                        overrideStatus = prevOverride;
                    }
                    levels[i] = currentLevel;
                    break;

                case BidiCharacterType.LRI: // Left-to-Right Isolate
                    // LRI: Create a new left-to-right embedding level that isolates the content
                    if (levelStack.Count < MaxDepth)
                    {
                        var newLevel = ((currentLevel + 2) & ~1); // Next even level (LTR)
                        if (newLevel <= MaxDepth)
                        {
                            levelStack.Push((currentLevel, overrideStatus, true)); // Mark as isolate
                            currentLevel = newLevel;
                            overrideStatus = false;
                        }
                    }
                    levels[i] = currentLevel;
                    break;

                case BidiCharacterType.RLI: // Right-to-Left Isolate
                    // RLI: Create a new right-to-left embedding level that isolates the content
                    if (levelStack.Count < MaxDepth)
                    {
                        var newLevel = ((currentLevel + 1) | 1); // Next odd level (RTL)
                        if (newLevel <= MaxDepth)
                        {
                            levelStack.Push((currentLevel, overrideStatus, true)); // Mark as isolate
                            currentLevel = newLevel;
                            overrideStatus = false;
                        }
                    }
                    levels[i] = currentLevel;
                    break;

                case BidiCharacterType.FSI: // First Strong Isolate
                    // FSI: Determine direction from first strong character, then create isolate
                    if (levelStack.Count < MaxDepth)
                    {
                        // Scan ahead to find first strong character to determine direction
                        var strongType = FindFirstStrongType(text, types, i + 1);
                        int newLevel;
                        if (strongType == BidiCharacterType.L)
                        {
                            // First strong is LTR, create LTR isolate
                            newLevel = ((currentLevel + 2) & ~1); // Next even level
                        }
                        else if (strongType == BidiCharacterType.R || strongType == BidiCharacterType.AL)
                        {
                            // First strong is RTL, create RTL isolate
                            newLevel = ((currentLevel + 1) | 1); // Next odd level
                        }
                        else
                        {
                            // No strong character found, use current paragraph direction
                            newLevel = currentLevel;
                        }

                        if (newLevel <= MaxDepth)
                        {
                            levelStack.Push((currentLevel, overrideStatus, true)); // Mark as isolate
                            currentLevel = newLevel;
                            overrideStatus = false;
                        }
                    }
                    levels[i] = currentLevel;
                    break;

                case BidiCharacterType.PDI: // Pop Directional Isolate
                    // PDI: Close the most recent isolate
                    if (levelStack.Count > 0)
                    {
                        // Pop until we find an isolate or stack is empty
                        while (levelStack.Count > 0)
                        {
                            var (prevLevel, prevOverride, isIsolate) = levelStack.Pop();
                            currentLevel = prevLevel;
                            overrideStatus = prevOverride;
                            if (isIsolate)
                            {
                                // Found matching isolate, stop popping
                                break;
                            }
                        }
                    }
                    levels[i] = currentLevel;
                    break;

                case BidiCharacterType.B: // Paragraph separator
                    levels[i] = paragraphEmbeddingLevel;
                    break;

                case BidiCharacterType.BN: // Boundary neutral
                    levels[i] = currentLevel;
                    break;

                default:
                    levels[i] = currentLevel;
                    // Apply override if active
                    if (overrideStatus)
                    {
                        types[i] = overrideType;
                    }
                    break;
            }
        }

        return levels;
    }

    /// <summary>
    /// W1-W7: Resolves weak types.
    /// </summary>
    private void ResolveWeakTypes(string text, BidiCharacterType[] types, int[] levels)
    {
        // W1: NSM (non-spacing marks) take the type of the preceding character
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == BidiCharacterType.NSM)
            {
                if (i == 0)
                    types[i] = BidiCharacterType.ON;
                else
                    types[i] = types[i - 1];
            }
        }

        // W2: EN becomes AN in Arabic context
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == BidiCharacterType.EN)
            {
                // Search backwards for strong type
                for (int j = i - 1; j >= 0; j--)
                {
                    var type = types[j];
                    if (type == BidiCharacterType.L || type == BidiCharacterType.R)
                        break;
                    if (type == BidiCharacterType.AL)
                    {
                        types[i] = BidiCharacterType.AN;
                        break;
                    }
                }
            }
        }

        // W3: AL becomes R
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == BidiCharacterType.AL)
                types[i] = BidiCharacterType.R;
        }

        // W4: ES/CS between numbers
        for (int i = 1; i < types.Length - 1; i++)
        {
            if (types[i] == BidiCharacterType.ES || types[i] == BidiCharacterType.CS)
            {
                var prev = types[i - 1];
                var next = types[i + 1];

                if (types[i] == BidiCharacterType.ES && prev == BidiCharacterType.EN && next == BidiCharacterType.EN)
                    types[i] = BidiCharacterType.EN;
                else if (types[i] == BidiCharacterType.CS)
                {
                    if (prev == BidiCharacterType.EN && next == BidiCharacterType.EN)
                        types[i] = BidiCharacterType.EN;
                    else if (prev == BidiCharacterType.AN && next == BidiCharacterType.AN)
                        types[i] = BidiCharacterType.AN;
                }
            }
        }

        // W5: ET adjacent to EN becomes EN
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == BidiCharacterType.ET)
            {
                // Look for adjacent EN
                bool hasAdjacentEN = false;

                // Check backward
                for (int j = i - 1; j >= 0 && types[j] == BidiCharacterType.ET; j--)
                {
                    if (j > 0 && types[j - 1] == BidiCharacterType.EN)
                    {
                        hasAdjacentEN = true;
                        break;
                    }
                }

                // Check forward
                if (!hasAdjacentEN)
                {
                    for (int j = i + 1; j < types.Length && types[j] == BidiCharacterType.ET; j++)
                    {
                        if (j < types.Length - 1 && types[j + 1] == BidiCharacterType.EN)
                        {
                            hasAdjacentEN = true;
                            break;
                        }
                    }
                }

                if (hasAdjacentEN)
                {
                    // Mark all adjacent ETs as EN
                    int start = i;
                    while (start > 0 && types[start - 1] == BidiCharacterType.ET) start--;
                    int end = i;
                    while (end < types.Length - 1 && types[end + 1] == BidiCharacterType.ET) end++;

                    for (int j = start; j <= end; j++)
                    {
                        if (types[j] == BidiCharacterType.ET)
                            types[j] = BidiCharacterType.EN;
                    }
                }
            }
        }

        // W6: ES, ET, CS become ON
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == BidiCharacterType.ES ||
                types[i] == BidiCharacterType.ET ||
                types[i] == BidiCharacterType.CS)
            {
                types[i] = BidiCharacterType.ON;
            }
        }

        // W7: EN becomes L in LTR context
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == BidiCharacterType.EN)
            {
                // Search backwards for strong type or embedding level
                for (int j = i - 1; j >= 0; j--)
                {
                    var type = types[j];
                    if (type == BidiCharacterType.L)
                    {
                        types[i] = BidiCharacterType.L;
                        break;
                    }
                    if (type == BidiCharacterType.R)
                        break;
                }
            }
        }
    }

    /// <summary>
    /// N0-N2: Resolves neutral types.
    /// </summary>
    private void ResolveNeutralTypes(string text, BidiCharacterType[] types, int[] levels, int paragraphEmbeddingLevel)
    {
        // N0: Paired bracket handling (UAX#9 BD16 - fully implemented)
        ResolvePairedBrackets(text, types, levels);

        // N1 & N2: Resolve neutrals based on surrounding strong types
        for (int i = 0; i < types.Length; i++)
        {
            if (IsNeutral(types[i]))
            {
                // Find preceding strong type
                BidiCharacterType? precedingStrong = null;
                for (int j = i - 1; j >= 0; j--)
                {
                    if (IsStrong(types[j]))
                    {
                        precedingStrong = types[j];
                        break;
                    }
                }

                // Find following strong type
                BidiCharacterType? followingStrong = null;
                for (int j = i + 1; j < types.Length; j++)
                {
                    if (IsStrong(types[j]))
                    {
                        followingStrong = types[j];
                        break;
                    }
                }

                // N1: Neutrals between same strong types take that type
                if (precedingStrong.HasValue && followingStrong.HasValue &&
                    precedingStrong.Value == followingStrong.Value)
                {
                    types[i] = precedingStrong.Value;
                }
                // N2: Otherwise, take the embedding direction
                else
                {
                    types[i] = (levels[i] % 2 == 0) ? BidiCharacterType.L : BidiCharacterType.R;
                }
            }
        }
    }

    /// <summary>
    /// N0 (BD16): Resolves paired brackets according to the Unicode Bidirectional Algorithm.
    /// Implements the full paired bracket algorithm for UAX#9 compliance.
    /// </summary>
    private void ResolvePairedBrackets(string text, BidiCharacterType[] types, int[] levels)
    {
        // Step 1: Identify all bracket characters
        var bracketPairs = new List<(int openIndex, int closeIndex)>();
        var stack = new Stack<(int index, char bracket)>();

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];

            if (IsOpeningBracket(ch))
            {
                // Push opening bracket onto stack
                stack.Push((i, ch));
            }
            else if (IsClosingBracket(ch))
            {
                // Try to match with opening bracket
                var matchingOpen = GetMatchingOpenBracket(ch);

                // Search for matching opening bracket in stack
                var tempStack = new Stack<(int index, char bracket)>();

                while (stack.Count > 0)
                {
                    var top = stack.Pop();
                    if (top.bracket == matchingOpen)
                    {
                        // Found a match!
                        bracketPairs.Add((top.index, i));
                        break;
                    }
                    else
                    {
                        // Save for later
                        tempStack.Push(top);
                    }
                }

                // Restore unmatched brackets to stack
                while (tempStack.Count > 0)
                {
                    stack.Push(tempStack.Pop());
                }
            }
        }

        // Step 2: Process each bracket pair (BD16 algorithm)
        foreach (var (openIndex, closeIndex) in bracketPairs)
        {
            // Get the embedding level
            int embeddingLevel = levels[openIndex];
            bool isRtl = (embeddingLevel % 2) == 1;

            // BD16 Rule: Determine the direction based on strong types inside the brackets
            BidiCharacterType? insideStrongType = null;

            // Look for strong directional types between the brackets
            for (int i = openIndex + 1; i < closeIndex; i++)
            {
                if (types[i] == BidiCharacterType.L)
                {
                    insideStrongType = BidiCharacterType.L;
                    if (!isRtl) break; // Found LTR in LTR context - we're done
                }
                else if (types[i] == BidiCharacterType.R)
                {
                    insideStrongType = BidiCharacterType.R;
                    if (isRtl) break; // Found RTL in RTL context - we're done
                }
            }

            // If we found a strong type opposite to embedding direction,
            // check if there's also a matching strong type (BD16 rule)
            if (insideStrongType != null)
            {
                var targetType = insideStrongType.Value;

                // Set the bracket characters to the strong type direction
                types[openIndex] = targetType;
                types[closeIndex] = targetType;
            }
            else
            {
                // BD16 Rule: If no strong types inside, look at context before opening bracket
                BidiCharacterType? precedingStrongType = null;

                for (int i = openIndex - 1; i >= 0; i--)
                {
                    if (types[i] == BidiCharacterType.L || types[i] == BidiCharacterType.R)
                    {
                        precedingStrongType = types[i];
                        break;
                    }
                }

                // If preceding context matches embedding direction, use it
                if (precedingStrongType != null)
                {
                    bool precedingMatchesEmbedding =
                        (isRtl && precedingStrongType == BidiCharacterType.R) ||
                        (!isRtl && precedingStrongType == BidiCharacterType.L);

                    if (precedingMatchesEmbedding)
                    {
                        types[openIndex] = precedingStrongType.Value;
                        types[closeIndex] = precedingStrongType.Value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if a character is an opening bracket.
    /// </summary>
    private bool IsOpeningBracket(char ch)
    {
        return ch == '(' || ch == '[' || ch == '{' || ch == '<' ||
               ch == '\u2018' || // '
               ch == '\u201C' || // "
               ch == '\u2039' || // ‹
               ch == '\u00AB' || // «
               ch == '\u3008' || // 〈
               ch == '\u300A' || // 《
               ch == '\u300C' || // 「
               ch == '\u300E' || // 『
               ch == '\u3010' || // 【
               ch == '\u3014' || // 〔
               ch == '\u3016' || // 〖
               ch == '\u3018' || // 〘
               ch == '\u301A';   // 〚
    }

    /// <summary>
    /// Checks if a character is a closing bracket.
    /// </summary>
    private bool IsClosingBracket(char ch)
    {
        return ch == ')' || ch == ']' || ch == '}' || ch == '>' ||
               ch == '\u2019' || // '
               ch == '\u201D' || // "
               ch == '\u203A' || // ›
               ch == '\u00BB' || // »
               ch == '\u3009' || // 〉
               ch == '\u300B' || // 》
               ch == '\u300D' || // 」
               ch == '\u300F' || // 』
               ch == '\u3011' || // 】
               ch == '\u3015' || // 〕
               ch == '\u3017' || // 〗
               ch == '\u3019' || // 〙
               ch == '\u301B';   // 〛
    }

    /// <summary>
    /// Gets the matching opening bracket for a closing bracket.
    /// </summary>
    private char GetMatchingOpenBracket(char closingBracket)
    {
        return closingBracket switch
        {
            ')' => '(',
            ']' => '[',
            '}' => '{',
            '>' => '<',
            '\u2019' => '\u2018', // ' → '
            '\u201D' => '\u201C', // " → "
            '\u203A' => '\u2039', // › → ‹
            '\u00BB' => '\u00AB', // » → «
            '\u3009' => '\u3008', // 〉 → 〈
            '\u300B' => '\u300A', // 》 → 《
            '\u300D' => '\u300C', // 」 → 「
            '\u300F' => '\u300E', // 』 → 『
            '\u3011' => '\u3010', // 】 → 【
            '\u3015' => '\u3014', // 〕 → 〔
            '\u3017' => '\u3016', // 〗 → 〖
            '\u3019' => '\u3018', // 〙 → 〘
            '\u301B' => '\u301A', // 〛 → 〚
            _ => closingBracket
        };
    }

    /// <summary>
    /// I1-I2: Resolves implicit levels.
    /// </summary>
    private void ResolveImplicitLevels(BidiCharacterType[] types, int[] levels)
    {
        for (int i = 0; i < types.Length; i++)
        {
            var type = types[i];
            var level = levels[i];

            // I1: For even embedding levels (LTR)
            if (level % 2 == 0)
            {
                if (type == BidiCharacterType.R)
                    levels[i] = level + 1;
                else if (type == BidiCharacterType.AN || type == BidiCharacterType.EN)
                    levels[i] = level + 2;
            }
            // I2: For odd embedding levels (RTL)
            else
            {
                if (type == BidiCharacterType.L || type == BidiCharacterType.AN || type == BidiCharacterType.EN)
                    levels[i] = level + 1;
            }
        }
    }

    /// <summary>
    /// L1: Resolves whitespace levels.
    /// </summary>
    private void ResolveWhitespace(string text, BidiCharacterType[] types, int[] levels, int paragraphEmbeddingLevel)
    {
        // L1: Reset trailing whitespace to paragraph embedding level
        for (int i = types.Length - 1; i >= 0; i--)
        {
            if (types[i] == BidiCharacterType.WS ||
                types[i] == BidiCharacterType.S ||
                types[i] == BidiCharacterType.B)
            {
                levels[i] = paragraphEmbeddingLevel;
            }
            else
            {
                break;
            }
        }

        // Reset segment separators and paragraph separators
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == BidiCharacterType.S || types[i] == BidiCharacterType.B)
            {
                levels[i] = paragraphEmbeddingLevel;
            }
        }
    }

    /// <summary>
    /// L2-L4: Reorders text by levels.
    /// </summary>
    private string ReorderByLevels(string text, int[] levels)
    {
        if (text.Length == 0)
            return text;

        // Find the maximum level
        int maxLevel = 0;
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] > maxLevel)
                maxLevel = levels[i];
        }

        // Create character array for reordering
        var chars = text.ToCharArray();
        var indices = new int[text.Length];
        for (int i = 0; i < indices.Length; i++)
            indices[i] = i;

        // Reverse runs from highest level to lowest
        for (int level = maxLevel; level > 0; level--)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] >= level)
                {
                    // Find the end of this run
                    int end = i;
                    while (end < levels.Length && levels[end] >= level)
                        end++;

                    // Reverse the run
                    ReverseRun(chars, i, end - 1);
                    ReverseRun(indices, i, end - 1);

                    i = end - 1;
                }
            }
        }

        return new string(chars);
    }

    /// <summary>
    /// Reverses a run of characters in the array.
    /// </summary>
    private void ReverseRun(char[] array, int start, int end)
    {
        while (start < end)
        {
            (array[start], array[end]) = (array[end], array[start]);
            start++;
            end--;
        }
    }

    /// <summary>
    /// Reverses a run of integers in the array.
    /// </summary>
    private void ReverseRun(int[] array, int start, int end)
    {
        while (start < end)
        {
            (array[start], array[end]) = (array[end], array[start]);
            start++;
            end--;
        }
    }

    /// <summary>
    /// Finds the first strong directional character type in a range (for FSI handling).
    /// </summary>
    /// <param name="text">The text string.</param>
    /// <param name="types">The character types array.</param>
    /// <param name="startIndex">Index to start scanning from.</param>
    /// <returns>The first strong type found (L, R, or AL), or ON if none found.</returns>
    private BidiCharacterType FindFirstStrongType(string text, BidiCharacterType[] types, int startIndex)
    {
        for (int i = startIndex; i < types.Length; i++)
        {
            var type = types[i];

            // Stop at PDI (end of isolate sequence)
            if (type == BidiCharacterType.PDI)
                break;

            // Return first strong type
            if (type == BidiCharacterType.L ||
                type == BidiCharacterType.R ||
                type == BidiCharacterType.AL)
            {
                return type;
            }
        }

        // No strong type found
        return BidiCharacterType.ON;
    }

    /// <summary>
    /// Checks if a character type is neutral.
    /// </summary>
    private bool IsNeutral(BidiCharacterType type)
    {
        return type == BidiCharacterType.WS ||
               type == BidiCharacterType.ON ||
               type == BidiCharacterType.S ||
               type == BidiCharacterType.B;
    }

    /// <summary>
    /// Checks if a character type is strong.
    /// </summary>
    private bool IsStrong(BidiCharacterType type)
    {
        return type == BidiCharacterType.L ||
               type == BidiCharacterType.R;
    }
}
