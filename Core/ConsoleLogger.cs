using System;

namespace Rauch.Core;

/// <summary>
/// Console logger implementation with color support
/// </summary>
public class ConsoleLogger : ILogger
{
    private readonly bool _enableColors;

    public ConsoleLogger(bool enableColors = true)
    {
        _enableColors = enableColors;
    }

    public void Info(string message)
    {
        WriteColored(message, ConsoleColor.Cyan);
    }

    public void Success(string message)
    {
        WriteColored(message, ConsoleColor.Green);
    }

    public void Warning(string message)
    {
        WriteColored(message, ConsoleColor.Yellow);
    }

    public void Error(string message)
    {
        WriteColored(message, ConsoleColor.Red);
    }

    public void Debug(string message)
    {
        WriteColored(message, ConsoleColor.DarkGray);
    }

    public void Write(string message)
    {
        Console.Write(message);
    }

    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    private void WriteColored(string message, ConsoleColor color)
    {
        if (_enableColors)
        {
            var previousColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = previousColor;
            }
        }
        else
        {
            Console.WriteLine(message);
        }
    }
}
