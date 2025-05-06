using dotVariant;

namespace Atomic.Net.Asp.Domain;

[Variant]
public partial class Result<T>
{
    static partial void VariantOf(
        T a,
        NotFound c,
        Conflict d,
        Unauthorized e
    );
}

public readonly struct NotFound(
    string name, object idValue
)
{
    public string Message { get; } = $"No {name} found with id '{idValue}'";
}

public readonly struct Conflict(
    string name, object value
)
{
    public string Message { get; } = $"{name} already exists with value of '{value}'";
}

public readonly record struct Unauthorized();
