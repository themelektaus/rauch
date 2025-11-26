Environment.CurrentDirectory = AppContext.BaseDirectory;

// Setup dependency injection container
var services = new ServiceContainer();

// Register logger
var logger = new ConsoleLogger(enableColors: true);
services.RegisterSingleton<ILogger>(logger);

// Load all available commands via reflection (including plugins)
// Show verbose plugin logging only when displaying help
var commands = CommandLoader.LoadCommands(logger, verbosePluginLogging: args.Length == 0 || args[0] == "update");

// Register Help command
var helpCommand = CommandLoader.FindCommand<Help>(commands, "help");
if (helpCommand is not null)
{
    services.RegisterSingleton(helpCommand);
}

if (args.Length == 0)
{
    helpCommand?.WriteTitleLine(logger);
    helpCommand?.WriteHelpLine(logger);
    return;
}

if (args.Length >= 1)
{
    var command = CommandLoader.FindCommand(commands, args[0]);
    if (command is not null)
    {
        await ValidateAndExecuteAsync(command, args.Skip(1).ToArray());
        return;
    }
}

if (args.Length >= 2)
{
    var command = CommandLoader.FindCommand(commands, args[0], args[1]);
    if (command is not null)
    {
        await ValidateAndExecuteAsync(command, args.Skip(2).ToArray());
        return;
    }
}

if (helpCommand is not null)
{
    await helpCommand.ExecuteAsync(args, services);
}

async Task ValidateAndExecuteAsync(ICommand command, string[] args)
{
    var validationResult = CommandMetadata.ValidateArguments(command, args);
    if (validationResult.IsValid)
    {
        await command.ExecuteAsync(args, services);
        return;
    }

    logger.Error($"Validation error: {validationResult.ErrorMessage}");
    logger.Info($"Usage: {CommandMetadata.GetUsage(command)}");
}
