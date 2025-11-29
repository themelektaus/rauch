namespace Rauch.Core;

/// <summary>
/// Interface for logging with different log levels
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs an informational message
    /// </summary>
    void Info(string message, bool newLine = true);

    /// <summary>
    /// Logs a success message
    /// </summary>
    void Success(string message, bool newLine = true, bool preventSound = false);

    /// <summary>
    /// Logs a warning
    /// </summary>
    void Warning(string message, bool newLine = true, bool preventSound = false);

    /// <summary>
    /// Logs an error
    /// </summary>
    void Error(string message, bool newLine = true, bool preventSound = false);

    /// <summary>
    /// Logs a debug message
    /// </summary>
    void Debug(string message, bool newLine = true);

    void Write(string message = "", bool newLine = true, ConsoleColor? color = null);

    void Exit(int exitCode);

    string Question(string message, string[] possibleValues = null, string defaultValue = null, bool allowEmpty = false);

    int Choice(string message, string[] items, int defaultIndex = 0);
}
