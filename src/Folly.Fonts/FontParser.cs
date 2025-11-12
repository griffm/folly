using System;
using System.IO;
using Folly.Fonts.Models;
using Folly.Fonts.Tables;

namespace Folly.Fonts;

/// <summary>
/// High-level API for parsing TrueType and OpenType font files.
/// This is the main entry point for font file loading.
/// </summary>
public class FontParser
{
    /// <summary>
    /// Parses a font file from a file path.
    /// </summary>
    /// <param name="filePath">Path to the .ttf or .otf font file.</param>
    /// <returns>Parsed font file with all metrics and mappings.</returns>
    public static FontFile Parse(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Parse(stream);
    }

    /// <summary>
    /// Parses a font file from a stream.
    /// </summary>
    /// <param name="stream">Stream containing the font data.</param>
    /// <returns>Parsed font file with all metrics and mappings.</returns>
    public static FontFile Parse(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanSeek)
            throw new ArgumentException("Stream must be seekable", nameof(stream));

        var font = new FontFile();

        try
        {
            // Step 1: Read table directory
            var directory = FontFileReader.ReadTableDirectory(stream);

            // Validate required tables are present
            ValidateRequiredTables(directory);

            // Step 2: Parse tables in dependency order

            // Parse 'head' first (needed by many other tables)
            if (directory.HasTable("head"))
            {
                var headTable = directory.GetTable("head")!;
                HeadTableParser.Parse(stream, headTable, font);
            }

            // Parse 'maxp' (needed for glyph count)
            if (directory.HasTable("maxp"))
            {
                var maxpTable = directory.GetTable("maxp")!;
                MaxpTableParser.Parse(stream, maxpTable, font);
            }

            // Parse 'hhea' (needed for horizontal metrics count)
            if (directory.HasTable("hhea"))
            {
                var hheaTable = directory.GetTable("hhea")!;
                HheaTableParser.Parse(stream, hheaTable, font);
            }

            // Parse 'hmtx' (depends on hhea and maxp)
            if (directory.HasTable("hmtx"))
            {
                var hmtxTable = directory.GetTable("hmtx")!;
                HmtxTableParser.Parse(stream, hmtxTable, font);
            }

            // Parse 'name' (font naming information)
            if (directory.HasTable("name"))
            {
                var nameTable = directory.GetTable("name")!;
                NameTableParser.Parse(stream, nameTable, font);
            }

            // Parse 'cmap' (character to glyph mapping)
            if (directory.HasTable("cmap"))
            {
                var cmapTable = directory.GetTable("cmap")!;
                CmapTableParser.Parse(stream, cmapTable, font);
            }

            // Parse 'loca' (glyph locations for TrueType fonts only)
            if (directory.HasTable("loca") && font.IsTrueType)
            {
                var locaTable = directory.GetTable("loca")!;
                LocaTableParser.Parse(stream, locaTable, font);
            }

            // Parse 'post' (PostScript information)
            if (directory.HasTable("post"))
            {
                var postTable = directory.GetTable("post")!;
                PostTableParser.Parse(stream, postTable, font);
            }

            // Parse 'OS/2' (Windows metrics)
            if (directory.HasTable("OS/2"))
            {
                var os2Table = directory.GetTable("OS/2")!;
                OS2TableParser.Parse(stream, os2Table, font);
            }

            // TODO: Parse 'kern' table for kerning pairs
            // TODO: Parse 'GPOS' table for advanced positioning (OpenType)
            // TODO: Parse 'GSUB' table for glyph substitution (OpenType)
            // TODO: Parse 'CFF ' table for OpenType/CFF fonts

            return font;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to parse font file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validates that all required tables are present in the font.
    /// </summary>
    private static void ValidateRequiredTables(TableDirectory directory)
    {
        // Required tables for all fonts
        string[] requiredTables = { "head", "hhea", "hmtx", "maxp", "name", "cmap" };

        foreach (var tableName in requiredTables)
        {
            if (!directory.HasTable(tableName))
            {
                throw new InvalidDataException($"Required table '{tableName}' is missing");
            }
        }

        // TrueType fonts require 'loca' and 'glyf'
        // OpenType/CFF fonts require 'CFF '
        bool hasTrueTypeOutlines = directory.HasTable("glyf") && directory.HasTable("loca");
        bool hasCffOutlines = directory.HasTable("CFF ");

        if (!hasTrueTypeOutlines && !hasCffOutlines)
        {
            throw new InvalidDataException(
                "Font must have either TrueType outlines ('glyf' and 'loca') " +
                "or CFF outlines ('CFF ')");
        }
    }

    /// <summary>
    /// Quick check if a file appears to be a valid font file without full parsing.
    /// </summary>
    /// <param name="filePath">Path to the potential font file.</param>
    /// <returns>True if the file has a valid font signature.</returns>
    public static bool IsValidFontFile(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            return IsValidFontFile(stream);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Quick check if a stream appears to be a valid font file without full parsing.
    /// </summary>
    /// <param name="stream">Stream containing potential font data.</param>
    /// <returns>True if the stream has a valid font signature.</returns>
    public static bool IsValidFontFile(Stream stream)
    {
        try
        {
            if (stream.Length < 12)
                return false;

            using var reader = new BigEndianBinaryReader(stream, leaveOpen: true);
            uint sfntVersion = reader.ReadUInt32();

            return sfntVersion == 0x00010000  // TrueType
                || sfntVersion == 0x4F54544F  // 'OTTO' - OpenType/CFF
                || sfntVersion == 0x74727565  // 'true' - Apple TrueType
                || sfntVersion == 0x74797031; // 'typ1' - Type 1
        }
        catch
        {
            return false;
        }
    }
}
