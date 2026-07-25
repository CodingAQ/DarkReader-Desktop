// Test05: Overlay - Verify overlay window visibility with games
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DarkReader.Test
{
    static class Program
    {
        private static Form _form;
        private static TextBox _log;
        private static Button _btnClear;
        private static System.Windows.Forms.Timer _timer;
        private static int _tickCount;
        private static bool _overlayVisible = true;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            NativeMethods.SetProcessDPIAware();

            // Create overlay window with WS_EX_LAYERED | WS_EX_TRANSPARENT | TopMost
            _form = new Form
            {
                Text = "Test05: Overlay",
                Size = new Size(400, 300),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(100, 100),
                FormBorderStyle = FormBorderStyle.Sizable,
                BackColor = Color.Red,
                Opacity = 0.5,
                ShowInTaskbar = true
            };

            // Make it layered and transparent
            int exStyle = GetWindowLong(_form.Handle, GWL_EXSTYLE);
            SetWindowLong(_form.Handle, GWL_EXSTYLE, exStyle | WindowStyles.WS_EX_LAYERED | WindowStyles.WS_EX_TRANSPARENT | WindowStyles.WS_EX_TOPMOST);

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight
            };

            _btnClear = new Button { Text = "Clear Log", AutoSize = true };
            _btnClear.Click += (s, e) => _log.Clear();

            panel.Controls.Add(_btnClear);

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
                Log("Test05: Overlay initialized");
                Log("Purpose: Verify overlay window visibility with games running");
                Log("WS_EX_LAYERED | WS_EX_TRANSPARENT | TopMost");
                Log("50% red transparent window at fixed position");
                Log("");
                Log("Start a game and observe if this overlay remains visible");
            };

            _timer = new System.Windows.Forms.Timer { Interval = 500 };
            _timer.Tick += (s, e) => Heartbeat();
            _timer.Start();

            Application.Run(_form);
        }

        private static void Heartbeat()
        {
            _tickCount++;

            bool isWindow = NativeMethods.IsWindow(_form.Handle);
            if (!isWindow)
            {
                Log($"[{Now}] *** OVERLAY WINDOW DESTROYED ***");
                return;
            }

            // Check if window is still visible
            bool visible = NativeMethods.IsWindowVisible(_form.Handle);
            if (visible != _overlayVisible)
            {
                Log($"[{Now}] *** VISIBILITY CHANGED: {_overlayVisible} -> {visible} ***");
                _overlayVisible = visible;
            }

            // Check foreground window
            IntPtr fg = NativeMethods.GetForegroundWindow();
            bool isForeground = fg == _form.Handle;

            // Check if still topmost
            int exStyle = GetWindowLong(_form.Handle, GWL_EXSTYLE);
            bool isTopMost = (exStyle & WindowStyles.WS_EX_TOPMOST) != 0;
            bool isLayered = (exStyle & WindowStyles.WS_EX_LAYERED) != 0;
            bool isTransparent = (exStyle & WindowStyles.WS_EX_TRANSPARENT) != 0;

            if (_tickCount % 10 == 0 || !visible)
            {
                Log($"[{Now}] Tick={_tickCount}");
                Log($"  Visible={visible}, Foreground={isForeground}");
                Log($"  TopMost={isTopMost}, Layered={isLayered}, Transparent={isTransparent}");
                Log($"  ForegroundWindow=0x{fg:X}");
                Log($"");
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

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
