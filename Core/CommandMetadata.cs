using System.Collections.Concurrent;
using System.Reflection;

namespace Rauch.Core;

public static class CommandMetadata
{
    static readonly ConcurrentDictionary<Type, Metadata> _metadataCache = new();

    public class Metadata
    {
        public string Name { get; init; }
        public string Keywords { get; init; }
        public string Description { get; init; }
        public ValidationAttribute[] Validations { get; set; }

        public bool MatchesName(string name)
        {
            return Name.Equals(name, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static Metadata Get(ICommand command)
    {
        var type = command.GetType();
        var name = type.GetCustomAttribute<NameAttribute>()?.Name ?? command.GetType().Name.ToLower();
        var keywords = type.GetCustomAttribute<KeywordsAttribute>()?.Keywords ?? string.Empty;
        return _metadataCache.GetOrAdd(type, type => new()
        {
            Name = name,
            Keywords = $"{name} {keywords}".Trim().ToLower(),
            Description = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
            Validations = [.. type.GetCustomAttributes<ValidationAttribute>()]
        });
    }

    /// <summary>
    /// Validates arguments against all ValidationAttributes
    /// </summary>
    public static bool ValidateArguments(ICommand command, string[] args, out string errorMessage)
    {
        foreach (var validation in Get(command).Validations)
        {
            if (!validation.Validate(args, out errorMessage))
            {
                return false;
            }
        }

        errorMessage = null;
        return true;
    }
}
