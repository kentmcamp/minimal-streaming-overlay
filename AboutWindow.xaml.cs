using System.Windows;
using System.Diagnostics;
using System.Windows.Navigation;

namespace minol;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        // Set version information dynamically from AppVersion class
        VersionText.Text = $"{AppVersion.DisplayName} - {AppVersion.Status}";
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
        {
            UseShellExecute = true
        });

        e.Handled = true;
    }
}


