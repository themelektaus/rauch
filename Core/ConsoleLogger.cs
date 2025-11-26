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

    public string Question(string message, string[] possibleValues = null, string defaultValue = null, bool allowEmpty = false)
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
                else if (allowEmpty)
                {
                    input = string.Empty;
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

    public int Choice(string message, string[] items, int defaultIndex = 0)
    {
        Write();
        var menu = new ChoiceMenu();
        menu.AddItems(items);
        Write($"{message} (", newLine: false);
        menu.Write();
        Write(")", newLine: false);
        menu.SetIndex(defaultIndex);
        var index = menu.ReadIndex();
        menu.WriteResult();
        return index;
    }

    class ChoiceMenu
    {
        public string separator = "/";
        public int x;
        public int y;

        readonly List<ChoiceItem> items = [];

        public void AddItems(params string[] items)
        {
            foreach (var item in items)
            {
                this.items.Add(new()
                {
                    menu = this,
                    text = item
                });
            }
        }

        public void Write()
        {
            (x, y) = Console.GetCursorPosition();

            foreach (var item in items)
            {
                item.Write();

                if (items.LastOrDefault() != item)
                {
                    Console.Write(separator);
                }
            }
        }

        public void WriteResult()
        {
            Console.SetCursorPosition(x - 1, y);
            var text = $" {items.FirstOrDefault(x => x.selected).text}";
            var width = GetWidth();
            while (text.Length <= width)
            {
                text += " ";
            }
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(text);
            Console.ForegroundColor = color;
        }

        public int ReadIndex()
        {
            Console.CursorVisible = false;

            for (; ; )
            {
                var index = items.FindIndex(x => x.selected);

                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.LeftArrow)
                {
                    SetIndex(Math.Max(0, index - 1));
                }
                else if (key.Key == ConsoleKey.RightArrow)
                {
                    SetIndex(Math.Min(items.Count - 1, index + 1));
                }

                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
            }

            Console.CursorVisible = true;
            return items.FindIndex(x => x.selected);
        }

        int GetWidth()
        {
            return items.Sum(i => i.text.Length) + items.Count * separator.Length;
        }

        public (int x, int y) GetItemPosition(ChoiceItem item)
        {
            var x = this.x;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] != item)
                {
                    x += items[i].text.Length + separator.Length;
                }
                else
                {
                    return (x, y);
                }
            }
            throw new("item not found");
        }

        public void SetIndex(int index)
        {
            var item = items.FirstOrDefault(x => x.selected);
            if (item is not null)
            {
                item.selected = false;
                item.Write();
            }

            item = items[index];
            item.selected = true;
            item.Write();
        }
    }

    class ChoiceItem
    {
        public ChoiceMenu menu;
        public string text;
        public bool selected;

        public void Write()
        {
            var (x, y) = menu.GetItemPosition(this);
            Console.SetCursorPosition(x, y);

            if (selected)
            {
                var foreground = Console.ForegroundColor;
                var background = Console.BackgroundColor;
                Console.ForegroundColor = background;
                Console.BackgroundColor = foreground;
                Console.Write(text);
                Console.ForegroundColor = foreground;
                Console.BackgroundColor = background;
            }
            else
            {
                Console.Write(text);
            }
        }
    }
}
