using System;
using System.Collections.Generic;

namespace Folly.Fonts.OpenType;

/// <summary>
/// Represents an OpenType layout feature tag (e.g., 'liga', 'kern', 'mark').
/// OpenType features enable advanced typography like ligatures, kerning, and contextual alternates.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/featuretags
/// </summary>
public class OpenTypeFeature
{
    /// <summary>
    /// Four-character feature tag (e.g., 'liga', 'kern', 'smcp').
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// List of lookup indices that implement this feature.
    /// </summary>
    public List<ushort> LookupIndices { get; set; } = new();

    /// <summary>
    /// Common OpenType feature tags
    /// </summary>
    public static class CommonFeatures
    {
        /// <summary>Standard ligatures (fi, fl, ffi, ffl)</summary>
        public const string StandardLigatures = "liga";
        /// <summary>Contextual ligatures</summary>
        public const string ContextualLigatures = "clig";
        /// <summary>Discretionary ligatures</summary>
        public const string DiscretionaryLigatures = "dlig";
        /// <summary>Historical ligatures</summary>
        public const string HistoricalLigatures = "hlig";
        /// <summary>Contextual alternates</summary>
        public const string ContextualAlternates = "calt";
        /// <summary>Stylistic alternates</summary>
        public const string StylisticAlternates = "salt";
        /// <summary>Small capitals</summary>
        public const string SmallCapitals = "smcp";
        /// <summary>Swash alternates</summary>
        public const string Swash = "swsh";

        /// <summary>Arabic initial forms</summary>
        public const string Initial = "init";
        /// <summary>Arabic medial forms</summary>
        public const string Medial = "medi";
        /// <summary>Arabic final forms</summary>
        public const string Final = "fina";
        /// <summary>Arabic isolated forms</summary>
        public const string Isolated = "isol";

        /// <summary>Kerning</summary>
        public const string Kerning = "kern";
        /// <summary>Mark-to-base positioning</summary>
        public const string MarkPositioning = "mark";
        /// <summary>Mark-to-mark positioning</summary>
        public const string MarkToMark = "mkmk";
        /// <summary>Cursive attachment</summary>
        public const string CursiveAttachment = "curs";
    }
}

/// <summary>
/// Represents a script in the OpenType layout system (e.g., Latin, Arabic, Cyrillic).
/// </summary>
public class OpenTypeScript
{
    /// <summary>
    /// Four-character script tag (e.g., 'latn', 'arab', 'cyrl').
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Language systems for this script.
    /// </summary>
    public List<OpenTypeLanguageSystem> LanguageSystems { get; set; } = new();
}

/// <summary>
/// Represents a language system within a script.
/// </summary>
public class OpenTypeLanguageSystem
{
    /// <summary>
    /// Four-character language tag (e.g., 'dflt', 'ENG ', 'ARA ').
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Features enabled for this language system.
    /// </summary>
    public List<ushort> FeatureIndices { get; set; } = new();
}
