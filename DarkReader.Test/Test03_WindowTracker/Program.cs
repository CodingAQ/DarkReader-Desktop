// Test03: WindowTracker - Verify window tracking behavior
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DarkReader.Test
{
    static class Program
    {
        private static Form _form;
        private static TextBox _log;
        private static Button _btnPickWindow;
        private static Button _btnClear;
        private static Label _lblTarget;
        private static System.Windows.Forms.Timer _timer;
        private static IntPtr _targetHwnd = IntPtr.Zero;
        private static string _targetTitle = "(none)";
        private static int _tickCount;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            NativeMethods.SetProcessDPIAware();

            _form = new Form
            {
                Text = "Test03: WindowTracker",
                Size = new Size(700, 500),
                StartPosition = FormStartPosition.CenterScreen
            };

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight
            };

            _btnPickWindow = new Button { Text = "Pick Window", AutoSize = true };
            _btnClear = new Button { Text = "Clear Log", AutoSize = true };
            _lblTarget = new Label { Text = "Target: (none)", AutoSize = true, Padding = new Padding(10, 8, 0, 0) };

            _btnPickWindow.Click += (s, e) => PickWindow();
            _btnClear.Click += (s, e) => _log.Clear();

            panel.Controls.AddRange(new Control[] { _btnPickWindow, _btnClear, _lblTarget });

            _log = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White
            };

            _form.Controls.Add(_log);
            _form.Controls.Add(panel);

            _form.Load += (s, e) =>
            {
                Log("Test03: WindowTracker initialized");
                Log("Purpose: Verify window tracking behavior with games running");
                Log("No Overlay, No Magnifier created");
                Log("100ms interval polling");
                Log("");
                Log("Click 'Pick Window' to select a target window");
            };

            _timer = new System.Windows.Forms.Timer { Interval = 100 };
            _timer.Tick += (s, e) => PollWindow();
            _timer.Start();

            Application.Run(_form);
        }

        private static void PickWindow()
        {
            Log($"[{Now}] Click 'Pick Window', then click on target window within 3 seconds...");

            var pickForm = new Form
            {
                Text = "Click on target window...",
                Size = new Size(400, 200),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lbl = new Label
            {
                Text = "Click anywhere on the window you want to track",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pickForm.Controls.Add(lbl);

            var countdown = 3;
            var countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            countdownTimer.Tick += (s, e) =>
            {
                countdown--;
                if (countdown <= 0)
                {
                    countdownTimer.Stop();
                    pickForm.Close();
                }
                else
                {
                    lbl.Text = $"Click on target window... ({countdown}s)";
                }
            };

            pickForm.MouseClick += (s, e) =>
            {
                var cursorPos = Cursor.Position;
                var pt = new DarkReader.POINT { x = cursorPos.X, y = cursorPos.Y };
                IntPtr hwnd = WindowFromPoint(pt);
                if (hwnd != IntPtr.Zero)
                {
                    _targetHwnd = hwnd;
                    _targetTitle = GetWindowTitle(hwnd);
                    _lblTarget.Text = $"Target: {_targetTitle} (0x{hwnd:X})";
                    Log($"[{Now}] Target selected: {_targetTitle} (0x{hwnd:X})");
                }
                countdownTimer.Stop();
                pickForm.Close();
            };

            countdownTimer.Start();
            pickForm.ShowDialog();
        }

        private static void PollWindow()
        {
            _tickCount++;

            if (_targetHwnd == IntPtr.Zero)
            {
                if (_tickCount % 50 == 0)
                    Log($"[{Now}] No target selected (tick={_tickCount})");
                return;
            }

            if (!NativeMethods.IsWindow(_targetHwnd))
            {
                Log($"[{Now}] *** TARGET WINDOW DESTROYED ***");
                _targetHwnd = IntPtr.Zero;
                _targetTitle = "(none)";
                _lblTarget.Text = "Target: (none)";
                return;
            }

            RECT windowRect;
            NativeMethods.GetWindowRect(_targetHwnd, out windowRect);

            RECT clientRect;
            NativeMethods.GetClientRect(_targetHwnd, out clientRect);

            bool visible = NativeMethods.IsWindowVisible(_targetHwnd);
            bool iconic = NativeMethods.IsIconic(_targetHwnd);
            bool isForeground = NativeMethods.GetForegroundWindow() == _targetHwnd;

            int style = GetWindowLong(_targetHwnd, GWL_STYLE);
            int exStyle = GetWindowLong(_targetHwnd, GWL_EXSTYLE);

            string title = GetWindowTitle(_targetHwnd);
            string className = GetClassName(_targetHwnd);

            Log($"[{Now}] Tick={_tickCount}");
            Log($"  Title: {title}");
            Log($"  Class: {className}");
            Log($"  WindowRect: ({windowRect.left},{windowRect.top})-({windowRect.right},{windowRect.bottom}) [{windowRect.right - windowRect.left}x{windowRect.bottom - windowRect.top}]");
            Log($"  ClientRect: ({clientRect.left},{clientRect.top})-({clientRect.right},{clientRect.bottom}) [{clientRect.right - clientRect.left}x{clientRect.bottom - clientRect.top}]");
            Log($"  Visible={visible}, Minimized={iconic}, Foreground={isForeground}");
            Log($"  Style=0x{style:X}, ExStyle=0x{exStyle:X}");
            Log($"");
        }

        private static string GetWindowTitle(IntPtr hwnd)
        {
            var sb = new StringBuilder(512);
            NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string GetClassName(IntPtr hwnd)
        {
            var sb = new StringBuilder(256);
            GetClassName(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(DarkReader.POINT pt);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;

        private static void Log(string message)
        {
            if (_log.InvokeRequired)
            {
                _log.Invoke(new Action(() => Log(message)));
                return;
            }

            _log.AppendText(message + Environment.NewLine);
            _log.SelectionStart = _log.Text.Length;
            _log.ScrollToCaret();
        }

        private static string Now => DateTime.Now.ToString("HH:mm:ss.fff");
    }
}
