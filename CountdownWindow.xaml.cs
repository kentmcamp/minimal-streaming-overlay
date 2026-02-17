using System;
using System.Windows;

namespace minol
{
    public partial class CountdownWindow : Window
    {
        public TimeSpan SelectedTime { get; private set; } = TimeSpan.Zero;

        public CountdownWindow()
        {
            InitializeComponent();
        }

        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn)
            {
                switch (btn.Content.ToString())
                {
                    case "30s": SelectedTime = TimeSpan.FromSeconds(30); break;
                    case "1m": SelectedTime = TimeSpan.FromMinutes(1); break;
                    case "2m": SelectedTime = TimeSpan.FromMinutes(2); break;
                    case "5m": SelectedTime = TimeSpan.FromMinutes(5); break;
                }

                // Fill the textboxes for user reference
                HoursBox.Text = SelectedTime.Hours.ToString();
                MinutesBox.Text = SelectedTime.Minutes.ToString();
                SecondsBox.Text = SelectedTime.Seconds.ToString();
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            // Parse custom input
            if (!int.TryParse(HoursBox.Text, out int hours)) hours = 0;
            if (!int.TryParse(MinutesBox.Text, out int minutes)) minutes = 0;
            if (!int.TryParse(SecondsBox.Text, out int seconds)) seconds = 0;

            SelectedTime = new TimeSpan(hours, minutes, seconds);

            if (SelectedTime.TotalSeconds <= 0)
            {
                MessageBox.Show("Please enter a time greater than zero.", "Invalid Time", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
