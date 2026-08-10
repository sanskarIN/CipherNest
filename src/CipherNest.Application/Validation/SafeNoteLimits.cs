namespace CipherNest.Application.Validation;

public static class SafeNoteLimits
{
    public const int MaximumCharacters = 200_000;
    public const int MaximumLines = 5_000;

    public static bool ExceedsLineLimit(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var lines = 1;
        for (var index = 0; index < value.Length; index++)
        {
            var ch = value[index];
            if (ch == '\n')
            {
                lines++;
            }
            else if (ch == '\r')
            {
                lines++;
                if (index + 1 < value.Length && value[index + 1] == '\n') index++;
            }

            if (lines > MaximumLines) return true;
        }
        return false;
    }
}
