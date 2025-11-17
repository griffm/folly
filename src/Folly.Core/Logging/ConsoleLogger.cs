using System;

namespace Folly.Core.Logging;

/// <summary>
/// A simple logger implementation that writes messages to the console.
/// Useful for debugging and simple applications.
/// </summary>
public sealed class ConsoleLogger : ILogger
{
    private readonly LogLevel _minLevel;

    /// <summary>
    /// Initializes a new instance of the ConsoleLogger class.
    /// </summary>
    /// <param name="minLevel">The minimum log level to output. Defaults to Info.</param>
    public ConsoleLogger(LogLevel minLevel = LogLevel.Info)
    {
        _minLevel = minLevel;
    }

    /// <inheritdoc />
    public void Debug(string message)
    {
        if (_minLevel <= LogLevel.Debug)
            Console.WriteLine($"[DEBUG] {message}");
    }

    /// <inheritdoc />
    public void Info(string message)
    {
        if (_minLevel <= LogLevel.Info)
            Console.WriteLine($"[INFO] {message}");
    }

    /// <inheritdoc />
    public void Warning(string message)
    {
        if (_minLevel <= LogLevel.Warning)
            Console.Error.WriteLine($"[WARNING] {message}");
    }

    /// <inheritdoc />
    public void Warning(string message, Exception exception)
    {
        if (_minLevel <= LogLevel.Warning)
            Console.Error.WriteLine($"[WARNING] {message}: {exception.Message}");
    }

    /// <inheritdoc />
    public void Error(string message)
    {
        if (_minLevel <= LogLevel.Error)
            Console.Error.WriteLine($"[ERROR] {message}");
    }

    /// <inheritdoc />
    public void Error(string message, Exception exception)
    {
        if (_minLevel <= LogLevel.Error)
            Console.Error.WriteLine($"[ERROR] {message}: {exception.Message}\n{exception.StackTrace}");
    }
}

/// <summary>
/// Defines logging severity levels.
/// </summary>
public enum LogLevel
{
    /// <summary>Debug-level messages for detailed troubleshooting.</summary>
    Debug = 0,
    /// <summary>Informational messages about normal operation.</summary>
    Info = 1,
    /// <summary>Warning messages about potentially problematic situations.</summary>
    Warning = 2,
    /// <summary>Error messages about failures and exceptions.</summary>
    Error = 3
}
