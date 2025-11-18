namespace Folly.Xslfo;

/// <summary>
/// Represents the fo:index-range-begin element.
/// Marks the beginning of an index range for automatic index generation.
/// </summary>
public sealed class FoIndexRangeBegin : FoElement
{
    /// <inheritdoc/>
    public override string Name => "index-range-begin";

    /// <summary>
    /// Gets the unique identifier for this index range.
    /// Required property.
    /// </summary>
    public string Id => Properties.GetString("id", "");

    /// <summary>
    /// Gets the index key that this range belongs to.
    /// Required property.
    /// </summary>
    public string IndexKey => Properties.GetString("index-key", "");

    /// <summary>
    /// Gets the index class, used to group index entries.
    /// Optional property.
    /// </summary>
    public string IndexClass => Properties.GetString("index-class", "");
}

/// <summary>
/// Represents the fo:index-range-end element.
/// Marks the end of an index range for automatic index generation.
/// </summary>
public sealed class FoIndexRangeEnd : FoElement
{
    /// <inheritdoc/>
    public override string Name => "index-range-end";

    /// <summary>
    /// Gets the ref-id that references the corresponding index-range-begin element.
    /// Required property.
    /// </summary>
    public string RefId => Properties.GetString("ref-id", "");
}

/// <summary>
/// Represents the fo:index-key-reference element.
/// References an index key and generates a list of page numbers where the key appears.
/// This is the main element used in index generation to create index entries.
/// </summary>
public sealed class FoIndexKeyReference : FoElement
{
    /// <inheritdoc/>
    public override string Name => "index-key-reference";

    /// <summary>
    /// Gets the index key to reference.
    /// Required property.
    /// </summary>
    public string RefIndexKey => Properties.GetString("ref-index-key", "");

    /// <summary>
    /// Gets the index class to filter by.
    /// Optional property.
    /// </summary>
    public string IndexClass => Properties.GetString("index-class", "");

    /// <summary>
    /// Gets the font family.
    /// </summary>
    public string FontFamily => GetComputedProperty("font-family", "Helvetica") ?? "Helvetica";

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public double FontSize
    {
        get
        {
            var value = GetComputedProperty("font-size", "12pt");
            if (string.IsNullOrEmpty(value)) return 12;
            return LengthParser.Parse(value);
        }
    }

    /// <summary>
    /// Gets the font weight (normal, bold, 100-900).
    /// </summary>
    public string FontWeight => GetComputedProperty("font-weight", "normal") ?? "normal";

    /// <summary>
    /// Gets the font style (normal, italic, oblique).
    /// </summary>
    public string FontStyle => GetComputedProperty("font-style", "normal") ?? "normal";

    /// <summary>
    /// Gets the text color in CSS format.
    /// </summary>
    public string Color => GetComputedProperty("color", "black") ?? "black";

    /// <summary>
    /// Gets the page number prefix elements.
    /// </summary>
    public IReadOnlyList<FoIndexPageNumberPrefix> PageNumberPrefixes { get; init; } = Array.Empty<FoIndexPageNumberPrefix>();

    /// <summary>
    /// Gets the page number suffix elements.
    /// </summary>
    public IReadOnlyList<FoIndexPageNumberSuffix> PageNumberSuffixes { get; init; } = Array.Empty<FoIndexPageNumberSuffix>();

    /// <summary>
    /// Gets the page citation list elements.
    /// </summary>
    public IReadOnlyList<FoIndexPageCitationList> PageCitationLists { get; init; } = Array.Empty<FoIndexPageCitationList>();
}

/// <summary>
/// Represents the fo:index-page-number-prefix element.
/// Provides a prefix to be added before page numbers in index entries (e.g., "p. " or "pp. ").
/// </summary>
public sealed class FoIndexPageNumberPrefix : FoElement
{
    /// <inheritdoc/>
    public override string Name => "index-page-number-prefix";

    /// <summary>
    /// Gets the text content of this prefix.
    /// </summary>
    public string Text => TextContent ?? "";

    /// <summary>
    /// Gets the font family.
    /// </summary>
    public string FontFamily => GetComputedProperty("font-family", "Helvetica") ?? "Helvetica";

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public double FontSize
    {
        get
        {
            var value = GetComputedProperty("font-size", "12pt");
            if (string.IsNullOrEmpty(value)) return 12;
            return LengthParser.Parse(value);
        }
    }

    /// <summary>
    /// Gets the font weight (normal, bold, 100-900).
    /// </summary>
    public string FontWeight => GetComputedProperty("font-weight", "normal") ?? "normal";

    /// <summary>
    /// Gets the font style (normal, italic, oblique).
    /// </summary>
    public string FontStyle => GetComputedProperty("font-style", "normal") ?? "normal";

    /// <summary>
    /// Gets the text color in CSS format.
    /// </summary>
    public string Color => GetComputedProperty("color", "black") ?? "black";
}

/// <summary>
/// Represents the fo:index-page-number-suffix element.
/// Provides a suffix to be added after page numbers in index entries.
/// </summary>
public sealed class FoIndexPageNumberSuffix : FoElement
{
    /// <inheritdoc/>
    public override string Name => "index-page-number-suffix";

    /// <summary>
    /// Gets the text content of this suffix.
    /// </summary>
    public string Text => TextContent ?? "";

    /// <summary>
    /// Gets the font family.
    /// </summary>
    public string FontFamily => GetComputedProperty("font-family", "Helvetica") ?? "Helvetica";

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public double FontSize
    {
        get
        {
            var value = GetComputedProperty("font-size", "12pt");
            if (string.IsNullOrEmpty(value)) return 12;
            return LengthParser.Parse(value);
        }
    }

    /// <summary>
    /// Gets the font weight (normal, bold, 100-900).
    /// </summary>
    public string FontWeight => GetComputedProperty("font-weight", "normal") ?? "normal";

    /// <summary>
    /// Gets the font style (normal, italic, oblique).
    /// </summary>
    public string FontStyle => GetComputedProperty("font-style", "normal") ?? "normal";

    /// <summary>
    /// Gets the text color in CSS format.
    /// </summary>
    public string Color => GetComputedProperty("color", "black") ?? "black";
}

/// <summary>
/// Represents the fo:index-page-citation-list element.
/// Container for page citations in an index entry, handles formatting of multiple page references.
/// </summary>
public sealed class FoIndexPageCitationList : FoElement
{
    /// <inheritdoc/>
    public override string Name => "index-page-citation-list";

    /// <summary>
    /// Gets whether to merge consecutive page numbers into a range.
    /// Default is "no-merge".
    /// </summary>
    public string MergeSequentialPageNumbers => Properties.GetString("merge-sequential-page-numbers", "no-merge");

    /// <summary>
    /// Gets whether to merge pages across index ranges.
    /// Default is "no-merge".
    /// </summary>
    public string MergePagesAcrossIndexKeyReferences => Properties.GetString("merge-pages-across-index-key-references", "no-merge");

    /// <summary>
    /// Gets the merge range threshold.
    /// Default is "2" (merge if 2 or more consecutive pages).
    /// </summary>
    public int MergeRangesMinimumLength
    {
        get
        {
            var value = Properties.GetString("merge-ranges-minimum-length", "2");
            return int.TryParse(value, out var result) ? result : 2;
        }
    }

    /// <summary>
    /// Gets the page citation list separators.
    /// </summary>
    public IReadOnlyList<FoIndexPageCitationListSeparator> ListSeparators { get; init; } = Array.Empty<FoIndexPageCitationListSeparator>();

    /// <summary>
    /// Gets the page citation range separators.
    /// </summary>
    public IReadOnlyList<FoIndexPageCitationRangeSeparator> RangeSeparators { get; init; } = Array.Empty<FoIndexPageCitationRangeSeparator>();
}

/// <summary>
/// Represents the fo:index-page-citation-list-separator element.
/// Defines the separator between individual page citations in an index entry (e.g., ", ").
/// </summary>
public sealed class FoIndexPageCitationListSeparator : FoElement
{
    /// <inheritdoc/>
    public override string Name => "index-page-citation-list-separator";

    /// <summary>
    /// Gets the text content of this separator.
    /// </summary>
    public string Text => TextContent ?? ", ";

    /// <summary>
    /// Gets the font family.
    /// </summary>
    public string FontFamily => GetComputedProperty("font-family", "Helvetica") ?? "Helvetica";

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public double FontSize
    {
        get
        {
            var value = GetComputedProperty("font-size", "12pt");
            if (string.IsNullOrEmpty(value)) return 12;
            return LengthParser.Parse(value);
        }
    }

    /// <summary>
    /// Gets the font weight (normal, bold, 100-900).
    /// </summary>
    public string FontWeight => GetComputedProperty("font-weight", "normal") ?? "normal";

    /// <summary>
    /// Gets the font style (normal, italic, oblique).
    /// </summary>
    public string FontStyle => GetComputedProperty("font-style", "normal") ?? "normal";

    /// <summary>
    /// Gets the text color in CSS format.
    /// </summary>
    public string Color => GetComputedProperty("color", "black") ?? "black";
}

/// <summary>
/// Represents the fo:index-page-citation-range-separator element.
/// Defines the separator between start and end page numbers in a page range (e.g., "–" or "-").
/// </summary>
public sealed class FoIndexPageCitationRangeSeparator : FoElement
{
    /// <inheritdoc/>
    public override string Name => "index-page-citation-range-separator";

    /// <summary>
    /// Gets the text content of this separator.
    /// </summary>
    public string Text => TextContent ?? "–";

    /// <summary>
    /// Gets the font family.
    /// </summary>
    public string FontFamily => GetComputedProperty("font-family", "Helvetica") ?? "Helvetica";

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public double FontSize
    {
        get
        {
            var value = GetComputedProperty("font-size", "12pt");
            if (string.IsNullOrEmpty(value)) return 12;
            return LengthParser.Parse(value);
        }
    }

    /// <summary>
    /// Gets the font weight (normal, bold, 100-900).
    /// </summary>
    public string FontWeight => GetComputedProperty("font-weight", "normal") ?? "normal";

    /// <summary>
    /// Gets the font style (normal, italic, oblique).
    /// </summary>
    public string FontStyle => GetComputedProperty("font-style", "normal") ?? "normal";

    /// <summary>
    /// Gets the text color in CSS format.
    /// </summary>
    public string Color => GetComputedProperty("color", "black") ?? "black";
}
