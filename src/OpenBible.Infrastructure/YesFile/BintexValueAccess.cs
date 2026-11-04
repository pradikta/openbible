namespace OpenBible.Infrastructure.YesFile;

/// <summary>
/// Strongly typed accessors over the values produced by <see cref="BintexValueMapReader"/>.
/// </summary>
internal static class BintexValueAccess
{
    public static string? GetString(IReadOnlyDictionary<string, object?> values, string key) =>
        values.TryGetValue(key, out var value) ? value as string : null;

    public static int GetInt(IReadOnlyDictionary<string, object?> values, string key, int defaultValue) =>
        values.TryGetValue(key, out var value) && value is int integer ? integer : defaultValue;

    public static int[] GetIntArray(IReadOnlyDictionary<string, object?> values, string key) =>
        values.TryGetValue(key, out var value) && value is int[] integers ? integers : [];

    public static IReadOnlyDictionary<string, object?>? GetMap(IReadOnlyDictionary<string, object?> values, string key) =>
        values.TryGetValue(key, out var value) ? value as IReadOnlyDictionary<string, object?> : null;
}
