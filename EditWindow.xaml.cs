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
    private string? currentlyLoadedThemeName;
    private static readonly string[] AvailableColors = new string[]
    {
        // Light
        "White", "Snow", "Ivory", "Beige",
        "LightGray", "Gainsboro", "Silver",
        "LightYellow", "LightBlue", "LightCyan",
        "LightPink", "Lavender", "MistyRose",

        // Mid
        "Gray", "SlateGray", "SteelBlue",
        "CornflowerBlue", "Teal", "Turquoise",
        "MediumPurple", "MediumSeaGreen",
        "Orange", "Goldenrod",

        // Strong
        "Red", "Crimson", "OrangeRed",
        "Yellow", "Lime", "Cyan",
        "DodgerBlue", "RoyalBlue",
        "Magenta", "HotPink",

        // Dark
        "Black", "DimGray", "DarkGray",
        "DarkSlateGray", "DarkBlue",
        "MidnightBlue", "Navy",
        "DarkRed", "DarkGreen",
        "DarkMagenta"
    };

    private bool fontsLoaded = false;

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

        // ✅ Use the shared color list
        FontColorCombo.ItemsSource = AvailableColors;
        BackgroundColorCombo.ItemsSource = AvailableColors;
        KeyFontColorCombo.ItemsSource = AvailableColors;

        InitializeSettingsFromOwner();
        RefreshThemesList();

        OpacityValue.Text = ((int)(OpacitySlider.Value * 100)).ToString() + "%";
        OpacitySlider.ValueChanged += (s, e) =>
            OpacityValue.Text = ((int)(OpacitySlider.Value * 100)).ToString() + "%";

        AboutVersionText.Text = AppVersion.DisplayName;
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

    private void KeyFontFamilyCombo_DropDownOpened(object sender, EventArgs e)
    {
        if (fontsLoaded) return;

        var families = Fonts.SystemFontFamilies
            .OrderBy(f => f.Source)
            .ToList();

        KeyFontFamilyCombo.ItemsSource = families;

        fontsLoaded = true;
    }
    private void FontFamilyCombo_DropDownOpened(object sender, EventArgs e)
    {
        if (fontsLoaded) return;

        var families = Fonts.SystemFontFamilies
            .OrderBy(f => f.Source)
            .ToList();

        KeyFontFamilyCombo.ItemsSource = families;

        fontsLoaded = true;
    }
    private void UpdateCurrentThemeDisplay()
    {
        if (!string.IsNullOrWhiteSpace(currentlyLoadedThemeName))
        {
            CurrentThemeText.Text = currentlyLoadedThemeName;
        }
        else
        {
            var defaultTheme = ThemeManager.GetDefaultTheme();
            if (string.IsNullOrWhiteSpace(defaultTheme))
            {
                CurrentThemeText.Text = "(default)";
            }
            else
            {
                CurrentThemeText.Text = defaultTheme;
            }
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
        foreach (var colorName in AvailableColors)
        {
            var namedColor = ParseColorFromName(colorName, Colors.Black);

            if (namedColor.R == color.R &&
                namedColor.G == color.G &&
                namedColor.B == color.B)
            {
                return colorName;
            }
        }

        return null;
    }
    private AppSettings GetCurrentSettings()
    {
        var ff = FontFamilyCombo.SelectedItem as FontFamily ?? ownerWindow.TimerFontFamily;
        var size = GetComboBoxDouble(FontSizeCombo, ownerWindow.TimerFontSize);
        var fgName = FontColorCombo.SelectedItem as string ?? "Lime";
        var bgName = BackgroundColorCombo.SelectedItem as string ?? "Black";
        var fgColor = ParseColorFromName(fgName, Colors.Lime);
        var bgColor = ParseColorFromName(bgName, Colors.Black);
        var opacity = OpacitySlider.Value;

        var keyFontFamily = KeyFontFamilyCombo.SelectedItem as FontFamily ?? ownerWindow.KeyFontFamily;
        var keyFont = GetComboBoxDouble(KeyFontSizeCombo, ownerWindow.KeyFontSize);
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

        // Update display to show which theme was applied
        UpdateCurrentThemeDisplay();
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
        if (string.IsNullOrWhiteSpace(currentlyLoadedThemeName))
        {
            MessageBox.Show("Please load a theme first before setting it as default.", "Set Default Theme", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var settings = GetCurrentSettings();
        ThemeManager.SaveTheme(currentlyLoadedThemeName, settings);
        ThemeManager.SetDefaultTheme(currentlyLoadedThemeName);

        UpdateCurrentThemeDisplay();

        MessageBox.Show($"Theme '{currentlyLoadedThemeName}' set as default and will load on next startup.", "Set Default Theme", MessageBoxButton.OK, MessageBoxImage.Information);
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

        // Track the loaded theme name (but don't update display until Apply is clicked)
        currentlyLoadedThemeName = themeName;

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

    double GetComboBoxDouble(ComboBox combo, double fallback)
    {
        if (combo.SelectedItem is double d) return d;
        if (double.TryParse(combo.Text, out double typed)) return typed;
        return fallback;
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        catch { }
        ;
    }
}

