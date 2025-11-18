# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**rauch** is a .NET 10.0 console application with a reflection-based command architecture featuring automatic command discovery, validation, async execution, dependency injection, and a powerful runtime plugin system.

## Build and Run Commands

```bash
# Build the project
dotnet build

# Run without arguments (shows help)
dotnet run

# Run a specific command
dotnet run <command> [args]

# Examples
dotnet run help
dotnet run run ping              # Run ping command
dotnet run windows winrm         # Enable WinRM
dotnet run install claude        # Plugin: Install Claude Code
dotnet run install rauchmelder   # Plugin: Install Rauchmelder with .NET 9
dotnet run update                # Update rauch from GitHub
```

## Architecture

### Command Discovery Pattern

The application uses a **namespace-based reflection system** to automatically discover and load commands:

1. **Command Groups** (`_Index.cs` files):
   - Located in `Commands/<GroupName>/_Index.cs` or `Plugins/<GroupName>/_Index.cs`
   - Must inherit from `BaseCommandGroup`
   - Automatically loads all `ICommand` implementations with matching namespace suffix
   - Uses namespace suffix matching (e.g., `Rauch.Commands.Run` loads all types ending with `.Run`)
   - Example: `Plugins/Install/_Index.cs` (namespace `Rauch.Plugins.Install`) loads `Plugins/Install/Claude.cs`, `Plugins/Install/Rauchmelder.cs`, etc.
   - Example: `Commands/Run/_Index.cs` (namespace `Rauch.Commands.Run`) loads `Commands/Run/Ping.cs`
   - Example: `Commands/Windows/_Index.cs` (namespace `Rauch.Commands.Windows`) loads `Commands/Windows/WinRm.cs`, `Commands/Windows/Update.cs`

2. **Individual Commands**:
   - Located in `Commands/` (root level)
   - Implement `ICommand` interface directly
   - Must be in `Rauch.Commands` namespace
   - Loaded as top-level commands (e.g., `help`, `update`, `debug`)

3. **Plugin Commands**:
   - Located in `plugins/` directory as `.cs` source files
   - Compiled at runtime using Roslyn (Microsoft.CodeAnalysis.CSharp)
   - Automatically cached with SHA256-based invalidation
   - Auto-injection of required using statements and namespace
   - See **Plugin System** section for details

4. **Command Loading Rules** (see `CommandLoader.cs`):
   - **Multi-stage loading process**:
     1. Load `ICommandGroup` from `Rauch.Commands.*` subnamespaces (e.g., `Rauch.Commands.Run`, `Rauch.Commands.Windows`)
     2. Load `ICommand` from `Rauch.Commands` namespace (top-level commands)
     3. Load `ICommandGroup` from `Rauch.Plugins.*` subnamespaces (with duplicate name avoidance)
     4. Load `ICommand` from `Rauch.Plugins` namespace (with duplicate name avoidance)
   - SubCommands are automatically loaded by `BaseCommandGroup` and NOT loaded as top-level commands
   - Duplicate avoidance ensures Commands take precedence over Plugins with same name
   - `Help` command is special-cased and added after all other commands

### Attribute-Based Metadata System

All command metadata is declared via attributes, **never as properties**:

```csharp
namespace Rauch.Commands;

[Command("sum", "Adds the specified numbers", Parameters = "<number1> <number2> ...")]
[MinArguments(1)]
[NumericArguments]
public class Sum : ICommand
{
    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        // Implementation
    }
}
```

**CommandAttribute Properties**:
- `Name` (required): Command name
- `Description` (required): Command description
- `Parameters`: Optional usage string (auto-generated if omitted)
- `IsGroup`: Set to `true` for command groups
- `Hidden`: Hides command from help output (still executable)

### Validation System

**ValidationAttributes** automatically validate arguments before command execution:
- `MinArgumentsAttribute(int count)`: Minimum required arguments
- `MaxArgumentsAttribute(int count)`: Maximum allowed arguments
- `ExactArgumentsAttribute(int count)`: Exact argument count required
- `NumericArgumentsAttribute`: All arguments must be valid numbers

Validation errors are caught in `Program.cs` before `ExecuteAsync` is called.

### Performance Optimizations

**CommandMetadata Caching** (`CommandMetadata.cs`):
- Uses `ConcurrentDictionary` to cache reflection results
- `_attributeCache`: Caches `CommandAttribute` lookups
- `_validationCache`: Caches `ValidationAttribute[]` lookups
- Always use `CommandMetadata.GetName()`, `GetDescription()`, etc. instead of direct reflection

**Plugin Compilation Caching** (`PluginLoader.cs`):
- Compiled plugins cached in `plugins/.cache/` as DLLs
- SHA256 hash comparison for source change detection
- Only recompiles when source code changes
- Dramatically improves startup performance for cached plugins

### Dependency Injection

**ServiceContainer** (`Core/ServiceContainer.cs`):
- Lightweight DI container supporting singletons and factories
- Configured in `Program.cs` `Main` method
- Passed to all commands via `ExecuteAsync(args, services, ct)`
- Access services with `services.GetService<TService>()`

### Logging System

**ILogger Interface** (`Core/Interfaces/ILogger.cs`):
- Color-coded console output with different severity levels
- `Info()`: Cyan - informational messages
- `Success()`: Green - success messages
- `Warning()`: Yellow - warnings
- `Error()`: Red - error messages
- `Debug()`: Dark gray - debug messages
- Access logger via DI: `services.GetService<ILogger>()`

**Verbose Logging Control**:
- Plugin loading messages only shown when displaying help or during compilation
- Silent operation for normal command execution
- Controlled via `verbosePluginLogging` parameter in `CommandLoader.LoadCommands()`

## Plugin System

### Overview

The plugin system allows dynamic loading of commands from `.cs` source files at runtime without recompiling the main application.

**Key Features**:
- Runtime C# compilation using Roslyn
- Automatic caching with SHA256-based invalidation
- Auto-injection of required using statements and namespace
- Hot-reload capability (detects source changes)
- No build step required for plugin development

### Plugin Directory Structure

```
Plugins/
├── .cache/                  # Auto-generated compiled DLLs (gitignored)
│   ├── Install.dll
│   ├── Install.hash
│   └── Install_debug.cs     # Debug output (combined source)
└── Install/                 # Plugin command group (subdirectory)
    ├── _Index.cs            # Group definition
    ├── Everything.cs        # Download and run Everything Search Engine
    └── Claude.cs            # Install Claude Code and portable Git Bash
```

### Creating a Plugin

**Minimal Plugin** (everything auto-injected):
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

**Explicit Plugin** (with all usings and namespace):
```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Rauch.Commands;
using Rauch.Core;
using Rauch.Core.Attributes;

namespace Rauch.Plugins;

[Command("hello", "Greets the user", Parameters = "[name]")]
public class HelloPlugin : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();
        var name = args.Length > 0 ? args[0] : "World";

        logger?.Success($"Hello, {name}!");

        await Task.CompletedTask;
    }
}
```

### Auto-Injection Feature

The plugin loader (`PluginLoader.cs`) automatically injects missing code:

**Automatically Injected Using Statements** (from `Usings.cs`):
```csharp
using Rauch.Commands;
using Rauch.Core;
using Rauch.Core.Attributes;
using static Rauch.Core.CommandUtils;  // Provides SetWorkingDirectory, DownloadFile, Unzip, StartProcess
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
```

**Note**: The using statements are automatically generated during build from `Usings.cs` into `Usings.g.cs` via the `Usings.ps1` PowerShell script.

**Automatically Injected Namespace**:
```csharp
namespace Rauch.Plugins;
```

**Behavior**:
- Only injects what's missing (no duplicates)
- Detects existing using statements and namespace declarations
- Debug messages show what was auto-injected during compilation
- Allows minimal plugin code for rapid development

### Plugin Compilation Process

1. **Load**: Plugin `.cs` files discovered in `plugins/` directory
2. **Hash Check**: SHA256 hash of source compared with cached `.hash` file
3. **Cache Hit**: If hash matches, load pre-compiled `.dll` from cache
4. **Cache Miss**: If hash differs or no cache exists:
   - Auto-inject missing using statements and namespace
   - Compile source to `.dll` using Roslyn
   - Save `.dll` and `.hash` to cache
   - Load compiled assembly
5. **Instantiate**: Extract `ICommand` implementations and instantiate

### Plugin Development Workflow

1. Create `.cs` file in `plugins/` directory
2. Write minimal command class (auto-injection handles the rest)
3. Run application - plugin compiles automatically
4. Edit plugin source - changes detected and recompiled automatically
5. No restart needed for subsequent runs (cached until changed)

### Plugin Cache Management

**Cache Location**: `plugins/.cache/` (excluded from git via `.gitignore`)

**Cache Files**:
- `<PluginName>.dll`: Compiled assembly
- `<PluginName>.hash`: SHA256 hash of source code

**Cache Invalidation**:
- Automatic when source file changes
- Manual: Delete `plugins/.cache/` directory to force full recompilation

**Performance**:
- First run: ~1-3 seconds compilation time
- Cached runs: <100ms load time
- Significant startup performance improvement

## Adding New Commands

### Creating an Individual Command

1. Create file in `Commands/<CommandName>.cs`
2. Use namespace `Rauch.Commands`
3. Implement `ICommand` interface
4. Add `[Command]` attribute with name and description
5. Add validation attributes as needed
6. Add required using statements (no ImplicitUsings)
7. Command will be automatically discovered and loaded

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

### Creating a Command Group with Subcommands

1. Create folder `Commands/<GroupName>/`
2. Create `_Index.cs` with `BaseCommandGroup`:
   ```csharp
   namespace Rauch.Commands.<GroupName>;

   [Command("groupname", "Description", IsGroup = true)]
   public class _Index : BaseCommandGroup { }
   ```
3. Add subcommand files in same folder (e.g., `Commands/GroupName/SubCommand.cs`)
4. Subcommands are **automatically loaded** by `BaseCommandGroup` via namespace reflection

**Important**: Subcommands in a group namespace are **NOT** loaded as top-level commands by `CommandLoader`.

### Creating a Plugin Command

1. Create file in `plugins/<PluginName>.cs`
2. Write minimal command class (using/namespace optional - auto-injected)
3. Plugin compiles automatically on next run
4. See **Plugin System** section for details

### Hidden/Debug Commands

```csharp
[Command("debug", "Internal debug command", Hidden = true)]
public class DebugCommand : ICommand { }
```

Hidden commands execute normally but don't appear in help output.

## Key Files

**Application Entry**:
- `Program.cs`: Entry point, DI setup, command routing, validation, plugin verbose logging control

**Core Infrastructure**:
- `Core/CommandLoader.cs`: Reflection-based command discovery (top-level + plugins)
- `Core/PluginLoader.cs`: Runtime C# compilation, caching, auto-injection
- `Core/BaseCommandGroup.cs`: Base class for groups, loads subcommands via reflection
- `Core/CommandMetadata.cs`: Cached reflection helper for reading attributes
- `Core/CommandUtils.cs`: Static utility methods for plugins (SetWorkingDirectory, DownloadFile, Unzip, StartProcess)
- `Core/ServiceContainer.cs`: Lightweight DI container
- `Core/ConsoleLogger.cs`: Console logger implementation with color support

**Interfaces**:
- `Core/Interfaces/ICommand.cs`: All commands implement this (async pattern)
- `Core/Interfaces/ICommandGroup.cs`: Extends ICommand with SubCommands property
- `Core/Interfaces/ILogger.cs`: Logger interface with severity levels

**Attributes**:
- `Core/Attributes/CommandAttribute.cs`: Command metadata declaration
- `Core/Attributes/ValidationAttribute.cs`: Base class for argument validators

**Project Configuration**:
- `rauch.csproj`: .NET 10.0 project, excludes `plugins/` from compilation, generates `Usings.g.cs` during build, includes `Logo.ico` as application icon
- `Usings.cs`: Global using statements (converted to plugin usings during build)
- `Usings.g.cs`: Auto-generated file containing required usings for plugins (generated by `Usings.ps1`)
- `Usings.ps1`: PowerShell script that generates `Usings.g.cs` from `Usings.cs` during build
- `.gitignore`: Ignores build artifacts and plugin cache

**Installation Scripts**:
- `install.ps1`: PowerShell installation script that downloads rauch.exe to `~/.rauch/bin`, adds it to user PATH, and launches it
- `ConvertPngToIco.ps1`: Utility script to convert PNG images to multi-resolution ICO files (256x256, 128x128, 64x64, 48x48, 32x32, 16x16)

**Assets**:
- `Logo.ico`: Application icon (256x256 max, embedded in executable)
- `Logo.png`: Source PNG logo (256x256)
- `Logo.svg`: Vector logo source

**Build Process**:
1. MSBuild target `Usings` runs before `CoreCompile`
2. Executes `Usings.ps1` to read `Usings.cs`
3. Generates `Usings.g.cs` with `RequiredUsings` class containing all using statements
4. `PluginLoader.EnumerateRequiredUsings()` returns usings from generated class
5. Plugins automatically receive all required usings during compilation

## Critical Design Decisions

1. **No manual command registration**: All discovery is automatic via reflection
2. **Attributes over properties**: Metadata is declared via attributes, never exposed as properties
3. **Async-first**: All commands use `Task ExecuteAsync()` for future-proofing
4. **Validation before execution**: Arguments validated in `Program.cs` before calling `ExecuteAsync`
5. **Namespace-based organization**: Command groups use namespace + `_Index.cs` pattern with suffix matching
6. **Multi-stage loading**: `CommandLoader` uses separate stages for ICommandGroup and ICommand from Commands/Plugins namespaces
7. **Duplicate avoidance**: Commands take precedence over Plugins with same namespace suffix
8. **Separation of concerns**: Top-level commands loaded by `CommandLoader`, subcommands loaded by `BaseCommandGroup`
9. **Colored logging**: All console output uses ILogger with color-coded severity levels
10. **Centralized using statements**: All using statements managed in `Usings.cs`, auto-generated for plugins via build process
11. **Runtime plugin compilation**: Roslyn-based C# compilation for plugins
12. **Aggressive caching**: SHA256-based cache invalidation for fast startup
13. **Auto-injection**: Minimal plugin boilerplate via automatic code injection
14. **Verbose logging control**: Silent by default, verbose only when needed (help/compilation)
15. **Static utility methods**: Common plugin operations (download, unzip, process start) in `CommandUtils` with `using static`

## Dependencies

**NuGet Packages**:
- `Microsoft.CodeAnalysis.CSharp` (v4.12.0): Roslyn compiler for plugin system

**Framework**:
- .NET 10.0 (RC)

## Development Guidelines

1. **Adding Commands**:
   - Use individual commands in `Commands/` namespace for core functionality
   - Prefer plugins for experimental/optional commands
   - Use command groups for related commands (e.g., `install`, `uninstall`)
2. **Using Statements**:
   - Core code: Use global using statements in `Usings.cs`
   - Plugins: Using statements auto-injected from `Usings.g.cs` (no manual usings needed)
3. **Logging**: Use ILogger for all output, respect severity levels
4. **Validation**: Use ValidationAttributes instead of manual checks
5. **Async**: Always use async/await properly, even if no async operations
6. **Cache**: Trust the plugin cache system - it handles invalidation automatically
7. **Namespace**: Commands in `Rauch.Commands`, groups in `Rauch.Commands.<GroupName>`, plugins in `Rauch.Plugins.<GroupName>`
8. **Utility Methods**: Use `CommandUtils` static methods in plugins (available via `using static Rauch.Core.CommandUtils`):
   - `SetWorkingDirectory(path, logger)`: Create and set working directory
   - `DownloadFile(url, filePath, logger, ct)`: Download file with progress
   - `Unzip(zipPath, destinationPath, logger, ct)`: Extract ZIP archive
   - `StartProcess(filePath, arguments, flags, logger, ct)`: Launch executable with arguments and flags
   - `EnsureAdministrator(logger)`: Check for Windows administrator privileges
   - `ExecutePowershellCommand(command, flags, logger, ct)`: Execute PowerShell command
   - `ExecutePowershellFile(file, arguments, flags, logger, ct)`: Execute PowerShell script file
   - `ExecutePowershellFile<T>(arguments, flags, logger, ct)`: Execute embedded PowerShell script by type
9. **CommandFlags Enum**: Use flags to control process behavior:
   - `CommandFlags.None`: Default behavior
   - `CommandFlags.NoProfile`: PowerShell -NoProfile flag
   - `CommandFlags.UseShellExecute`: Use shell execute for process
   - `CommandFlags.CreateNoWindow`: Create process without window

## Plugin Examples

### Example 1: Simple Download and Install Plugin
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

### Example 2: Plugin with ZIP Download and Extraction
```csharp
namespace Rauch.Plugins.Install;

[Command("portable-app", "Install and run portable application")]
public class PortableApp : ICommand
{
    const string ZIP_URL = "https://example.com/app.zip";
    const string INSTALL_DIR = "apps";

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        try
        {
            SetWorkingDirectory(INSTALL_DIR, logger);

            var zipPath = Path.Combine(INSTALL_DIR, "app.zip");
            await DownloadFile(ZIP_URL, zipPath, logger, ct);
            await Unzip(zipPath, INSTALL_DIR, logger, ct);
            File.Delete(zipPath);

            var exePath = Path.Combine(INSTALL_DIR, "app.exe");
            await StartProcess(exePath, logger: logger, ct: ct);
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed: {ex.Message}");
        }
    }
}
```

### Example 3: Plugin with Remote PowerShell Script Execution
```csharp
namespace Rauch.Plugins.Install;

[Command("teams", "Install Microsoft Teams via remote PowerShell script")]
public class Teams : ICommand
{
    const string SCRIPT_URL = "https://raw.githubusercontent.com/mohammedha/Posh/refs/heads/main/O365/Teams/Install_TeamsV2.0.ps1";

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        try
        {
            logger?.Info("Downloading and executing Teams installation script...");

            // Execute remote PowerShell script using CommandUtils
            var exitCode = await ExecutePowershellCommand(
                $"irm '{SCRIPT_URL}' | iex",
                CommandFlags.NoProfile,
                logger,
                ct
            );

            if (exitCode == 0)
            {
                logger?.Success("Teams installation completed successfully");
            }
            else
            {
                logger?.Error($"Teams installation failed with exit code {exitCode}");
            }
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed to install Teams: {ex.Message}");
        }
    }
}
```

### Example 4: Plugin with Embedded PowerShell Script
```csharp
namespace Rauch.Commands.Activate;

[Command("winrm", "Enable WinRM and configure remote management")]
public class WinRm : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        try
        {
            // Check for administrator privileges
            if (!EnsureAdministrator(logger))
            {
                return;
            }

            logger?.Info("Executing WinRM configuration script...");

            // Execute embedded PowerShell script by type
            var exitCode = await ExecutePowershellFile<WinRm>(logger: logger, ct: ct);

            if (exitCode == 0)
            {
                logger?.Success("WinRM configuration completed successfully");
            }
            else
            {
                logger?.Error($"WinRM configuration failed with exit code {exitCode}");
            }
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed to configure WinRM: {ex.Message}");
        }
    }
}
```
**Note**: The PowerShell script must be embedded as a resource in rauch.csproj:
```xml
<ItemGroup>
  <EmbeddedResource Include="Commands\**\*.ps1" />
</ItemGroup>
```

### Example 5: Plugin with .NET Runtime Detection and Installation
```csharp
namespace Rauch.Plugins.Install;

[Command("rauchmelder", "Install Rauchmelder application with .NET 9 runtime")]
public class Rauchmelder : ICommand
{
    const string DOTNET_RUNTIME_URL = "http://cloud.it-guards.at/download/dotnet-runtime-9.0.4-win-x64.exe";
    const string RAUCHMELDER_URL = "http://cloud.it-guards.at/download/rauchmelder/windows/Rauchmelder.exe";
    const string INSTALL_DIR = @"C:\ProgramData\Rauchmelder";

    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        try
        {
            if (!EnsureAdministrator(logger))
            {
                return;
            }

            SetWorkingDirectory(INSTALL_DIR, logger);

            // Check and install .NET runtime if needed
            if (!IsDotNetRuntimeInstalled(logger, 9))
            {
                logger?.Warning(".NET 9 runtime not found. Installing...");
                var runtimeInstaller = "dotnet-runtime-9.0.4-win-x64.exe";
                await DownloadFile(DOTNET_RUNTIME_URL, runtimeInstaller, logger, ct);

                var exitCode = await StartProcess(
                    runtimeInstaller,
                    "/install /quiet /norestart",
                    CommandFlags.None,
                    logger,
                    ct
                );

                if (exitCode != 0)
                {
                    logger?.Error($".NET runtime installation failed with exit code {exitCode}");
                    return;
                }
            }

            // Download and configure application
            var rauchmelderExe = "Rauchmelder.exe";
            if (File.Exists(rauchmelderExe))
            {
                File.Delete(rauchmelderExe);
            }
            await DownloadFile(RAUCHMELDER_URL, rauchmelderExe, logger, ct);

            // Create configuration file
            var configPath = Path.Combine(INSTALL_DIR, "Config.ini");
            await File.WriteAllLinesAsync(configPath, [
                "[General]",
                "InformUrl=https://feuerwehr.cloud.it-guards.at/inform",
                "DownloadUrl=https://cloud.it-guards.at/download/rauchmelder",
                "TunnelUrl=https://feuerwehr.cloud.it-guards.at/api/tunnel"
            ], ct);

            logger?.Success("Rauchmelder installation completed successfully");

            // Launch application
            await StartProcess(rauchmelderExe, "interactive", logger: logger, ct: ct);
        }
        catch (Exception ex)
        {
            logger?.Error($"Failed to install Rauchmelder: {ex.Message}");
        }
    }

    private bool IsDotNetRuntimeInstalled(ILogger logger, uint version)
    {
        // Check if .NET runtime is installed by executing 'dotnet --list-runtimes'
        // and searching for "Microsoft.NETCore.App {version}."
        // Implementation details omitted for brevity
        return false;
    }
}
```
