namespace Rauch.Commands;

[Command("help", "Show this help text")]
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
        public string description;
        public bool forceVisible;
        public Dictionary<string, CommandInfo> commands;
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

        var groups = new Dictionary<string, GroupInfo>();

        foreach (var group in _availableCommands.OfType<ICommandGroup>().Where(c => !CommandMetadata.IsHidden(c)))
        {
            var groupName = CommandMetadata.GetName(group);
            var groupDescription = CommandMetadata.GetDescription(group);

            if (!groups.TryGetValue(groupName, out var groupInfo))
            {
                groupInfo = new()
                {
                    name = groupName,
                    description = groupDescription,
                    forceVisible = Filter(groupName),
                    commands = []
                };

                groups.Add(groupName, groupInfo);
            }

            var exactMatch = FilterExact(groupName);

            foreach (var command in group.SubCommands)
            {
                var commandName = CommandMetadata.GetName(command);

                if (!exactMatch && !Filter(commandName))
                {
                    continue;
                }

                groupInfo.commands.Add(commandName, new()
                {
                    name = commandName,
                    description = CommandMetadata.GetDescription(command),
                    type = command.GetType().Namespace.Contains(".Plugins.") ? "Plugin" : "Core"
                });
            }
        }

        var logger = services.GetService<ILogger>();

        logger?.Write();
        logger?.Write(" >_ ", newLine: false, color: ConsoleColor.DarkCyan);
        logger?.Write("rauch", color: ConsoleColor.Cyan);
        logger?.Write();

        foreach (var group in groups.Values.OrderBy(x => x.name))
        {
            if (!group.forceVisible && group.commands.Count == 0)
            {
                continue;
            }

            logger?.Write($"  {group.name,-15}", newLine: false, color: ConsoleColor.Yellow);
            logger?.Write(group.description, newLine: false);
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

        foreach (var command in _availableCommands.Where(c => c is not ICommandGroup && !CommandMetadata.IsHidden(c)).OrderBy(c => CommandMetadata.GetName(c)))
        {
            if (!Filter(CommandMetadata.GetName(command)))
            {
                continue;
            }

            var usage = CommandMetadata.GetUsage(command);
            var desc = CommandMetadata.GetDescription(command);
            logger?.Write($"  {usage[6..],-15}", newLine: false, color: ConsoleColor.Yellow);
            logger?.Write(desc);
            logger?.Write();
        }

        return Task.CompletedTask;
    }
}
