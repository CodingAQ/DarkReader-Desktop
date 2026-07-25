// Test02: Region - Verify SetWindowRgn behavior with games
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
        private static Button _btnRect;
        private static Button _btnCircle;
        private static Button _btnTriangle;
        private static Button _btnStar;
        private static Button _btnClear;
        private static ComboBox _cmbRegion;
        private static System.Windows.Forms.Timer _timer;
        private static IntPtr _currentRgn = IntPtr.Zero;
        private static string _currentShape = "None";
        private static int _applyCount;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            NativeMethods.SetProcessDPIAware();

            _form = new Form
            {
                Text = "Test02: Region",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterScreen
            };

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight
            };

            _btnRect = new Button { Text = "Rectangle", AutoSize = true };
            _btnCircle = new Button { Text = "Circle", AutoSize = true };
            _btnTriangle = new Button { Text = "Triangle", AutoSize = true };
            _btnStar = new Button { Text = "Star", AutoSize = true };
            _btnClear = new Button { Text = "Clear Log", AutoSize = true };

            _btnRect.Click += (s, e) => ApplyRegion("Rectangle");
            _btnCircle.Click += (s, e) => ApplyRegion("Circle");
            _btnTriangle.Click += (s, e) => ApplyRegion("Triangle");
            _btnStar.Click += (s, e) => ApplyRegion("Star");
            _btnClear.Click += (s, e) => _log.Clear();

            panel.Controls.AddRange(new Control[] { _btnRect, _btnCircle, _btnTriangle, _btnStar, _btnClear });

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
                Log("Test02: Region initialized");
                Log("Purpose: Verify SetWindowRgn behavior with games running");
                Log("Fixed position window with various region shapes");
                Log("");
                ApplyRegion("Rectangle");
            };

            _form.FormClosing += (s, e) =>
            {
                _timer?.Stop();
                if (_currentRgn != IntPtr.Zero)
                    NativeMethods.DeleteObject(_currentRgn);
            };

            _timer = new System.Windows.Forms.Timer { Interval = 500 };
            _timer.Tick += (s, e) => Heartbeat();
            _timer.Start();

            Application.Run(_form);
        }

        private static void ApplyRegion(string shape)
        {
            Log($"[{Now}] Applying region: {shape}");

            if (_currentRgn != IntPtr.Zero)
            {
                NativeMethods.SetWindowRgn(_form.Handle, IntPtr.Zero, true);
                NativeMethods.DeleteObject(_currentRgn);
                _currentRgn = IntPtr.Zero;
            }

            _currentRgn = CreateRegion(shape);
            _currentShape = shape;
            _applyCount++;

            if (_currentRgn != IntPtr.Zero)
            {
                int result = NativeMethods.SetWindowRgn(_form.Handle, _currentRgn, true);
                Log($"[{Now}] SetWindowRgn({shape}) = {result}, LastError={Marshal.GetLastWin32Error()}, ApplyCount={_applyCount}");

                RECT box;
                int rgnType = NativeMethods.GetRgnBox(_currentRgn, out box);
                Log($"  Region type={rgnType}, box=({box.left},{box.top})-({box.right},{box.bottom})");
            }
            else
            {
                Log($"[{Now}] FAILED to create region for {shape}");
            }
        }

        private static IntPtr CreateRegion(string shape)
        {
            var bounds = _form.ClientRectangle;
            int cx = bounds.Width / 2;
            int cy = bounds.Height / 2;
            int radius = Math.Min(cx, cy) - 20;

            switch (shape)
            {
                case "Rectangle":
                    return NativeMethods.CreateRectRgn(bounds.Left + 10, bounds.Top + 10, bounds.Right - 10, bounds.Bottom - 10);

                case "Circle":
                    return CreateEllipticRgn(cx, cy, radius);

                case "Triangle":
                    return CreatePolygonRgn(new Point[]
                    {
                        new Point(cx, cy - radius),
                        new Point(cx - radius, cy + radius),
                        new Point(cx + radius, cy + radius)
                    });

                case "Star":
                    return CreateStarRgn(cx, cy, radius, radius / 2, 5);

                default:
                    return IntPtr.Zero;
            }
        }

        private static IntPtr CreateEllipticRgn(int cx, int cy, int radius)
        {
            return CreateRectRgnIndirect(cx - radius, cy - radius, cx + radius, cy + radius);
        }

        private static IntPtr CreateRectRgnIndirect(int left, int top, int right, int bottom)
        {
            RECT rc = new RECT { left = left, top = top, right = right, bottom = bottom };
            return NativeMethods.CreateRectRgnIndirect(ref rc);
        }

        private static IntPtr CreatePolygonRgn(Point[] points)
        {
            // Simple approach: use a region from a path
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddPolygon(points);
            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            {
                var region = new Region(path);
                var hRgn = region.GetHgn(g);
                region.Dispose();
                return hRgn;
            }
        }

        private static IntPtr CreateStarRgn(int cx, int cy, int outerR, int innerR, int points)
        {
            var starPoints = new System.Collections.Generic.List<Point>();
            double angle = -Math.PI / 2;
            double step = Math.PI / points;

            for (int i = 0; i < points * 2; i++)
            {
                int r = (i % 2 == 0) ? outerR : innerR;
                int x = cx + (int)(r * Math.Cos(angle));
                int y = cy + (int)(r * Math.Sin(angle));
                starPoints.Add(new Point(x, y));
                angle += step;
            }

            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddPolygon(starPoints.ToArray());
            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            {
                var region = new Region(path);
                var hRgn = region.GetHgn(g);
                region.Dispose();
                return hRgn;
            }
        }

        private static void Heartbeat()
        {
            bool isWindow = NativeMethods.IsWindow(_form.Handle);
            if (!isWindow)
            {
                Log($"[{Now}] *** FORM WINDOW DESTROYED ***");
                return;
            }

            // Note: After SetWindowRgn, the system owns the region handle.
            // We cannot call GetRgnBox on _currentRgn anymore.
            // Just check if the window is still visible.
            bool visible = NativeMethods.IsWindowVisible(_form.Handle);
            if (!visible)
            {
                Log($"[{Now}] *** WINDOW NO LONGER VISIBLE ***");
            }
        }

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

    internal static class RegionExtensions
    {
        public static IntPtr GetHgn(this Region region, Graphics g)
        {
            return region.GetHrgn(g);
        }
    }
}
