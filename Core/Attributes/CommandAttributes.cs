namespace Rauch.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class NameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class KeywordsAttribute(string keyword) : Attribute
{
    public string Keywords { get; } = keyword;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class DescriptionAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}
