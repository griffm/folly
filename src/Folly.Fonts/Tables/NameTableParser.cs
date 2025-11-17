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

        // Fall back to Macintosh (Mac Roman encoding)
        foreach (var record in records)
        {
            if (record.NameId == nameId && record.PlatformId == PlatformMacintosh)
            {
                // Macintosh platform uses Mac Roman encoding (script code 0)
                // Characters 0-127 are ASCII, 128-255 are Mac-specific (accents, symbols, etc.)
                return ReadNameString(stream, tableStart, storageOffset, record, GetMacintoshEncoding(record.EncodingId));
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the appropriate encoding for Macintosh platform strings.
    /// </summary>
    /// <param name="encodingId">The Macintosh encoding/script ID (0 = Roman, 1 = Japanese, etc.)</param>
    /// <returns>Encoding instance for decoding Macintosh strings.</returns>
    private static Encoding GetMacintoshEncoding(ushort encodingId)
    {
        // For now, only support Mac Roman (encoding 0)
        // Other scripts (Japanese, Traditional Chinese, etc.) would need additional encodings
        if (encodingId == 0)
        {
            return new MacRomanEncoding();
        }

        // Fall back to ASCII for unsupported scripts
        return Encoding.ASCII;
    }

    /// <summary>
    /// Mac Roman encoding implementation.
    /// Characters 0-127 are ASCII, 128-255 map to Mac-specific characters.
    /// </summary>
    private class MacRomanEncoding : Encoding
    {
        // Mac Roman character mappings for bytes 128-255
        private static readonly ushort[] MacRomanHighChars = new ushort[]
        {
            0x00C4, 0x00C5, 0x00C7, 0x00C9, 0x00D1, 0x00D6, 0x00DC, 0x00E1, // 128-135: Ä Å Ç É Ñ Ö Ü á
            0x00E0, 0x00E2, 0x00E4, 0x00E3, 0x00E5, 0x00E7, 0x00E9, 0x00E8, // 136-143: à â ä ã å ç é è
            0x00EA, 0x00EB, 0x00ED, 0x00EC, 0x00EE, 0x00EF, 0x00F1, 0x00F3, // 144-151: ê ë í ì î ï ñ ó
            0x00F2, 0x00F4, 0x00F6, 0x00F5, 0x00FA, 0x00F9, 0x00FB, 0x00FC, // 152-159: ò ô ö õ ú ù û ü
            0x2020, 0x00B0, 0x00A2, 0x00A3, 0x00A7, 0x2022, 0x00B6, 0x00DF, // 160-167: † ° ¢ £ § • ¶ ß
            0x00AE, 0x00A9, 0x2122, 0x00B4, 0x00A8, 0x2260, 0x00C6, 0x00D8, // 168-175: ® © ™ ´ ¨ ≠ Æ Ø
            0x221E, 0x00B1, 0x2264, 0x2265, 0x00A5, 0x00B5, 0x2202, 0x2211, // 176-183: ∞ ± ≤ ≥ ¥ µ ∂ ∑
            0x220F, 0x03C0, 0x222B, 0x00AA, 0x00BA, 0x03A9, 0x00E6, 0x00F8, // 184-191: ∏ π ∫ ª º Ω æ ø
            0x00BF, 0x00A1, 0x00AC, 0x221A, 0x0192, 0x2248, 0x2206, 0x00AB, // 192-199: ¿ ¡ ¬ √ ƒ ≈ ∆ «
            0x00BB, 0x2026, 0x00A0, 0x00C0, 0x00C3, 0x00D5, 0x0152, 0x0153, // 200-207: » … (nbsp) À Ã Õ Œ œ
            0x2013, 0x2014, 0x201C, 0x201D, 0x2018, 0x2019, 0x00F7, 0x25CA, // 208-215: – — " " ' ' ÷ ◊
            0x00FF, 0x0178, 0x2044, 0x20AC, 0x2039, 0x203A, 0xFB01, 0xFB02, // 216-223: ÿ Ÿ ⁄ € ‹ › ﬁ ﬂ
            0x2021, 0x00B7, 0x201A, 0x201E, 0x2030, 0x00C2, 0x00CA, 0x00C1, // 224-231: ‡ · ‚ „ ‰ Â Ê Á
            0x00CB, 0x00C8, 0x00CD, 0x00CE, 0x00CF, 0x00CC, 0x00D3, 0x00D4, // 232-239: Ë È Í Î Ï Ì Ó Ô
            0xF8FF, 0x00D2, 0x00DA, 0x00DB, 0x00D9, 0x0131, 0x02C6, 0x02DC, // 240-247: (Apple logo) Ò Ú Û Ù ı ˆ ˜
            0x00AF, 0x02D8, 0x02D9, 0x02DA, 0x00B8, 0x02DD, 0x02DB, 0x02C7  // 248-255: ¯ ˘ ˙ ˚ ¸ ˝ ˛ ˇ
        };

        public override int GetByteCount(char[] chars, int index, int count)
        {
            throw new NotImplementedException("Mac Roman encoding only supports GetString (decoding)");
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            throw new NotImplementedException("Mac Roman encoding only supports GetString (decoding)");
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return count; // One byte per character
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            for (int i = 0; i < byteCount; i++)
            {
                chars[charIndex + i] = bytes[byteIndex + i] < 128
                    ? (char)bytes[byteIndex + i]  // ASCII passthrough
                    : (char)MacRomanHighChars[bytes[byteIndex + i] - 128]; // Mac Roman mapping
            }
            return byteCount;
        }

        public override int GetMaxByteCount(int charCount)
        {
            return charCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }

        public override string GetString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            char[] chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i] = bytes[i] < 128
                    ? (char)bytes[i]  // ASCII passthrough (0-127)
                    : (char)MacRomanHighChars[bytes[i] - 128]; // Mac Roman mapping (128-255)
            }
            return new string(chars);
        }
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
