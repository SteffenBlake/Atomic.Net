using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Extension methods for Type used by the JsonExpression system.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Attempts to resolve a dot-split property path on a type, returning the full PropertyInfo chain.
    /// Used by both converters (at load time) and InferJsonExpressionType to avoid duplicate navigation logic.
    /// </summary>
    public static bool TryResolvePropertyChain(
        this Type startType,
        string[] properties,
        [NotNullWhen(true)] out PropertyInfo[]? chain
    )
    {
        var result = new PropertyInfo[properties.Length];
        var currentType = startType;

        for (var i = 0; i < properties.Length; i++)
        {
            var info = currentType.GetProperty(properties[i]);
            if (info is null)
            {
                chain = null;
                return false;
            }

            result[i] = info;
            currentType = info.PropertyType;
        }

        chain = result;
        return true;
    }
}
