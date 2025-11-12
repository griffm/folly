using System;
using System.Collections.Generic;

namespace Folly.Fonts.Models;

/// <summary>
/// Represents the table directory of a font file.
/// The table directory is the index of all tables in the font.
/// </summary>
public class TableDirectory
{
    /// <summary>
    /// Font type identifier:
    /// - 0x00010000 for TrueType fonts with TrueType outlines
    /// - 'OTTO' (0x4F54544F) for OpenType fonts with CFF outlines
    /// - 'true' (0x74727565) for Apple TrueType fonts
    /// - 'typ1' (0x74797031) for Type 1 fonts
    /// </summary>
    public uint SfntVersion { get; set; }

    /// <summary>
    /// Number of tables in the font.
    /// </summary>
    public ushort NumTables { get; set; }

    /// <summary>
    /// Table records indexed by table tag (e.g., "head", "name", "cmap").
    /// </summary>
    public Dictionary<string, TableRecord> Tables { get; set; } = new();

    /// <summary>
    /// Checks if the font contains a specific table.
    /// </summary>
    public bool HasTable(string tag)
    {
        return Tables.ContainsKey(tag);
    }

    /// <summary>
    /// Gets a table record by tag, or null if not present.
    /// </summary>
    public TableRecord? GetTable(string tag)
    {
        Tables.TryGetValue(tag, out var table);
        return table;
    }
}

/// <summary>
/// Represents a single table record in the table directory.
/// </summary>
public class TableRecord
{
    /// <summary>
    /// Four-character table identifier (e.g., "head", "name", "cmap").
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Checksum for this table.
    /// </summary>
    public uint CheckSum { get; set; }

    /// <summary>
    /// Offset from beginning of font file.
    /// </summary>
    public uint Offset { get; set; }

    /// <summary>
    /// Length of this table in bytes.
    /// </summary>
    public uint Length { get; set; }

    /// <summary>
    /// Returns a string representation of this table record.
    /// </summary>
    public override string ToString()
    {
        return $"Table '{Tag}': Offset={Offset}, Length={Length}";
    }
}
