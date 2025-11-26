namespace Rauch.Core.Attributes;

/// <summary>
/// Base attribute for argument validation
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public abstract class ValidationAttribute : Attribute
{
    /// <summary>
    /// Validates the provided arguments
    /// </summary>
    /// <param name="args">The arguments to validate</param>
    /// <returns>Tuple with (IsValid, ErrorMessage)</returns>
    public abstract (bool IsValid, string ErrorMessage) Validate(string[] args);
}

/// <summary>
/// Defines the minimum number of arguments
/// </summary>
public sealed class MinArgumentsAttribute : ValidationAttribute
{
    public int MinCount { get; }

    public MinArgumentsAttribute(int minCount)
    {
        MinCount = minCount;
    }

    public override (bool IsValid, string ErrorMessage) Validate(string[] args)
    {
        if (args.Length < MinCount)
        {
            return (false, $"At least {MinCount} argument(s) required, but only {args.Length} provided.");
        }
        return (true, null);
    }
}

/// <summary>
/// Defines the maximum number of arguments
/// </summary>
public sealed class MaxArgumentsAttribute : ValidationAttribute
{
    public int MaxCount { get; }

    public MaxArgumentsAttribute(int maxCount)
    {
        MaxCount = maxCount;
    }

    public override (bool IsValid, string ErrorMessage) Validate(string[] args)
    {
        if (args.Length > MaxCount)
        {
            return (false, $"Maximum {MaxCount} argument(s) allowed, but {args.Length} provided.");
        }
        return (true, null);
    }
}

/// <summary>
/// Defines the exact number of arguments
/// </summary>
public sealed class ExactArgumentsAttribute : ValidationAttribute
{
    public int Count { get; }

    public ExactArgumentsAttribute(int count)
    {
        Count = count;
    }

    public override (bool IsValid, string ErrorMessage) Validate(string[] args)
    {
        if (args.Length != Count)
        {
            return (false, $"Exactly {Count} argument(s) required, but {args.Length} provided.");
        }
        return (true, null);
    }
}

/// <summary>
/// Validates that all arguments are numeric
/// </summary>
public sealed class NumericArgumentsAttribute : ValidationAttribute
{
    public override (bool IsValid, string ErrorMessage) Validate(string[] args)
    {
        foreach (var arg in args)
        {
            if (!int.TryParse(arg, out _))
            {
                return (false, $"Argument '{arg}' is not a valid number.");
            }
        }
        return (true, null);
    }
}
