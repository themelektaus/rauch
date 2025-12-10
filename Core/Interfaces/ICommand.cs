namespace Rauch.Commands;

// Note: Kept in Rauch.Commands namespace for backward compatibility
// Physical location: Core/Interfaces/

/// <summary>
/// Base interface for all commands (commands and subcommands)
/// Name, description, and usage are automatically read from the CommandAttribute
/// Supports asynchronous execution with CancellationToken and dependency injection
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Executes the command asynchronously
    /// </summary>
    /// <param name="args">The arguments without the command name itself</param>
    /// <param name="services">Service provider for dependency injection</param>
    /// <param name="ct">Token for cancellation requests</param>
    Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken ct);
}
