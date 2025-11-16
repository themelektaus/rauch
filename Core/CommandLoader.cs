using Rauch.Commands.Standalone;
using System.Reflection;

namespace Rauch.Core;

/// <summary>
/// Dynamically loads all available commands via reflection
/// </summary>
public class CommandLoader
{
    /// <summary>
    /// Finds and instantiates all ICommand implementations in the current assembly
    /// and loads plugin commands from the plugins directory
    /// </summary>
    public static List<ICommand> LoadCommands(ILogger logger = null, bool verbosePluginLogging = false)
    {
        var commands = new List<ICommand>();
        var assembly = Assembly.GetExecutingAssembly();

        // Find all top-level commands: _Index (groups) and Standalone
        var commandTypes = assembly.GetTypes()
            .Where(t => typeof(ICommand).IsAssignableFrom(t) &&
                       !t.IsInterface &&
                       !t.IsAbstract &&
                       t != typeof(Help) &&
                       (t.Name == "_Index" || // Group commands
                        t.Namespace == "Rauch.Commands.Standalone")) // Standalone commands
            .ToList();

        // Instantiate all commands (except Help)
        foreach (var type in commandTypes)
        {
            try
            {
                // Try parameterless constructor
                var instance = Activator.CreateInstance(type) as ICommand;
                if (instance != null)
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
        var pluginCommands = pluginLoader.LoadPlugins(verbosePluginLogging);
        commands.AddRange(pluginCommands);

        // Add Help (needs the list of all commands)
        var help = new Help(commands);
        commands.Insert(0, help); // Help should always be the first command

        return commands;
    }

    /// <summary>
    /// Finds a command by its name
    /// </summary>
    public static ICommand FindCommand(List<ICommand> commands, string commandName)
    {
        return commands.FirstOrDefault(c => CommandMetadata.MatchesName(c, commandName));
    }
}
