using System;

namespace Folly.Fonts.Logging;

/// <summary>
/// Represents a logger for diagnostic and error messages.
/// This interface allows the library to report errors and warnings without taking a dependency
/// on a specific logging framework.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs a debug message.
    /// </summary>
    void Debug(string message);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    void Info(string message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    void Warning(string message);

    /// <summary>
    /// Logs a warning with an associated exception.
    /// </summary>
    void Warning(string message, Exception exception);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    void Error(string message);

    /// <summary>
    /// Logs an error with an associated exception.
    /// </summary>
    void Error(string message, Exception exception);
}
