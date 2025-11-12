using System;
using System.IO;
using Folly.Fonts.Models;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'post' (PostScript) table.
/// This table contains PostScript information including italic angle and fixed pitch flag.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/post
/// </summary>
public static class PostTableParser
{
    /// <summary>
    /// Parses the 'post' table and populates PostScript information.
    /// </summary>
    public static void Parse(Stream stream, TableRecord table, FontFile font)
    {
        using var reader = FontFileReader.CreateTableReader(stream, table);

        var post = new PostTable();

        // Version (Fixed) - typically 1.0, 2.0, 2.5, or 3.0
        post.Version = reader.ReadFixed();

        // Italic angle (Fixed)
        post.ItalicAngle = reader.ReadFixed();

        // Underline position (FWORD/int16)
        post.UnderlinePosition = reader.ReadInt16();

        // Underline thickness (FWORD/int16)
        post.UnderlineThickness = reader.ReadInt16();

        // isFixedPitch (uint32)
        post.IsFixedPitch = reader.ReadUInt32();

        // minMemType42 (uint32) - minimum memory usage when downloaded as Type 42 font
        reader.Skip(4);

        // maxMemType42 (uint32)
        reader.Skip(4);

        // minMemType1 (uint32) - minimum memory usage when downloaded as Type 1 font
        reader.Skip(4);

        // maxMemType1 (uint32)
        reader.Skip(4);

        // Note: Version 2.0 includes glyph name index mapping, but we don't need it
        // for basic font metrics and rendering.

        font.Post = post;
    }
}
