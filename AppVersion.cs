namespace minol;

/// <summary>
/// Application version information using semantic versioning (major.minor.patch)
/// </summary>
public static class AppVersion
{
    // Single-source version numbers
    public const int Major = 1;
    public const int Minor = 1;
    public const int Patch = 5;
    public const string PreReleaseLabel = "beta";


    public static string VersionString =>
        string.IsNullOrEmpty(PreReleaseLabel)
            ? $"{Major}.{Minor}.{Patch}"
            : $"{Major}.{Minor}.{Patch}-{PreReleaseLabel}";

    public static string DisplayName => $"v{VersionString}";

    public static string Status => "Pre-release";

    public static string GetFullVersion() => $"{DisplayName} - {Status}";
}
