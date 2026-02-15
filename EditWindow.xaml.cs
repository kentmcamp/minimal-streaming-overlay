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

        // Window position settings - position anchor
        var anchors = new[] { "Bottom-Left", "Bottom-Right", "Top-Left", "Top-Right" };
        WindowAnchorCombo.ItemsSource = anchors;
        WindowAnchorCombo.SelectedItem = GetAnchorDisplayText(ownerWindow.WindowAnchor);
        WindowAnchorCombo.SelectionChanged += (s, e) => UpdatePositionPreview();

        // Margins
        WindowMarginLeftBox.Text = ownerWindow.WindowMarginLeft.ToString();
        WindowMarginBottomBox.Text = ownerWindow.WindowMarginBottom.ToString();
        UpdatePositionPreview();

        // Event handlers for position preview
        WindowMarginLeftBox.TextChanged += (s, e) => UpdatePositionPreview();
        WindowMarginBottomBox.TextChanged += (s, e) => UpdatePositionPreview();

        // Update current theme display
        UpdateCurrentThemeDisplay();
    }

    private string GetAnchorDisplayText(WindowAnchor anchor)
    {
        return anchor switch
        {
            WindowAnchor.BottomLeft => "Bottom-Left",
            WindowAnchor.BottomRight => "Bottom-Right",
            WindowAnchor.TopLeft => "Top-Left",
            WindowAnchor.TopRight => "Top-Right",
            _ => "Bottom-Left"
        };
    }

    private WindowAnchor GetAnchorFromDisplayText(string? text)
    {
        return text switch
        {
            "Bottom-Right" => WindowAnchor.BottomRight,
            "Top-Left" => WindowAnchor.TopLeft,
            "Top-Right" => WindowAnchor.TopRight,
            _ => WindowAnchor.BottomLeft
        };
    }

    private void UpdateCurrentThemeDisplay()
    {
        var currentTheme = ThemeManager.GetDefaultTheme();
        if (string.IsNullOrWhiteSpace(currentTheme))
        {
            CurrentThemeText.Text = "(default)";
        }
        else
        {
            CurrentThemeText.Text = currentTheme;
        }
    }

    private void UpdatePositionPreview()
    {
        var anchor = GetAnchorFromDisplayText(WindowAnchorCombo.SelectedItem as string);
        if (double.TryParse(WindowMarginLeftBox.Text, out var left) && double.TryParse(WindowMarginBottomBox.Text, out var bottom))
        {
            string description = anchor switch
            {
                WindowAnchor.BottomLeft => $"Bottom-Left corner: {left}px from left, {bottom}px from bottom",
                WindowAnchor.BottomRight => $"Bottom-Right corner: {left}px from right, {bottom}px from bottom",
                WindowAnchor.TopLeft => $"Top-Left corner: {left}px from left, {bottom}px from top",
                WindowAnchor.TopRight => $"Top-Right corner: {left}px from right, {bottom}px from top",
                _ => "Position: Unknown"
            };
            PositionPreviewText.Text = description;
        }
    }

    private void RefreshThemesList()
    {
        var themes = ThemeManager.GetAvailableThemes();
        ThemesListBox.ItemsSource = themes;
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

        if (!double.TryParse(WindowMarginLeftBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double marginLeft)) marginLeft = 20d;
        if (!double.TryParse(WindowMarginBottomBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double marginBottom)) marginBottom = 40d;

        var anchor = GetAnchorFromDisplayText(WindowAnchorCombo.SelectedItem as string);

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
            KeyChordHoldSeconds = chordHoldSec,
            WindowMarginLeft = marginLeft,
            WindowMarginBottom = marginBottom,
            WindowAnchor = anchor
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

        // Apply window position with anchor
        ownerWindow.ApplyWindowPosition(settings.WindowAnchor, settings.WindowMarginLeft, settings.WindowMarginBottom);
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

    private void SetAsDefaultTheme_Click(object sender, RoutedEventArgs e)
    {
        var currentTheme = ThemeManager.GetDefaultTheme();
        if (string.IsNullOrWhiteSpace(currentTheme))
        {
            MessageBox.Show("No theme is currently set as default. Please save this configuration as a theme first, then set it as default.", "Set Default Theme", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var settings = GetCurrentSettings();
        ThemeManager.SaveTheme(currentTheme, settings);
        UpdateCurrentThemeDisplay();

        MessageBox.Show($"Current configuration saved as default theme.", "Set Default Theme", MessageBoxButton.OK, MessageBoxImage.Information);
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

        // Load window position settings
        WindowAnchorCombo.SelectedItem = GetAnchorDisplayText(settings.WindowAnchor);
        WindowMarginLeftBox.Text = settings.WindowMarginLeft.ToString();
        WindowMarginBottomBox.Text = settings.WindowMarginBottom.ToString();
        UpdatePositionPreview();

        MessageBox.Show($"Theme '{themeName}' loaded. Click 'Apply' to apply changes.", "Theme Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
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
