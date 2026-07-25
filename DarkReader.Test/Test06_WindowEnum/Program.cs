// Test06: Window Enumeration - Check for hidden windows created by games
using System;
using System.Collections.Generic;
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
        private static Button _btnRefresh;
        private static Button _btnClear;
        private static Button _btnFilterGame;
        private static ListView _listView;
        private static int _enumCount;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            NativeMethods.SetProcessDPIAware();

            _form = new Form
            {
                Text = "Test06: Window Enumeration",
                Size = new Size(1000, 600),
                StartPosition = FormStartPosition.CenterScreen
            };

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight
            };

            _btnRefresh = new Button { Text = "Refresh", AutoSize = true };
            _btnClear = new Button { Text = "Clear Log", AutoSize = true };
            _btnFilterGame = new Button { Text = "Filter: All", AutoSize = true };

            _btnRefresh.Click += (s, e) => Refresh();
            _btnClear.Click += (s, e) => _log.Clear();
            _btnFilterGame.Click += (s, e) =>
            {
                _showAll = !_showAll;
                _btnFilterGame.Text = _showAll ? "Filter: All" : "Filter: Visible Only";
                Refresh();
            };

            panel.Controls.AddRange(new Control[] { _btnRefresh, _btnClear, _btnFilterGame });

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 350
            };

            _listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            _listView.Columns.Add("HWND", 80);
            _listView.Columns.Add("PID", 60);
            _listView.Columns.Add("Process", 120);
            _listView.Columns.Add("Title", 200);
            _listView.Columns.Add("Class", 120);
            _listView.Columns.Add("Visible", 50);
            _listView.Columns.Add("Enabled", 50);
            _listView.Columns.Add("Owner", 80);
            _listView.Columns.Add("Parent", 80);
            _listView.Columns.Add("Style", 80);
            _listView.Columns.Add("ExStyle", 80);
            _listView.Columns.Add("Rect", 150);
            _listView.Columns.Add("Z-Order", 60);

            _log = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White
            };

            split.Panel1.Controls.Add(_listView);
            split.Panel2.Controls.Add(_log);

            _form.Controls.Add(split);
            _form.Controls.Add(panel);

            _form.Load += (s, e) =>
            {
                Log("Test06: Window Enumeration initialized");
                Log("Purpose: Check for hidden windows created by games");
                Log("Enumerates all top-level windows with full details");
                Log("");
                Refresh();
            };

            Application.Run(_form);
        }

        private static bool _showAll = true;

        private static void Refresh()
        {
            _enumCount++;
            _listView.Items.Clear();
            Log($"[{Now}] === Refresh #{_enumCount} ===");

            var windows = new List<WindowInfo>();
            NativeMethods.EnumWindows((hwnd, lParam) =>
            {
                var info = GetWindowInfo(hwnd);
                if (info != null)
                {
                    if (_showAll || info.Visible)
                    {
                        windows.Add(info);
                    }
                }
                return true;
            }, IntPtr.Zero);

            // Sort by Z-order (approximate: use enumeration order)
            for (int i = 0; i < windows.Count; i++)
            {
                windows[i].ZOrder = i;
                var item = new ListViewItem($"0x{windows[i].Handle:X}");
                item.SubItems.Add(windows[i].PID.ToString());
                item.SubItems.Add(windows[i].ProcessName);
                item.SubItems.Add(windows[i].Title);
                item.SubItems.Add(windows[i].ClassName);
                item.SubItems.Add(windows[i].Visible ? "Yes" : "No");
                item.SubItems.Add(windows[i].Enabled ? "Yes" : "No");
                item.SubItems.Add($"0x{windows[i].Owner:X}");
                item.SubItems.Add($"0x{windows[i].Parent:X}");
                item.SubItems.Add($"0x{windows[i].Style:X}");
                item.SubItems.Add($"0x{windows[i].ExStyle:X}");
                item.SubItems.Add(windows[i].Rect);
                item.SubItems.Add(i.ToString());

                // Highlight invisible windows
                if (!windows[i].Visible)
                {
                    item.BackColor = Color.LightYellow;
                }

                _listView.Items.Add(item);
            }

            Log($"  Total windows: {windows.Count}");
            Log($"  Visible: {windows.FindAll(w => w.Visible).Count}");
            Log($"  Invisible: {windows.FindAll(w => !w.Visible).Count}");
            Log($"[{Now}] === End Refresh ===");
            Log("");
        }

        private static WindowInfo GetWindowInfo(IntPtr hwnd)
        {
            try
            {
                var info = new WindowInfo { Handle = hwnd };

                uint pid;
                NativeMethods.GetWindowThreadProcessId(hwnd, out pid);
                info.PID = pid;

                try
                {
                    var proc = Process.GetProcessById((int)pid);
                    info.ProcessName = proc.ProcessName;
                }
                catch
                {
                    info.ProcessName = "(access denied)";
                }

                var sb = new StringBuilder(512);
                NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
                info.Title = sb.ToString();

                var cn = new StringBuilder(256);
                GetClassName(hwnd, cn, cn.Capacity);
                info.ClassName = cn.ToString();

                info.Visible = NativeMethods.IsWindowVisible(hwnd);
                info.Enabled = IsWindowEnabled(hwnd);
                info.Owner = NativeMethods.GetWindow(hwnd, 4); // GW_OWNER
                info.Parent = NativeMethods.GetParent(hwnd);
                info.Style = GetWindowLong(hwnd, GWL_STYLE);
                info.ExStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

                RECT rect;
                NativeMethods.GetWindowRect(hwnd, out rect);
                info.Rect = $"({rect.left},{rect.top})-({rect.right},{rect.bottom})";

                return info;
            }
            catch (Exception ex)
            {
                Log($"  Error getting info for 0x{hwnd:X}: {ex.Message}");
                return null;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(IntPtr hWnd);

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

    class WindowInfo
    {
        public IntPtr Handle;
        public uint PID;
        public string ProcessName;
        public string Title;
        public string ClassName;
        public bool Visible;
        public bool Enabled;
        public IntPtr Owner;
        public IntPtr Parent;
        public int Style;
        public int ExStyle;
        public string Rect;
        public int ZOrder;
    }
}
