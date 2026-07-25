// Test04: WindowRegionCalculator - Verify region calculation algorithm
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
        private static ComboBox _cmbWindows;
        private static Button _btnRefresh;
        private static Button _btnRecalculate;
        private static Button _btnClear;
        private static Label _lblTarget;
        private static IntPtr _targetHwnd = IntPtr.Zero;
        private static string _targetTitle = "(none)";
        private static int _calcCount;
        private static IntPtr[] _windowList = Array.Empty<IntPtr>();

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            NativeMethods.SetProcessDPIAware();

            _form = new Form
            {
                Text = "Test04: WindowRegionCalculator",
                Size = new Size(700, 500),
                StartPosition = FormStartPosition.CenterScreen
            };

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight
            };

            _cmbWindows = new ComboBox { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbWindows.SelectedIndexChanged += (s, e) =>
            {
                int idx = _cmbWindows.SelectedIndex;
                if (idx >= 0 && idx < _windowList.Length)
                {
                    _targetHwnd = _windowList[idx];
                    _targetTitle = GetWindowTitle(_targetHwnd);
                    _lblTarget.Text = $"Target: {_targetTitle} (0x{_targetHwnd:X})";
                    Log($"[{Now}] Target selected: {_targetTitle} (0x{_targetHwnd:X})");
                }
            };

            _btnRefresh = new Button { Text = "Refresh", AutoSize = true };
            _btnRecalculate = new Button { Text = "Recalculate", AutoSize = true };
            _btnClear = new Button { Text = "Clear Log", AutoSize = true };
            _lblTarget = new Label { Text = "Target: (none)", AutoSize = true, Padding = new Padding(10, 8, 0, 0) };

            _btnRefresh.Click += (s, e) => RefreshWindowList();
            _btnRecalculate.Click += (s, e) => Recalculate();
            _btnClear.Click += (s, e) => _log.Clear();

            panel.Controls.AddRange(new Control[] { _cmbWindows, _btnRefresh, _btnRecalculate, _btnClear, _lblTarget });

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
                Log("Test04: WindowRegionCalculator initialized");
                Log("Purpose: Verify region calculation algorithm with games running");
                Log("Calls CalculateVisibleRegion() and reports results");
                Log("");
                Log("Select a window from the dropdown, then click 'Recalculate'");
                RefreshWindowList();
            };

            Application.Run(_form);
        }

        private static void RefreshWindowList()
        {
            _cmbWindows.Items.Clear();
            _windowList = Array.Empty<IntPtr>();

            var windows = new System.Collections.Generic.List<IntPtr>();
            var titles = new System.Collections.Generic.List<string>();

            NativeMethods.EnumWindows((hwnd, lParam) =>
            {
                if (NativeMethods.IsWindowVisible(hwnd))
                {
                    string title = GetWindowTitle(hwnd);
                    if (!string.IsNullOrEmpty(title))
                    {
                        windows.Add(hwnd);
                        titles.Add(title);
                    }
                }
                return true;
            }, IntPtr.Zero);

            _windowList = windows.ToArray();
            _cmbWindows.Items.AddRange(titles.ToArray());

            Log($"[{Now}] Refreshed window list: {titles.Count} windows found");
        }

        private static void Recalculate()
        {
            if (_targetHwnd == IntPtr.Zero)
            {
                Log($"[{Now}] No target selected");
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

            _calcCount++;
            Log($"[{Now}] === Recalculate #{_calcCount} ===");
            Log($"  Target: {_targetTitle} (0x{_targetHwnd:X})");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var region = WindowRegionCalculator.CalculateVisibleRegion(_targetHwnd, _form.Handle);
            sw.Stop();

            Log($"  Calculation time: {sw.ElapsedMilliseconds}ms");
            Log($"  IsEmpty: {region.IsEmpty}");

            if (!region.IsEmpty)
            {
                Log($"  Region Bounds: ({region.Bounds.Left},{region.Bounds.Top})-({region.Bounds.Right},{region.Bounds.Bottom}) [{region.Bounds.Width}x{region.Bounds.Height}]");

                int area = 0;
                if (region.HRgn != IntPtr.Zero)
                {
                    area = CalculateRegionArea(region.HRgn);
                }
                Log($"  Region Area: {area} pixels");

                RECT box;
                int rgnType = NativeMethods.GetRgnBox(region.HRgn, out box);
                Log($"  Region Type: {rgnType} (1=NULL, 2=SIMPLEREGION, 3=COMPLEXREGION)");
                Log($"  HRgn: 0x{region.HRgn:X}");

                // Count rectangles in region
                int rectCount = GetRegionRectCount(region.HRgn);
                Log($"  Rect Count: {rectCount}");
            }
            else
            {
                Log($"  *** REGION IS EMPTY - target is fully covered! ***");
            }

            Log($"[{Now}] === End Recalculate ===");
            Log($"");

            // Clean up
            if (region.HRgn != IntPtr.Zero)
            {
                WindowRegionCalculator.ReleaseRegion(region.HRgn);
            }
        }

        private static int CalculateRegionArea(IntPtr hRgn)
        {
            RECT box;
            NativeMethods.GetRgnBox(hRgn, out box);
            int width = box.right - box.left;
            int height = box.bottom - box.top;
            return width * height;
        }

        private static int GetRegionRectCount(IntPtr hRgn)
        {
            // Get region data to count rectangles
            int size = GetRegionData(hRgn, 0, IntPtr.Zero);
            if (size == 0) return 0;

            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                GetRegionData(hRgn, size, buffer);
                // RGNDATAHEADER has 4 DWORDs (16 bytes) for rcBound + 2 DWORDs for nCount + 2 DWORDs for nRgnSize
                // Actually: RGNDATAHEADER is 32 bytes: DWORD dwSize, DWORD iType, DWORD nCount, DWORD nRgnSize, RECT rcBound
                int nCount = Marshal.ReadInt32(buffer, 16); // offset to nCount
                return nCount;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        [DllImport("gdi32.dll")]
        private static extern int GetRegionData(IntPtr hRgn, int dwCount, IntPtr lpRgnData);

        private static string GetWindowTitle(IntPtr hwnd)
        {
            var sb = new StringBuilder(512);
            NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string Now => DateTime.Now.ToString("HH:mm:ss.fff");

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
    }
}
