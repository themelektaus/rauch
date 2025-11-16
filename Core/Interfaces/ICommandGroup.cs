namespace Rauch.Commands;

// Note: Kept in Rauch.Commands namespace for backward compatibility
// Physical location: Core/Interfaces/

/// <summary>
/// Interface for command groups that contain multiple subcommands
/// </summary>
public interface ICommandGroup : ICommand
{
    /// <summary>
    /// List of all available subcommands (also ICommand)
    /// </summary>
    IEnumerable<ICommand> SubCommands { get; }
}
