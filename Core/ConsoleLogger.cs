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

    public void Info(string message, bool newLine = true)
    {
        Write(message, newLine, ConsoleColor.Cyan);
    }

    public void Success(string message, bool newLine = true)
    {
        Write(message, newLine, ConsoleColor.Green);
    }

    public void Warning(string message, bool newLine = true)
    {
        Write(message, newLine, ConsoleColor.Yellow);
    }

    public void Error(string message, bool newLine = true)
    {
        Write(message, newLine, ConsoleColor.Red);
    }

    public void Debug(string message, bool newLine = true)
    {

#if DEBUG
        Write(message, newLine, ConsoleColor.DarkGray);
#endif
    }

    public void Write(string message = "", bool newLine = true, ConsoleColor? color = null)
    {
        if (color.HasValue)
        {
            if (_enableColors)
            {
                var previousColor = Console.ForegroundColor;
                try
                {
                    Console.ForegroundColor = color.Value;
                    Write();
                }
                finally
                {
                    Console.ForegroundColor = previousColor;
                }
            }
            else
            {
                Write();
            }
        }
        else
        {
            Write();
        }

        void Write()
        {
            if (newLine)
            {
                Console.WriteLine(message);
            }
            else
            {
                Console.Write(message);
            }
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
        Write();
        Write(message + " ", newLine: false);

        var values = possibleValues?.ToList() ?? [];

        if (values.Count == 0)
        {
            if (defaultValue is not null)
            {
                Write($"[{defaultValue}] ", newLine: false);
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

            Write($"({string.Join("/", values)}) ", newLine: false);
        }

        string input = string.Empty;

        var (x, y) = Console.GetCursorPosition();

        for (; ; )
        {
            var (endX, _) = Console.GetCursorPosition();

            Console.SetCursorPosition(x, y);
            for (var i = 0; i < input.Length; i++)
            {
                Write(" ", newLine: false);
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

