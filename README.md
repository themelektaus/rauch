![](Logo.svg)

# rauch

A modern .NET 10.0-windows command-line tool with dynamic plugin support, automatic command discovery, interactive console UI, and audio feedback.

## Features

- **Automatic Command Discovery**: Commands are automatically discovered via reflection - no manual registration needed
- **Namespace-Based Grouping**: Commands are automatically grouped based on their namespace (e.g., `Rauch.Commands.Run` → group `run`)
- **Runtime Plugin System**: Load commands from `.cs` files at runtime using Roslyn C# 13 compilation
- **Smart Caching**: Plugin compilation results are cached with SHA256-based invalidation
- **Validation Attributes**: Declarative argument validation with custom attributes
- **Dependency Injection**: Lightweight DI container for service management
- **Color-Coded Logging**: Console output with different severity levels (Info, Success, Warning, Error, Debug)
- **Interactive Console UI**: Arrow-key menu selection and text input prompts
- **Audio Feedback**: Embedded sound effects for user feedback (success, error, confirmation sounds)

## Installation

### Prerequisites

- .NET 10.0 or later (Windows only)

### Powershell

```powershell
irm https://raw.githubusercontent.com/themelektaus/rauch/main/install.ps1 | iex
```

### Build from Source

```bash
git clone <repository-url>
cd rauch
dotnet build
```

## Usage

### Basic Commands

```bash
# Show help and list all available commands
dotnet run help

# Run a specific command
dotnet run <command> [arguments]

# Examples
dotnet run update              # Update rauch to latest version
dotnet run run ping localhost  # Run ping command
dotnet run windows winrm       # Enable WinRM and configure remote management
```

### Plugin Commands

```bash
# Install tools
dotnet run install claude        # Install Claude Code with portable Git Bash
dotnet run install rauchmelder   # Install Rauchmelder with .NET 9 runtime
dotnet run install office        # Install Microsoft Office
dotnet run install teams         # Install Microsoft Teams
dotnet run install vcredist22    # Install Visual C++ Redistributable 2022
dotnet run install nxlog         # Install NXLog

# Run portable tools
dotnet run run everything        # Run Everything Search Engine
dotnet run run procexp           # Run Process Explorer
dotnet run run treesize          # Run TreeSize Free
dotnet run run psexec            # Run PsExec
dotnet run run speedtest         # Run Speedtest CLI

# Windows configuration
dotnet run windows activate      # Activate Windows
dotnet run windows win11ready    # Check Windows 11 readiness

# Windows setup wizards
dotnet run gump basic            # System-level configuration (admin required)
dotnet run gump usr              # User-level configuration

# Uninstall tools
dotnet run uninstall cwa         # Uninstall ConnectWise Automate agents
dotnet run uninstall nxlog       # Uninstall NXLog
```

## Architecture

### Command Types

1. **Top-Level Commands**: Commands in `Rauch.Commands` namespace (e.g., `help`, `update`)
2. **Grouped Commands**: Commands in `Rauch.Commands.<GroupName>` namespace are automatically grouped (e.g., `run ping`, `windows winrm`)
3. **Runtime Plugins**: External commands loaded from `Plugins/` directory at runtime with hot-reload support

### Namespace-Based Grouping

Commands are automatically grouped based on their namespace:

- `Rauch.Commands` → Top-level commands (e.g., `rauch help`)
- `Rauch.Commands.Run` → `run` group (e.g., `rauch run ping`)
- `Rauch.Commands.Windows` → `windows` group (e.g., `rauch windows winrm`)
- `Rauch.Plugins.Install` → `install` group (e.g., `rauch install claude`)

No explicit group definition files are needed - groups are created automatically from namespaces!

### Plugin System

Plugins are `.cs` files that are compiled at runtime using Roslyn with C# 13 support:

- **Auto-injection**: Missing using statements and namespaces are automatically added
- **Hot-reload**: Changes to plugin source files are automatically detected and recompiled
- **Performance**: Compiled plugins are cached to minimize startup time
- **No build step**: Simply drop a `.cs` file in `Plugins/` and run

#### Creating a Plugin

**Minimal Plugin Example:**

```csharp
[Command("hello", "Greets the user")]
public class HelloPlugin : ICommand
{
    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();
        logger?.Success("Hello from plugin!");
        return Task.CompletedTask;
    }
}
```

**Plugin in a Group:**

```csharp
namespace Rauch.Plugins.Install;

[Command("mytool", "Download and run MyTool")]
public class MyTool : ICommand
{
    const string DOWNLOAD_URL = "https://example.com/mytool.exe";
    const string FILE_NAME = "mytool.exe";

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        try
        {
            SetWorkingDirectory("data", logger);
            await DownloadFile(DOWNLOAD_URL, FILE_NAME, logger, ct);
            await StartProcess(FILE_NAME, logger: logger, ct: ct);
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed: {ex.Message}");
        }
    }
}
```

**Interactive Configuration Plugin:**

```csharp
namespace Rauch.Plugins.Gump;

[Command("config")]
public class Config : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        // Interactive menu selection with arrow keys
        var choice = logger?.Choice("Select option", ["Option A", "Option B", "Option C"], 0);

        // Text input with default value
        var name = logger?.Question("Enter name:", defaultValue: "Default");

        logger?.Success($"Selected: {choice}, Name: {name}");
    }
}
```

### Available Utility Methods

The `CommandUtils` class provides static helper methods for plugins:

- `SetWorkingDirectory(path, logger)`: Create and navigate to directory
- `DownloadFile(url, filePath, logger, ct)`: Download file with progress logging
- `Unzip(zipPath, destinationPath, logger, ct)`: Extract ZIP archive
- `StartProcess(filePath, arguments, flags, logger, ct)`: Launch executable with arguments and flags
- `EnsureAdministrator(logger)`: Check for Windows administrator privileges
- `ExecutePowershellCommand(command, flags, logger, ct)`: Execute PowerShell command
- `ExecutePowershellFile(file, arguments, flags, logger, ct)`: Execute PowerShell script file
- `ExecutePowershellFile<T>(arguments, flags, logger, ct)`: Execute embedded PowerShell script by type

**CommandFlags Enum:**
- `CommandFlags.None`: Default behavior
- `CommandFlags.NoProfile`: PowerShell -NoProfile flag
- `CommandFlags.UseShellExecute`: Use shell execute for process
- `CommandFlags.CreateNoWindow`: Create process without window

### Interactive Console UI

The `ILogger` interface provides interactive methods:

- `Choice(message, options, defaultIndex)`: Arrow-key menu selection, returns selected index
- `Question(message, possibleValues, defaultValue, allowEmpty)`: Text input with validation

### Sound Effects

The `SoundPlayer` class provides audio feedback:

- `SoundPlayer.PlaySuccess()`: Success completion sound
- `SoundPlayer.PlayError1/2/3()`: Various error sounds
- `SoundPlayer.PlayEnter()`: Enter/confirm sound
- `SoundPlayer.PlayGranted()`: Access granted sound
- `SoundPlayer.PlayNope()`: Rejection sound
- `SoundPlayer.PlayWhip()`: Quick action sound

## Project Structure

```
rauch/
├── Commands/              # Core commands (Rauch.Commands namespace)
│   ├── Help.cs           # Help command
│   ├── Update.cs         # Self-update command
│   ├── Debug.cs          # Debug command (hidden)
│   ├── Run/              # Run command group (Rauch.Commands.Run)
│   │   ├── Ping.cs       # Ping subcommand
│   │   └── Ping.ps1      # Embedded PowerShell script
│   └── Windows/          # Windows command group (Rauch.Commands.Windows)
│       ├── WinRm.cs      # WinRM configuration subcommand
│       ├── WinRm.ps1     # Embedded PowerShell script
│       ├── Update.cs     # Windows Update subcommand
│       └── Update.ps1    # Embedded PowerShell script
├── Plugins/               # Runtime plugins (compiled at runtime)
│   ├── .cache/           # Compiled plugin cache (auto-generated)
│   ├── Install/          # Install command group (Rauch.Plugins.Install)
│   │   ├── Claude.cs     # Install Claude Code
│   │   ├── Rauchmelder.cs # Install Rauchmelder
│   │   ├── Office.cs     # Install Microsoft Office
│   │   ├── Teams.cs      # Install Microsoft Teams
│   │   ├── VcRedist22.cs # Install VC++ Redistributable
│   │   ├── VcRedist22.ps1 # PowerShell script
│   │   ├── Nxlog.cs      # Install NXLog
│   │   └── Nxlog.ps1     # PowerShell script
│   ├── Run/              # Run command group (Rauch.Plugins.Run)
│   │   ├── Everything.cs # Everything Search Engine
│   │   ├── ProcExp.cs    # Process Explorer
│   │   ├── TreeSize.cs   # TreeSize Free
│   │   ├── PsExec.cs     # PsExec
│   │   ├── Speedtest.cs  # Speedtest CLI
│   │   └── Speedtest.ps1 # PowerShell script
│   ├── Windows/          # Windows command group (Rauch.Plugins.Windows)
│   │   ├── Activate.cs   # Activate Windows
│   │   ├── Win11Ready.cs # Windows 11 readiness check
│   │   └── Logout.ps1    # Logout script
│   ├── Uninstall/        # Uninstall command group (Rauch.Plugins.Uninstall)
│   │   ├── Cwa.cs        # Uninstall ConnectWise Automate
│   │   ├── Nxlog.cs      # Uninstall NXLog
│   │   └── Nxlog.ps1     # PowerShell script
│   └── Gump/             # Windows setup wizards (Rauch.Plugins.Gump)
│       ├── Basic.cs      # System-level configuration
│       └── Usr.cs        # User-level configuration
├── Core/                  # Core infrastructure
│   ├── CommandLoader.cs  # Command discovery with namespace-based grouping
│   ├── PluginLoader.cs   # Runtime plugin compilation
│   ├── CommandUtils.cs   # Utility methods for plugins
│   ├── ConsoleLogger.cs  # Logger with interactive UI
│   ├── SoundPlayer.cs    # Audio feedback via embedded WAV sounds
│   └── Attributes/       # Validation and metadata attributes
├── Sounds/                # Sound effect files (embedded as resources)
├── LiveCode/              # Runtime C# compilation
│   ├── CSharpCompiler.cs # Roslyn C# 13 compiler
│   ├── AssemblyReference.cs # Assembly loading
│   └── AssemblyLoadContext.cs # Plugin isolation
├── CLAUDE.md              # Detailed developer documentation
├── README.md              # User documentation
├── install.ps1            # Installation script
└── Logo.ico               # Application icon
```

## Development

### Adding a New Top-Level Command

1. Create a file in `Commands/<CommandName>.cs`
2. Use namespace `Rauch.Commands`
3. Implement `ICommand` interface
4. Add `[Command]` attribute with name and description
5. Add validation attributes as needed

```csharp
namespace Rauch.Commands;

[Command("mycommand", "Description of my command", Parameters = "<arg1>")]
[MinArguments(1)]
public class MyCommand : ICommand
{
    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();
        logger?.Info("Processing command...");
        // Implementation
        return Task.CompletedTask;
    }
}
```

### Creating a Grouped Command

1. Create folder `Commands/<GroupName>/`
2. Create command files with namespace `Rauch.Commands.<GroupName>`
3. Groups are created automatically based on namespace - no index file needed!

```csharp
namespace Rauch.Commands.Run;

[Command("ping", "Ping hosts", Parameters = "<host1> <host2> ...")]
[MinArguments(1)]
public class Ping : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();
        // Implementation
        await Task.CompletedTask;
    }
}
```

This command is automatically available as `rauch run ping`.

### Creating a Plugin

1. Create `.cs` file in `Plugins/<GroupName>/` directory
2. Use namespace `Rauch.Plugins.<GroupName>` (or let it be auto-injected)
3. Write minimal command class (using statements auto-injected)
4. Run application - plugin compiles automatically
5. Edit source - changes detected and recompiled

### Validation Attributes

- `[MinArguments(n)]`: Require at least n arguments
- `[MaxArguments(n)]`: Allow at most n arguments
- `[ExactArguments(n)]`: Require exactly n arguments
- `[NumericArguments]`: All arguments must be valid numbers

## Dependencies

- **Microsoft.CodeAnalysis.CSharp** (v4.12.0): Roslyn compiler for plugin system
- **System.Windows.Extensions** (v9.0.0): Windows-specific APIs for SoundPlayer

## Documentation

For detailed developer documentation, see [CLAUDE.md](CLAUDE.md).
