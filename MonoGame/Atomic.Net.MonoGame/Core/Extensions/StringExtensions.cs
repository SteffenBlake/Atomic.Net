using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Attempts to sanitize text by replacing invalid characters with '!'.
    /// Returns true if text was modified (had invalid characters), false if text was already clean or null/empty.
    /// The sanitized parameter will contain the result: modified text if changed, original text if clean, or null/empty if input was null/empty.
    /// </summary>
    public static bool TrySanitizeText(this string text, IReadOnlyCollection<char> validChars, out string sanitized)
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
