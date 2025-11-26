namespace Rauch.Commands;

[Command("help", "Show help text")]
public class Help : ICommand
{
    private readonly IEnumerable<ICommand> _availableCommands;

    public Help(IEnumerable<ICommand> availableCommands)
    {
        _availableCommands = availableCommands;
    }

    public class GroupInfo
    {
        public string name;
        public bool forceVisible;
        public Dictionary<string, CommandInfo> commands = [];
    }

    public class CommandInfo
    {
        public string name;
        public string description;
        public string type;
    }

    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var searchTerms = args.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        bool Filter(string term)
        {
            if (searchTerms.Count > 0)
            {
                if (!searchTerms.Any(x => term.Contains(x, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            return true;
        }

        bool FilterExact(string term)
        {
            if (searchTerms.Count > 0)
            {
                if (!searchTerms.All(x => term.Equals(x, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            return true;
        }

        var groups = new Dictionary<string, GroupInfo>(StringComparer.OrdinalIgnoreCase);

        // Group commands by namespace (auto-detect groups)
        foreach (var command in _availableCommands.Where(c => !CommandMetadata.IsHidden(c)))
        {
            var groupName = CommandLoader.GetGroupName(command);

            // Skip top-level commands for now (will be added at the end)
            if (groupName == null)
            {
                continue;
            }

            if (!groups.TryGetValue(groupName, out var groupInfo))
            {
                groupInfo = new()
                {
                    name = groupName,
                    forceVisible = Filter(groupName),
                };

                groups.Add(groupName, groupInfo);
            }

            var commandName = CommandMetadata.GetName(command);
            var exactMatch = FilterExact(groupName);

            if (!exactMatch && !Filter(commandName))
            {
                continue;
            }

            // Avoid duplicate command names in same group
            if (!groupInfo.commands.ContainsKey(commandName))
            {
                groupInfo.commands.Add(commandName, new()
                {
                    name = commandName,
                    description = CommandMetadata.GetDescription(command),
                    type = CommandLoader.IsPlugin(command) ? "Plugin" : "Core"
                });
            }
        }

        var logger = services.GetService<ILogger>();

        WriteTitleLine(logger);

        foreach (var group in groups.Values.OrderBy(x => x.name))
        {
            if (!group.forceVisible && group.commands.Count == 0)
            {
                continue;
            }

            logger?.Write($"  {group.name,-15}", newLine: false, color: ConsoleColor.Yellow);
            logger?.Write();

            foreach (var command in group.commands.Values.OrderBy(x => x.name))
            {
                logger?.Write($"    └─ ", newLine: false);
                logger?.Write($"{command.name,-13} ", newLine: false, color: ConsoleColor.DarkYellow);
                logger?.Write($"{command.type,-10}", newLine: false, color: ConsoleColor.DarkGray);
                logger?.Write(command.description, newLine: false);
                logger?.Write();
            }
            logger?.Write();
        }

        // Show top-level commands (not in any group)
        foreach (var command in _availableCommands.Where(c => !CommandLoader.IsGroupedCommand(c) && !CommandMetadata.IsHidden(c)).OrderBy(CommandMetadata.GetName))
        {
            if (Filter(CommandMetadata.GetName(command)))
            {
                WriteHelpLine(logger, command);
            }
        }

        return Task.CompletedTask;
    }

    public void WriteTitleLine(ILogger logger)
    {
        SoundPlayer.PlayWhip();
        logger?.Write();
        logger?.Write(" >_ ", newLine: false, color: ConsoleColor.DarkCyan);
        logger?.Write("rauch", color: ConsoleColor.Cyan);
        logger?.Write();

    }

    public void WriteHelpLine(ILogger logger)
    {
        WriteHelpLine(logger, this);
    }

    static void WriteHelpLine(ILogger logger, ICommand command)
    {
        var usage = CommandMetadata.GetUsage(command);
        var desc = CommandMetadata.GetDescription(command);
        logger?.Write($"  {usage[6..],-15}", newLine: false, color: ConsoleColor.Yellow);
        logger?.Write(desc);
        logger?.Write();
    }
}
