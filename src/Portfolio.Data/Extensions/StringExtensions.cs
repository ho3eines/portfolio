namespace Portfolio.Data;

public static class StringExtensions
{
    public static string? Truncate(this string? value, int maxLength)
        => string.IsNullOrEmpty(value) ? value : value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
}
