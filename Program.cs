Environment.CurrentDirectory = AppContext.BaseDirectory;

// Setup console appearance
Console.CursorVisible = true;
Console.ForegroundColor = ConsoleColor.Gray;
Console.BackgroundColor = ConsoleColor.Black;

var soundEnabled = ConfigIni.Read(data => data["Sound"]["Enabled"]);
if (soundEnabled == "1")
{
    SoundPlayer.LoadSounds();
}

// Setup dependency injection container
var services = new ServiceContainer();

// Register logger
var logger = new ConsoleLogger(enableColors: true);
services.RegisterSingleton<ILogger>(logger);

// Load all available commands via reflection (including plugins)
var commands = CommandLoader.LoadCommands(logger);

// Register Help command
var helpCommand = CommandLoader.FindCommand<Help>(commands, "help");
if (helpCommand is not null)
{
    services.RegisterSingleton(helpCommand);
}

if (args.Length >= 1)
{
    var command = CommandLoader.FindCommand(commands, args[0]);
    if (command is not null)
    {
        await ValidateAndExecuteAsync(command, [.. args.Skip(1)]);
        goto Exit;
    }
}

if (args.Length >= 2)
{
    var command = CommandLoader.FindCommand(commands, args[0], args[1]);
    if (command is not null)
    {
        await ValidateAndExecuteAsync(command, [.. args.Skip(2)]);
        goto Exit;
    }
}

if (helpCommand is not null)
{
    var commandLines = helpCommand.GetCommandLines(args);

    if (commandLines.Count == 1 && commandLines.First().Value.Count == 1)
    {
        var commandLine = commandLines.First();

        string[] commandLineArgs;

        if (commandLine.Key is Help.GroupInfo group)
        {
            commandLineArgs = [group.name, commandLine.Value.First().name];
        }
        else if (commandLine.Key is ICommand command)
        {
            commandLineArgs = [CommandMetadata.GetName(command), ""];
        }
        else
        {
            logger.Error("Unexpected command line key type.");
            goto Exit;
        }

        logger.Write(" >_ ", newLine: false, color: ConsoleColor.DarkCyan);
        logger.Write("rauch ", newLine: false, color: ConsoleColor.Cyan);
        logger.Write(string.Join(" ", commandLineArgs), newLine: false, color: ConsoleColor.Yellow);
        logger.Write(" ... ", newLine: false);

        if (logger.Choice("  ", ["continue", "cancel"]) == 0)
        {
            var command = CommandLoader.FindCommand(commands, commandLineArgs[0], commandLineArgs[1]);
            if (command is not null)
            {
                await ValidateAndExecuteAsync(command, [.. args.Skip(1)]);
                goto Exit;
            }
        }
        else
        {
            logger.Warning("Canceled.");
        }
    }
    else
    {
        await helpCommand.ExecuteAsync(args, services);
    }
}

Exit:
await SoundPlayer.WaitAndDispose();

async Task ValidateAndExecuteAsync(ICommand command, string[] args)
{
    var (isValid, errorMessage) = CommandMetadata.ValidateArguments(command, args);
    if (isValid)
    {
        await command.ExecuteAsync(args, services);
        return;
    }

    logger.Error($"Validation error: {errorMessage}");
    logger.Info($"Usage: {CommandMetadata.GetUsage(command)}");
}
