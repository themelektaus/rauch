namespace Rauch.Core;

/// <summary>
/// Abstract base class for command groups with automatic subcommand loading
/// Metadata (name, description, usage) is read from the CommandAttribute
/// Supports async, validation, and dependency injection
/// </summary>
public abstract class BaseCommandGroup : ICommandGroup
{
    private readonly List<ICommand> _subCommands;

    public IEnumerable<ICommand> SubCommands => _subCommands;

    public void AddSubCommandsFromOtherGroup(ICommandGroup otherGroup)
    {
        _subCommands.AddRange(otherGroup.SubCommands);
    }

    protected BaseCommandGroup()
    {
        _subCommands = LoadSubCommands();
    }

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        if (args.Length == 0)
        {
            ShowSubCommandHelp(logger);
            return;
        }

        var subCommandName = args[0];
        var subCommand = _subCommands.FirstOrDefault(c => CommandMetadata.MatchesName(c, subCommandName));

        if (subCommand != null)
        {
            var subCommandArgs = args.Skip(1).ToArray();

            // Validate arguments
            var validationResult = CommandMetadata.ValidateArguments(subCommand, subCommandArgs);
            if (!validationResult.IsValid)
            {
                logger?.Error($"Validation error: {validationResult.ErrorMessage}");
                logger?.Info($"Usage: {CommandMetadata.GetUsage(subCommand, CommandMetadata.GetName(this))}");
                return;
            }

            await subCommand.ExecuteAsync(subCommandArgs, services, ct);
        }
        else
        {
            logger?.Error($"Unknown subcommand: {subCommandName}");
            ShowSubCommandHelp(logger);
        }
    }

    private void ShowSubCommandHelp(ILogger logger = null)
    {
        if (!_subCommands.Any())
        {
            var name = CommandMetadata.GetName(this);
            logger?.Warning($"Warning: No subcommands found for '{name}'.");
            return;
        }

        logger?.Error("Error: No subcommand specified.");
        logger?.Info($"Usage: {CommandMetadata.GetUsage(this)}");
        logger?.WriteLine("");
        logger?.WriteLine("Available subcommands:");

        foreach (var subCmd in _subCommands.OrderBy(c => CommandMetadata.GetName(c)))
        {
            // Skip hidden commands
            if (CommandMetadata.IsHidden(subCmd))
                continue;

            var name = CommandMetadata.GetName(subCmd);
            var desc = CommandMetadata.GetDescription(subCmd);

            logger?.WriteLine($"  {name,-15} {desc}");
        }
    }

    /// <summary>
    /// Loads all subcommands from the corresponding namespace via reflection
    /// </summary>
    private List<ICommand> LoadSubCommands()
    {
        var subCommands = new List<ICommand>();
        var type = GetType();
        var assembly = type.Assembly;
        var namespaceSuffix = $".{type.Namespace.Split('.').Last()}";

        // Find all ICommand implementations in the corresponding namespace (except _Index)
        var subCommandTypes = assembly.GetTypes()
            .Where(t => typeof(ICommand).IsAssignableFrom(t)
                && !t.IsInterface && !t.IsAbstract
                && t.Name != "_Index"
                && (t.Namespace?.EndsWith(namespaceSuffix) ?? false))
            .ToList();

        foreach (var subCommandType in subCommandTypes)
        {
            try
            {
                var instance = Activator.CreateInstance(subCommandType) as ICommand;
                if (instance != null)
                {
                    subCommands.Add(instance);
                }
            }
            catch (Exception ex)
            {
                // Loading errors are ignored (no logger available in constructor)
                Console.WriteLine($"Warning: Subcommand {subCommandType.Name} could not be loaded: {ex.Message}");
            }
        }

        return subCommands;
    }
}
