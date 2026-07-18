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
using System.Runtime.InteropServices;

namespace DarkReader
{
    /// <summary>
    /// Creates a magnifier control window that applies color effect to a specific screen region.
    /// Uses MagSetColorEffect on the magnifier window instead of MagSetFullscreenColorEffect.
    /// </summary>
    public class RegionOverlay : IDisposable
    {
        private IntPtr _hwnd;
        private bool _disposed;
        private RECT _screenRect;

        public bool IsCreated => _hwnd != IntPtr.Zero;

        /// <summary>
        /// Create and show the magnifier overlay covering the specified screen region.
        /// </summary>
        public void Show(Rectangle region)
        {
            if (_hwnd != IntPtr.Zero) return;

            _screenRect = new RECT
            {
                left = region.Left,
                top = region.Top,
                right = region.Right,
                bottom = region.Bottom
            };

            // Get hInstance for this module
            IntPtr hInstance = NativeMethods.GetModuleHandle(null);

            // Create a layered, transparent, topmost, non-activatable popup window
            // with the "Magnifier" class
            _hwnd = NativeMethods.CreateWindowEx(
                WindowStyles.WS_EX_LAYERED |
                WindowStyles.WS_EX_TRANSPARENT |
                WindowStyles.WS_EX_TOPMOST |
                WindowStyles.WS_EX_TOOLWINDOW |
                WindowStyles.WS_EX_NOACTIVATE,
                NativeMethods.WC_MAGNIFIER,
                "DarkReader Region Overlay",
                WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                region.Left, region.Top, region.Width, region.Height,
                IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to create magnifier window");

            // Set the source region to the same area
            NativeMethods.MagSetWindowSource(_hwnd, _screenRect);

            // Set transform to 1.0 (no magnification, just color effect)
            var transform = new Transformation(1.0f);
            NativeMethods.MagSetWindowTransform(_hwnd, ref transform);
        }

        /// <summary>
        /// Apply a color matrix to the region overlay.
        /// </summary>
        public void ApplyColorEffect(float[,] matrix)
        {
            if (_hwnd == IntPtr.Zero) return;

            var effect = new ColorEffect(matrix);
            NativeMethods.MagSetColorEffect(_hwnd, ref effect);
        }

        /// <summary>
        /// Move/resize the overlay to a new region.
        /// </summary>
        public void UpdateRegion(Rectangle region)
        {
            if (_hwnd == IntPtr.Zero) return;

            _screenRect = new RECT
            {
                left = region.Left,
                top = region.Top,
                right = region.Right,
                bottom = region.Bottom
            };

            NativeMethods.MagSetWindowSource(_hwnd, _screenRect);
            NativeMethods.SetWindowPos(_hwnd, WindowStyles.HWND_TOPMOST,
                region.Left, region.Top, region.Width, region.Height,
                WindowStyles.SWP_NOACTIVATE | WindowStyles.SWP_SHOWWINDOW);
        }

        /// <summary>
        /// Hide and destroy the overlay window.
        /// </summary>
        public void Hide()
        {
            if (_hwnd != IntPtr.Zero)
            {
                NativeMethods.DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Hide();
                _disposed = true;
            }
        }
    }
}
