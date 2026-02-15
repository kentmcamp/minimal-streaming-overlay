using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;

namespace minol;

public partial class EditWindow : Window
{
    private readonly MainWindow ownerWindow;

    public EditWindow(MainWindow owner)
    {
        InitializeComponent();
        ownerWindow = owner;

        // Populate font families
        var families = Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList();
        FontFamilyCombo.ItemsSource = families;
        KeyFontFamilyCombo.ItemsSource = families;

        // Populate font sizes
        double[] sizes = new double[] { 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72 };
        FontSizeCombo.ItemsSource = sizes;
        KeyFontSizeCombo.ItemsSource = sizes;

        // Colors list
        string[] colors = new string[] { "White", "Black", "Red", "Green", "Blue", "Yellow", "Gray", "Orange", "Purple", "Pink", "Teal", "LightGray", "DarkGray", "DarkBlue", "DarkMagenta" };
        FontColorCombo.ItemsSource = colors;
        BackgroundColorCombo.ItemsSource = colors;
        KeyFontColorCombo.ItemsSource = colors;

        // Initialize with current values
        InitializeSettingsFromOwner();
        RefreshThemesList();
        CheckDefaultTheme();

        // Opacity value display
        OpacityValue.Text = ((int)(OpacitySlider.Value * 100)).ToString() + "%";
        OpacitySlider.ValueChanged += (s, e) => OpacityValue.Text = ((int)(OpacitySlider.Value * 100)).ToString() + "%";
    }

    private void InitializeSettingsFromOwner()
    {
        // Timer settings
        FontFamilyCombo.SelectedItem = ownerWindow.TimerFontFamily;
        FontSizeCombo.SelectedItem = ownerWindow.TimerFontSize;
        FontColorCombo.SelectedItem = GetColorName(ownerWindow.TimerForegroundColor) ?? "Lime";
        BackgroundColorCombo.SelectedItem = GetColorName(ownerWindow.TimerBackgroundColor) ?? "Black";
        OpacitySlider.Value = ownerWindow.TimerBackgroundOpacity;

        // Key settings
        KeyFontSizeCombo.SelectedItem = ownerWindow.KeyFontSize;
        KeyFontFamilyCombo.SelectedItem = ownerWindow.KeyFontFamily;
        KeyFontColorCombo.SelectedItem = GetColorName(ownerWindow.KeyForegroundColor) ?? "Lime";
        KeyShowBox.Text = ownerWindow.KeyShowSeconds.ToString();
        KeyFadeBox.Text = ownerWindow.KeyFadeSeconds.ToString();
        KeyChordHoldBox.Text = ownerWindow.KeyChordHoldSeconds.ToString();
    }

    private void RefreshThemesList()
    {
        var themes = ThemeManager.GetAvailableThemes();
        ThemesListBox.ItemsSource = themes;
        ThemeCombo.ItemsSource = themes;

        if (themes.Count > 0)
        {
            ThemeCombo.SelectedIndex = 0;
        }
    }

    private void CheckDefaultTheme()
    {
        var defaultTheme = ThemeManager.GetDefaultTheme();
        SetDefaultCheckBox.IsChecked = !string.IsNullOrWhiteSpace(defaultTheme);
    }

    private Color ParseColorFromName(string? name, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(name)) return fallback;
        try
        {
            if (name == "Green")
                return Colors.Lime;

            var prop = typeof(Colors).GetProperty(name);
            if (prop != null)
                return (Color)prop.GetValue(null);

            var conv = (Color)ColorConverter.ConvertFromString(name);
            return conv;
        }
        catch { return fallback; }
    }

    private string? GetColorName(Color color)
    {
        string[] availableColors = new string[] { "White", "Black", "Red", "Green", "Blue", "Yellow", "Gray", "Orange", "Purple", "Pink", "Teal", "LightGray", "DarkGray", "DarkBlue", "DarkMagenta" };

        foreach (var colorName in availableColors)
        {
            var namedColor = ParseColorFromName(colorName, Colors.Black);
            if (namedColor.R == color.R && namedColor.G == color.G && namedColor.B == color.B)
                return colorName;
        }

        if (color.R == 0 && color.G == 255 && color.B == 0)
            return "Green";

        return null;
    }

    private AppSettings GetCurrentSettings()
    {
        var ff = FontFamilyCombo.SelectedItem as FontFamily ?? ownerWindow.TimerFontFamily;
        var size = FontSizeCombo.SelectedItem is double d ? d : ownerWindow.TimerFontSize;
        var fgName = FontColorCombo.SelectedItem as string ?? "Lime";
        var bgName = BackgroundColorCombo.SelectedItem as string ?? "Black";
        var fgColor = ParseColorFromName(fgName, Colors.Lime);
        var bgColor = ParseColorFromName(bgName, Colors.Black);
        var opacity = OpacitySlider.Value;

        var keyFontFamily = KeyFontFamilyCombo.SelectedItem as FontFamily ?? ownerWindow.KeyFontFamily;
        double keyFont = KeyFontSizeCombo.SelectedItem is double kd ? kd : ownerWindow.KeyFontSize;
        var keyFgName = KeyFontColorCombo.SelectedItem as string ?? "Lime";
        var keyFgColor = ParseColorFromName(keyFgName, Colors.Lime);

        if (!double.TryParse(KeyShowBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double showSec)) showSec = 1.2;
        if (!double.TryParse(KeyFadeBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double fadeSec)) fadeSec = 0.6;
        if (!double.TryParse(KeyChordHoldBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double chordHoldSec)) chordHoldSec = 0.3;

        return new AppSettings
        {
            TimerFontFamily = ff.Source,
            TimerFontSize = size,
            TimerForegroundColor = AppSettings.ColorToHex(fgColor),
            TimerBackgroundColor = AppSettings.ColorToHex(bgColor),
            TimerBackgroundOpacity = opacity,
            KeyFontFamily = keyFontFamily.Source,
            KeyFontSize = keyFont,
            KeyForegroundColor = AppSettings.ColorToHex(keyFgColor),
            KeyShowSeconds = showSec,
            KeyFadeSeconds = fadeSec,
            KeyChordHoldSeconds = chordHoldSec
        };
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = GetCurrentSettings();

        var fgColor = AppSettings.ParseColor(settings.TimerForegroundColor);
        var bgColor = AppSettings.ParseColor(settings.TimerBackgroundColor);
        var keyFgColor = AppSettings.ParseColor(settings.KeyForegroundColor);

        try
        {
            var ff = new FontFamily(settings.TimerFontFamily);
            ownerWindow.ApplyOverlaySettings(ff, settings.TimerFontSize, fgColor, bgColor, settings.TimerBackgroundOpacity);
        }
        catch { }

        try
        {
            var keyFf = new FontFamily(settings.KeyFontFamily);
            ownerWindow.ApplyKeyDisplaySettings(keyFf, settings.KeyFontSize, keyFgColor, settings.KeyShowSeconds, settings.KeyFadeSeconds, settings.KeyChordHoldSeconds);
        }
        catch { }

        if (SetDefaultCheckBox.IsChecked == true)
        {
            // Theme name comes from ComboBox or "Current" if creating new
            var themeName = ThemeCombo.SelectedItem as string ?? "Default";
            ThemeManager.SetDefaultTheme(themeName);
        }
    }

    private void SaveThemeButton_Click(object sender, RoutedEventArgs e)
    {
        var themeName = NewThemeNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(themeName))
        {
            MessageBox.Show("Please enter a theme name.", "Save Theme", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var settings = GetCurrentSettings();
        ThemeManager.SaveTheme(themeName, settings);

        MessageBox.Show($"Theme '{themeName}' saved successfully.", "Save Theme", MessageBoxButton.OK, MessageBoxImage.Information);

        NewThemeNameBox.Text = "My Theme";
        RefreshThemesList();
    }

    private void LoadTheme_Click(object sender, RoutedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is string themeName)
        {
            LoadThemeByName(themeName);
        }
    }

    private void LoadSelectedTheme_Click(object sender, RoutedEventArgs e)
    {
        if (ThemesListBox.SelectedItem is string themeName)
        {
            LoadThemeByName(themeName);
        }
    }

    private void LoadThemeByName(string themeName)
    {
        var settings = ThemeManager.LoadTheme(themeName);

        // Update UI with loaded settings
        try
        {
            var ff = new FontFamily(settings.TimerFontFamily);
            FontFamilyCombo.SelectedItem = ff;
        }
        catch { }

        FontSizeCombo.SelectedItem = settings.TimerFontSize;
        FontColorCombo.SelectedItem = GetColorName(AppSettings.ParseColor(settings.TimerForegroundColor)) ?? "Lime";
        BackgroundColorCombo.SelectedItem = GetColorName(AppSettings.ParseColor(settings.TimerBackgroundColor)) ?? "Black";
        OpacitySlider.Value = settings.TimerBackgroundOpacity;

        try
        {
            var keyFf = new FontFamily(settings.KeyFontFamily);
            KeyFontFamilyCombo.SelectedItem = keyFf;
        }
        catch { }

        KeyFontSizeCombo.SelectedItem = settings.KeyFontSize;
        KeyFontColorCombo.SelectedItem = GetColorName(AppSettings.ParseColor(settings.KeyForegroundColor)) ?? "Lime";
        KeyShowBox.Text = settings.KeyShowSeconds.ToString();
        KeyFadeBox.Text = settings.KeyFadeSeconds.ToString();
        KeyChordHoldBox.Text = settings.KeyChordHoldSeconds.ToString();

        MessageBox.Show($"Theme '{themeName}' loaded. Click 'Apply' to apply changes.", "Theme Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeleteTheme_Click(object sender, RoutedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is string themeName)
        {
            if (MessageBox.Show($"Delete theme '{themeName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ThemeManager.DeleteTheme(themeName);
                RefreshThemesList();
                MessageBox.Show($"Theme '{themeName}' deleted.", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void DeleteSelectedTheme_Click(object sender, RoutedEventArgs e)
    {
        if (ThemesListBox.SelectedItem is string themeName)
        {
            if (MessageBox.Show($"Delete theme '{themeName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ThemeManager.DeleteTheme(themeName);
                RefreshThemesList();
            }
        }
    }

    private void SaveTheme_Click(object sender, RoutedEventArgs e)
    {
        // This is for the top button bar - same as SaveThemeButton_Click
        SaveThemeButton_Click(sender, e);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow() { Owner = this };
        about.ShowDialog();
    }
}
