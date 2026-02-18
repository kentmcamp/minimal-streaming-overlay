namespace minol;

/// <summary>
/// Application version information using semantic versioning (major.minor.patch)
/// </summary>
public static class AppVersion
{
    // Single-source version numbers
    public const int Major = 1;
    public const int Minor = 3;
    public const int Patch = 0;

    public static string VersionString => $"{Major}.{Minor}.{Patch}";

    public static string DisplayName => $"v{VersionString}";

    public static string Status => "Stable";

    public static string GetFullVersion() => $"{DisplayName} - {Status}";
}
