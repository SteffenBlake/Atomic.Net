using System.Text.Json.Serialization;
using dotVariant;

namespace Atomic.Net.MonoGame.Properties;

// SteffenBlake: Review https://github.com/mknejp/dotvariant/blob/stable/README.md for more info

/// <summary>
/// Union type of supported Property types
/// Has implicit conversion operators from its supported types
/// "default" will return an "Empty" type
/// </summary>
[JsonConverter(typeof(PropertyValueConverter))]
[Variant]
public readonly partial struct PropertyValue
{
    static partial void VariantOf(
        string s, float f, bool b
    );
}
