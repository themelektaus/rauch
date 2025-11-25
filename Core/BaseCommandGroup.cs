using Rauch.Commands;

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
        var groupName = CommandMetadata.GetName(this);

        if (args.Length == 0)
        {
            // No subcommand - show help filtered by group name
            await ShowHelpWithSearch(services, [groupName], ct);
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
                logger?.Info($"Usage: {CommandMetadata.GetUsage(subCommand, groupName)}");
                return;
            }

            await subCommand.ExecuteAsync(subCommandArgs, services, ct);
        }
        else
        {
            // Unknown subcommand - show help filtered by group name and subcommand name
            await ShowHelpWithSearch(services, [groupName, subCommandName], ct);
        }
    }

    private static async Task ShowHelpWithSearch(IServiceProvider services, string[] searchTerms, CancellationToken ct)
    {
        var helpCommand = services.GetService<Help>();
        if (helpCommand is not null)
        {
            await helpCommand.ExecuteAsync(searchTerms, services, ct);
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

        // Find all ICommand implementations in the corresponding namespace (except ICommandGroup)
        var subCommandTypes = assembly.GetTypes()
            .Where(t => typeof(ICommand).IsAssignableFrom(t)
                && !t.IsInterface && !t.IsAbstract
                && !typeof(ICommandGroup).IsAssignableFrom(t)
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
