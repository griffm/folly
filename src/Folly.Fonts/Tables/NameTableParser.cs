using System;
using System.IO;
using System.Text;
using Folly.Fonts.Models;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'name' (naming) table.
/// This table contains human-readable names for the font (family, style, version, etc.).
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/name
/// </summary>
public static class NameTableParser
{
    // Name IDs as defined in the OpenType spec
    private const ushort NameIdFamilyName = 1;
    private const ushort NameIdSubfamilyName = 2;
    private const ushort NameIdFullFontName = 4;
    private const ushort NameIdVersionString = 5;
    private const ushort NameIdPostScriptName = 6;

    // Platform IDs
    private const ushort PlatformUnicode = 0;
    private const ushort PlatformMacintosh = 1;
    private const ushort PlatformWindows = 3;

    // Windows encoding IDs
    private const ushort WindowsEncodingUnicodeBmp = 1;
    private const ushort WindowsEncodingUnicodeFull = 10;

    /// <summary>
    /// Parses the 'name' table and populates the font file with naming information.
    /// </summary>
    public static void Parse(Stream stream, TableRecord table, FontFile font)
    {
        using var reader = FontFileReader.CreateTableReader(stream, table);

        long tableStart = reader.Position;

        // Version (uint16) - should be 0 or 1
        ushort version = reader.ReadUInt16();

        // Count (uint16) - number of name records
        ushort count = reader.ReadUInt16();

        // Storage offset (uint16) - offset to start of string storage (from start of table)
        ushort storageOffset = reader.ReadUInt16();

        // Read all name records (12 bytes each)
        var nameRecords = new NameRecord[count];
        for (int i = 0; i < count; i++)
        {
            nameRecords[i] = new NameRecord
            {
                PlatformId = reader.ReadUInt16(),
                EncodingId = reader.ReadUInt16(),
                LanguageId = reader.ReadUInt16(),
                NameId = reader.ReadUInt16(),
                Length = reader.ReadUInt16(),
                Offset = reader.ReadUInt16()
            };
        }

        // Extract the names we care about
        // Prefer Windows Unicode names, fall back to Mac/Unicode
        font.FamilyName = GetBestName(stream, tableStart, storageOffset, nameRecords, NameIdFamilyName);
        font.SubfamilyName = GetBestName(stream, tableStart, storageOffset, nameRecords, NameIdSubfamilyName);
        font.FullName = GetBestName(stream, tableStart, storageOffset, nameRecords, NameIdFullFontName);
        font.Version = GetBestName(stream, tableStart, storageOffset, nameRecords, NameIdVersionString);
        font.PostScriptName = GetBestName(stream, tableStart, storageOffset, nameRecords, NameIdPostScriptName);
    }

    private static string GetBestName(
        Stream stream,
        long tableStart,
        ushort storageOffset,
        NameRecord[] records,
        ushort nameId)
    {
        // First, try to find Windows Unicode name
        foreach (var record in records)
        {
            if (record.NameId == nameId &&
                record.PlatformId == PlatformWindows &&
                (record.EncodingId == WindowsEncodingUnicodeBmp ||
                 record.EncodingId == WindowsEncodingUnicodeFull))
            {
                return ReadNameString(stream, tableStart, storageOffset, record, Encoding.BigEndianUnicode);
            }
        }

        // Fall back to Unicode platform
        foreach (var record in records)
        {
            if (record.NameId == nameId && record.PlatformId == PlatformUnicode)
            {
                return ReadNameString(stream, tableStart, storageOffset, record, Encoding.BigEndianUnicode);
            }
        }

        // Fall back to Macintosh (usually ASCII/Roman)
        foreach (var record in records)
        {
            if (record.NameId == nameId && record.PlatformId == PlatformMacintosh)
            {
                // TODO: Macintosh encoding varies by script (Roman, Japanese, etc.)
                // For now, assume ASCII/Roman (encoding 0)
                return ReadNameString(stream, tableStart, storageOffset, record, Encoding.ASCII);
            }
        }

        return string.Empty;
    }

    private static string ReadNameString(
        Stream stream,
        long tableStart,
        ushort storageOffset,
        NameRecord record,
        Encoding encoding)
    {
        using var reader = new BigEndianBinaryReader(stream, leaveOpen: true);

        // Seek to string location
        long stringOffset = tableStart + storageOffset + record.Offset;
        reader.Seek(stringOffset);

        // Read string bytes
        byte[] bytes = reader.ReadBytes(record.Length);

        // Decode using appropriate encoding
        return encoding.GetString(bytes);
    }

    private class NameRecord
    {
        public ushort PlatformId { get; set; }
        public ushort EncodingId { get; set; }
        public ushort LanguageId { get; set; }
        public ushort NameId { get; set; }
        public ushort Length { get; set; }
        public ushort Offset { get; set; }
    }
}
