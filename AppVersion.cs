namespace minol;

/// <summary>
/// Application version information using semantic versioning (major.minor.patch)
/// </summary>
public static class AppVersion
{
    // Single-source version numbers
    public const int Major = 2;
    public const int Minor = 4;
    public const int Patch = 3;
    public const string PreReleaseLabel = "rc";


    public static string VersionString =>
        string.IsNullOrEmpty(PreReleaseLabel)
            ? $"{Major}.{Minor}.{Patch}"
            : $"{Major}.{Minor}.{Patch}-{PreReleaseLabel}";

    public static string DisplayName => $"v{VersionString}";

    public static string Status => "Release Candidate";

    public static string GetFullVersion() => $"{DisplayName} - {Status}";
}
