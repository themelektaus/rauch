Environment.CurrentDirectory = AppContext.BaseDirectory;

// Setup dependency injection container
var services = new ServiceContainer();

// Register logger
var logger = new ConsoleLogger(enableColors: true);
services.RegisterSingleton<ILogger>(logger);

// Load all available commands via reflection (including plugins)
// Show verbose plugin logging only when displaying help
var commands = CommandLoader.LoadCommands(logger, verbosePluginLogging: args.Length == 0 || args[0] == "update");

// Register Help command for use in BaseCommandGroup
var helpCommand = CommandLoader.FindCommand<Help>(commands, "help");
if (helpCommand is not null)
{
    services.RegisterSingleton(helpCommand);
}

// If no arguments or "help" is provided, show help
if (args.Length > 0 && args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
{
    if (helpCommand is not null)
    {
        await helpCommand.ExecuteAsync([], services);
    }
    return;
}

// Find the matching command
var command = args.Length > 0 ? CommandLoader.FindCommand(commands, args[0]) : null;

if (command is null)
{
    helpCommand?.WriteTitleLine(logger);
    helpCommand?.WriteHelpLine(logger);
}
else
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
