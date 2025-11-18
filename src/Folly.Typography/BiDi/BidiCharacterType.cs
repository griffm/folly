namespace Folly.Typography.BiDi;

/// <summary>
/// Unicode Bidirectional Character Types as defined in UAX#9.
/// See: https://www.unicode.org/reports/tr9/#Table_Bidirectional_Character_Types
/// </summary>
public enum BidiCharacterType
{
    /// <summary>
    /// Left-to-Right - Most alphabetic and syllabic characters (Latin, CJK, etc.)
    /// </summary>
    L,

    /// <summary>
    /// Right-to-Left - Hebrew, Arabic letters (not digits or punctuation)
    /// </summary>
    R,

    /// <summary>
    /// Arabic Letter - Arabic-Indic digits, Arabic letter characters
    /// </summary>
    AL,

    /// <summary>
    /// European Number - ASCII digits, Eastern Arabic-Indic digits, etc.
    /// </summary>
    EN,

    /// <summary>
    /// European Separator - Plus, minus signs
    /// </summary>
    ES,

    /// <summary>
    /// European Terminator - Degree, currency symbols, percent, etc.
    /// </summary>
    ET,

    /// <summary>
    /// Arabic Number - Arabic-Indic digits, Arabic decimal/thousands separators
    /// </summary>
    AN,

    /// <summary>
    /// Common Separator - Colon, comma, period, etc. (in common number context)
    /// </summary>
    CS,

    /// <summary>
    /// Non-Spacing Mark - Non-spacing marks (combining marks)
    /// </summary>
    NSM,

    /// <summary>
    /// Boundary Neutral - Default ignorables, non-characters, control characters (except for those explicitly given types)
    /// </summary>
    BN,

    /// <summary>
    /// Paragraph Separator - Paragraph separator, appropriate Newline Functions, higher-level protocol paragraph determination
    /// </summary>
    B,

    /// <summary>
    /// Segment Separator - Tab
    /// </summary>
    S,

    /// <summary>
    /// Whitespace - Space, figure space, line separator, etc.
    /// </summary>
    WS,

    /// <summary>
    /// Other Neutral - All other characters (punctuation, symbols, etc.)
    /// </summary>
    ON,

    /// <summary>
    /// Left-to-Right Embedding - U+202A
    /// </summary>
    LRE,

    /// <summary>
    /// Left-to-Right Override - U+202D
    /// </summary>
    LRO,

    /// <summary>
    /// Right-to-Left Embedding - U+202B
    /// </summary>
    RLE,

    /// <summary>
    /// Right-to-Left Override - U+202E
    /// </summary>
    RLO,

    /// <summary>
    /// Pop Directional Format - U+202C
    /// </summary>
    PDF,

    /// <summary>
    /// Left-to-Right Isolate - U+2066
    /// </summary>
    LRI,

    /// <summary>
    /// Right-to-Left Isolate - U+2067
    /// </summary>
    RLI,

    /// <summary>
    /// First Strong Isolate - U+2068
    /// </summary>
    FSI,

    /// <summary>
    /// Pop Directional Isolate - U+2069
    /// </summary>
    PDI
}
