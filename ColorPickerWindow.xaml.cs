using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace minol
{
    public partial class ColorPickerWindow : Window
    {
        public ColorPickerWindow()
        {
            InitializeComponent();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PickColorUnderCursor();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PickColorUnderCursor()
        {
            // Hide overlay so we don't sample ourselves
            this.Hide();

            // Let Windows redraw the screen
            System.Threading.Thread.Sleep(50);

            var p = System.Windows.Forms.Control.MousePosition;

            using (Bitmap bmp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(p.X, p.Y, 0, 0, new System.Drawing.Size(1, 1));
                var drawingColor = bmp.GetPixel(0, 0);

                var mediaColor = Color.FromRgb(
                    drawingColor.R,
                    drawingColor.G,
                    drawingColor.B);

                // Show window again before updating UI
                this.Show();
                this.Activate();

                DisplayColor(mediaColor);
            }
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
{
    PickColorUnderCursor();
}


        private void DisplayColor(Color c)
        {
            ColorPreview.Background = new SolidColorBrush(c);

            string hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            HexText.Text = $"HEX: {hex}";
            RgbText.Text = $"RGB: {c.R}, {c.G}, {c.B}";

            var (cmykC, cmykM, cmykY, cmykK) = ConvertToCmyk(c);
            CmykText.Text = $"CMYK: {cmykC}% {cmykM}% {cmykY}% {cmykK}%";
        }



        private void ClearDisplay()
        {
            ColorPreview.Background = System.Windows.Media.Brushes.Transparent;
            HexText.Text = "";
            RgbText.Text = "";
            CmykText.Text = "";
        }

        private (int, int, int, int) ConvertToCmyk(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double k = 1 - Math.Max(r, Math.Max(g, b));

            if (k == 1)
                return (0, 0, 0, 100);

            double c = (1 - r - k) / (1 - k);
            double m = (1 - g - k) / (1 - k);
            double y = (1 - b - k) / (1 - k);

            return (
                (int)(c * 100),
                (int)(m * 100),
                (int)(y * 100),
                (int)(k * 100)
            );
        }
    }
}
