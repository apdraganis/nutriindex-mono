using System.Reflection;

namespace NutriIndex.Web;

public static class AppBuildInfo
{
    private const string BuildIdKey = "NutriIndex.BuildId";

    public static string BuildId { get; } = Resolve(BuildIdKey);

    public static (string Commit, string BuiltAtUtc) Parse()
    {
        var parts = BuildId.Split('@', 2);
        return parts.Length == 2
            ? (parts[0], parts[1])
            : (BuildId, string.Empty);
    }

    private static string Resolve(string key) =>
        typeof(App).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key)?.Value
        ?? "unknown";
}
