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
using System.Windows.Forms;

namespace DarkReader
{
    /// <summary>
    /// Full-screen transparent overlay that lets the user drag-select a rectangular region.
    /// </summary>
    internal class RegionSelectorForm : Form
    {
        private Point _startPoint;
        private bool _isDragging;
        private Rectangle _selection;
        private bool _cancelled;

        public Rectangle SelectedRegion => _selection;
        public bool Cancelled => _cancelled;

        public RegionSelectorForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.BackColor = Color.Black;
            this.Opacity = 0.3;
            this.Cursor = Cursors.Cross;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false;

            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            this.KeyDown += OnKeyDown;
            this.Paint += OnPaint;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _startPoint = e.Location;
                _isDragging = true;
                _cancelled = false;
            }
            else if (e.Button == MouseButtons.Right)
            {
                _cancelled = true;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            int x = Math.Min(_startPoint.X, e.X);
            int y = Math.Min(_startPoint.Y, e.Y);
            int w = Math.Abs(e.X - _startPoint.X);
            int h = Math.Abs(e.Y - _startPoint.Y);

            _selection = new Rectangle(x, y, w, h);
            this.Invalidate();
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;

            if (_selection.Width > 10 && _selection.Height > 10)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                // Too small, cancel
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
            if (_selection.Width > 0 && _selection.Height > 0)
            {
                using var pen = new Pen(Color.FromArgb(0, 122, 204), 2);
                e.Graphics.DrawRectangle(pen, _selection);

                // Fill with semi-transparent highlight
                using var brush = new SolidBrush(Color.FromArgb(40, 0, 122, 204));
                e.Graphics.FillRectangle(brush, _selection);
            }

            // Draw instruction text
            using var font = new Font("Segoe UI", 14);
            using var textBrush = new SolidBrush(Color.White);
            string text = "拖拽选择翻转区域 | 右键取消 | Esc 退出";
                e.Graphics.DrawString(text, font, textBrush, 20, 20);
        }
    }
}
