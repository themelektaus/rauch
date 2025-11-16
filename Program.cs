Environment.CurrentDirectory = AppContext.BaseDirectory;

// Setup dependency injection container
var services = new ServiceContainer();

// Register logger
var logger = new ConsoleLogger(enableColors: true);
services.RegisterSingleton<ILogger>(logger);

// Check if we should show help (no arguments or "help" command)
var showingHelp = args.Length == 0 || args[0].Equals("help", StringComparison.OrdinalIgnoreCase);

// Load all available commands via reflection (including plugins)
// Show verbose plugin logging only when displaying help
var commands = CommandLoader.LoadCommands(logger, verbosePluginLogging: showingHelp);

// If no arguments or "help" is provided, show help
if (showingHelp)
{
    var helpCommand = CommandLoader.FindCommand(commands, "help");
    if (helpCommand != null)
    {
        await helpCommand.ExecuteAsync([], services);
    }
    return;
}

// Find the matching command
var command = CommandLoader.FindCommand(commands, args[0]);

if (command != null)
{
    // Execute command with remaining arguments
    var commandArgs = args.Skip(1).ToArray();

    // Validate arguments
    var validationResult = CommandMetadata.ValidateArguments(command, commandArgs);
    if (!validationResult.IsValid)
    {
        logger.Error($"Validation error: {validationResult.ErrorMessage}");
        logger.Info($"Usage: {CommandMetadata.GetUsage(command)}");
        return;
    }

    await command.ExecuteAsync(commandArgs, services);
}
else
{
    logger.Error($"Unknown command: {args[0]}");
    logger.Info("Use 'rauch help' for more information.");
}
