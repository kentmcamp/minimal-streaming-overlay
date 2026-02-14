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

        // Populate font sizes
        double[] sizes = new double[] { 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72 };
        FontSizeCombo.ItemsSource = sizes;

        // Key font sizes
        KeyFontSizeCombo.ItemsSource = sizes;

        // Key font families
        KeyFontFamilyCombo.ItemsSource = families;

        // Colors list
        string[] colors = new string[] { "White", "Black", "Red", "Green", "Blue", "Yellow", "Gray", "Orange", "Purple", "Pink", "Teal", "LightGray", "DarkGray", "DarkBlue", "DarkMagenta" };
        FontColorCombo.ItemsSource = colors;
        BackgroundColorCombo.ItemsSource = colors;
        KeyFontColorCombo.ItemsSource = colors;

        // initialize with current values from owner
        FontFamilyCombo.SelectedItem = owner.TimeText.FontFamily;
        FontSizeCombo.SelectedItem = owner.TimeText.FontSize;

        if (owner.TimeText.Foreground is SolidColorBrush fbrush)
        {
            var colorName = GetColorName(fbrush.Color);
            FontColorCombo.SelectedItem = colorName;
        }

        if (owner.RootGrid.Background is SolidColorBrush bbrush)
        {
            var colorName = GetColorName(bbrush.Color);
            BackgroundColorCombo.SelectedItem = colorName ?? "Black";
            // Extract opacity from the color's alpha channel (0-255) and convert to 0-1
            OpacitySlider.Value = bbrush.Color.A / 255.0;
        }

        OpacityValue.Text = ((int)(OpacitySlider.Value * 100)).ToString() + "%";
        OpacitySlider.ValueChanged += (s, e) => OpacityValue.Text = ((int)(OpacitySlider.Value * 100)).ToString() + "%";

        // Key display defaults - read from owner's current settings
        KeyFontSizeCombo.SelectedItem = owner.KeyFontSize;
        KeyFontFamilyCombo.SelectedItem = owner.KeyFontFamily;

        var keyColorName = GetColorName(owner.KeyForegroundColor) ?? "White";
        KeyFontColorCombo.SelectedItem = keyColorName;

        KeyShowBox.Text = owner.KeyShowSeconds.ToString();
        KeyFadeBox.Text = owner.KeyFadeSeconds.ToString();
    }

    private Color ParseColorFromName(string? name, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(name)) return fallback;
        try
        {
            // Special case: "Green" should map to Lime for consistency with the visual appearance
            if (name == "Green")
                return Colors.Lime;

            // If it's a named color
            var prop = typeof(Colors).GetProperty(name);
            if (prop != null)
                return (Color)prop.GetValue(null);

            // Try converter (e.g., #FF0000)
            var conv = (Color)ColorConverter.ConvertFromString(name);
            return conv;
        }
        catch { return fallback; }
    }

    private string? GetColorName(Color color)
    {
        // Map color to the available colors in the dropdown
        string[] availableColors = new string[] { "White", "Black", "Red", "Green", "Blue", "Yellow", "Gray", "Orange", "Purple", "Pink", "Teal", "LightGray", "DarkGray", "DarkBlue", "DarkMagenta" };

        foreach (var colorName in availableColors)
        {
            var namedColor = ParseColorFromName(colorName, Colors.Black);

            // Exact match
            if (namedColor.R == color.R && namedColor.G == color.G && namedColor.B == color.B)
                return colorName;
        }

        // Special case: #00FF00 (pure lime) is likely meant to be "Green" even though Colors.Green is #008000
        if (color.R == 0 && color.G == 255 && color.B == 0)
            return "Green";

        return null;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        var ff = FontFamilyCombo.SelectedItem as FontFamily ?? ownerWindow.TimeText.FontFamily;
        var size = FontSizeCombo.SelectedItem is double d ? d : ownerWindow.TimeText.FontSize;
        var fgName = FontColorCombo.SelectedItem as string ?? "White";
        var bgName = BackgroundColorCombo.SelectedItem as string ?? "#80000000";
        var fgColor = ParseColorFromName(fgName, Colors.White);
        var bgColor = ParseColorFromName(bgName, Colors.Black);
        var opacity = OpacitySlider.Value;

        ownerWindow.ApplyOverlaySettings(ff, size, fgColor, bgColor, opacity);

        // apply key display settings
        var keyFontFamily = KeyFontFamilyCombo.SelectedItem as FontFamily ?? ownerWindow.KeyText.FontFamily;
        double keyFont = KeyFontSizeCombo.SelectedItem is double kd ? kd : ownerWindow.KeyText.FontSize;
        var keyFgName = KeyFontColorCombo.SelectedItem as string ?? "White";
        var keyFgColor = ParseColorFromName(keyFgName, Colors.White);

        if (!double.TryParse(KeyShowBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double showSec)) showSec = 1.2;
        if (!double.TryParse(KeyFadeBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double fadeSec)) fadeSec = 0.6;

        ownerWindow.ApplyKeyDisplaySettings(keyFontFamily, keyFont, keyFgColor, showSec, fadeSec);
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
