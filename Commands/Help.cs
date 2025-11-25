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
        var groups = new Dictionary<string, GroupInfo>();

        foreach (var group in _availableCommands.OfType<ICommandGroup>().Where(c => !CommandMetadata.IsHidden(c)))
        {
            var name = CommandMetadata.GetName(group);
            var description = CommandMetadata.GetDescription(group);

            if (!groups.TryGetValue(name, out var groupInfo))
            {
                groupInfo = new()
                {
                    name = name,
                    description = description,
                    commands = []
                };
                groups.Add(name, groupInfo);
            }

            foreach (var command in group.SubCommands)
            {
                name = CommandMetadata.GetName(command);
                groupInfo.commands.Add(name, new()
                {
                    name = name,
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
            var usage = CommandMetadata.GetUsage(command);
            var desc = CommandMetadata.GetDescription(command);
            logger?.Write($"  {usage[6..],-15}", newLine: false, color: ConsoleColor.Yellow);
            logger?.Write(desc);
            logger?.Write();
        }

        return Task.CompletedTask;
    }
}
