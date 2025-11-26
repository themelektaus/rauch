namespace Rauch.Core.Attributes;

/// <summary>
/// Attribute for describing commands and subcommands
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CommandAttribute : Attribute
{
    /// <summary>
    /// The name of the command
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Description of the command
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Optional parameter description for usage (e.g. "<number1> <number2>")
    /// If null, will be auto-generated
    /// </summary>
    public string Parameters { get; set; }

    /// <summary>
    /// Hides the command in help output (for debug/internal commands)
    /// </summary>
    public bool Hidden { get; set; }

    public CommandAttribute(string name, string description = "")
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Generates the usage text based on the command name and parameters
    /// </summary>
    public string GetUsage(string parentCommand = null)
    {
        var prefix = parentCommand != null ? $"rauch {parentCommand}" : "rauch";

        if (!string.IsNullOrEmpty(Parameters))
        {
            return $"{prefix} {Name} {Parameters}";
        }

        return $"{prefix} {Name}";
    }

    /// <summary>
    /// Checks if a given name matches this command
    /// </summary>
    public bool MatchesName(string name)
    {
        return Name.Equals(name, StringComparison.OrdinalIgnoreCase);
    }
}
