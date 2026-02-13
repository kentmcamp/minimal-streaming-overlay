using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;

namespace minol;


public partial class MainWindow : Window
{
    private Stopwatch stopwatch = new Stopwatch();
    private DispatcherTimer timer = new DispatcherTimer();
    private DispatcherTimer keyHideTimer = new DispatcherTimer();
    private System.Collections.Generic.HashSet<System.Windows.Input.Key> keysDown = new System.Collections.Generic.HashSet<System.Windows.Input.Key>();

    // Global keyboard hook fields
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private LowLevelKeyboardProc _proc;
    private IntPtr _hookID = IntPtr.Zero;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    // Key display settings
    private System.Windows.Media.FontFamily keyFontFamily = new System.Windows.Media.FontFamily("Segoe UI");
    private double keyFontSize = 28d;
    private System.Windows.Media.Color keyForegroundColor = System.Windows.Media.Colors.Lime;
    private double keyShowSeconds = 1.2d;
    private double keyFadeSeconds = 0.6d;

    // Public properties to read config settings
    public System.Windows.Media.FontFamily KeyFontFamily => keyFontFamily;
    public double KeyFontSize => keyFontSize;
    public System.Windows.Media.Color KeyForegroundColor => keyForegroundColor;
    public double KeyShowSeconds => keyShowSeconds;
    public double KeyFadeSeconds => keyFadeSeconds;

    public MainWindow()
    {
        InitializeComponent();

        stopwatch.Start();

        timer.Interval = TimeSpan.FromMilliseconds(100);
        timer.Tick += Timer_Tick;
        timer.Start();

        this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
        this.PreviewKeyDown += Window_PreviewKeyDown;
        this.PreviewKeyUp += Window_PreviewKeyUp;
        this.Loaded += (s, e) => Keyboard.Focus(this);

        keyHideTimer.Tick += KeyHideTimer_Tick;

        // global keyboard hook
        _proc = HookCallback;
        _hookID = SetHook(_proc);

        this.Closed += (s, e) => { if (_hookID != IntPtr.Zero) UnhookWindowsHookEx(_hookID); };
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        TimeText.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
    }

    private void Window_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
        {
            try
            {
                this.DragMove();
            }
            catch { }
        }
    }

    private void Window_PreviewKeyDown(object? sender, KeyEventArgs e)
    {
        // add key to set and update display (ignore repeats)
        if (!keysDown.Contains(e.Key))
        {
            keysDown.Add(e.Key);
            UpdateKeyDisplay();
        }

        e.Handled = false;
    }

    private void Window_PreviewKeyUp(object? sender, KeyEventArgs e)
    {
        if (keysDown.Contains(e.Key))
            keysDown.Remove(e.Key);

        if (keysDown.Count == 0)
        {
            keyHideTimer.Stop();
            keyHideTimer.Interval = TimeSpan.FromSeconds(keyShowSeconds);
            keyHideTimer.Start();
        }
        else
        {
            UpdateKeyDisplay();
        }

        e.Handled = false;
    }

    private bool IsModifierKey(System.Windows.Input.Key k)
    {
        return k == System.Windows.Input.Key.LeftCtrl || k == System.Windows.Input.Key.RightCtrl
            || k == System.Windows.Input.Key.LeftShift || k == System.Windows.Input.Key.RightShift
            || k == System.Windows.Input.Key.LeftAlt || k == System.Windows.Input.Key.RightAlt
            || k == System.Windows.Input.Key.LWin || k == System.Windows.Input.Key.RWin
            || k == System.Windows.Input.Key.System;
    }

    private void UpdateKeyDisplay()
    {
        var mods = System.Windows.Input.Keyboard.Modifiers;
        var parts = new System.Collections.Generic.List<string>();

        if ((mods & System.Windows.Input.ModifierKeys.Control) != 0)
            parts.Add("Ctrl");
        if ((mods & System.Windows.Input.ModifierKeys.Alt) != 0)
            parts.Add("Alt");
        if ((mods & System.Windows.Input.ModifierKeys.Shift) != 0)
            parts.Add("Shift");
        if ((mods & System.Windows.Input.ModifierKeys.Windows) != 0)
            parts.Add("Win");

        // add non-modifier keys from keysDown
        var mainKeys = keysDown.Where(k => !IsModifierKey(k)).Select(k => KeyToDisplayString(k)).ToList();
        if (mainKeys.Count > 0)
            parts.AddRange(mainKeys);

        if (parts.Count == 0)
        {
            // if nothing pressed thefadeout will kick in
            // but also ensure text is blank
            KeyText.Text = string.Empty;
        }
        else
        {
            var text = string.Join("+", parts);
            KeyText.Text = text;
            KeyText.FontSize = keyFontSize;
            KeyText.FontFamily = keyFontFamily;
            KeyText.Foreground = new System.Windows.Media.SolidColorBrush(keyForegroundColor);

            // make visible and stop hide timer
            KeyText.BeginAnimation(UIElement.OpacityProperty, null);
            KeyText.Opacity = 1.0;
            keyHideTimer.Stop();
        }
    }

    private string KeyToDisplayString(System.Windows.Input.Key k)
    {
        // normalize special keys
        switch (k)
        {
            case System.Windows.Input.Key.Return: return "Enter";
            case System.Windows.Input.Key.Escape: return "Esc";
            case System.Windows.Input.Key.Space: return "Space";
            case System.Windows.Input.Key.OemPlus: return "+";
            case System.Windows.Input.Key.OemMinus: return "-";
            default:
                return k.ToString();
        }
    }

    private void ShowKey(string text)
    {
        KeyText.Text = text;
        KeyText.FontSize = keyFontSize;

        // stop any running animation and make fully visible
        KeyText.BeginAnimation(UIElement.OpacityProperty, null);
        KeyText.Opacity = 1.0;

        // restart timer to begin fade after `keyShowSeconds`
        keyHideTimer.Stop();
        keyHideTimer.Interval = TimeSpan.FromSeconds(keyShowSeconds);
        keyHideTimer.Start();
    }

    private void KeyHideTimer_Tick(object? sender, EventArgs e)
    {
        keyHideTimer.Stop();

        var fade = new DoubleAnimation(0.0, TimeSpan.FromSeconds(keyFadeSeconds)) { EasingFunction = new QuadraticEase() };
        KeyText.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (!stopwatch.IsRunning)
            stopwatch.Start();
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        if (stopwatch.IsRunning)
            stopwatch.Stop();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        stopwatch.Reset();
        TimeText.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new EditWindow(this)
        {
            Owner = this
        };
        dlg.ShowDialog();
    }

    public void ApplyOverlaySettings(System.Windows.Media.FontFamily fontFamily, double fontSize, System.Windows.Media.Color foregroundColor, System.Windows.Media.Color backgroundColor, double backgroundOpacity)
    {
        TimeText.FontFamily = fontFamily;
        TimeText.FontSize = fontSize;
        TimeText.Foreground = new System.Windows.Media.SolidColorBrush(foregroundColor);

        var brush = new System.Windows.Media.SolidColorBrush(backgroundColor);
        brush.Opacity = backgroundOpacity;
        RootGrid.Background = brush;
    }

    public void ApplyKeyDisplaySettings(System.Windows.Media.FontFamily fontFamily, double fontSize, System.Windows.Media.Color foregroundColor, double showSeconds, double fadeSeconds)
    {
        keyFontFamily = fontFamily ?? keyFontFamily;
        keyFontSize = fontSize;
        keyForegroundColor = foregroundColor;
        keyShowSeconds = showSeconds;
        keyFadeSeconds = fadeSeconds;
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            IntPtr moduleHandle = GetModuleHandle(curModule.ModuleName);
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, moduleHandle, 0);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int wm = wParam.ToInt32();
            if (wm == WM_KEYDOWN || wm == WM_SYSKEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = KeyInterop.KeyFromVirtualKey(vkCode);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!keysDown.Contains(key))
                    {
                        keysDown.Add(key);
                        UpdateKeyDisplay();
                    }
                }));
            }
            else if (wm == WM_KEYUP || wm == WM_SYSKEYUP)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = KeyInterop.KeyFromVirtualKey(vkCode);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (keysDown.Contains(key))
                        keysDown.Remove(key);

                    if (keysDown.Count == 0)
                    {
                        keyHideTimer.Stop();
                        keyHideTimer.Interval = TimeSpan.FromSeconds(keyShowSeconds);
                        keyHideTimer.Start();
                    }
                    else
                    {
                        UpdateKeyDisplay();
                    }
                }));
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
