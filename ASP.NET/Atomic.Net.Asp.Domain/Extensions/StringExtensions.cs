namespace Atomic.Net.Asp.Domain.Extensions;

public static class StringExtensions
{
    // Example atomic unit testable code
    // Note that this code is pure and functional
    // No side effects, static, etc etc
    public static string Sanitize(this string s)
    {
        if (s.Length == 0)
        {
            return "";
        }
        if (s.Length <= 4)
        {
            return "****";
        }

        return $"{s[0]}****{s[^1]}";
    }
}
