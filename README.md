![](Logo.svg)

# rauch

A modern .NET 10.0 command-line tool with dynamic plugin support and automatic command discovery.

## Features

- **Automatic Command Discovery**: Commands are automatically discovered via reflection - no manual registration needed
- **Runtime Plugin System**: Load commands from `.cs` files at runtime using Roslyn compilation
- **Smart Caching**: Plugin compilation results are cached with SHA256-based invalidation
- **Command Groups**: Organize related commands into groups with subcommands
- **Validation Attributes**: Declarative argument validation with custom attributes
- **Dependency Injection**: Lightweight DI container for service management
- **Color-Coded Logging**: Console output with different severity levels (Info, Success, Warning, Error, Debug)

## Installation

### Prerequisites

- .NET 10.0 RC or later

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
dotnet run run ping            # Run ping command
dotnet run windows winrm       # Enable WinRM and configure remote management
```

### Plugin Commands

```bash
# Install Claude Code with portable Git Bash
dotnet run install claude

# Install Rauchmelder with .NET 9 runtime
dotnet run install rauchmelder

# Install Microsoft Office
dotnet run install office

# Install Microsoft Teams
dotnet run install teams

# Install Visual C++ Redistributable 2022
dotnet run install vcredist22

# Install NXLog
dotnet run install nxlog

# Uninstall ConnectWise Automate agents
dotnet run uninstall cwa

# Uninstall NXLog
dotnet run uninstall nxlog
```

## Architecture

### Command Types

1. **Individual Commands**: Core commands located in `Commands/` namespace (e.g., `help`, `update`, `debug`)
2. **Command Groups**: Related commands organized in subdirectories with automatic subcommand loading (e.g., `run ping`, `windows winrm`)
3. **Runtime Plugins**: External commands loaded from `plugins/` directory at runtime with hot-reload support

### Command Discovery

Commands are discovered automatically using a **multi-stage reflection process**:

1. Load `ICommandGroup` from `Rauch.Commands.*` subnamespaces (e.g., `Run`, `Windows`)
2. Load `ICommand` from `Rauch.Commands` namespace (top-level commands)
3. Load `ICommandGroup` from `Rauch.Plugins.*` subnamespaces (with duplicate avoidance)
4. Load `ICommand` from `Rauch.Plugins` namespace (with duplicate avoidance)

Command groups automatically load their subcommands using namespace suffix matching, ensuring Commands take precedence over Plugins with the same name.

### Plugin System

Plugins are `.cs` files that are compiled at runtime using Roslyn:

- **Auto-injection**: Missing using statements and namespaces are automatically added
- **Hot-reload**: Changes to plugin source files are automatically detected and recompiled
- **Performance**: Compiled plugins are cached to minimize startup time
- **No build step**: Simply drop a `.cs` file in `plugins/` and run

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

**Plugin with Utility Methods:**

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
            await StartProcess(FILE_NAME, logger);
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed: {ex.Message}");
        }
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

## Project Structure

```
rauch/
├── Commands/              # Core commands (Rauch.Commands namespace)
│   ├── Help.cs           # Help command
│   ├── Update.cs         # Self-update command
│   ├── Debug.cs          # Debug command
│   ├── Run/              # Run command group
│   │   ├── _Index.cs     # Group definition
│   │   ├── Ping.cs       # Ping subcommand
│   │   └── Ping.ps1      # Embedded PowerShell script
│   └── Windows/          # Windows command group
│       ├── _Index.cs     # Group definition
│       ├── WinRm.cs      # WinRM configuration subcommand
│       ├── WinRm.ps1     # Embedded PowerShell script
│       ├── Update.cs     # Windows Update subcommand
│       └── Update.ps1    # Embedded PowerShell script
├── Plugins/               # Runtime plugins (compiled at runtime)
│   ├── .cache/           # Compiled plugin cache (auto-generated)
│   ├── Install/          # Install command group
│   │   ├── _Index.cs     # Group definition
│   │   ├── Claude.cs     # Install Claude Code
│   │   ├── Rauchmelder.cs # Install Rauchmelder
│   │   ├── Office.cs     # Install Microsoft Office
│   │   ├── Teams.cs      # Install Microsoft Teams
│   │   ├── VcRedist22.cs # Install VC++ Redistributable
│   │   ├── VcRedist22.ps1 # PowerShell script
│   │   ├── Nxlog.cs      # Install NXLog
│   │   └── Nxlog.ps1     # PowerShell script
│   └── Uninstall/        # Uninstall command group
│       ├── _Index.cs     # Group definition
│       ├── Cwa.cs        # Uninstall ConnectWise Automate
│       ├── Nxlog.cs      # Uninstall NXLog
│       └── Nxlog.ps1     # PowerShell script
├── Core/                  # Core infrastructure
│   ├── CommandLoader.cs  # Command discovery system
│   ├── PluginLoader.cs   # Runtime plugin compilation
│   ├── CommandUtils.cs   # Utility methods for plugins
│   └── Attributes/       # Validation and metadata attributes
├── CLAUDE.md              # Detailed developer documentation
├── README.md              # User documentation
├── install.ps1            # Installation script
└── Logo.ico               # Application icon
```

## Development

### Adding a New Command

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

### Creating a Command Group

1. Create folder `Commands/<GroupName>/`
2. Create a file (by convention `_Index.cs`) with `BaseCommandGroup`:
   ```csharp
   namespace Rauch.Commands.<GroupName>;

   [Command("groupname", IsGroup = true)]
   public class _Index : BaseCommandGroup { }
   ```
   Note: The filename can be anything; what matters is the `IsGroup = true` attribute and inheriting from `BaseCommandGroup`.
3. Add subcommand files in the same folder with matching namespace
4. Subcommands (all `ICommand` implementations that are not `ICommandGroup`) are automatically loaded via namespace suffix matching

### Creating a Plugin

1. Create `.cs` file in `plugins/` directory
2. Write minimal command class (using statements auto-injected)
3. Run application - plugin compiles automatically
4. Edit source - changes detected and recompiled

### Validation Attributes

- `[MinArguments(n)]`: Require at least n arguments
- `[MaxArguments(n)]`: Allow at most n arguments
- `[ExactArguments(n)]`: Require exactly n arguments
- `[NumericArguments]`: All arguments must be valid numbers

## Dependencies

- **Microsoft.CodeAnalysis.CSharp** (v4.12.0): Roslyn compiler for plugin system

## Documentation

For detailed developer documentation, see [CLAUDE.md](CLAUDE.md).

## License

[Add your license here]

## Contributing

[Add contributing guidelines here]
