using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace minol;

public partial class ValueAnalyzerWindow : Window
{
    private Bitmap? originalBitmap;
    private Bitmap? currentProcessedBitmap;
    private byte posterizeLevels = 8;
    private byte blackLevel = 0;
    private byte whiteLevel = 255;
    private bool flipHorizontal = false;
    private bool flipVertical = false;

    private DispatcherTimer? updateTimer;

    public ValueAnalyzerWindow(Bitmap? screenshot)
    {
        InitializeComponent();

        posterizeLevels = (byte)PosterizeSlider.Value;
        blackLevel = (byte)BlackLevelSlider.Value;
        whiteLevel = (byte)WhiteLevelSlider.Value;

        PosterizeLevelText.Text = posterizeLevels.ToString();
        BlackLevelText.Text = blackLevel.ToString();
        WhiteLevelText.Text = whiteLevel.ToString();

        //Timer for debouncing sliders
        updateTimer = new DispatcherTimer();
        updateTimer.Interval = TimeSpan.FromMilliseconds(30);
        updateTimer.Tick += (s, e) =>
        {
            updateTimer.Stop();
            ProcessAndDisplay();
        };

        if (screenshot == null)
        {
            MessageBox.Show("Failed to capture screenshot", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            this.Close();
            return;
        }

        originalBitmap = screenshot;
        currentProcessedBitmap = null;

        // Defer processing and centering until window is loaded
        this.Loaded += (s, e) =>
        {
            CenterWindowOnMonitor();
            ProcessAndDisplay();
        };
    }

    private void CenterWindowOnMonitor()
    {
        try
        {
            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var workingArea = screen.WorkingArea;

            this.Left = workingArea.Left + (workingArea.Width - this.Width) / 2;
            this.Top = workingArea.Top + (workingArea.Height - this.Height) / 2;
        }
        catch { }
    }

    private void ProcessAndDisplay()
    {
        if (originalBitmap == null)
            return;

        try
        {
            // Process the image based on current settings
            var processed = ProcessImage(originalBitmap, posterizeLevels, blackLevel, whiteLevel, flipHorizontal, flipVertical);

            // Update current processed bitmap for reprocessing
            if (currentProcessedBitmap != null)
                currentProcessedBitmap.Dispose();
            currentProcessedBitmap = processed;

            // Convert to WriteableBitmap for display
            var writeableBitmap = ConvertBitmapToWriteableBitmap(processed);
            DisplayImage.Source = writeableBitmap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Image processing error: {ex.Message}");
        }
    }


    /// Process the image from the original, applying all transformations in sequence.
    private Bitmap ProcessImage(Bitmap source, byte posterizeLevels, byte blackLevel, byte whiteLevel, bool flipH, bool flipV)
    {
        // Create a working copy
        var working = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppRgb);

        // Convert to grayscale and apply levels and posterization
        ApplyGrayscaleAndLevels(source, working, posterizeLevels, blackLevel, whiteLevel);

        // Apply flips if needed
        if (flipH || flipV)
        {
            ApplyFlips(working, flipH, flipV);
        }

        return working;
    }


    /// Apply grayscale conversion, level remapping, and posterization using LockBits for performance.
    private void ApplyGrayscaleAndLevels(Bitmap source, Bitmap destination, byte posterizeLevels, byte blackLevel, byte whiteLevel)
    {
        BitmapData srcData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData dstData = destination.LockBits(new Rectangle(0, 0, destination.Width, destination.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);

        try
        {
            unsafe
            {
                byte* srcPtr = (byte*)srcData.Scan0.ToPointer();
                byte* dstPtr = (byte*)dstData.Scan0.ToPointer();

                int srcStride = srcData.Stride;
                int dstStride = dstData.Stride;

                for (int y = 0; y < source.Height; y++)
                {
                    for (int x = 0; x < source.Width; x++)
                    {
                        int srcOffset = y * srcStride + x * 4;
                        int dstOffset = y * dstStride + x * 4;

                        // Read ARGB (source is ARGB format)
                        byte b = srcPtr[srcOffset];
                        byte g = srcPtr[srcOffset + 1];
                        byte r = srcPtr[srcOffset + 2];

                        // Convert to grayscale using proper luminance formula
                        // L = 0.2126R + 0.7152G + 0.0722B
                        byte luminance = (byte)(0.2126 * r + 0.7152 * g + 0.0722 * b);

                        // Apply black/white level remapping
                        float remapped = (luminance - blackLevel) / (float)(whiteLevel - blackLevel);
                        remapped = Math.Max(0, Math.Min(1, remapped)); // Clamp 0-1
                        byte remappedValue = (byte)(remapped * 255);

                        // Apply posterization
                        byte posterized = PosterizeValue(remappedValue, posterizeLevels);

                        // Write to destination (RGB format, so same value for all channels)
                        dstPtr[dstOffset] = posterized;      // B
                        dstPtr[dstOffset + 1] = posterized;  // G
                        dstPtr[dstOffset + 2] = posterized;  // R
                        dstPtr[dstOffset + 3] = 255;         // A (fully opaque)
                    }
                }
            }
        }
        finally
        {
            source.UnlockBits(srcData);
            destination.UnlockBits(dstData);
        }
    }


    /// Apply posterization to a single value.
    /// Quantizes the value to one of N discrete levels.
    /// step = 255 / (levels - 1)
    /// index = round(value / step)  // which level?
    /// newValue = index * step       // that level's value

    private byte PosterizeValue(byte value, byte levels)
    {
        if (levels <= 1)
            return 0;

        float step = 255f / (levels - 1);
        float index = MathF.Round((float)value / step);
        byte quantized = (byte)(index * step);
        return quantized;
    }

    /// Apply horizontal and/or vertical flips to the bitmap.
    private void ApplyFlips(Bitmap bitmap, bool horizontal, bool vertical)
    {
        if (horizontal)
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
        if (vertical)
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
    }

    /// Convert a System.Drawing.Bitmap to a WPF WriteableBitmap for display.
    private WriteableBitmap ConvertBitmapToWriteableBitmap(Bitmap bitmap)
    {
        var writeableBitmap = new WriteableBitmap(bitmap.Width, bitmap.Height, 96, 96, System.Windows.Media.PixelFormats.Bgr32, null);

        BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

        try
        {
            writeableBitmap.WritePixels(
                new Int32Rect(0, 0, bitmap.Width, bitmap.Height),
                bitmapData.Scan0,
                bitmapData.Stride * bitmap.Height,
                bitmapData.Stride
            );
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        writeableBitmap.Freeze(); // Freeze for better performance
        return writeableBitmap;
    }

    // Level Sliders
    private void PosterizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;

        posterizeLevels = (byte)e.NewValue;
        PosterizeLevelText.Text = posterizeLevels.ToString();
        // ProcessAndDisplay();
        updateTimer?.Stop();
        updateTimer?.Start();
    }

    private void BlackLevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;

        blackLevel = (byte)e.NewValue;
        BlackLevelText.Text = blackLevel.ToString();

        if (blackLevel >= whiteLevel)
        {
            whiteLevel = (byte)Math.Min(255, blackLevel + 1);
            WhiteLevelSlider.Value = whiteLevel;
            WhiteLevelText.Text = whiteLevel.ToString();
        }

        // ProcessAndDisplay();
        updateTimer?.Stop();
        updateTimer?.Start();
    }

    private void WhiteLevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;

        whiteLevel = (byte)e.NewValue;
        WhiteLevelText.Text = whiteLevel.ToString();

        // Prevent invalid state where black >= white
        if (whiteLevel <= blackLevel)
        {
            blackLevel = (byte)Math.Max(0, whiteLevel - 1);
            BlackLevelSlider.Value = blackLevel;
            BlackLevelText.Text = blackLevel.ToString();
        }

        // ProcessAndDisplay();
        updateTimer?.Stop();
        updateTimer?.Start();
    }

    // Flip/mirror buttons
    private void FlipHorizontal_Click(object sender, RoutedEventArgs e)
    {
        flipHorizontal = !flipHorizontal;
        FlipHorizontalButton.Background = flipHorizontal ? System.Windows.Media.Brushes.DarkGreen : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64));
        // ProcessAndDisplay();
        updateTimer?.Stop();
        updateTimer?.Start();
    }

    private void FlipVertical_Click(object sender, RoutedEventArgs e)
    {
        flipVertical = !flipVertical;
        FlipVerticalButton.Background = flipVertical ? System.Windows.Media.Brushes.DarkGreen : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64));
        updateTimer?.Stop();
        updateTimer?.Start();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Clean up resources
        if (originalBitmap != null)
        {
            originalBitmap.Dispose();
            originalBitmap = null;
        }

        if (currentProcessedBitmap != null)
        {
            currentProcessedBitmap.Dispose();
            currentProcessedBitmap = null;
        }
    }
}
