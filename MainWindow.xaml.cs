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
    private DispatcherTimer chordDisplayTimer = new DispatcherTimer();
    private System.Collections.Generic.HashSet<System.Windows.Input.Key> keysDown = new System.Collections.Generic.HashSet<System.Windows.Input.Key>();
    private string frozenChordText = string.Empty;
    private bool isChordFrozen = false;

    // Global keyboard hook fields
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private LowLevelKeyboardProc _proc;
    private IntPtr _hookID = IntPtr.Zero;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    // Click vs drag detection
    private System.Windows.Point mouseDownPos;
    private bool isDragging = false;

    // Timer display settings
    private System.Windows.Media.FontFamily timerFontFamily = new System.Windows.Media.FontFamily("Consolas");
    private double timerFontSize = 36d;
    private System.Windows.Media.Color timerForegroundColor = System.Windows.Media.Colors.Lime;
    private System.Windows.Media.Color timerBackgroundColor = System.Windows.Media.Colors.Black;
    private double timerBackgroundOpacity = 0.5d;

    // Key display settings
    private System.Windows.Media.FontFamily keyFontFamily = new System.Windows.Media.FontFamily("Consolas");
    private double keyFontSize = 28d;
    private System.Windows.Media.Color keyForegroundColor = System.Windows.Media.Colors.Lime;
    private double keyShowSeconds = 2d;
    private double keyFadeSeconds = 0.5d;
    private double keyChordHoldSeconds = 0.3d;

    // Public properties to read config settings
    public System.Windows.Media.FontFamily TimerFontFamily => timerFontFamily;
    public double TimerFontSize => timerFontSize;
    public System.Windows.Media.Color TimerForegroundColor => timerForegroundColor;
    public System.Windows.Media.Color TimerBackgroundColor => timerBackgroundColor;
    public double TimerBackgroundOpacity => timerBackgroundOpacity;

    public System.Windows.Media.FontFamily KeyFontFamily => keyFontFamily;
    public double KeyFontSize => keyFontSize;
    public System.Windows.Media.Color KeyForegroundColor => keyForegroundColor;
    public double KeyShowSeconds => keyShowSeconds;
    public double KeyFadeSeconds => keyFadeSeconds;
    public double KeyChordHoldSeconds => keyChordHoldSeconds;

    public MainWindow()
    {
        InitializeComponent();

        // Load theme system - check for default theme first
        var defaultThemeName = ThemeManager.GetDefaultTheme();
        AppSettings settings;

        if (!string.IsNullOrWhiteSpace(defaultThemeName))
        {
            // Load default theme
            settings = ThemeManager.LoadTheme(defaultThemeName);
        }
        else
        {
            // Load regular settings
            settings = AppSettings.Load();
        }

        LoadSettingsIntoVariables(settings);
        ApplyUISettings();

        stopwatch.Start();

        timer.Interval = TimeSpan.FromMilliseconds(100);
        timer.Tick += Timer_Tick;
        timer.Start();

        this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
        this.MouseLeftButtonUp += Window_MouseLeftButtonUp;
        this.MouseMove += Window_MouseMove;
        this.PreviewKeyDown += Window_PreviewKeyDown;
        this.PreviewKeyUp += Window_PreviewKeyUp;
        this.Loaded += (s, e) => Keyboard.Focus(this);

        keyHideTimer.Tick += KeyHideTimer_Tick;
        chordDisplayTimer.Tick += ChordDisplayTimer_Tick;

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
        mouseDownPos = e.GetPosition(this);
        isDragging = false;
    }

    private void Window_MouseMove(object? sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPos = e.GetPosition(this);
            var delta = currentPos - mouseDownPos;

            // If moved more than 5px, it's a drag
            if (Math.Abs(delta.X) > 5 || Math.Abs(delta.Y) > 5)
            {
                if (!isDragging)
                {
                    isDragging = true;
                    try
                    {
                        this.DragMove();
                    }
                    catch { }
                }
            }
        }
    }

    private void Window_MouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
    {
        // If not dragging, it's a click — toggle pause
        if (!isDragging)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        if (stopwatch.IsRunning)
            stopwatch.Stop();
        else
            stopwatch.Start();
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
            // All keys released, unfreeze if frozen and start hide timer
            isChordFrozen = false;
            chordDisplayTimer.Stop();
            keyHideTimer.Stop();
            keyHideTimer.Interval = TimeSpan.FromSeconds(keyShowSeconds);
            keyHideTimer.Start();
        }
        else if (!isChordFrozen)
        {
            // Some keys still down but chord not yet frozen.
            // Capture current display and freeze it.
            frozenChordText = KeyText.Text;
            if (!string.IsNullOrEmpty(frozenChordText))
            {
                isChordFrozen = true;
                chordDisplayTimer.Stop();
                chordDisplayTimer.Interval = TimeSpan.FromSeconds(keyChordHoldSeconds);
                chordDisplayTimer.Start();
            }
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

    private void ChordDisplayTimer_Tick(object? sender, EventArgs e)
    {
        chordDisplayTimer.Stop();
        isChordFrozen = false;

        // Chord hold time expired, now check if all keys are truly released
        if (keysDown.Count == 0)
        {
            // Yes, all keys released - start the hide timer
            keyHideTimer.Stop();
            keyHideTimer.Interval = TimeSpan.FromSeconds(keyShowSeconds);
            keyHideTimer.Start();
        }
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
        timerFontFamily = fontFamily ?? timerFontFamily;
        timerFontSize = fontSize;
        timerForegroundColor = foregroundColor;
        timerBackgroundColor = backgroundColor;
        timerBackgroundOpacity = backgroundOpacity;

        TimeText.FontFamily = timerFontFamily;
        TimeText.FontSize = timerFontSize;
        TimeText.Foreground = new System.Windows.Media.SolidColorBrush(timerForegroundColor);

        var brush = new System.Windows.Media.SolidColorBrush(timerBackgroundColor);
        brush.Opacity = timerBackgroundOpacity;
        RootGrid.Background = brush;

        // Save to settings file
        SaveSettingsToFile();
    }

    public void ApplyKeyDisplaySettings(System.Windows.Media.FontFamily fontFamily, double fontSize, System.Windows.Media.Color foregroundColor, double showSeconds, double fadeSeconds, double chordHoldSeconds = 0.3d)
    {
        keyFontFamily = fontFamily ?? keyFontFamily;
        keyFontSize = fontSize;
        keyForegroundColor = foregroundColor;
        keyShowSeconds = showSeconds;
        keyFadeSeconds = fadeSeconds;
        keyChordHoldSeconds = chordHoldSeconds;

        // Save to settings file
        SaveSettingsToFile();
    }

    private void LoadSettingsIntoVariables(AppSettings settings)
    {
        // Timer settings
        try { timerFontFamily = new System.Windows.Media.FontFamily(settings.TimerFontFamily); } catch { }
        timerFontSize = settings.TimerFontSize;
        timerForegroundColor = AppSettings.ParseColor(settings.TimerForegroundColor);
        timerBackgroundColor = AppSettings.ParseColor(settings.TimerBackgroundColor);
        timerBackgroundOpacity = settings.TimerBackgroundOpacity;

        // Key settings
        try { keyFontFamily = new System.Windows.Media.FontFamily(settings.KeyFontFamily); } catch { }
        keyFontSize = settings.KeyFontSize;
        keyForegroundColor = AppSettings.ParseColor(settings.KeyForegroundColor);
        keyShowSeconds = settings.KeyShowSeconds;
        keyFadeSeconds = settings.KeyFadeSeconds;
        keyChordHoldSeconds = settings.KeyChordHoldSeconds;
    }

    private void ApplyUISettings()
    {
        // Apply timer display
        TimeText.FontFamily = timerFontFamily;
        TimeText.FontSize = timerFontSize;
        TimeText.Foreground = new System.Windows.Media.SolidColorBrush(timerForegroundColor);

        var brush = new System.Windows.Media.SolidColorBrush(timerBackgroundColor);
        brush.Opacity = timerBackgroundOpacity;
        RootGrid.Background = brush;
    }

    private void SaveSettingsToFile()
    {
        var settings = new AppSettings
        {
            TimerFontFamily = timerFontFamily.Source,
            TimerFontSize = timerFontSize,
            TimerForegroundColor = AppSettings.ColorToHex(timerForegroundColor),
            TimerBackgroundColor = AppSettings.ColorToHex(timerBackgroundColor),
            TimerBackgroundOpacity = timerBackgroundOpacity,

            KeyFontFamily = keyFontFamily.Source,
            KeyFontSize = keyFontSize,
            KeyForegroundColor = AppSettings.ColorToHex(keyForegroundColor),
            KeyShowSeconds = keyShowSeconds,
            KeyFadeSeconds = keyFadeSeconds,
            KeyChordHoldSeconds = keyChordHoldSeconds
        };

        settings.Save();
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
                        // All keys released, unfreeze if frozen and start hide timer
                        isChordFrozen = false;
                        chordDisplayTimer.Stop();
                        keyHideTimer.Stop();
                        keyHideTimer.Interval = TimeSpan.FromSeconds(keyShowSeconds);
                        keyHideTimer.Start();
                    }
                    else if (!isChordFrozen)
                    {
                        // Some keys still down but chord not yet frozen.
                        // Capture current display and freeze it.
                        frozenChordText = KeyText.Text;
                        if (!string.IsNullOrEmpty(frozenChordText))
                        {
                            isChordFrozen = true;
                            chordDisplayTimer.Stop();
                            chordDisplayTimer.Interval = TimeSpan.FromSeconds(keyChordHoldSeconds);
                            chordDisplayTimer.Start();
                        }
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
