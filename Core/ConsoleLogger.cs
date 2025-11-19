namespace Rauch.Core;

/// <summary>
/// Console logger implementation with color support
/// </summary>
public class ConsoleLogger : ILogger
{
    readonly bool _enableColors;

    public ConsoleLogger(bool enableColors = true)
    {
        _enableColors = enableColors;
    }

    public void Info(string message)
    {
        WriteLineColored(message, ConsoleColor.Cyan);
    }

    public void Success(string message)
    {
        WriteLineColored(message, ConsoleColor.Green);
    }

    public void Warning(string message)
    {
        WriteLineColored(message, ConsoleColor.Yellow);
    }

    public void Error(string message)
    {
        WriteLineColored(message, ConsoleColor.Red);
    }

    public void Debug(string message)
    {
        WriteLineColored(message, ConsoleColor.DarkGray);
    }

    public void Write(string message, ConsoleColor? color = null)
    {
        if (color.HasValue)
        {
            WriteColored(message, color.Value);
        }
        else
        {
            Console.Write(message);
        }
    }

    public void WriteLine(string message, ConsoleColor? color = null)
    {
        if (color.HasValue)
        {
            WriteLineColored(message, color.Value);
        }
        else
        {
            Console.WriteLine(message);
        }
    }

    void WriteColored(string message, ConsoleColor color)
    {
        if (_enableColors)
        {
            var previousColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.Write(message);
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

    void WriteLineColored(string message, ConsoleColor color)
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

    public void Exit(int exitCode)
    {
        var message = $"Exit Code {exitCode}";

        if (exitCode == 0)
        {
            Success(message);
        }
        else
        {
            Error(message);
        }
    }
}
