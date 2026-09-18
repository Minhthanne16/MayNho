namespace MayNho.Domain;

public static class Title
{
    public static string Required(string? value, int maximumLength = 200)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumLength);
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > maximumLength)
            throw new ArgumentException($"Title must contain 1–{maximumLength} characters.", nameof(value));
        return trimmed;
    }
}
