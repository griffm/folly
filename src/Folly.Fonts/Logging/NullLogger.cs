using System;

namespace Folly.Fonts.Logging;

/// <summary>
/// A logger implementation that discards all messages.
/// Used as a default when no logger is configured.
/// </summary>
public sealed class NullLogger : ILogger
{
    /// <summary>
    /// Gets the singleton instance of the null logger.
    /// </summary>
    public static NullLogger Instance { get; } = new NullLogger();

    private NullLogger() { }

    /// <inheritdoc />
    public void Debug(string message) { }

    /// <inheritdoc />
    public void Info(string message) { }

    /// <inheritdoc />
    public void Warning(string message) { }

    /// <inheritdoc />
    public void Warning(string message, Exception exception) { }

    /// <inheritdoc />
    public void Error(string message) { }

    /// <inheritdoc />
    public void Error(string message, Exception exception) { }
}
