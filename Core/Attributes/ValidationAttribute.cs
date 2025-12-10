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
    /// <returns>Tuple with (isValid, errorMessage)</returns>
    public abstract bool Validate(string[] args, out string errorMessage);
}

/// <summary>
/// Defines the minimum number of arguments
/// </summary>
public sealed class MinArgumentsAttribute(int minCount) : ValidationAttribute
{
    public int MinCount { get; } = minCount;

    public override bool Validate(string[] args, out string errorMessage)
    {
        if (args.Length < MinCount)
        {
            errorMessage = $"At least {MinCount} argument(s) required, but only {args.Length} provided.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}

/// <summary>
/// Defines the maximum number of arguments
/// </summary>
public sealed class MaxArgumentsAttribute(int maxCount) : ValidationAttribute
{
    public int MaxCount { get; } = maxCount;

    public override bool Validate(string[] args, out string errorMessage)
    {
        if (args.Length > MaxCount)
        {
            errorMessage = $"Maximum {MaxCount} argument(s) allowed, but {args.Length} provided.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}

/// <summary>
/// Defines the exact number of arguments
/// </summary>
public sealed class ExactArgumentsAttribute(int count) : ValidationAttribute
{
    public int Count { get; } = count;

    public override bool Validate(string[] args, out string errorMessage)
    {
        if (args.Length != Count)
        {
            errorMessage = $"Exactly {Count} argument(s) required, but {args.Length} provided.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}

/// <summary>
/// Validates that all arguments are numeric
/// </summary>
public sealed class NumericArgumentsAttribute : ValidationAttribute
{
    public override bool Validate(string[] args, out string errorMessage)
    {
        foreach (var arg in args)
        {
            if (!int.TryParse(arg, out _))
            {
                errorMessage = $"Argument '{arg}' is not a valid number.";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }
}
