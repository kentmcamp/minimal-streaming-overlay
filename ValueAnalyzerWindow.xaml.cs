using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace minol;

public partial class ValueAnalyzerWindow : Window
{
    private Bitmap? originalBitmap;
    private Bitmap? currentProcessedBitmap;
    private byte posterizeLevels = 20;
    private byte blackLevel = 0;
    private byte whiteLevel = 255;
    private bool flipHorizontal = false;
    private bool flipVertical = false;

    // Drag and resize fields
    private System.Windows.Point dragStartPoint;
    private bool isDragging = false;
    private bool isResizing = false;
    private ResizeDirection resizeDirection = ResizeDirection.None;

    private enum ResizeDirection { None, TopLeft, Top, TopRight, Left, Right, BottomLeft, Bottom, BottomRight }

    public ValueAnalyzerWindow(Bitmap? screenshot)
    {
        InitializeComponent();

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

    /// <summary>
    /// Process the image from the original, applying all transformations in sequence.
    /// </summary>
    private Bitmap ProcessImage(Bitmap source, byte posterizeLevels, byte blackLevel, byte whiteLevel, bool flipH, bool flipV)
    {
        // Create a working copy
        var working = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppRgb);

        // Step 1: Convert to grayscale and apply levels and posterization
        ApplyGrayscaleAndLevels(source, working, posterizeLevels, blackLevel, whiteLevel);

        // Step 2: Apply flips if needed
        if (flipH || flipV)
        {
            ApplyFlips(working, flipH, flipV);
        }

        return working;
    }

    /// <summary>
    /// Apply grayscale conversion, level remapping, and posterization using LockBits for performance.
    /// </summary>
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

    /// <summary>
    /// Apply posterization to a single value.
    /// step = 255 / (levels - 1)
    /// newValue = (value / step) * step
    /// </summary>
    private byte PosterizeValue(byte value, byte levels)
    {
        if (levels <= 1)
            return 0;

        float step = 255f / (levels - 1);
        byte quantized = (byte)(((float)value / step) * step);
        return quantized;
    }

    /// <summary>
    /// Apply horizontal and/or vertical flips to the bitmap.
    /// </summary>
    private void ApplyFlips(Bitmap bitmap, bool horizontal, bool vertical)
    {
        if (horizontal)
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
        if (vertical)
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
    }

    /// <summary>
    /// Convert a System.Drawing.Bitmap to a WPF WriteableBitmap for display.
    /// </summary>
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

    // Event handlers for UI controls

    private void PosterizeSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        posterizeLevels = (byte)PosterizeSlider.Value;
        PosterizeLevelText.Text = posterizeLevels.ToString();
        ProcessAndDisplay();
    }

    private void BlackLevelSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        blackLevel = (byte)BlackLevelSlider.Value;
        BlackLevelText.Text = blackLevel.ToString();

        // Prevent invalid state where black >= white
        if (blackLevel >= whiteLevel)
        {
            whiteLevel = (byte)Math.Min(255, blackLevel + 1);
            WhiteLevelSlider.Value = whiteLevel;
            WhiteLevelText.Text = whiteLevel.ToString();
        }

        ProcessAndDisplay();
    }

    private void WhiteLevelSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        whiteLevel = (byte)WhiteLevelSlider.Value;
        WhiteLevelText.Text = whiteLevel.ToString();

        // Prevent invalid state where black >= white
        if (whiteLevel <= blackLevel)
        {
            blackLevel = (byte)Math.Max(0, whiteLevel - 1);
            BlackLevelSlider.Value = blackLevel;
            BlackLevelText.Text = blackLevel.ToString();
        }

        ProcessAndDisplay();
    }

    private void FlipHorizontal_Click(object sender, RoutedEventArgs e)
    {
        flipHorizontal = !flipHorizontal;
        FlipHorizontalButton.Background = flipHorizontal ? System.Windows.Media.Brushes.DarkGreen : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64));
        ProcessAndDisplay();
    }

    private void FlipVertical_Click(object sender, RoutedEventArgs e)
    {
        flipVertical = !flipVertical;
        FlipVerticalButton.Background = flipVertical ? System.Windows.Media.Brushes.DarkGreen : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64));
        ProcessAndDisplay();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        dragStartPoint = e.GetPosition(this);

        const int resizeBorder = 5;
        double x = dragStartPoint.X;
        double y = dragStartPoint.Y;

        // Determine if we're clicking on a resize area
        if (y < resizeBorder)
        {
            if (x < resizeBorder) resizeDirection = ResizeDirection.TopLeft;
            else if (x > this.ActualWidth - resizeBorder) resizeDirection = ResizeDirection.TopRight;
            else resizeDirection = ResizeDirection.Top;
            isResizing = true;
        }
        else if (y > this.ActualHeight - resizeBorder)
        {
            if (x < resizeBorder) resizeDirection = ResizeDirection.BottomLeft;
            else if (x > this.ActualWidth - resizeBorder) resizeDirection = ResizeDirection.BottomRight;
            else resizeDirection = ResizeDirection.Bottom;
            isResizing = true;
        }
        else if (x < resizeBorder)
        {
            resizeDirection = ResizeDirection.Left;
            isResizing = true;
        }
        else if (x > this.ActualWidth - resizeBorder)
        {
            resizeDirection = ResizeDirection.Right;
            isResizing = true;
        }
        else if (y < 32) // Click on title bar
        {
            isDragging = true;
            this.CaptureMouse();
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging)
        {
            System.Windows.Point currentPoint = e.GetPosition(null);
            this.Left += currentPoint.X - dragStartPoint.X;
            this.Top += currentPoint.Y - dragStartPoint.Y;
            dragStartPoint = new System.Windows.Point(currentPoint.X, currentPoint.Y);
        }
        else if (isResizing)
        {
            System.Windows.Point currentPoint = e.GetPosition(this);
            double deltaX = currentPoint.X - dragStartPoint.X;
            double deltaY = currentPoint.Y - dragStartPoint.Y;

            switch (resizeDirection)
            {
                case ResizeDirection.Top:
                    this.Top += deltaY;
                    this.Height -= deltaY;
                    break;
                case ResizeDirection.Bottom:
                    this.Height += deltaY;
                    break;
                case ResizeDirection.Left:
                    this.Left += deltaX;
                    this.Width -= deltaX;
                    break;
                case ResizeDirection.Right:
                    this.Width += deltaX;
                    break;
                case ResizeDirection.TopLeft:
                    this.Top += deltaY;
                    this.Height -= deltaY;
                    this.Left += deltaX;
                    this.Width -= deltaX;
                    break;
                case ResizeDirection.TopRight:
                    this.Top += deltaY;
                    this.Height -= deltaY;
                    this.Width += deltaX;
                    break;
                case ResizeDirection.BottomLeft:
                    this.Height += deltaY;
                    this.Left += deltaX;
                    this.Width -= deltaX;
                    break;
                case ResizeDirection.BottomRight:
                    this.Height += deltaY;
                    this.Width += deltaX;
                    break;
            }
        }
        else
        {
            // Update cursor based on position
            double x = e.GetPosition(this).X;
            double y = e.GetPosition(this).Y;
            const int resizeBorder = 5;

            if ((y < resizeBorder && x < resizeBorder) || (y > this.ActualHeight - resizeBorder && x > this.ActualWidth - resizeBorder))
                this.Cursor = Cursors.SizeNWSE;
            else if ((y < resizeBorder && x > this.ActualWidth - resizeBorder) || (y > this.ActualHeight - resizeBorder && x < resizeBorder))
                this.Cursor = Cursors.SizeNESW;
            else if (y < resizeBorder || y > this.ActualHeight - resizeBorder)
                this.Cursor = Cursors.SizeNS;
            else if (x < resizeBorder || x > this.ActualWidth - resizeBorder)
                this.Cursor = Cursors.SizeWE;
            else
                this.Cursor = Cursors.Arrow;
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        isDragging = false;
        isResizing = false;
        resizeDirection = ResizeDirection.None;
        this.ReleaseMouseCapture();
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
