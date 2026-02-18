using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace minol
{
    public partial class ColorPickerWindow : Window
    {
        private const int MagnifierSize = 120;  // Width/height of magnifier panel
        private const int CaptureSize = 20;     // Area of screen to capture around cursor
        private const double Zoom = 6.0;        // Zoom factor
        private bool magnifierActive = false;

        public ColorPickerWindow()
        {
            InitializeComponent();

            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;

            // Move InfoPanel programmatically
            Canvas.SetLeft(InfoPanel, 800);
            Canvas.SetTop(InfoPanel, 400);

            // Attach input events
            this.PreviewMouseRightButtonDown += Window_PreviewMouseRightButtonDown;
            this.PreviewMouseRightButtonUp += Window_PreviewMouseRightButtonUp;
            this.MouseMove += Window_MouseMove;
            this.PreviewMouseDown += Window_PreviewMouseDown;
        }

        // ------------------- Color Picking -------------------
        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !InfoPanel.IsMouseOver)
            {
                PickColorUnderCursor();
            }
        }

        private void PickColorUnderCursor()
        {
            this.Hide(); // Hide window so we don't sample ourselves
            System.Threading.Thread.Sleep(50); // Allow screen to redraw

            var p = System.Windows.Forms.Control.MousePosition;

            using (Bitmap bmp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(p.X, p.Y, 0, 0, new System.Drawing.Size(1, 1));
                var drawingColor = bmp.GetPixel(0, 0);
                var mediaColor = Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);

                this.Show();
                this.Activate();

                DisplayColor(mediaColor);
            }
        }

        private void DisplayColor(Color c)
        {
            ColorPreview.Background = new SolidColorBrush(c);

            string hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            HexText.Text = $"HEX: {hex}";
            RgbText.Text = $"RGB: {c.R}, {c.G}, {c.B}";

            var (cmykC, cmykM, cmykY, cmykK) = ConvertToCmyk(c);
            CmykText.Text = $"CMYK: {cmykC}% {cmykM}% {cmykY}% {cmykK}%";

            CopyHexButton.Visibility = Visibility.Visible;
        }

        private (int, int, int, int) ConvertToCmyk(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double k = 1 - Math.Max(r, Math.Max(g, b));
            if (k == 1) return (0, 0, 0, 100);

            double c = (1 - r - k) / (1 - k);
            double m = (1 - g - k) / (1 - k);
            double y = (1 - b - k) / (1 - k);

            return ((int)(c * 100), (int)(m * 100), (int)(y * 100), (int)(k * 100));
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        // ------------------- Magnifier -------------------
        private void Window_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            magnifierActive = true;
            MagnifierPanel.Visibility = Visibility.Visible;
        }

        private void Window_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!magnifierActive) // only if not already toggled on
            {
                MagnifierPanel.Visibility = Visibility.Visible;
                magnifierActive = true;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!magnifierActive) return;
            UpdateMagnifier(e);
        }

        private void UpdateMagnifier(MouseEventArgs e)
        {
            var mousePos = System.Windows.Forms.Control.MousePosition;

            using (var bmp = new Bitmap(CaptureSize, CaptureSize))
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(mousePos.X - CaptureSize / 2,
                                 mousePos.Y - CaptureSize / 2,
                                 0, 0,
                                 new System.Drawing.Size(CaptureSize, CaptureSize));

                var hBitmap = bmp.GetHbitmap();
                try
                {
                    var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap, IntPtr.Zero, Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(CaptureSize, CaptureSize));

                    MagnifierImage.Source = new System.Windows.Media.Imaging.TransformedBitmap(
                        source, new System.Windows.Media.ScaleTransform(Zoom, Zoom));
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }

            // Move magnifier near cursor
            var cursor = e.GetPosition(RootCanvas);
            double offset = 20;
            Canvas.SetLeft(MagnifierPanel, cursor.X + offset);
            Canvas.SetTop(MagnifierPanel, cursor.Y + offset);
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        private void UpdateCrosshair()
        {
            double w = MagnifierPanel.ActualWidth;
            double h = MagnifierPanel.ActualHeight;

            CrosshairHorizontal.X1 = 0;
            CrosshairHorizontal.Y1 = h / 2;
            CrosshairHorizontal.X2 = w;
            CrosshairHorizontal.Y2 = h / 2;

            CrosshairVertical.X1 = w / 2;
            CrosshairVertical.Y1 = 0;
            CrosshairVertical.X2 = w / 2;
            CrosshairVertical.Y2 = h;
        }

        private void MagnifierToggleButton_Click(object sender, RoutedEventArgs e)
        {
            magnifierActive = !magnifierActive; // toggle state
            MagnifierPanel.Visibility = magnifierActive ? Visibility.Visible : Visibility.Collapsed;
        }

        // ------------------- Info Panel Drag -------------------
        private void InfoPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            InfoPanel.CaptureMouse();
            InfoPanel.MouseMove += InfoPanel_MouseMove;
            InfoPanel.MouseLeftButtonUp += InfoPanel_MouseLeftButtonUp;
        }



        private void InfoPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(this);
                Canvas.SetLeft(InfoPanel, pos.X - InfoPanel.ActualWidth / 2);
                Canvas.SetTop(InfoPanel, pos.Y - InfoPanel.ActualHeight / 2);
            }
        }

        private void InfoPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            InfoPanel.ReleaseMouseCapture();
            InfoPanel.MouseMove -= InfoPanel_MouseMove;
            InfoPanel.MouseLeftButtonUp -= InfoPanel_MouseLeftButtonUp;
        }

        private void CopyHexButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(HexText.Text))
            {
                // Extract HEX code from TextBlock (assumes format: "HEX: #RRGGBB")
                string hexCode = HexText.Text.Split(':')[1].Trim();
                Clipboard.SetText(hexCode);
            }
        }
    }
}

