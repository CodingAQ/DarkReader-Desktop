// This file is part of DarkReader.
// Copyright (C) 2026 DarkReader Contributors.
//
// Derived from NegativeScreen by mlaily (https://github.com/mlaily/NegativeScreen),
// originally licensed under GPL-3.0.
//
// DarkReader is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License version 3 as published
// by the Free Software Foundation.
//
// DarkReader is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with DarkReader. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DarkReader
{
    /// <summary>
    /// Full-screen overlay that highlights windows under the cursor and lets the user click to select one.
    /// </summary>
    internal class WindowPickerForm : Form
    {
        private IntPtr _selectedHwnd;
        private IntPtr _highlightedHwnd;
        private bool _cancelled;

        public IntPtr SelectedHandle => _selectedHwnd;
        public bool Cancelled => _cancelled;

        public WindowPickerForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.BackColor = Color.Black;
            this.Opacity = 0.25;
            this.Cursor = Cursors.Hand;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false;

            this.MouseMove += OnMouseMove;
            this.MouseClick += OnMouseClick;
            this.KeyDown += OnKeyDown;
            this.Paint += OnPaint;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            // Get window under cursor
            var pt = new POINT { x = e.X, y = e.Y };
            IntPtr hwnd = WindowFromPoint(pt);
            hwnd = GetRootWindow(hwnd);

            // Skip our own window
            if (hwnd == this.Handle) hwnd = IntPtr.Zero;

            if (hwnd != _highlightedHwnd)
            {
                _highlightedHwnd = hwnd;
                this.Invalidate();
            }
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _highlightedHwnd != IntPtr.Zero)
            {
                _selectedHwnd = _highlightedHwnd;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else if (e.Button == MouseButtons.Right)
            {
                _cancelled = true;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                _cancelled = true;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            if (_highlightedHwnd != IntPtr.Zero && NativeMethods.IsWindow(_highlightedHwnd))
            {
                if (NativeMethods.GetWindowRect(_highlightedHwnd, out RECT rect))
                {
                    var screenRect = new Rectangle(rect.left, rect.top,
                        rect.right - rect.left, rect.bottom - rect.top);

                    // Draw highlight border
                    using var pen = new Pen(Color.FromArgb(0, 122, 204), 3);
                    e.Graphics.DrawRectangle(pen, screenRect);

                    // Fill with semi-transparent highlight
                    using var brush = new SolidBrush(Color.FromArgb(30, 0, 122, 204));
                    e.Graphics.FillRectangle(brush, screenRect);

                    // Draw window title
                    var sb = new StringBuilder(512);
                    NativeMethods.GetWindowText(_highlightedHwnd, sb, sb.Capacity);
                    string title = sb.ToString();
                    if (!string.IsNullOrEmpty(title))
                    {
                        using var font = new Font("Segoe UI", 10, FontStyle.Bold);
                        using var textBrush = new SolidBrush(Color.White);
                        using var bgBrush = new SolidBrush(Color.FromArgb(180, 0, 122, 204));
                        var textSize = e.Graphics.MeasureString(title, font);
                        var bgRect = new Rectangle(screenRect.X, screenRect.Y - 25,
                            (int)textSize.Width + 10, 25);
                        e.Graphics.FillRectangle(bgBrush, bgRect);
                        e.Graphics.DrawString(title, font, textBrush, screenRect.X + 5, screenRect.Y - 22);
                    }
                }
            }

            // Draw instruction
            using var instrFont = new Font("Segoe UI", 14);
            using var instrBrush = new SolidBrush(Color.White);
            e.Graphics.DrawString("点击窗口以跟随翻转 | 右键/Esc 取消", instrFont, instrBrush, 20, 20);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT pt);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        private const uint GA_ROOT = 2;

        private static IntPtr GetRootWindow(IntPtr hwnd)
        {
            var root = GetAncestor(hwnd, GA_ROOT);
            return root != IntPtr.Zero ? root : hwnd;
        }
    }
}
