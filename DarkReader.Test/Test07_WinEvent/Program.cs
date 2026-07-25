// Test07: WinEvent Hook - Verify WinEvent hook behavior with games
using System;
using System.Diagnostics;
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
        private static Button _btnClear;
        private static Button _btnToggleHook;
        private static IntPtr _hookHandle = IntPtr.Zero;
        private static NativeMethods.WinEventProc _proc;
        private static int _eventCount;
        private static bool _hookActive;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            NativeMethods.SetProcessDPIAware();

            _form = new Form
            {
                Text = "Test07: WinEvent Hook",
                Size = new Size(800, 500),
                StartPosition = FormStartPosition.CenterScreen
            };

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight
            };

            _btnClear = new Button { Text = "Clear Log", AutoSize = true };
            _btnToggleHook = new Button { Text = "Start Hook", AutoSize = true };

            _btnClear.Click += (s, e) => _log.Clear();
            _btnToggleHook.Click += (s, e) => ToggleHook();

            panel.Controls.AddRange(new Control[] { _btnClear, _btnToggleHook });

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
                Log("Test07: WinEvent Hook initialized");
                Log("Purpose: Verify WinEvent hook behavior with games running");
                Log("Listening: LOCATIONCHANGE, FOREGROUND, SHOW, HIDE, DESTROY");
                Log("");
                InstallHook();
            };

            _form.FormClosing += (s, e) => UninstallHook();

            Application.Run(_form);
        }

        private static void ToggleHook()
        {
            if (_hookActive)
            {
                UninstallHook();
            }
            else
            {
                InstallHook();
            }
        }

        private static void InstallHook()
        {
            if (_hookActive) return;

            _proc = new NativeMethods.WinEventProc(WinEventProc);

            _hookHandle = NativeMethods.SetWinEventHook(
                EVENT_OBJECT_LOCATIONCHANGE, EVENT_OBJECT_DESTROY,
                IntPtr.Zero, _proc,
                0, 0,
                NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

            _hookActive = _hookHandle != IntPtr.Zero;
            _btnToggleHook.Text = _hookActive ? "Stop Hook" : "Start Hook";

            Log($"[{Now}] SetWinEventHook() = 0x{_hookHandle:X}, Active={_hookActive}");
        }

        private static void UninstallHook()
        {
            if (!_hookActive) return;

            if (_hookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }

            _hookActive = false;
            _btnToggleHook.Text = "Start Hook";
            Log($"[{Now}] UnhookWinEvent()");
        }

        private static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            _eventCount++;

            string eventName = GetEventName(eventType);
            string hwndStr = $"0x{hwnd:X}";
            string objId = idObject.ToString();
            string childId = idChild.ToString();

            // Try to get window info
            string title = "";
            uint pid = 0;
            try
            {
                NativeMethods.GetWindowThreadProcessId(hwnd, out pid);
                var sb = new StringBuilder(256);
                NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
                title = sb.ToString();
            }
            catch { }

            string message = $"[{Now}] Event #{_eventCount}: {eventName} (0x{eventType:X})" +
                             $"\n  HWND={hwndStr}, PID={pid}, Thread={dwEventThread}" +
                             $"\n  ObjectId={objId}, ChildId={childId}, Time={dwmsEventTime}" +
                             $"\n  Title={title}";

            Log(message);
        }

        private static string GetEventName(uint eventType)
        {
            switch (eventType)
            {
                case 0x800B: return "EVENT_OBJECT_LOCATIONCHANGE";
                case 0x0003: return "EVENT_SYSTEM_FOREGROUND";
                case 0x8002: return "EVENT_OBJECT_SHOW";
                case 0x8003: return "EVENT_OBJECT_HIDE";
                case 0x8001: return "EVENT_OBJECT_DESTROY";
                case 0x8000: return "EVENT_OBJECT_CREATE";
                case 0x800A: return "EVENT_OBJECT_FOCUS";
                case 0x0005: return "EVENT_SYSTEM_MENUSTART";
                case 0x0006: return "EVENT_SYSTEM_MENUEND";
                case 0x0009: return "EVENT_SYSTEM_MENUPOPUPSTART";
                case 0x000A: return "EVENT_SYSTEM_MENUPOPUPEND";
                case 0x8004: return "EVENT_OBJECT_REORDER";
                default: return $"Unknown(0x{eventType:X4})";
            }
        }

        private const uint EVENT_OBJECT_CREATE = 0x8000;
        private const uint EVENT_OBJECT_DESTROY = 0x8001;
        private const uint EVENT_OBJECT_SHOW = 0x8002;
        private const uint EVENT_OBJECT_HIDE = 0x8003;
        private const uint EVENT_OBJECT_REORDER = 0x8004;
        private const uint EVENT_OBJECT_FOCUS = 0x800A;
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;

        private static void Log(string message)
        {
            if (_log.InvokeRequired)
            {
                _log.Invoke(new Action(() => Log(message)));
                return;
            }

            _log.AppendText(message + Environment.NewLine + Environment.NewLine);
            _log.SelectionStart = _log.Text.Length;
            _log.ScrollToCaret();
        }

        private static string Now => DateTime.Now.ToString("HH:mm:ss.fff");
    }
}
