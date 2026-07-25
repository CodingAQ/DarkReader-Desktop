// Test01: Magnifier - Verify Magnification API behavior with games
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
        private static Button _btnReapply;
        private static Button _btnRecreate;
        private static Button _btnStatus;
        private static Button _btnClear;
        private static IntPtr _magHwnd = IntPtr.Zero;
        private static System.Windows.Forms.Timer _timer;
        private static int _applyCount;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            NativeMethods.SetProcessDPIAware();

            _form = new Form
            {
                Text = "Test01: Magnifier",
                Size = new Size(600, 500),
                StartPosition = FormStartPosition.CenterScreen
            };

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight
            };

            _btnReapply = new Button { Text = "Reapply Matrix", AutoSize = true };
            _btnRecreate = new Button { Text = "Recreate Magnifier", AutoSize = true };
            _btnStatus = new Button { Text = "Print Status", AutoSize = true };
            _btnClear = new Button { Text = "Clear Log", AutoSize = true };

            _btnReapply.Click += (s, e) => ReapplyMatrix();
            _btnRecreate.Click += (s, e) => RecreateMagnifier();
            _btnStatus.Click += (s, e) => PrintStatus();
            _btnClear.Click += (s, e) => _log.Clear();

            panel.Controls.AddRange(new Control[] { _btnReapply, _btnRecreate, _btnStatus, _btnClear });

            _log = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White
            };

            _log.Text = "";
            AutoScroll = true;

            _form.Controls.Add(_log);
            _form.Controls.Add(panel);

            _form.Load += (s, e) => InitializeMagnifier();
            _form.FormClosing += (s, e) => Cleanup();

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) => Heartbeat();
            _timer.Start();

            Log("Test01: Magnifier initialized");
            Log("Purpose: Verify Magnification API behavior with games running");
            Log("No WindowTracker, Region, or WinEvent used");
            Log("");

            Application.Run(_form);
        }

        private static bool AutoScroll { get; set; }

        private static void InitializeMagnifier()
        {
            try
            {
                bool init = NativeMethods.MagInitialize();
                Log($"[{Now}] MagInitialize() = {init}, LastError={Marshal.GetLastWin32Error()}");

                CreateMagnifierWindow();
            }
            catch (Exception ex)
            {
                Log($"[{Now}] EXCEPTION: {ex}");
            }
        }

        private static void CreateMagnifierWindow()
        {
            if (_magHwnd != IntPtr.Zero)
            {
                NativeMethods.DestroyWindow(_magHwnd);
                _magHwnd = IntPtr.Zero;
            }

            IntPtr hInstance = NativeMethods.GetModuleHandle(null);

            _magHwnd = NativeMethods.CreateWindowEx(
                WindowStyles.WS_EX_LAYERED | WindowStyles.WS_EX_TRANSPARENT | WindowStyles.WS_EX_TOPMOST | WindowStyles.WS_EX_NOACTIVATE,
                NativeMethods.WC_MAGNIFIER,
                "Test01 Magnifier",
                WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                100, 100, 400, 300,
                IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

            Log($"[{Now}] CreateWindowEx(Magnifier) = 0x{_magHwnd:X}, LastError={Marshal.GetLastWin32Error()}");

            if (_magHwnd != IntPtr.Zero)
            {
                NativeMethods.SetWindowPos(_magHwnd, WindowStyles.HWND_TOPMOST, 100, 100, 400, 300,
                    WindowStyles.SWP_NOACTIVATE | WindowStyles.SWP_SHOWWINDOW);

                var transform = new Transformation(1.0f);
                bool transformResult = NativeMethods.MagSetWindowTransform(_magHwnd, ref transform);
                Log($"[{Now}] MagSetWindowTransform() = {transformResult}, LastError={Marshal.GetLastWin32Error()}");

                RECT sourceRect = new RECT { left = 100, top = 100, right = 500, bottom = 400 };
                bool sourceResult = NativeMethods.MagSetWindowSource(_magHwnd, sourceRect);
                Log($"[{Now}] MagSetWindowSource() = {sourceResult}, LastError={Marshal.GetLastWin32Error()}");

                ReapplyMatrix();
            }
        }

        private static void ReapplyMatrix()
        {
            if (_magHwnd == IntPtr.Zero)
            {
                Log($"[{Now}] Cannot reapply - magnifier not created");
                return;
            }

            try
            {
                var effect = new ColorEffect(BuiltinMatrices.SimpleInversion);
                bool result = NativeMethods.MagSetColorEffect(_magHwnd, ref effect);
                _applyCount++;
                Log($"[{Now}] MagSetColorEffect(SimpleInversion) = {result}, LastError={Marshal.GetLastWin32Error()}, ApplyCount={_applyCount}");
            }
            catch (Exception ex)
            {
                Log($"[{Now}] EXCEPTION: {ex}");
            }
        }

        private static void RecreateMagnifier()
        {
            Log($"[{Now}] Recreating magnifier...");
            CreateMagnifierWindow();
        }

        private static void PrintStatus()
        {
            Log($"[{Now}] === STATUS ===");
            Log($"  MagHandle = 0x{_magHwnd:X}");
            Log($"  ApplyCount = {_applyCount}");

            if (_magHwnd != IntPtr.Zero)
            {
                var effect = new ColorEffect();
                bool getResult = NativeMethods.MagGetColorEffect(_magHwnd, ref effect);
                Log($"  MagGetColorEffect() = {getResult}, LastError={Marshal.GetLastWin32Error()}");

                if (getResult)
                {
                    var matrix = effect.GetMatrix();
                    Log($"  Current matrix[0,0]={matrix[0,0]}, [1,1]={matrix[1,1]}, [2,2]={matrix[2,2]}");
                }

                RECT rect;
                NativeMethods.GetWindowRect(_magHwnd, out rect);
                Log($"  WindowRect = ({rect.left},{rect.top})-({rect.right},{rect.bottom})");
            }

            float magLevel = 0;
            int offsetX = 0, offsetY = 0;
            bool transformResult = NativeMethods.MagGetFullscreenTransform(ref magLevel, ref offsetX, ref offsetY);
            Log($"  FullscreenTransform: level={magLevel}, offset=({offsetX},{offsetY}), result={transformResult}");

            Log($"[{Now}] === END STATUS ===");
        }

        private static void Heartbeat()
        {
            if (_magHwnd != IntPtr.Zero)
            {
                bool isWindow = NativeMethods.IsWindow(_magHwnd);
                if (!isWindow)
                {
                    Log($"[{Now}] *** MAGNIFIER WINDOW DESTROYED ***");
                    _magHwnd = IntPtr.Zero;
                }
            }
        }

        private static void Cleanup()
        {
            _timer?.Stop();

            if (_magHwnd != IntPtr.Zero)
            {
                NativeMethods.DestroyWindow(_magHwnd);
                _magHwnd = IntPtr.Zero;
            }

            NativeMethods.MagUninitialize();
            Log($"[{Now}] MagUninitialize()");
        }

        private static void Log(string message)
        {
            if (_log.InvokeRequired)
            {
                _log.Invoke(new Action(() => Log(message)));
                return;
            }

            _log.AppendText(message + Environment.NewLine);
            if (AutoScroll)
            {
                _log.SelectionStart = _log.Text.Length;
                _log.ScrollToCaret();
            }
        }

        private static string Now => DateTime.Now.ToString("HH:mm:ss.fff");
    }
}
