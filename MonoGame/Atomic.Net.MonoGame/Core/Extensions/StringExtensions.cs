using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core.Extensions;

public static class StringExtensions
{
    public static bool TrySanitizeText(this string text, IReadOnlyCollection<char> validChars, [NotNullWhen(true)] out string? sanitized)
    {
        if (string.IsNullOrEmpty(text))
        {
            sanitized = text;
            return false;
        }

        var buffer = text.ToCharArray();
        bool changed = false;

        for (int i = 0; i < buffer.Length; i++)
        {
            if (!validChars.Contains(buffer[i]))
            {
                buffer[i] = '!';
                changed = true;
            }
        }

        sanitized = changed ? new string(buffer) : text;
        return changed;
    }
}
