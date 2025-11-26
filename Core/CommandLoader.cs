using System.Reflection;

namespace Rauch.Core;

/// <summary>
/// Dynamically loads all available commands via reflection
/// </summary>
public sealed class CommandLoader
{
    /// <summary>
    /// Finds and instantiates all ICommand implementations in the current assembly
    /// and loads plugin commands from the plugins directory
    /// </summary>
    public static List<ICommand> LoadCommands(ILogger logger = null, bool verbosePluginLogging = false)
    {
        var commands = new List<ICommand>();
        var assembly = Assembly.GetExecutingAssembly();

        var commandTypes = new List<Type>();

        commandTypes.AddRange(
            assembly.GetTypes().Where(t => typeof(ICommandGroup).IsAssignableFrom(t)
                && !t.IsInterface && !t.IsAbstract
                && t.Namespace.StartsWith("Rauch.Commands.")
            )
        );

        commandTypes.AddRange(
            assembly.GetTypes().Where(t => typeof(ICommand).IsAssignableFrom(t)
                && !t.IsInterface && !t.IsAbstract
                && t != typeof(Help)
                && t.Namespace == "Rauch.Commands"
            )
        );

        commandTypes.AddRange(
            assembly.GetTypes().Where(t => typeof(ICommandGroup).IsAssignableFrom(t)
                && !t.IsInterface && !t.IsAbstract
                && t.Namespace.StartsWith("Rauch.Plugins.")
                && !commandTypes.Any(x => t.Namespace.Split('.').Last() == x.Namespace.Split('.').Last())
            )
        );

        commandTypes.AddRange(
            assembly.GetTypes().Where(t => typeof(ICommand).IsAssignableFrom(t)
                && !t.IsInterface && !t.IsAbstract
                && t.Namespace == "Rauch.Plugins"
                && !commandTypes.Any(x => t.Namespace.Split('.').Last() == x.Namespace.Split('.').Last())
            )
        );

        // Instantiate all commands (except Help)
        foreach (var type in commandTypes)
        {
            try
            {
                // Try parameterless constructor
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
        pluginLoader.LoadPluginsInto(commands, verbosePluginLogging);

        // Add Help (needs the list of all commands)
        var help = new Help(commands);
        commands.Add(help); // Help should always be the first command

        return commands;
    }

    /// <summary>
    /// Finds a command by its name
    /// </summary>
    public static ICommand FindCommand(List<ICommand> commands, string commandName)
    {
        return commands.FirstOrDefault(c => CommandMetadata.MatchesName(c, commandName));
    }

    public static T FindCommand<T>(List<ICommand> commands, string commandName) where T : class, ICommand
    {
        return FindCommand(commands, commandName) as T;
    }
}
