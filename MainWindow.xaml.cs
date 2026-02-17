using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;
using System.Drawing;

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

    // Window position and margin settings
    private WindowAnchor windowAnchor = WindowAnchor.BottomLeft;
    private double windowMarginLeft = 20d;
    private double windowMarginBottom = 40d;

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

    public WindowAnchor WindowAnchor => windowAnchor;
    public double WindowMarginLeft => windowMarginLeft;
    public double WindowMarginBottom => windowMarginBottom;

    // For implementing countdown logic into stopwatch logic
    private enum TimerMode { CountUp, CountDown }
    private TimerMode currentTimerMode = TimerMode.CountUp;
    private TimeSpan countdownTime = TimeSpan.Zero;  // only used for countdown

    // For countdown finished flashing
    private bool flashStarted = false;


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
        this.Loaded += (s, e) =>
        {
            this.UpdateLayout();
            // Delay positioning to ensure content width is fully calculated
            this.Dispatcher.BeginInvoke(new Action(() => PositionWindow()), System.Windows.Threading.DispatcherPriority.Render);
            Keyboard.Focus(this);
        };

        keyHideTimer.Tick += KeyHideTimer_Tick;
        chordDisplayTimer.Tick += ChordDisplayTimer_Tick;

        // global keyboard hook
        _proc = HookCallback;
        _hookID = SetHook(_proc);

        this.Closed += (s, e) => { if (_hookID != IntPtr.Zero) UnhookWindowsHookEx(_hookID); };
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        if (currentTimerMode == TimerMode.CountUp)
    {
        TimeText.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
    }
    else if (currentTimerMode == TimerMode.CountDown)
    {
        var remaining = countdownTime - stopwatch.Elapsed;
        if (remaining <= TimeSpan.Zero)
        {
            remaining = TimeSpan.Zero;
            TimeText.Text = remaining.ToString(@"hh\:mm\:ss");

            if (!flashStarted)
            {
                flashStarted = true;
                stopwatch.Stop();
                FlashCountdownFinished();

                // Optional: auto-switch back to count-up after 5 seconds
                var resetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                resetTimer.Tick += (s, ev) =>
                {
                    resetTimer.Stop();
                    currentTimerMode = TimerMode.CountUp;
                    stopwatch.Reset();
                    flashStarted = false;
                    TimeText.Foreground = new SolidColorBrush(timerForegroundColor); // restore original color
                };
                resetTimer.Start();
            }
        }
        else
        {
            TimeText.Text = remaining.ToString(@"hh\:mm\:ss");
        }
    }
    }

    private void Countdown_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new CountdownWindow
        {
            Owner = this
        };

        if (dlg.ShowDialog() == true)
        {
            countdownTime = dlg.SelectedTime;
            currentTimerMode = TimerMode.CountDown;
            stopwatch.Reset();
            stopwatch.Start();
        }
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

    private void FlashCountdownFinished()
    {
        var flashAnimation = new System.Windows.Media.Animation.ColorAnimation
        {
            From = System.Windows.Media.Colors.Red,
            To = timerForegroundColor, // original color
            Duration = TimeSpan.FromSeconds(0.5),
            AutoReverse = true,
            RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(5) // 5 seconds
        };

        var brush = new SolidColorBrush(timerForegroundColor);
        TimeText.Foreground = brush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty, flashAnimation);
    }

    private void TogglePause()
    {
        if (currentTimerMode == TimerMode.CountDown && stopwatch.Elapsed >= countdownTime)
        {
            // Countdown finished, switch back to count-up
            currentTimerMode = TimerMode.CountUp;
            stopwatch.Reset();
            flashStarted = false;
            TimeText.Foreground = new SolidColorBrush(timerForegroundColor);
        }

        if (stopwatch.IsRunning)
            stopwatch.Stop();
        else
            stopwatch.Start();
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
        var parts = new List<string>();

        // Add modifiers from keysDown directly
        if (keysDown.Contains(Key.LeftCtrl) || keysDown.Contains(Key.RightCtrl))
            parts.Add("Ctrl");

        if (keysDown.Contains(Key.LeftAlt) || keysDown.Contains(Key.RightAlt))
            parts.Add("Alt");

        if (keysDown.Contains(Key.LeftShift) || keysDown.Contains(Key.RightShift))
            parts.Add("Shift");

        if (keysDown.Contains(Key.LWin) || keysDown.Contains(Key.RWin))
            parts.Add("Win");

        // Add non-modifier keys
        var mainKeys = keysDown
            .Where(k => !IsModifierKey(k))
            .Select(k => KeyToDisplayString(k))
            .ToList();

        parts.AddRange(mainKeys);

        if (parts.Count == 0)
        {
            KeyText.Text = string.Empty;
        }
        else
        {
            KeyText.Text = string.Join("+", parts);
            KeyText.FontSize = keyFontSize;
            KeyText.FontFamily = keyFontFamily;
            KeyText.Foreground = new SolidColorBrush(keyForegroundColor);

            KeyText.BeginAnimation(UIElement.OpacityProperty, null);
            KeyText.Opacity = 1.0;
            keyHideTimer.Stop();
        }
    }


    // private void UpdateKeyDisplay()
    // {
    //     var mods = System.Windows.Input.Keyboard.Modifiers;
    //     var parts = new System.Collections.Generic.List<string>();

    //     if ((mods & System.Windows.Input.ModifierKeys.Control) != 0)
    //         parts.Add("Ctrl");
    //     if ((mods & System.Windows.Input.ModifierKeys.Alt) != 0)
    //         parts.Add("Alt");
    //     if ((mods & System.Windows.Input.ModifierKeys.Shift) != 0)
    //         parts.Add("Shift");
    //     if ((mods & System.Windows.Input.ModifierKeys.Windows) != 0)
    //         parts.Add("Win");

    //     // add non-modifier keys from keysDown
    //     var mainKeys = keysDown.Where(k => !IsModifierKey(k)).Select(k => KeyToDisplayString(k)).ToList();
    //     if (mainKeys.Count > 0)
    //         parts.AddRange(mainKeys);

    //     if (parts.Count == 0)
    //     {
    //         // if nothing pressed thefadeout will kick in
    //         // but also ensure text is blank
    //         KeyText.Text = string.Empty;
    //     }
    //     else
    //     {
    //         var text = string.Join("+", parts);
    //         KeyText.Text = text;
    //         KeyText.FontSize = keyFontSize;
    //         KeyText.FontFamily = keyFontFamily;
    //         KeyText.Foreground = new System.Windows.Media.SolidColorBrush(keyForegroundColor);

    //         // make visible and stop hide timer
    //         KeyText.BeginAnimation(UIElement.OpacityProperty, null);
    //         KeyText.Opacity = 1.0;
    //         keyHideTimer.Stop();
    //     }
    // }

    private string KeyToDisplayString(System.Windows.Input.Key k)
    {
        // normalize special keys
        // Numbers
    if (k >= Key.D0 && k <= Key.D9)
        return ((int)(k - Key.D0)).ToString();

    // Numpad numbers
    if (k >= Key.NumPad0 && k <= Key.NumPad9)
        return ((int)(k - Key.NumPad0)).ToString();

    // Letters
    if (k >= Key.A && k <= Key.Z)
        return k.ToString();

    // Special keys
    switch (k)
    {
        case Key.Return: return "Enter";
        case Key.Escape: return "Esc";
        case Key.Space: return "Space";
        case Key.OemPlus: return "+";
        case Key.OemMinus: return "-";
        case Key.OemOpenBrackets: return "[";
        case Key.Oem6: return "]";
        case Key.Oem1: return ";";
        case Key.OemQuotes: return "'";
        case Key.Oem5: return "\\";
        case Key.OemComma: return ",";
        case Key.OemPeriod: return ".";
        case Key.OemQuestion: return "/";
        case Key.OemTilde: return "`";
        case Key.Capital: return "CapsLock";
        case Key.Tab: return "Tab";
        case Key.Back: return "Backspace";
        default: return k.ToString(); // fallback
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
        if (currentTimerMode == TimerMode.CountUp)
            TimeText.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
        else
            TimeText.Text = countdownTime.ToString(@"hh\:mm\:ss");
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

    private void AnalyzeValues_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Capture screenshot of the monitor containing this window
            var screenshot = CaptureMonitorScreenshot();
            if (screenshot != null)
            {
                // Open the analyzer window
                var analyzer = new ValueAnalyzerWindow(screenshot)
                {
                    Owner = this
                };
                analyzer.Show();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening Analyze Values window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Bitmap CaptureMonitorScreenshot()
    {
        try
        {
            // Get the monitor containing this window
            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);

            // Capture the full bounds of that monitor
            var bounds = screen.Bounds;
            var bitmap = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(new System.Drawing.Point(bounds.X, bounds.Y), System.Drawing.Point.Empty, new System.Drawing.Size(bounds.Width, bounds.Height));
            }

            return bitmap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot capture error: {ex.Message}");
            return null;
        }
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

    public void ApplyWindowPosition(WindowAnchor anchor, double marginLeft, double marginBottom)
    {
        windowAnchor = anchor;
        windowMarginLeft = marginLeft;
        windowMarginBottom = marginBottom;
        PositionWindow();

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

        // Window position and margin settings
        windowAnchor = settings.WindowAnchor;
        windowMarginLeft = settings.WindowMarginLeft;
        windowMarginBottom = settings.WindowMarginBottom;
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

        // Apply key display styling
        KeyText.FontFamily = keyFontFamily;
        KeyText.FontSize = keyFontSize;
        KeyText.Foreground = new System.Windows.Media.SolidColorBrush(keyForegroundColor);
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
            KeyChordHoldSeconds = keyChordHoldSeconds,

            WindowAnchor = windowAnchor,
            WindowMarginLeft = windowMarginLeft,
            WindowMarginBottom = windowMarginBottom
        };

        settings.Save();
    }

    private void PositionWindow()
    {
        var workingArea = System.Windows.SystemParameters.WorkArea;

        // Force measurement of content if not yet available
        double width = this.ActualWidth;
        double height = this.ActualHeight;

        if (width <= 0 || height <= 0)
        {
            // Manually measure the root grid to get desired size
            RootGrid.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            width = RootGrid.DesiredSize.Width;
            height = RootGrid.DesiredSize.Height;
        }

        // Position based on anchor corner
        switch (windowAnchor)
        {
            case WindowAnchor.BottomLeft:
                this.Left = workingArea.Left + windowMarginLeft;
                this.Top = workingArea.Bottom - height - windowMarginBottom;
                break;
            case WindowAnchor.BottomRight:
                this.Left = workingArea.Right - width - windowMarginLeft;
                this.Top = workingArea.Bottom - height - windowMarginBottom;
                break;
            case WindowAnchor.TopLeft:
                this.Left = workingArea.Left + windowMarginLeft;
                this.Top = workingArea.Top + windowMarginBottom;
                break;
            case WindowAnchor.TopRight:
                this.Left = workingArea.Right - width - windowMarginLeft;
                this.Top = workingArea.Top + windowMarginBottom;
                break;
        }
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
