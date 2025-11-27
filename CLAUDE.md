# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**rauch** is a .NET 10.0 console application with a reflection-based command architecture featuring automatic command discovery, validation, async execution, dependency injection, a powerful runtime plugin system with C# 13 compilation, interactive console UI components, and cross-platform audio feedback via NAudio.

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
dotnet run run everything          # Run Everything Search Engine
dotnet run windows activate        # Activate Windows
dotnet run install claude          # Plugin: Install Claude Code
dotnet run install rauchmelder     # Plugin: Install Rauchmelder with .NET 9
dotnet run gump basic              # Run Windows configuration wizard
dotnet run update                  # Update rauch from GitHub
```

## Architecture

### Command Discovery Pattern

The application uses a **namespace-based reflection system** to automatically discover, load, and group commands:

1. **Command Groups** (automatic namespace-based grouping):
   - Commands in `Rauch.Commands.<GroupName>` or `Rauch.Plugins.<GroupName>` namespaces are automatically grouped
   - No explicit group definition file needed - groups are derived from namespace
   - Example: `Commands/Run/Ping.cs` with namespace `Rauch.Commands.Run` → group `run`, command `ping`
   - Example: `Plugins/Install/Claude.cs` with namespace `Rauch.Plugins.Install` → group `install`, command `claude`
   - Invoked as `rauch <group> <subcommand>` (e.g., `rauch run ping`, `rauch install claude`)

2. **Top-Level Commands**:
   - Located in `Commands/` (root level)
   - Implement `ICommand` interface directly
   - Must be in `Rauch.Commands` namespace (without subnamespace)
   - Loaded as top-level commands (e.g., `help`, `update`, `debug`)
   - Invoked as `rauch <command>` (e.g., `rauch help`, `rauch update`)

3. **Plugin Commands**:
   - Located in `Plugins/` directory as `.cs` source files
   - Compiled at runtime using Roslyn (Microsoft.CodeAnalysis.CSharp)
   - Supports C# 13 language features
   - Automatically cached with SHA256-based invalidation
   - Auto-injection of required using statements and namespace
   - See **Plugin System** section for details

4. **Command Loading Rules** (see `CommandLoader.cs`):
   - All `ICommand` types from `Rauch.Commands`, `Rauch.Commands.*`, `Rauch.Plugins`, and `Rauch.Plugins.*` namespaces are loaded
   - Groups are automatically created based on namespace suffix (e.g., `Rauch.Commands.Run` → group `run`)
   - `CommandLoader.GetGroupName(command)` extracts group name from namespace
   - `CommandLoader.IsGroupedCommand(command)` checks if command belongs to a group
   - `CommandLoader.IsPlugin(command)` checks if command is from `Rauch.Plugins` namespace
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
- `Description` (optional): Command description (default: empty string)
- `Parameters`: Optional usage string (auto-generated if omitted)
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
- Compiled plugins cached in `Plugins/.cache/` as DLLs
- SHA256 hash comparison for source change detection
- Only recompiles when source code changes
- Dramatically improves startup performance for cached plugins

### Dependency Injection

**ServiceContainer** (`Core/ServiceContainer.cs`):
- Lightweight DI container supporting singletons and factories
- Implements `IServiceProvider` interface
- Configured in `Program.cs` `Main` method
- Passed to all commands via `ExecuteAsync(args, services, ct)`
- Access services with `services.GetService<TService>()` or `services.GetRequiredService<TService>()`

### Logging System

**ILogger Interface** (`Core/Interfaces/ILogger.cs`):
- Color-coded console output with different severity levels
- `Info()`: Cyan - informational messages
- `Success()`: Green - success messages
- `Warning()`: Yellow - warnings
- `Error()`: Red - error messages
- `Debug()`: Dark gray - debug messages (only in DEBUG builds)
- `Write()`: Generic output with optional color
- `Exit()`: Display exit code with appropriate color
- `Question()`: Interactive text input with validation and default values
- `Choice()`: Interactive menu selection with arrow key navigation
- Access logger via DI: `services.GetService<ILogger>()`

**Verbose Logging Control**:
- Plugin loading messages only shown when displaying help or during compilation
- Silent operation for normal command execution
- Controlled via `verbosePluginLogging` parameter in `CommandLoader.LoadCommands()`

### Sound System

**SoundPlayer** (`Core/SoundPlayer.cs`):
- Static class for playing embedded WAV sound effects using NAudio
- Sounds are embedded as resources from `Sounds/*.wav`
- Uses `WaveOutEvent` for cross-platform audio playback
- `SoundEffect` inner class manages individual sound resources with volume control and duration

**Available Sound Methods**:
- `SoundPlayer.PlaySuccess()`: Success completion sound
- `SoundPlayer.PlayError()`: Error sound (used by `logger.Error()`)
- `SoundPlayer.PlayWarning()`: Warning sound (used by `logger.Warning()`)
- `SoundPlayer.PlayHelp()`: Help/quick action sound (used by help command)

**Automatic Sound Integration**:
- `logger.Error()` automatically plays `PlayError()` sound
- `logger.Warning()` automatically plays `PlayWarning()` sound
- Help command title plays `PlayHelp()` sound

**Usage in Commands**:
```csharp
await SoundPlayer.PlaySuccess();  // Play success sound
await SoundPlayer.PlayError();    // Play error sound
```

**Program Exit**:
- `Program.cs` calls `await SoundPlayer.Wait()` before exit to ensure all sounds complete

### LiveCode Compiler System

The `LiveCode/` namespace provides runtime C# compilation infrastructure:

**CSharpCompiler** (`LiveCode/CSharpCompiler.cs`):
- Roslyn-based C# compiler supporting C# 13 language features
- Compiles source code to DLL with optimized release settings
- Automatically resolves all assembly references from the executing assembly
- Uses unsafe code for efficient metadata access

**AssemblyReference** (`LiveCode/AssemblyReference.cs`):
- Loads compiled assemblies with weak references for potential unloading
- Uses custom `AssemblyLoadContext` for isolation

**AssemblyLoadContext** (`LiveCode/AssemblyLoadContext.cs`):
- Collectible assembly load context for plugin isolation
- Enables potential assembly unloading for hot-reload scenarios

## Plugin System

### Overview

The plugin system allows dynamic loading of commands from `.cs` source files at runtime without recompiling the main application.

**Key Features**:
- Runtime C# 13 compilation using Roslyn
- Automatic caching with SHA256-based invalidation
- Auto-injection of required using statements and namespace
- Hot-reload capability (detects source changes)
- No build step required for plugin development
- Support for embedded PowerShell scripts in plugin directories

### Plugin Directory Structure

```
Plugins/
├── .cache/                      # Auto-generated compiled DLLs (gitignored)
│   ├── Install.dll
│   ├── Install.hash
│   ├── Install_debug.cs         # Debug output (combined source)
│   ├── Run.dll
│   ├── Run.hash
│   └── ...
├── Install/                     # Plugin command group (namespace: Rauch.Plugins.Install)
│   ├── Claude.cs                # Install Claude Code
│   ├── Office.cs                # Install Microsoft Office
│   ├── Teams.cs                 # Install Microsoft Teams
│   ├── Rauchmelder.cs           # Install Rauchmelder
│   ├── VcRedist22.cs            # Install VC++ Redistributable
│   ├── Nxlog.cs                 # Install NXLog
│   └── Nxlog.ps1                # Embedded PowerShell script
├── Run/                         # Run portable tools (namespace: Rauch.Plugins.Run)
│   ├── Everything.cs            # Everything Search Engine
│   ├── ProcExp.cs               # Process Explorer
│   ├── TreeSize.cs              # TreeSize Free
│   ├── PsExec.cs                # PsExec
│   ├── Speedtest.cs             # Speedtest CLI
│   └── Speedtest.ps1
├── Windows/                     # Windows configuration (namespace: Rauch.Plugins.Windows)
│   ├── Win11Ready.cs            # Check Windows 11 readiness
│   ├── Activate.cs              # Activate Windows
│   └── Logout.ps1               # Logout script
├── Uninstall/                   # Uninstall tools (namespace: Rauch.Plugins.Uninstall)
│   ├── Cwa.cs                   # Uninstall ConnectWise
│   ├── Nxlog.cs                 # Uninstall NXLog
│   └── Nxlog.ps1
└── Gump/                        # Windows setup wizards (namespace: Rauch.Plugins.Gump)
    ├── Basic.cs                 # System-level configuration (admin)
    └── Usr.cs                   # User-level configuration
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

**Plugin in a Group** (with namespace):
```csharp
namespace Rauch.Plugins.Install;

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
using static Rauch.Core.CommandUtils;  // Provides SetWorkingDirectory, DownloadFile, Unzip, StartProcess, etc.
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

**Note**: The using statements are automatically generated during build from `Usings.cs` into `Usings.g.cs` via the `Usings.ps1` PowerShell script. The generated file contains a `Usings` class with a `Namespaces` array.

**Automatically Injected Namespace**:
```csharp
namespace Rauch.Plugins.<GroupName>;
```

**Behavior**:
- Only injects what's missing (no duplicates)
- Detects existing using statements and namespace declarations
- Debug messages show what was auto-injected during compilation
- Allows minimal plugin code for rapid development

### Plugin Compilation Process

1. **Load**: Plugin `.cs` files discovered in `Plugins/` directory
2. **Hash Check**: SHA256 hash of source compared with cached `.hash` file
3. **Cache Hit**: If hash matches, load pre-compiled `.dll` from cache
4. **Cache Miss**: If hash differs or no cache exists:
   - Auto-inject missing using statements and namespace
   - Compile source to `.dll` using Roslyn (C# 13)
   - Save `.dll` and `.hash` to cache
   - Save `_debug.cs` for inspection
   - Load compiled assembly
5. **Instantiate**: Extract `ICommand` implementations and instantiate

### Plugin Development Workflow

1. Create `.cs` file in `Plugins/<GroupName>/` directory
2. Use namespace `Rauch.Plugins.<GroupName>` (or let it be auto-injected)
3. Write minimal command class (auto-injection handles the rest)
4. Run application - plugin compiles automatically
5. Edit plugin source - changes detected and recompiled automatically
6. No restart needed for subsequent runs (cached until changed)

### Plugin Cache Management

**Cache Location**: `Plugins/.cache/` (excluded from git via `.gitignore`)

**Cache Files**:
- `<GroupName>.dll`: Compiled assembly
- `<GroupName>.hash`: SHA256 hash of combined source code
- `<GroupName>_debug.cs`: Combined source for debugging

**Cache Invalidation**:
- Automatic when any source file in the group changes
- Manual: Delete `Plugins/.cache/` directory to force full recompilation

**Performance**:
- First run: ~1-3 seconds compilation time per group
- Cached runs: <100ms load time
- Significant startup performance improvement

## Adding New Commands

### Creating a Top-Level Command

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

### Creating a Grouped Command (Subcommand)

1. Create folder `Commands/<GroupName>/`
2. Create subcommand files in the folder with namespace `Rauch.Commands.<GroupName>`
3. Each file implements `ICommand` with `[Command]` attribute
4. Groups are automatically created based on namespace - no index file needed!

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

### Creating a Plugin Command

1. Create file in `Plugins/<GroupName>/<PluginName>.cs`
2. Use namespace `Rauch.Plugins.<GroupName>` (or omit for auto-injection)
3. Write minimal command class (using/namespace optional - auto-injected)
4. Plugin compiles automatically on next run
5. See **Plugin System** section for details

### Hidden/Debug Commands

```csharp
[Command("debug", "Internal debug command", Hidden = true)]
public class DebugCommand : ICommand { }
```

Hidden commands execute normally but don't appear in help output.

## Key Files

**Application Entry**:
- `Program.cs`: Entry point, DI setup, command routing (supports `group subcommand` syntax), validation

**Core Infrastructure**:
- `Core/CommandLoader.cs`: Reflection-based command discovery with automatic namespace-based grouping
- `Core/PluginLoader.cs`: Runtime C# compilation, caching, auto-injection
- `Core/CommandMetadata.cs`: Cached reflection helper for reading attributes
- `Core/CommandUtils.cs`: Static utility methods for plugins (SetWorkingDirectory, DownloadFile, Unzip, StartProcess, etc.)
- `Core/ServiceContainer.cs`: Lightweight DI container
- `Core/ConsoleLogger.cs`: Console logger implementation with color support and interactive UI
- `Core/SoundPlayer.cs`: Static class for playing embedded WAV sound effects asynchronously

**LiveCode Compiler**:
- `LiveCode/CSharpCompiler.cs`: Roslyn-based C# 13 compiler for plugins
- `LiveCode/AssemblyReference.cs`: Assembly loading with weak references
- `LiveCode/AssemblyLoadContext.cs`: Collectible assembly load context for isolation

**Interfaces**:
- `Core/Interfaces/ICommand.cs`: All commands implement this (async pattern)
- `Core/Interfaces/ILogger.cs`: Logger interface with severity levels and interactive UI

**Attributes**:
- `Core/Attributes/CommandAttribute.cs`: Command metadata declaration
- `Core/Attributes/ValidationAttribute.cs`: Base class for argument validators

**Project Configuration**:
- `rauch.csproj`: .NET 10.0 project with unsafe blocks enabled, copies `Plugins/` to output, embeds `Sounds/*.wav` as resources, generates `Usings.g.cs` during build, includes `Logo.ico` as application icon
- `Usings.cs`: Global using statements (converted to plugin usings during build)
- `Usings.g.cs`: Auto-generated file containing `Usings` class with `Namespaces` array (generated by `Usings.ps1`)
- `Usings.ps1`: PowerShell script that generates `Usings.g.cs` from `Usings.cs` during build
- `.gitignore`: Ignores build artifacts and plugin cache

**Installation Scripts**:
- `install.ps1`: PowerShell installation script that downloads rauch.exe to `~/.rauch/bin`, adds it to user PATH, and launches it
- `ConvertPngToIco.ps1`: Utility script to convert PNG images to multi-resolution ICO files (256x256, 128x128, 64x64, 48x48, 32x32, 16x16)
- `PackPlugins.ps1`: PowerShell script to pack plugins into a ZIP file for distribution (runs before Publish)

**Assets**:
- `Logo.ico`: Application icon (256x256 max, embedded in executable)
- `Logo.png`: Source PNG logo (256x256)
- `Logo.svg`: Vector logo source
- `Sounds/*.wav`: Sound effect files (embedded as resources)

**Build Process**:
1. MSBuild target `Usings` runs before `CoreCompile`
2. Executes `Usings.ps1` to read `Usings.cs`
3. Generates `Usings.g.cs` with `Usings` class containing `Namespaces` array
4. `PluginLoader.EnumerateRequiredUsings()` returns usings from generated class
5. Plugins automatically receive all required usings during compilation
6. MSBuild target `PackPlugins` runs before `Publish` to create `Build/Plugins.zip`

## Critical Design Decisions

1. **No manual command registration**: All discovery is automatic via reflection
2. **Attributes over properties**: Metadata is declared via attributes, never exposed as properties
3. **Async-first**: All commands use `Task ExecuteAsync()` for future-proofing
4. **Validation before execution**: Arguments validated in `Program.cs` before calling `ExecuteAsync`
5. **Namespace-based grouping**: Command groups are automatically derived from namespace (e.g., `Rauch.Commands.Run` → group `run`)
6. **No explicit group definitions**: Groups are created automatically - no `_Index.cs` or `BaseCommandGroup` needed
7. **Colored logging**: All console output uses ILogger with color-coded severity levels
8. **Centralized using statements**: All using statements managed in `Usings.cs`, auto-generated for plugins via build process
9. **Runtime plugin compilation**: Roslyn-based C# 13 compilation for plugins
10. **Aggressive caching**: SHA256-based cache invalidation for fast startup
11. **Auto-injection**: Minimal plugin boilerplate via automatic code injection
12. **Verbose logging control**: Silent by default, verbose only when needed (help/compilation)
13. **Static utility methods**: Common plugin operations (download, unzip, process start) in `CommandUtils` with `using static`
14. **Interactive console UI**: Choice menus and question prompts for user interaction
15. **Plugin PowerShell support**: Plugins can have associated .ps1 scripts in their directories
16. **Audio feedback**: Embedded WAV sound effects via `SoundPlayer` class for user feedback

## Dependencies

**NuGet Packages**:
- `Microsoft.CodeAnalysis.CSharp` (v4.12.0): Roslyn compiler for plugin system
- `NAudio` (v2.2.1): Cross-platform audio playback for sound effects

**Framework**:
- .NET 10.0 (some plugins require Windows for Registry/PowerShell operations)

## Development Guidelines

1. **Adding Commands**:
   - Use top-level commands in `Rauch.Commands` namespace for core functionality
   - Use grouped commands in `Rauch.Commands.<GroupName>` for related commands
   - Prefer plugins for experimental/optional commands
2. **Using Statements**:
   - Core code: Use global using statements in `Usings.cs`
   - Plugins: Using statements auto-injected from `Usings.g.cs` (no manual usings needed)
3. **Logging**: Use ILogger for all output, respect severity levels
4. **Validation**: Use ValidationAttributes instead of manual checks
5. **Async**: Always use async/await properly, even if no async operations
6. **Cache**: Trust the plugin cache system - it handles invalidation automatically
7. **Namespace**:
   - Top-level commands: `Rauch.Commands`
   - Grouped commands: `Rauch.Commands.<GroupName>`
   - Top-level plugins: `Rauch.Plugins`
   - Grouped plugins: `Rauch.Plugins.<GroupName>`
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
10. **Interactive UI**: Use `logger.Choice()` for menu selection and `logger.Question()` for text input
11. **Sound Feedback**: Use `SoundPlayer` methods for audio feedback:
    - `await SoundPlayer.PlaySuccess()`: After successful operations
    - `await SoundPlayer.PlayError()`: For error conditions (also called automatically by `logger.Error()`)
    - `await SoundPlayer.PlayWarning()`: For warnings (also called automatically by `logger.Warning()`)
    - `await SoundPlayer.PlayHelp()`: For help/quick actions
12. **Platform Attributes**: Use `[System.Runtime.Versioning.SupportedOSPlatform("windows")]` on methods that use Windows-specific APIs (Registry, WindowsIdentity, etc.)

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

### Example 4: Core Command with Embedded PowerShell Script
```csharp
namespace Rauch.Commands.Windows;

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

### Example 6: Interactive Configuration Wizard Plugin
```csharp
namespace Rauch.Plugins.Gump;

[Command("usr")]
public class Usr : ICommand
{
    public async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetService<ILogger>();

        async Task Run(string powershellCommand)
        {
            await ExecutePowershellCommand(powershellCommand, CommandFlags.NoProfile, logger: logger, ct: ct);
        }

        // Interactive menu selection
        var windowsLanguage = (logger?.Choice("UI and Keyboard Language", ["de-AT", "de-DE", "en-US", "custom"], 0)) switch
        {
            0 => "de-AT",
            1 => "de-DE",
            2 => "en-US",
            _ => logger?.Question("Enter UI and Keyboard Language:", allowEmpty: true),
        };

        if (windowsLanguage != string.Empty)
        {
            await Run($"Set-WinUserLanguageList {windowsLanguage} -Force");
        }

        // Yes/No choice
        if (logger?.Choice("Enable Classic right Click for W11", ["yes", "no"]) == 0)
        {
            await Run(@"New-Item -Path ""HKCU:\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"" -Value """" -Force");
        }

        // Choice with default value
        var showHiddenFiles = logger?.Choice("Show Hidden Files", ["yes", "no"], 1);
        if (showHiddenFiles == 0)
        {
            await Run(@"Set-ItemProperty -Path ""HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" -Name ""Hidden"" -Type DWord -Value 1");
        }
    }
}
```
