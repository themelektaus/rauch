namespace Rauch.Commands;

[Command("help", "Show this help text")]
public class Help : ICommand
{
    private readonly IEnumerable<ICommand> _availableCommands;

    public Help(IEnumerable<ICommand> availableCommands)
    {
        _availableCommands = availableCommands;
    }

    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        logger?.WriteLine("");
        logger?.Info(" >_ rauch");
        logger?.WriteLine("");

        // Separate commands into groups and individual commands
        var groups = _availableCommands.OfType<ICommandGroup>()
            .Where(c => !CommandMetadata.IsHidden(c))
            .OrderBy(c => CommandMetadata.GetName(c))
            .ToList();

        var individualCommands = _availableCommands
            .Where(c => c is not ICommandGroup &&
                       CommandMetadata.GetName(c) != "help" &&
                       !CommandMetadata.IsHidden(c))
            .OrderBy(c => CommandMetadata.GetName(c))
            .ToList();

        // Show groups with their subcommands
        if (groups.Any())
        {
            logger?.WriteLine("Command groups:");
            foreach (var group in groups)
            {
                var groupName = CommandMetadata.GetName(group);
                var groupDesc = CommandMetadata.GetDescription(group);
                logger?.WriteLine($"  {groupName,-15} {groupDesc}");

                foreach (var subCmd in group.SubCommands
                    .Where(s => !CommandMetadata.IsHidden(s))
                    .OrderBy(s => CommandMetadata.GetName(s)))
                {
                    var subName = CommandMetadata.GetName(subCmd);
                    var subDesc = CommandMetadata.GetDescription(subCmd);
                    logger?.WriteLine($"    └─ {subName,-13} {subDesc}");
                }
                logger?.WriteLine("");
            }
        }

        // Show individual commands
        if (individualCommands.Any())
        {
            logger?.WriteLine("Commands:");
            foreach (var command in individualCommands)
            {
                var usage = CommandMetadata.GetUsage(command);
                var desc = CommandMetadata.GetDescription(command);

                logger?.WriteLine($"  {usage,-25} {desc}");
            }
            logger?.WriteLine("");
        }

        // Show help command separately
        logger?.WriteLine("Help:");
        var helpUsage = CommandMetadata.GetUsage(this);
        var helpDesc = CommandMetadata.GetDescription(this);
        logger?.WriteLine($"  {helpUsage,-25} {helpDesc}");

        return Task.CompletedTask;
    }
}
