using System.Text;

namespace TestResourceGenerator;

class Program
{
    static void Main(string[] args)
    {
        var outputDir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..",
            "Folly.UnitTests", "TestResources", "Images");

        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Test Resource Generator for Folly v1.0");
        Console.WriteLine($"Output directory: {outputDir}");
        Console.WriteLine();

        // Generate DPI test images
        GenerateDpiTestImages(outputDir);

        // Generate CMYK and ICC profile test images
        GenerateCmykTestImages(outputDir);

        Console.WriteLine();
        Console.WriteLine("✅ All test resources generated successfully!");
        Console.WriteLine($"📁 Location: {outputDir}");
        Console.WriteLine($"📊 Files created: {Directory.GetFiles(outputDir).Length}");
    }

    static void GenerateDpiTestImages(string outputDir)
    {
        Console.WriteLine("Generating DPI test images...");

        // JPEG images with various DPIs
        GenerateJpegWithDpi(Path.Combine(outputDir, "test-72dpi.jpg"), 100, 100, 72, 72);
        GenerateJpegWithDpi(Path.Combine(outputDir, "test-96dpi.jpg"), 100, 100, 96, 96);
        GenerateJpegWithDpi(Path.Combine(outputDir, "test-150dpi.jpg"), 100, 100, 150, 150);
        GenerateJpegWithDpi(Path.Combine(outputDir, "test-300dpi.jpg"), 100, 100, 300, 300);

        // JPEG without DPI metadata
        GenerateJpegWithoutDpi(Path.Combine(outputDir, "no-dpi-metadata.jpg"), 100, 100);

        // PNG with pHYs chunk
        GeneratePngWithDpi(Path.Combine(outputDir, "test-300dpi.png"), 100, 100, 300, 300);

        Console.WriteLine("  ✅ 6 DPI test images created");
    }

    static void GenerateCmykTestImages(string outputDir)
    {
        Console.WriteLine("Generating CMYK and ICC profile test images...");

        // CMYK JPEG
        GenerateCmykJpeg(Path.Combine(outputDir, "test-cmyk.jpg"), 200, 200);

        // RGB JPEG with ICC profile
        GenerateJpegWithIccProfile(Path.Combine(outputDir, "jpeg-with-icc.jpg"), 200, 200);

        // PNG with iCCP chunk
        GeneratePngWithIccProfile(Path.Combine(outputDir, "png-with-iccp.png"), 200, 200);

        // RGB JPEG without ICC (control)
        GenerateJpegWithDpi(Path.Combine(outputDir, "test-rgb.jpg"), 200, 200, 72, 72);

        Console.WriteLine("  ✅ 4 CMYK/ICC test images created");
    }

    static void GenerateJpegWithDpi(string path, int width, int height, int dpiX, int dpiY)
    {
        using var stream = File.Create(path);

        // JPEG SOI (Start of Image)
        stream.WriteByte(0xFF);
        stream.WriteByte(0xD8);

        // JFIF APP0 marker with DPI
        WriteJfifApp0(stream, dpiX, dpiY);

        // Minimal JPEG image data (2x2 solid blue)
        WriteMinimalJpegData(stream, width, height, 3); // RGB

        // JPEG EOI (End of Image)
        stream.WriteByte(0xFF);
        stream.WriteByte(0xD9);

        Console.WriteLine($"  Created: {Path.GetFileName(path)} ({dpiX}×{dpiY} DPI)");
    }

    static void GenerateJpegWithoutDpi(string path, int width, int height)
    {
        using var stream = File.Create(path);

        // JPEG SOI
        stream.WriteByte(0xFF);
        stream.WriteByte(0xD8);

        // Skip JFIF marker - write minimal JPEG data directly
        WriteMinimalJpegData(stream, width, height, 3);

        // JPEG EOI
        stream.WriteByte(0xFF);
        stream.WriteByte(0xD9);

        Console.WriteLine($"  Created: {Path.GetFileName(path)} (no DPI metadata)");
    }

    static void GenerateCmykJpeg(string path, int width, int height)
    {
        using var stream = File.Create(path);

        // JPEG SOI
        stream.WriteByte(0xFF);
        stream.WriteByte(0xD8);

        // Adobe APP14 marker (indicates CMYK)
        WriteAdobeApp14(stream, true); // true = CMYK

        // Minimal JPEG data with 4 components (CMYK)
        WriteMinimalJpegData(stream, width, height, 4); // CMYK

        // JPEG EOI
        stream.WriteByte(0xFF);
        stream.WriteByte(0xD9);

        Console.WriteLine($"  Created: {Path.GetFileName(path)} (CMYK)");
    }

    static void GenerateJpegWithIccProfile(string path, int width, int height)
    {
        using var stream = File.Create(path);

        // JPEG SOI
        stream.WriteByte(0xFF);
        stream.WriteByte(0xD8);

        // JFIF APP0
        WriteJfifApp0(stream, 72, 72);

        // ICC Profile APP2 marker
        WriteIccProfileApp2(stream);

        // Minimal JPEG data
        WriteMinimalJpegData(stream, width, height, 3);

        // JPEG EOI
        stream.WriteByte(0xFF);
        stream.WriteByte(0xD9);

        Console.WriteLine($"  Created: {Path.GetFileName(path)} (with ICC profile)");
    }

    static void GeneratePngWithDpi(string path, int width, int height, int dpiX, int dpiY)
    {
        using var stream = File.Create(path);

        // PNG signature
        stream.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // IHDR chunk
        WriteIhdrChunk(stream, width, height);

        // pHYs chunk (DPI to pixels per meter)
        var ppmX = (int)(dpiX / 0.0254); // DPI to pixels per meter
        var ppmY = (int)(dpiY / 0.0254);
        WritePhysChunk(stream, ppmX, ppmY);

        // IDAT chunk (compressed image data)
        WriteIdatChunk(stream, width, height);

        // IEND chunk
        WriteIendChunk(stream);

        Console.WriteLine($"  Created: {Path.GetFileName(path)} (PNG {dpiX}×{dpiY} DPI)");
    }

    static void GeneratePngWithIccProfile(string path, int width, int height)
    {
        using var stream = File.Create(path);

        // PNG signature
        stream.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // IHDR chunk
        WriteIhdrChunk(stream, width, height);

        // iCCP chunk (compressed ICC profile)
        WriteIccpChunk(stream);

        // IDAT chunk
        WriteIdatChunk(stream, width, height);

        // IEND chunk
        WriteIendChunk(stream);

        Console.WriteLine($"  Created: {Path.GetFileName(path)} (PNG with iCCP)");
    }

    // Helper methods for JPEG

    static void WriteJfifApp0(Stream stream, int dpiX, int dpiY)
    {
        // APP0 marker
        stream.WriteByte(0xFF);
        stream.WriteByte(0xE0);

        // Length (16 bytes)
        stream.WriteByte(0x00);
        stream.WriteByte(0x10);

        // JFIF identifier
        stream.Write(Encoding.ASCII.GetBytes("JFIF\0"));

        // Version 1.01
        stream.WriteByte(0x01);
        stream.WriteByte(0x01);

        // Density units (1 = dots per inch)
        stream.WriteByte(0x01);

        // X density
        stream.WriteByte((byte)(dpiX >> 8));
        stream.WriteByte((byte)(dpiX & 0xFF));

        // Y density
        stream.WriteByte((byte)(dpiY >> 8));
        stream.WriteByte((byte)(dpiY & 0xFF));

        // Thumbnail width/height (0 = no thumbnail)
        stream.WriteByte(0x00);
        stream.WriteByte(0x00);
    }

    static void WriteAdobeApp14(Stream stream, bool isCmyk)
    {
        // APP14 marker
        stream.WriteByte(0xFF);
        stream.WriteByte(0xEE);

        // Length (14 bytes)
        stream.WriteByte(0x00);
        stream.WriteByte(0x0E);

        // Adobe identifier
        stream.Write(Encoding.ASCII.GetBytes("Adobe"));

        // Version (100)
        stream.WriteByte(0x00);
        stream.WriteByte(0x64);

        // Flags
        stream.WriteByte(0x00);
        stream.WriteByte(0x00);
        stream.WriteByte(0x00);
        stream.WriteByte(0x00);

        // Transform (0 = CMYK, 1 = YCbCr)
        stream.WriteByte((byte)(isCmyk ? 0x00 : 0x01));
    }

    static void WriteIccProfileApp2(Stream stream)
    {
        // APP2 marker
        stream.WriteByte(0xFF);
        stream.WriteByte(0xE2);

        // Minimal sRGB ICC profile (simplified for testing)
        var iccData = CreateMinimalSrgbProfile();

        // Length (2 bytes for length + ICC_PROFILE\0 + sequence + data)
        var length = 2 + 12 + 2 + iccData.Length;
        stream.WriteByte((byte)(length >> 8));
        stream.WriteByte((byte)(length & 0xFF));

        // ICC_PROFILE identifier
        stream.Write(Encoding.ASCII.GetBytes("ICC_PROFILE\0"));

        // Sequence number (1) and total chunks (1)
        stream.WriteByte(0x01);
        stream.WriteByte(0x01);

        // ICC profile data
        stream.Write(iccData);
    }

    static byte[] CreateMinimalSrgbProfile()
    {
        // Minimal ICC profile header for sRGB
        // This is a simplified version for testing purposes
        var profile = new byte[128];

        // Profile size (128 bytes)
        profile[0] = 0x00;
        profile[1] = 0x00;
        profile[2] = 0x00;
        profile[3] = 0x80;

        // Preferred CMM type
        Array.Copy(Encoding.ASCII.GetBytes("scnr"), 0, profile, 4, 4);

        // Profile version
        profile[8] = 0x02;
        profile[9] = 0x10;

        // Profile class
        Array.Copy(Encoding.ASCII.GetBytes("mntr"), 0, profile, 12, 4);

        // Color space (RGB)
        Array.Copy(Encoding.ASCII.GetBytes("RGB "), 0, profile, 16, 4);

        // Connection space (XYZ)
        Array.Copy(Encoding.ASCII.GetBytes("XYZ "), 0, profile, 20, 4);

        return profile;
    }

    static void WriteMinimalJpegData(Stream stream, int width, int height, int components)
    {
        // SOF0 (Start of Frame - Baseline DCT)
        stream.WriteByte(0xFF);
        stream.WriteByte(0xC0);

        // Length
        var length = 8 + (components * 3);
        stream.WriteByte(0x00);
        stream.WriteByte((byte)length);

        // Precision
        stream.WriteByte(0x08);

        // Height
        stream.WriteByte((byte)(height >> 8));
        stream.WriteByte((byte)(height & 0xFF));

        // Width
        stream.WriteByte((byte)(width >> 8));
        stream.WriteByte((byte)(width & 0xFF));

        // Number of components
        stream.WriteByte((byte)components);

        // Component specifications
        for (int i = 0; i < components; i++)
        {
            stream.WriteByte((byte)(i + 1)); // Component ID
            stream.WriteByte(0x11); // Sampling factors
            stream.WriteByte(0x00); // Quantization table
        }

        // DHT (Define Huffman Table) - minimal
        stream.WriteByte(0xFF);
        stream.WriteByte(0xC4);
        stream.WriteByte(0x00);
        stream.WriteByte(0x1F);
        stream.Write(new byte[29]); // Empty huffman table

        // SOS (Start of Scan)
        stream.WriteByte(0xFF);
        stream.WriteByte(0xDA);
        stream.WriteByte(0x00);
        stream.WriteByte(0x0C);
        stream.WriteByte((byte)components);

        for (int i = 0; i < components; i++)
        {
            stream.WriteByte((byte)(i + 1));
            stream.WriteByte(0x00);
        }

        stream.WriteByte(0x00); // Start of spectral
        stream.WriteByte(0x3F); // End of spectral
        stream.WriteByte(0x00); // Successive approximation

        // Minimal scan data
        stream.Write(new byte[] { 0x00, 0x01, 0x02, 0x03 });
    }

    // Helper methods for PNG

    static void WriteIhdrChunk(Stream stream, int width, int height)
    {
        var data = new byte[13];
        // Width
        data[0] = (byte)(width >> 24);
        data[1] = (byte)(width >> 16);
        data[2] = (byte)(width >> 8);
        data[3] = (byte)(width & 0xFF);

        // Height
        data[4] = (byte)(height >> 24);
        data[5] = (byte)(height >> 16);
        data[6] = (byte)(height >> 8);
        data[7] = (byte)(height & 0xFF);

        // Bit depth
        data[8] = 8;

        // Color type (2 = RGB)
        data[9] = 2;

        // Compression, filter, interlace
        data[10] = 0;
        data[11] = 0;
        data[12] = 0;

        WritePngChunk(stream, "IHDR", data);
    }

    static void WritePhysChunk(Stream stream, int ppmX, int ppmY)
    {
        var data = new byte[9];

        // Pixels per unit, X axis
        data[0] = (byte)(ppmX >> 24);
        data[1] = (byte)(ppmX >> 16);
        data[2] = (byte)(ppmX >> 8);
        data[3] = (byte)(ppmX & 0xFF);

        // Pixels per unit, Y axis
        data[4] = (byte)(ppmY >> 24);
        data[5] = (byte)(ppmY >> 16);
        data[6] = (byte)(ppmY >> 8);
        data[7] = (byte)(ppmY & 0xFF);

        // Unit specifier (1 = meter)
        data[8] = 1;

        WritePngChunk(stream, "pHYs", data);
    }

    static void WriteIccpChunk(Stream stream)
    {
        // Profile name + null byte + compression method + compressed profile
        var profileName = Encoding.ASCII.GetBytes("sRGB");
        var compressedProfile = CompressData(CreateMinimalSrgbProfile());

        var data = new byte[profileName.Length + 1 + 1 + compressedProfile.Length];
        Array.Copy(profileName, 0, data, 0, profileName.Length);
        data[profileName.Length] = 0; // Null terminator
        data[profileName.Length + 1] = 0; // Compression method (0 = deflate)
        Array.Copy(compressedProfile, 0, data, profileName.Length + 2, compressedProfile.Length);

        WritePngChunk(stream, "iCCP", data);
    }

    static void WriteIdatChunk(Stream stream, int width, int height)
    {
        // Create minimal image data (solid blue)
        var scanlines = new List<byte>();
        for (int y = 0; y < height; y++)
        {
            scanlines.Add(0); // Filter type (0 = None)
            for (int x = 0; x < width; x++)
            {
                scanlines.Add(0x00); // R
                scanlines.Add(0x00); // G
                scanlines.Add(0xFF); // B (blue)
            }
        }

        var compressed = CompressData(scanlines.ToArray());
        WritePngChunk(stream, "IDAT", compressed);
    }

    static void WriteIendChunk(Stream stream)
    {
        WritePngChunk(stream, "IEND", Array.Empty<byte>());
    }

    static void WritePngChunk(Stream stream, string type, byte[] data)
    {
        // Length
        var length = data.Length;
        stream.WriteByte((byte)(length >> 24));
        stream.WriteByte((byte)(length >> 16));
        stream.WriteByte((byte)(length >> 8));
        stream.WriteByte((byte)(length & 0xFF));

        // Type
        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);

        // Data
        stream.Write(data);

        // CRC32
        var crc = CalculateCrc32(typeBytes, data);
        stream.WriteByte((byte)(crc >> 24));
        stream.WriteByte((byte)(crc >> 16));
        stream.WriteByte((byte)(crc >> 8));
        stream.WriteByte((byte)(crc & 0xFF));
    }

    static byte[] CompressData(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var deflate = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Compress, true))
        {
            deflate.Write(data, 0, data.Length);
        }

        // Add zlib header (CMF + FLG)
        var compressed = ms.ToArray();
        var result = new byte[compressed.Length + 2];
        result[0] = 0x78; // CMF
        result[1] = 0x9C; // FLG
        Array.Copy(compressed, 0, result, 2, compressed.Length);

        return result;
    }

    static uint CalculateCrc32(byte[] type, byte[] data)
    {
        var crc = 0xFFFFFFFFu;

        foreach (var b in type)
        {
            crc = UpdateCrc32(crc, b);
        }

        foreach (var b in data)
        {
            crc = UpdateCrc32(crc, b);
        }

        return crc ^ 0xFFFFFFFFu;
    }

    static uint UpdateCrc32(uint crc, byte b)
    {
        var c = crc ^ b;
        for (int k = 0; k < 8; k++)
        {
            c = (c & 1) == 1 ? (0xEDB88320 ^ (c >> 1)) : (c >> 1);
        }
        return c;
    }
}
