using System.Reflection;

namespace Rauch.Core;

/// <summary>
/// Dynamically loads all available commands via reflection
/// Groups commands automatically based on namespace (Rauch.Commands.* or Rauch.Plugins.*)
/// </summary>
public sealed class CommandLoader
{
    /// <summary>
    /// Finds and instantiates all ICommand implementations in the current assembly
    /// and loads plugin commands from the plugins directory
    /// </summary>
    public static List<ICommand> LoadCommands(ILogger logger = null)
    {
        var commands = new List<ICommand>();
        var assembly = Assembly.GetExecutingAssembly();

        // Load all ICommand types from Rauch.Commands and Rauch.Commands.* namespaces
        var commandTypes = assembly.GetTypes().Where(t =>
            typeof(ICommand).IsAssignableFrom(t)
            && !t.IsInterface && !t.IsAbstract
            && t != typeof(Help)
            && (
                t.Namespace == "Rauch.Commands" ||
                t.Namespace?.StartsWith("Rauch.Commands.") == true ||
                t.Namespace == "Rauch.Plugins" ||
                t.Namespace?.StartsWith("Rauch.Plugins.") == true
            )
        ).ToList();

        // Instantiate all commands (except Help)
        foreach (var type in commandTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is ICommand instance)
                {
                    commands.Add(instance);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Command {type.Name} could not be loaded: {ex.Message}");
            }
        }

        // Load plugin commands from plugins directory
        var pluginDirectory = Path.Combine(AppContext.BaseDirectory, "plugins");
        var pluginLoader = new PluginLoader(pluginDirectory, logger);
        pluginLoader.LoadPluginsInto(commands);

        // Add Help (needs the list of all commands)
        var help = new Help(commands);
        commands.Add(help);

        return commands;
    }

    /// <summary>
    /// Finds a command by its name. Supports both simple names (e.g., "help")
    /// and group.subcommand syntax (e.g., "run ping" or "install claude")
    /// </summary>
    public static ICommand FindCommand(List<ICommand> commands, string commandName, string subCommandName = null)
    {
        // If subCommandName is provided, look for a command in a specific group
        if (!string.IsNullOrEmpty(subCommandName))
        {
            return commands.FirstOrDefault(c =>
                GetGroupName(c)?.Equals(commandName, StringComparison.OrdinalIgnoreCase) == true
                && CommandMetadata.MatchesName(c, subCommandName));
        }

        // else try to find a top-level command (no group)
        return commands.FirstOrDefault(c =>
            GetGroupName(c) == null
            && CommandMetadata.MatchesName(c, commandName));
    }

    public static T FindCommand<T>(List<ICommand> commands, string commandName) where T : class, ICommand
    {
        return FindCommand(commands, commandName) as T;
    }

    /// <summary>
    /// Gets all commands that belong to a specific group
    /// </summary>
    public static IEnumerable<ICommand> GetCommandsInGroup(List<ICommand> commands, string groupName)
    {
        return commands.Where(c =>
            GetGroupName(c)?.Equals(groupName, StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <summary>
    /// Gets all unique group names from the loaded commands
    /// </summary>
    public static IEnumerable<string> GetGroupNames(List<ICommand> commands)
    {
        return commands
            .Select(GetGroupName)
            .Where(g => g != null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g);
    }

    /// <summary>
    /// Gets the group name from a command's namespace.
    /// Returns null for top-level commands (Rauch.Commands or Rauch.Plugins namespace)
    /// Returns the last part of the namespace for grouped commands (e.g., "Run" for Rauch.Commands.Run)
    /// </summary>
    public static string GetGroupName(ICommand command)
    {
        var ns = command.GetType().Namespace;
        if (ns == null) return null;

        // Top-level commands have no group
        if (ns == "Rauch.Commands" || ns == "Rauch.Plugins")
        {
            return null;
        }

        // Extract group name from namespace (last segment after Rauch.Commands. or Rauch.Plugins.)
        if (ns.StartsWith("Rauch.Commands.") || ns.StartsWith("Rauch.Plugins."))
        {
            var parts = ns.Split('.');
            return parts.Length > 2 ? parts[2].ToLowerInvariant() : null;
        }

        return null;
    }

    /// <summary>
    /// Checks if a command belongs to a group (i.e., is not a top-level command)
    /// </summary>
    public static bool IsGroupedCommand(ICommand command)
    {
        return GetGroupName(command) != null;
    }

    /// <summary>
    /// Checks if the command is from a plugin (Rauch.Plugins namespace)
    /// </summary>
    public static bool IsPlugin(ICommand command)
    {
        var ns = command.GetType().Namespace;
        return ns?.StartsWith("Rauch.Plugins") == true;
    }
}
