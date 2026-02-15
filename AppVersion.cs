namespace minol;

/// <summary>
/// Application version information using semantic versioning (major.minor.patch)
/// </summary>
public static class AppVersion
{
    // Single-source version numbers
    public const int Major = 0;
    public const int Minor = 5;
    public const int Patch = 0;

    // Optional prerelease label (e.g. "alpha", "beta", "rc.1"). Empty = none.
    public const string PreReleaseLabel = "alpha";


    public static string VersionString =>
        string.IsNullOrEmpty(PreReleaseLabel)
            ? $"{Major}.{Minor}.{Patch}"
            : $"{Major}.{Minor}.{Patch}-{PreReleaseLabel}";

    public static string DisplayName => $"v{VersionString}";

    public static string Status => Major == 0 ? "Pre-release" : "Stable Release";

    public static string GetFullVersion() => $"{DisplayName} - {Status}";
}
