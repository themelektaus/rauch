using System.Collections.Concurrent;
using System.Reflection;

namespace Rauch.Core;

/// <summary>
/// Helper class for reading command metadata from attributes with caching
/// </summary>
public static class CommandMetadata
{
    // Cache for attributes to reduce reflection overhead
    private static readonly ConcurrentDictionary<Type, CommandAttribute> _attributeCache = new();
    private static readonly ConcurrentDictionary<Type, ValidationAttribute[]> _validationCache = new();

    /// <summary>
    /// Reads the CommandAttribute from a command class (with caching)
    /// </summary>
    public static CommandAttribute GetAttribute(ICommand command)
    {
        return GetAttribute(command.GetType());
    }

    /// <summary>
    /// Reads the CommandAttribute from a type (with caching)
    /// </summary>
    public static CommandAttribute GetAttribute(Type type)
    {
        return _attributeCache.GetOrAdd(type, t => t.GetCustomAttribute<CommandAttribute>());
    }

    /// <summary>
    /// Reads all ValidationAttributes from a command (with caching)
    /// </summary>
    public static ValidationAttribute[] GetValidationAttributes(ICommand command)
    {
        return GetValidationAttributes(command.GetType());
    }

    /// <summary>
    /// Reads all ValidationAttributes from a type (with caching)
    /// </summary>
    public static ValidationAttribute[] GetValidationAttributes(Type type)
    {
        return _validationCache.GetOrAdd(type, t => t.GetCustomAttributes<ValidationAttribute>().ToArray());
    }

    /// <summary>
    /// Returns the name of the command
    /// </summary>
    public static string GetName(ICommand command)
    {
        var attr = GetAttribute(command);
        return attr?.Name ?? command.GetType().Name.ToLower();
    }

    /// <summary>
    /// Returns the description of the command
    /// </summary>
    public static string GetDescription(ICommand command)
    {
        var attr = GetAttribute(command);
        return attr?.Description ?? "No description available";
    }

    /// <summary>
    /// Returns the usage text of the command
    /// </summary>
    public static string GetUsage(ICommand command, string parentCommand = null)
    {
        var attr = GetAttribute(command);
        if (attr != null)
        {
            return attr.GetUsage(parentCommand);
        }

        // Fallback if no attribute is present
        var name = GetName(command);
        return parentCommand != null ? $"rauch {parentCommand} {name}" : $"rauch {name}";
    }

    /// <summary>
    /// Checks if a command is hidden
    /// </summary>
    public static bool IsHidden(ICommand command)
    {
        var attr = GetAttribute(command);
        return attr?.Hidden ?? false;
    }

    /// <summary>
    /// Checks if a command name matches
    /// </summary>
    public static bool MatchesName(ICommand command, string name)
    {
        var attr = GetAttribute(command);
        return attr?.MatchesName(name) ?? GetName(command).Equals(name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates arguments against all ValidationAttributes
    /// </summary>
    public static (bool isValid, string errorMessage) ValidateArguments(ICommand command, string[] args)
    {
        var validations = GetValidationAttributes(command);

        foreach (var validation in validations)
        {
            var result = validation.Validate(args);
            if (!result.isValid)
            {
                return result;
            }
        }

        return (true, null);
    }
}
