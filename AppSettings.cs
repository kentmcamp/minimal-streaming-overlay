using System;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace minol;

public class AppSettings
{
    // Timer display settings
    public string TimerFontFamily { get; set; } = "Consolas";
    public double TimerFontSize { get; set; } = 48d;
    public string TimerForegroundColor { get; set; } = "#00FF00";  // Lime
    public string TimerBackgroundColor { get; set; } = "#000000";  // Black
    public double TimerBackgroundOpacity { get; set; } = 0.1d;

    // Key display settings
    public string KeyFontFamily { get; set; } = "Consolas";
    public double KeyFontSize { get; set; } = 28d;
    public string KeyForegroundColor { get; set; } = "#00FF00";  // Lime
    public double KeyShowSeconds { get; set; } = 1.2d;
    public double KeyFadeSeconds { get; set; } = 0.6d;
    public double KeyChordHoldSeconds { get; set; } = 0.3d;

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "minol",
        "settings.json"
    );

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }

    public static Color ParseColor(string colorHex)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(colorHex))
                return Colors.Lime;

            colorHex = colorHex.TrimStart('#');
            if (colorHex.Length == 6)
                colorHex = "FF" + colorHex;

            return (Color)ColorConverter.ConvertFromString("#" + colorHex);
        }
        catch
        {
            return Colors.Lime;
        }
    }

    public static string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
