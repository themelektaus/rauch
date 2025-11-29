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
        public bool match;
        public Dictionary<string, CommandInfo> commands = [];
    }

    public class CommandInfo
    {
        public string name;
        public string description;
        public string type;
        public bool match;
    }

    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        WriteTitleLine(logger);

        var commandLines = GetCommandLines(args);

        if (commandLines.Count == 0)
        {
            logger?.Warning("No commands found matching the specified search terms.");
            return Task.CompletedTask;
        }

        foreach (var (key, value) in commandLines)
        {
            if (key is GroupInfo group)
            {
                logger?.Write($"  {group.name,-15}", newLine: false, color: ConsoleColor.Yellow);
                logger?.Write();

                foreach (var command in value)
                {
                    logger?.Write($"    └─ ", newLine: false);
                    logger?.Write($"{command.name,-13} ", newLine: false, color: ConsoleColor.DarkYellow);
                    logger?.Write($"{command.type,-10}", newLine: false, color: ConsoleColor.DarkGray);
                    logger?.Write(command.description, newLine: false);
                    logger?.Write();
                }

                logger?.Write();

                continue;
            }

            if (key is ICommand rootCommand)
            {
                WriteHelpLine(logger, rootCommand);
                continue;
            }

            logger?.Warning("Unexpected key type in command lines.");
        }

        return Task.CompletedTask;
    }

    public static void WriteTitleLine(ILogger logger)
    {
        SoundPlayer.Play("Help");
        logger?.Write();
        logger?.Write(" >_ ", newLine: false, color: ConsoleColor.DarkCyan);
        logger?.Write("rauch", color: ConsoleColor.Cyan);
        logger?.Write();

    }

    public static void WriteHelpLine(ILogger logger, ICommand command)
    {
        var usage = CommandMetadata.GetUsage(command);
        var desc = CommandMetadata.GetDescription(command);
        logger?.Write($"  {usage[6..],-15}", newLine: false, color: ConsoleColor.Yellow);
        logger?.Write(desc);
        logger?.Write();
    }

    public Dictionary<string, GroupInfo> GetGroups(string[] args)
    {
        var groups = new Dictionary<string, GroupInfo>(StringComparer.OrdinalIgnoreCase);

        // Group commands by namespace (auto-detect groups)
        foreach (var command in _availableCommands)
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
                    match = Filter(args, groupName, matchAll: false),
                };

                groups.Add(groupName, groupInfo);
            }

            var commandName = CommandMetadata.GetName(command);

            if (!groupInfo.commands.ContainsKey(commandName))
            {
                groupInfo.commands.Add(commandName, new()
                {
                    name = commandName,
                    description = CommandMetadata.GetDescription(command),
                    type = CommandLoader.IsPlugin(command) ? "Plugin" : "Core",
                    match = Filter(args, commandName, matchAll: !groupInfo.match)
                });
            }
        }

        return groups;
    }

    public IEnumerable<ICommand> EnumerateRootCommands(string[] args)
    {
        foreach (var command in _availableCommands.Where(c => !CommandLoader.IsGroupedCommand(c)).OrderBy(CommandMetadata.GetName))
        {
            var commandName = CommandMetadata.GetName(command);
            if (Filter(args, commandName, matchAll: true))
            {
                yield return command;
            }
        }
    }

    public Dictionary<object, List<CommandInfo>> GetCommandLines(string[] args)
    {
        var commandLines = new Dictionary<object, List<CommandInfo>>();

        var groups = GetGroups(args);
        var hasMatchingCommand = groups.Any(x => x.Value.commands.Values.Any(c => c.match));

        foreach (var group in groups.Values.OrderBy(x => x.name))
        {
            if (!group.match && !group.commands.Values.Any(x => x.match))
            {
                continue;
            }

            var commands = group.commands.Values.Where(x => !hasMatchingCommand || x.match).ToList();
            if (commands.Count == 0)
            {
                continue;
            }

            commandLines[group] = [];

            foreach (var command in commands.OrderBy(x => x.name))
            {
                commandLines[group].Add(command);
            }
        }

        foreach (var command in EnumerateRootCommands(args))
        {
            commandLines.Add(command, null);
        }

        return commandLines;
    }

    static List<string> GetSearchTerms(string[] args)
    {
        return [.. args.Where(x => !string.IsNullOrWhiteSpace(x))];
    }

    static bool Filter(string[] args, string term, bool matchAll)
    {
        var searchTerms = GetSearchTerms(args);

        if (searchTerms.Count > 0)
        {
            if (matchAll)
            {
                if (!searchTerms.All(x => term.Contains(x, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }
            else
            {
                if (!searchTerms.Any(x => term.Contains(x, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
