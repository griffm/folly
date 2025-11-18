using System.Globalization;

namespace Folly.Typography.BiDi;

/// <summary>
/// Embedded Unicode BiDi character property data.
/// Zero runtime dependencies - all data is compiled into the assembly.
///
/// This implementation uses the Unicode Character Database (UCD) BiDi properties
/// from Unicode 15.0. Character ranges are hardcoded for performance and to
/// maintain zero dependencies.
///
/// See: https://www.unicode.org/Public/UCD/latest/ucd/extracted/DerivedBidiClass.txt
/// </summary>
public static class UnicodeBidiData
{
    /// <summary>
    /// Gets the bidirectional character type for a character.
    /// </summary>
    public static BidiCharacterType GetCharacterType(char ch)
    {
        var codePoint = (int)ch;

        // ============================================
        // Explicit BiDi Formatting Characters
        // ============================================

        // LRE: U+202A
        if (codePoint == 0x202A) return BidiCharacterType.LRE;

        // RLE: U+202B
        if (codePoint == 0x202B) return BidiCharacterType.RLE;

        // PDF: U+202C
        if (codePoint == 0x202C) return BidiCharacterType.PDF;

        // LRO: U+202D
        if (codePoint == 0x202D) return BidiCharacterType.LRO;

        // RLO: U+202E
        if (codePoint == 0x202E) return BidiCharacterType.RLO;

        // LRI: U+2066
        if (codePoint == 0x2066) return BidiCharacterType.LRI;

        // RLI: U+2067
        if (codePoint == 0x2067) return BidiCharacterType.RLI;

        // FSI: U+2068
        if (codePoint == 0x2068) return BidiCharacterType.FSI;

        // PDI: U+2069
        if (codePoint == 0x2069) return BidiCharacterType.PDI;

        // ============================================
        // Strong Types
        // ============================================

        // Hebrew (U+0590..U+05FF)
        if (codePoint >= 0x0590 && codePoint <= 0x05FF)
        {
            // Most Hebrew characters are R
            // U+0591..U+05BD, U+05BF, U+05C1..U+05C2, U+05C4..U+05C5, U+05C7 are NSM (non-spacing marks)
            if ((codePoint >= 0x0591 && codePoint <= 0x05BD) ||
                codePoint == 0x05BF ||
                (codePoint >= 0x05C1 && codePoint <= 0x05C2) ||
                (codePoint >= 0x05C4 && codePoint <= 0x05C5) ||
                codePoint == 0x05C7)
            {
                return BidiCharacterType.NSM;
            }
            return BidiCharacterType.R;
        }

        // Arabic (U+0600..U+06FF)
        if (codePoint >= 0x0600 && codePoint <= 0x06FF)
        {
            // Arabic-Indic digits (U+0660..U+0669)
            if (codePoint >= 0x0660 && codePoint <= 0x0669)
                return BidiCharacterType.AN;

            // Most Arabic characters are AL (Arabic Letter)
            // Some combining marks are NSM
            if ((codePoint >= 0x064B && codePoint <= 0x065F) ||
                codePoint == 0x0670)
            {
                return BidiCharacterType.NSM;
            }

            return BidiCharacterType.AL;
        }

        // Arabic Supplement (U+0750..U+077F)
        if (codePoint >= 0x0750 && codePoint <= 0x077F)
        {
            if (codePoint >= 0x0730 && codePoint <= 0x074A)
                return BidiCharacterType.NSM;
            return BidiCharacterType.AL;
        }

        // Arabic Extended-A (U+08A0..U+08FF)
        if (codePoint >= 0x08A0 && codePoint <= 0x08FF)
        {
            if ((codePoint >= 0x08E3 && codePoint <= 0x08FF))
                return BidiCharacterType.NSM;
            return BidiCharacterType.AL;
        }

        // Arabic Presentation Forms-A (U+FB50..U+FDFF)
        if (codePoint >= 0xFB50 && codePoint <= 0xFDFF)
            return BidiCharacterType.AL;

        // Arabic Presentation Forms-B (U+FE70..U+FEFF)
        if (codePoint >= 0xFE70 && codePoint <= 0xFEFF)
        {
            if (codePoint == 0xFEFF) return BidiCharacterType.BN; // Zero Width No-Break Space
            return BidiCharacterType.AL;
        }

        // ============================================
        // Weak Types
        // ============================================

        // European Numbers (EN): ASCII digits 0-9
        if (codePoint >= '0' && codePoint <= '9')
            return BidiCharacterType.EN;

        // European Number Separator (ES): +, -
        if (codePoint == '+' || codePoint == '-' || codePoint == 0x2212) // Minus sign
            return BidiCharacterType.ES;

        // European Number Terminator (ET): currency symbols, percent, etc.
        if (codePoint == '$' || codePoint == '%' || codePoint == '°' ||
            codePoint == '#' || codePoint == 0x00A2 || codePoint == 0x00A3 || // ¢, £
            codePoint == 0x00A4 || codePoint == 0x00A5 || // ¤, ¥
            codePoint == 0x20AC || codePoint == 0x00B0) // €, °
            return BidiCharacterType.ET;

        // Common Number Separator (CS): comma, period, colon, slash (in number context)
        if (codePoint == ',' || codePoint == '.' || codePoint == ':' ||
            codePoint == '/' || codePoint == 0x060C || codePoint == 0x066B) // Arabic comma, decimal separator
            return BidiCharacterType.CS;

        // ============================================
        // Neutral Types
        // ============================================

        // Paragraph Separator (B): Line Feed, Carriage Return, etc.
        if (codePoint == 0x000A || codePoint == 0x000D || // LF, CR
            codePoint == 0x001C || codePoint == 0x001D || codePoint == 0x001E || // FS, GS, RS
            codePoint == 0x0085 || codePoint == 0x2029) // NEL, Paragraph Separator
            return BidiCharacterType.B;

        // Segment Separator (S): Tab
        if (codePoint == 0x0009) // Tab
            return BidiCharacterType.S;

        // Whitespace (WS): Space, No-Break Space, etc.
        if (codePoint == ' ' || codePoint == 0x000B || codePoint == 0x000C || // Space, VT, FF
            codePoint == 0x00A0 || codePoint == 0x1680 || // NBSP, Ogham Space Mark
            (codePoint >= 0x2000 && codePoint <= 0x200A) || // En Quad..Hair Space
            codePoint == 0x2028 || codePoint == 0x205F || codePoint == 0x3000) // Line Sep, Medium Math Space, Ideographic Space
            return BidiCharacterType.WS;

        // Boundary Neutral (BN): Default ignorables, format characters
        if (codePoint == 0x0000 || codePoint == 0x00AD || // NULL, Soft Hyphen
            (codePoint >= 0x200B && codePoint <= 0x200D) || // Zero Width Space, ZWNJ, ZWJ
            codePoint == 0xFEFF) // Zero Width No-Break Space (BOM)
            return BidiCharacterType.BN;

        // ============================================
        // Other Neutrals (ON): Punctuation, Symbols
        // ============================================

        // Most punctuation and symbols
        if ((codePoint >= '!' && codePoint <= '\'') || // !, ", #, $, %, &, '
            (codePoint >= '*' && codePoint <= '/') || // *, +, comma, -, period, /
            (codePoint >= ';' && codePoint <= '@') || // ;, <, =, >, ?, @
            (codePoint >= '[' && codePoint <= '`') || // [, \, ], ^, _, `
            (codePoint >= '{' && codePoint <= '~')) // {, |, }, ~
        {
            // But some of these are reclassified above (ES, CS, etc.)
            // This is a catch-all for remaining punctuation
            // We already handled specific cases above
        }

        // Extended punctuation and symbols (U+2000..U+206F)
        if (codePoint >= 0x2010 && codePoint <= 0x2027)
            return BidiCharacterType.ON;

        // General Punctuation, Currency Symbols, etc.
        if ((codePoint >= 0x20A0 && codePoint <= 0x20CF) || // Currency Symbols
            (codePoint >= 0x2100 && codePoint <= 0x214F) || // Letterlike Symbols
            (codePoint >= 0x2150 && codePoint <= 0x218F)) // Number Forms
            return BidiCharacterType.ON;

        // ============================================
        // Latin and other LTR scripts (L)
        // ============================================

        // Basic Latin (ASCII letters)
        if ((codePoint >= 'A' && codePoint <= 'Z') ||
            (codePoint >= 'a' && codePoint <= 'z'))
            return BidiCharacterType.L;

        // Latin-1 Supplement (U+00A0..U+00FF)
        if (codePoint >= 0x00C0 && codePoint <= 0x00FF)
            return BidiCharacterType.L;

        // Latin Extended-A (U+0100..U+017F)
        if (codePoint >= 0x0100 && codePoint <= 0x017F)
            return BidiCharacterType.L;

        // Latin Extended-B (U+0180..U+024F)
        if (codePoint >= 0x0180 && codePoint <= 0x024F)
            return BidiCharacterType.L;

        // Greek (U+0370..U+03FF)
        if (codePoint >= 0x0370 && codePoint <= 0x03FF)
        {
            // Combining marks are NSM
            if ((codePoint >= 0x0300 && codePoint <= 0x036F))
                return BidiCharacterType.NSM;
            return BidiCharacterType.L;
        }

        // Cyrillic (U+0400..U+04FF)
        if (codePoint >= 0x0400 && codePoint <= 0x04FF)
            return BidiCharacterType.L;

        // ============================================
        // Use .NET's UnicodeCategory as fallback
        // ============================================

        var category = CharUnicodeInfo.GetUnicodeCategory(ch);

        switch (category)
        {
            // Letters are generally L
            case UnicodeCategory.UppercaseLetter:
            case UnicodeCategory.LowercaseLetter:
            case UnicodeCategory.TitlecaseLetter:
            case UnicodeCategory.ModifierLetter:
            case UnicodeCategory.OtherLetter:
                return BidiCharacterType.L;

            // Non-spacing marks
            case UnicodeCategory.NonSpacingMark:
            case UnicodeCategory.EnclosingMark:
                return BidiCharacterType.NSM;

            // Spacing combining marks
            case UnicodeCategory.SpacingCombiningMark:
                return BidiCharacterType.NSM;

            // Decimal digits (default to EN if not caught above)
            case UnicodeCategory.DecimalDigitNumber:
                return BidiCharacterType.EN;

            // Letter numbers (like Roman numerals)
            case UnicodeCategory.LetterNumber:
                return BidiCharacterType.L;

            // Other numbers
            case UnicodeCategory.OtherNumber:
                return BidiCharacterType.ON;

            // Whitespace
            case UnicodeCategory.SpaceSeparator:
                return BidiCharacterType.WS;

            // Line and paragraph separators
            case UnicodeCategory.LineSeparator:
                return BidiCharacterType.WS;
            case UnicodeCategory.ParagraphSeparator:
                return BidiCharacterType.B;

            // Control characters
            case UnicodeCategory.Control:
                return BidiCharacterType.BN;

            // Format characters
            case UnicodeCategory.Format:
                return BidiCharacterType.BN;

            // Punctuation, symbols, etc.
            case UnicodeCategory.ConnectorPunctuation:
            case UnicodeCategory.DashPunctuation:
            case UnicodeCategory.OpenPunctuation:
            case UnicodeCategory.ClosePunctuation:
            case UnicodeCategory.InitialQuotePunctuation:
            case UnicodeCategory.FinalQuotePunctuation:
            case UnicodeCategory.OtherPunctuation:
            case UnicodeCategory.MathSymbol:
            case UnicodeCategory.CurrencySymbol:
            case UnicodeCategory.ModifierSymbol:
            case UnicodeCategory.OtherSymbol:
                return BidiCharacterType.ON;

            // Surrogates and private use
            case UnicodeCategory.Surrogate:
            case UnicodeCategory.PrivateUse:
                return BidiCharacterType.L; // Default to L

            // Default
            default:
                return BidiCharacterType.ON;
        }
    }

    /// <summary>
    /// Checks if a character is a mirrored bracket or parenthesis.
    /// Used for N0 (paired bracket) processing.
    /// </summary>
    public static bool IsMirroredBracket(char ch)
    {
        return ch == '(' || ch == ')' ||
               ch == '[' || ch == ']' ||
               ch == '{' || ch == '}' ||
               ch == '<' || ch == '>' ||
               ch == 0x2018 || ch == 0x2019 || // ' '
               ch == 0x201C || ch == 0x201D || // " "
               ch == 0x2039 || ch == 0x203A || // ‹ ›
               ch == 0x00AB || ch == 0x00BB;   // « »
    }

    /// <summary>
    /// Gets the mirrored counterpart of a bracket.
    /// </summary>
    public static char GetMirroredBracket(char ch)
    {
        return ch switch
        {
            '(' => ')',
            ')' => '(',
            '[' => ']',
            ']' => '[',
            '{' => '}',
            '}' => '{',
            '<' => '>',
            '>' => '<',
            '\u2018' => '\u2019', // ' → '
            '\u2019' => '\u2018', // ' → '
            '\u201C' => '\u201D', // " → "
            '\u201D' => '\u201C', // " → "
            '\u2039' => '\u203A', // ‹ → ›
            '\u203A' => '\u2039', // › → ‹
            '\u00AB' => '\u00BB', // « → »
            '\u00BB' => '\u00AB', // » → «
            _ => ch
        };
    }
}
