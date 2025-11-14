# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**rauch** is a .NET 10.0 console application with a reflection-based command architecture featuring automatic command discovery, validation, async execution, and dependency injection.

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
dotnet run sum 2 3 7
dotnet run run test1
dotnet run install everything
```

## Architecture

### Command Discovery Pattern

The application uses a **namespace-based reflection system** to automatically discover and load commands:

1. **Command Groups** (`_Index.cs` files):
   - Located in `Commands/<GroupName>/_Index.cs`
   - Must inherit from `BaseCommandGroup`
   - Automatically loads all `ICommand` implementations in the same namespace as subcommands
   - Example: `Commands/Run/_Index.cs` loads `Commands/Run/Test1.cs`, `Commands/Run/Test2.cs`, etc.

2. **Standalone Commands**:
   - Located in `Commands/Standalone/`
   - Implement `ICommand` interface directly
   - Loaded as top-level commands (e.g., `sum`, `help`)

3. **Command Loading Rules** (see `CommandLoader.cs`):
   - Only loads classes named `_Index` (groups) or in `Rauch.Commands.Standalone` namespace
   - SubCommands are NOT loaded as top-level commands (prevents duplication)
   - `Help` command is special-cased and added after all other commands

### Attribute-Based Metadata System

All command metadata is declared via attributes, **never as properties**:

```csharp
[Command("sum", "Adds the specified numbers", Parameters = "<number1> <number2> ...")]
[MinArguments(1)]
[NumericArguments]
public class Sum : ICommand
{
    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
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

### Dependency Injection

**ServiceContainer** (`Core/ServiceContainer.cs`):
- Lightweight DI container supporting singletons and factories
- Configured in `Program.cs` `Main` method
- Passed to all commands via `ExecuteAsync(args, services, cancellationToken)`
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

## Adding New Commands

### Creating a Standalone Command

1. Create file in `Commands/Standalone/<CommandName>.cs`
2. Implement `ICommand` interface
3. Add `[Command]` attribute with name and description
4. Add validation attributes as needed
5. Command will be automatically discovered and loaded

```csharp
[Command("mycommand", "Description of my command", Parameters = "<arg1>")]
[MinArguments(1)]
public class MyCommand : ICommand
{
    public Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancellationToken = default)
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
   [Command("groupname", "Description", IsGroup = true)]
   public class _Index : BaseCommandGroup { }
   ```
3. Add subcommand files in same folder (e.g., `Commands/GroupName/SubCommand.cs`)
4. Subcommands are **automatically loaded** by `BaseCommandGroup` via namespace reflection

**Important**: Subcommands in a group namespace are **NOT** loaded as top-level commands by `CommandLoader`.

### Hidden/Debug Commands

```csharp
[Command("debug", "Internal debug command", Hidden = true)]
public class DebugCommand : ICommand { }
```

Hidden commands execute normally but don't appear in help output.

## Key Files

- `Program.cs`: Entry point, DI setup, command routing, validation
- `Core/CommandLoader.cs`: Reflection-based command discovery (top-level only)
- `Core/BaseCommandGroup.cs`: Base class for groups, loads subcommands via reflection
- `Core/CommandMetadata.cs`: Cached reflection helper for reading attributes
- `Core/ServiceContainer.cs`: Simple DI container
- `Core/ConsoleLogger.cs`: Console logger implementation with color support
- `Core/Interfaces/ILogger.cs`: Logger interface with severity levels
- `Core/Attributes/CommandAttribute.cs`: Command metadata declaration
- `Core/Attributes/ValidationAttribute.cs`: Base class for argument validators
- `Core/Interfaces/ICommand.cs`: All commands implement this (async pattern)
- `Core/Interfaces/ICommandGroup.cs`: Extends ICommand with SubCommands property

## Critical Design Decisions

1. **No manual command registration**: All discovery is automatic via reflection
2. **Attributes over properties**: Metadata is declared via attributes, never exposed as properties
3. **Async-first**: All commands use `Task ExecuteAsync()` for future-proofing
4. **Validation before execution**: Arguments validated in `Program.cs` before calling `ExecuteAsync`
5. **Namespace-based organization**: Command groups use namespace + `_Index.cs` pattern
6. **Separation of concerns**: Top-level commands loaded by `CommandLoader`, subcommands loaded by `BaseCommandGroup`
7. **Colored logging**: All console output uses ILogger with color-coded severity levels
