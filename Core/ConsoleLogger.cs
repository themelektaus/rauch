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

    public string Question(string message, string[] possibleValues, string defaultValue)
    {
        Console.Write(message + " ");

        var values = possibleValues?.ToList() ?? [];

        if (values.Count == 0)
        {
            if (defaultValue is not null)
            {
                Console.Write($"[{defaultValue}] ");
            }
        }
        else
        {
            if (defaultValue is not null)
            {
                for (var i = 0; i < values.Count; i++)
                {
                    if (values[i] == defaultValue)
                    {
                        values[i] = $"[{values[i]}]";
                    }
                }
            }

            Console.Write($"({string.Join("/", values)}) ");
        }

        string input = string.Empty;

        var (x, y) = Console.GetCursorPosition();

        for (; ; )
        {
            var (endX, _) = Console.GetCursorPosition();

            Console.SetCursorPosition(x, y);
            for (var i = 0; i < input.Length; i++)
            {
                Console.Write(" ");
            }
            Console.SetCursorPosition(x, y);

            input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                if (defaultValue is not null)
                {
                    input = defaultValue;
                }
                else
                {
                    continue;
                }
            }
            else if (values.Count > 0)
            {
                var index = values.FindIndex(x => string.Equals(x.TrimStart('[').TrimEnd(']').ToLower(), input.Trim().ToLower()));
                if (index == -1)
                {
                    continue;
                }
                else
                {
                    input = values[index].TrimStart('[').TrimEnd(']');
                }
            }

            break;
        }

        return input;
    }
}

