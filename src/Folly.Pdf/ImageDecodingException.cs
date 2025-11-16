namespace Folly.Pdf;

/// <summary>
/// Exception thrown when image decoding fails during PDF generation.
/// </summary>
public class ImageDecodingException : Exception
{
    /// <summary>
    /// Gets the image path that failed to decode.
    /// </summary>
    public string? ImagePath { get; }

    /// <summary>
    /// Gets the detected or expected image format.
    /// </summary>
    public string? ImageFormat { get; }

    /// <summary>
    /// Gets the specific failure reason if known.
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageDecodingException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ImageDecodingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageDecodingException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ImageDecodingException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageDecodingException"/> class with detailed diagnostics.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="imagePath">The path to the image that failed to decode.</param>
    /// <param name="imageFormat">The detected or expected image format.</param>
    /// <param name="failureReason">The specific failure reason.</param>
    /// <param name="innerException">The inner exception.</param>
    public ImageDecodingException(
        string message,
        string? imagePath = null,
        string? imageFormat = null,
        string? failureReason = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ImagePath = imagePath;
        ImageFormat = imageFormat;
        FailureReason = failureReason;
    }

    /// <summary>
    /// Gets a message that describes the current exception with detailed diagnostics.
    /// </summary>
    public override string Message
    {
        get
        {
            var parts = new List<string> { base.Message };

            if (!string.IsNullOrEmpty(ImagePath))
                parts.Add($"Image path: {ImagePath}");

            if (!string.IsNullOrEmpty(ImageFormat))
                parts.Add($"Format: {ImageFormat}");

            if (!string.IsNullOrEmpty(FailureReason))
                parts.Add($"Reason: {FailureReason}");

            return string.Join("; ", parts);
        }
    }
}
