using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace minol;

public class ThemeManager
{
    private static readonly string ThemesDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "minol",
        "Themes"
    );

    private static readonly string DefaultThemeMarkerPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "minol",
        "default_theme.txt"
    );

    public static void EnsureThemesDirectory()
    {
        if (!Directory.Exists(ThemesDirectory))
        {
            Directory.CreateDirectory(ThemesDirectory);
        }
    }

    public static void SaveTheme(string themeName, AppSettings settings)
    {
        EnsureThemesDirectory();

        // Remove invalid filename characters
        var safeThemeName = string.Concat(themeName.Split(Path.GetInvalidFileNameChars()));
        var themePath = Path.Combine(ThemesDirectory, $"{safeThemeName}.json");

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(themePath, json);
        }
        catch { }
    }

    public static AppSettings LoadTheme(string themeName)
    {
        var safeThemeName = string.Concat(themeName.Split(Path.GetInvalidFileNameChars()));
        var themePath = Path.Combine(ThemesDirectory, $"{safeThemeName}.json");

        try
        {
            if (File.Exists(themePath))
            {
                var json = File.ReadAllText(themePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }

        return new AppSettings();
    }

    public static AppSettings LoadThemeFromPath(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
            {
                var json = File.ReadAllText(fullPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }

        return new AppSettings();
    }

    public static List<string> GetAvailableThemes()
    {
        EnsureThemesDirectory();

        try
        {
            return Directory.GetFiles(ThemesDirectory, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .OrderBy(name => name)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public static void SetDefaultTheme(string themeName)
    {
        try
        {
            var directory = Path.GetDirectoryName(DefaultThemeMarkerPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(DefaultThemeMarkerPath, themeName);
        }
        catch { }
    }

    public static string GetDefaultTheme()
    {
        try
        {
            if (File.Exists(DefaultThemeMarkerPath))
            {
                var themeName = File.ReadAllText(DefaultThemeMarkerPath).Trim();
                if (!string.IsNullOrWhiteSpace(themeName))
                {
                    return themeName;
                }
            }
        }
        catch { }

        return null;
    }

    public static void DeleteTheme(string themeName)
    {
        var safeThemeName = string.Concat(themeName.Split(Path.GetInvalidFileNameChars()));
        var themePath = Path.Combine(ThemesDirectory, $"{safeThemeName}.json");

        try
        {
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
            }
        }
        catch { }
    }

    public static bool ThemeExists(string themeName)
    {
        var safeThemeName = string.Concat(themeName.Split(Path.GetInvalidFileNameChars()));
        var themePath = Path.Combine(ThemesDirectory, $"{safeThemeName}.json");
        return File.Exists(themePath);
    }
}
