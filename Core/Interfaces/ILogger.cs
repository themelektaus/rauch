namespace Rauch.Core;

/// <summary>
/// Interface for logging with different log levels
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs an informational message
    /// </summary>
    void Info(string message);

    /// <summary>
    /// Logs a success message
    /// </summary>
    void Success(string message);

    /// <summary>
    /// Logs a warning
    /// </summary>
    void Warning(string message);

    /// <summary>
    /// Logs an error
    /// </summary>
    void Error(string message);

    /// <summary>
    /// Logs a debug message
    /// </summary>
    void Debug(string message);

    /// <summary>
    /// Writes a message without additional formatting
    /// </summary>
    void Write(string message);

    /// <summary>
    /// Writes a line without additional formatting
    /// </summary>
    void WriteLine(string message);
}
