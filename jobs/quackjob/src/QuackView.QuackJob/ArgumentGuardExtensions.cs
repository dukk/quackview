internal static class ArgumentGuardExtensions
{
    public static void NotNullOrWhiteSpace(this string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(paramName);
    }

    public static void NotNullOrEmpty<T>(this IReadOnlyCollection<T>? values, string paramName)
    {
        if (values is null || values.Count == 0)
            throw new ArgumentNullException(paramName);
    }

    public static void EnsureBefore(this DateTime start, DateTime end, string? message = null)
    {
        if (end <= start)
            throw new ArgumentException(message ?? "End date must be after start date.", nameof(end));
    }
}