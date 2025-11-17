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
dotnet run network ping        # Run network ping tool
```

### Plugin Commands

```bash
# Install Everything Search Engine
dotnet run install everything

# Install Claude Code with portable Git Bash
dotnet run install claude

# Install Microsoft Office
dotnet run install office

# Uninstall ConnectWise Automate agents
dotnet run uninstall cwa
```

## Architecture

### Command Types

1. **Individual Commands**: Core commands located in `Commands/` namespace
2. **Command Groups**: Related commands organized in subdirectories (e.g., `network ping`)
3. **Runtime Plugins**: External commands loaded from `plugins/` directory at runtime

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
- `DownloadFile(url, filePath, ct, logger)`: Download file with progress logging
- `Unzip(zipPath, destinationPath, ct, logger)`: Extract ZIP archive
- `StartProcess(filePath, logger)`: Launch executable and wait for exit
- `EnsureAdministrator(logger)`: Check for Windows administrator privileges

## Project Structure

```
rauch/
├── Commands/              # Core commands (Rauch.Commands namespace)
│   ├── Help.cs           # Help command
│   ├── Update.cs         # Self-update command
│   └── Network/          # Command group example
│       ├── _Index.cs     # Group definition
│       └── Ping.cs       # Subcommand
├── Plugins/               # Runtime plugins (compiled at runtime)
│   ├── .cache/           # Compiled plugin cache (auto-generated)
│   ├── Install/          # Install command group
│   └── Uninstall/        # Uninstall command group
├── Core/                  # Core infrastructure
│   ├── CommandLoader.cs  # Command discovery system
│   ├── PluginLoader.cs   # Runtime plugin compilation
│   ├── CommandUtils.cs   # Utility methods for plugins
│   └── Attributes/       # Validation and metadata attributes
└── CLAUDE.md              # Detailed developer documentation
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
2. Create `_Index.cs` with `BaseCommandGroup`
3. Add subcommand files in the same folder
4. Subcommands are automatically loaded via namespace reflection

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
