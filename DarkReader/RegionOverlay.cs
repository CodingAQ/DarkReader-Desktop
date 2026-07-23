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
        private Rectangle _lastAppliedBounds;
        private bool _lastWasEmpty = true;
        private bool _matrixDirty = true;
        private float[,] _lastMatrix;
        private RegionInfo _lastAppliedRegion;

        public bool IsCreated => _hwnd != IntPtr.Zero;
        public IntPtr WindowHandle => _hwnd;

        /// <summary>
        /// Create and show the magnifier overlay covering the specified screen region.
        /// </summary>
        public void Show(RegionInfo region)
        {
            if (_hwnd != IntPtr.Zero) return;

            _screenRect = new RECT
            {
                left = region.Bounds.Left,
                top = region.Bounds.Top,
                right = region.Bounds.Right,
                bottom = region.Bounds.Bottom
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
                region.Bounds.Left, region.Bounds.Top, region.Bounds.Width, region.Bounds.Height,
                IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to create magnifier window");

            // Set the source region to the same area
            NativeMethods.MagSetWindowSource(_hwnd, _screenRect);

            // Set transform to 1.0 (no magnification, just color effect)
            var transform = new Transformation(1.0f);
            NativeMethods.MagSetWindowTransform(_hwnd, ref transform);

            // Apply initial region clipping
            _lastAppliedBounds = region.Bounds;
            _lastWasEmpty = region.IsEmpty;
            ApplyRegionShape(region);
        }

        /// <summary>
        /// Apply a color matrix to the region overlay. Only applies if matrix changed.
        /// </summary>
        public void ApplyColorEffect(float[,] matrix)
        {
            if (_hwnd == IntPtr.Zero) return;

            // Only apply if matrix actually changed
            if (!_matrixDirty && _lastMatrix == matrix) return;

            var effect = new ColorEffect(matrix);
            NativeMethods.MagSetColorEffect(_hwnd, ref effect);
            _lastMatrix = matrix;
            _matrixDirty = false;
        }

        /// <summary>
        /// Mark matrix as dirty (force re-apply on next frame).
        /// </summary>
        public void InvalidateMatrix()
        {
            _matrixDirty = true;
        }

        /// <summary>
        /// Move/resize and reshape the overlay to a new region.
        /// </summary>
        public void UpdateRegion(RegionInfo region)
        {
            if (_hwnd == IntPtr.Zero) return;

            bool boundsChanged = region.Bounds != _lastAppliedBounds;
            bool emptyChanged = region.IsEmpty != _lastWasEmpty;

            // Update source rect
            _screenRect = new RECT
            {
                left = region.Bounds.Left,
                top = region.Bounds.Top,
                right = region.Bounds.Right,
                bottom = region.Bounds.Bottom
            };
            NativeMethods.MagSetWindowSource(_hwnd, _screenRect);

            // Only reposition if bounds changed
            if (boundsChanged)
            {
                NativeMethods.SetWindowPos(_hwnd, WindowStyles.HWND_TOPMOST,
                    region.Bounds.Left, region.Bounds.Top, region.Bounds.Width, region.Bounds.Height,
                    WindowStyles.SWP_NOACTIVATE | WindowStyles.SWP_NOZORDER | WindowStyles.SWP_NOSENDCHANGING);
                _lastAppliedBounds = region.Bounds;
            }

            // Always update region shape when visible (Z-order can change shape without changing bounds)
            if (!region.IsEmpty)
            {
                ApplyRegionShape(region);
                _lastWasEmpty = false;
                _lastAppliedRegion = region;
            }
            else if (emptyChanged)
            {
                ApplyRegionShape(region);
                _lastWasEmpty = true;
            }
        }

        /// <summary>
        /// Apply region clipping to the magnifier window.
        /// Note: SetWindowRgn takes ownership of the region handle.
        /// Region coordinates must be relative to window top-left (0,0), not screen coordinates.
        /// </summary>
        private void ApplyRegionShape(RegionInfo region)
        {
            if (_hwnd == IntPtr.Zero) return;

            if (!region.IsEmpty && region.HRgn != IntPtr.Zero)
            {
                // Offset region from screen coordinates to window-relative coordinates
                // Window is positioned at (Bounds.Left, Bounds.Top) in screen coordinates
                NativeMethods.OffsetRgn(region.HRgn, -region.Bounds.Left, -region.Bounds.Top);

                // SetWindowRgn takes ownership of the region handle
                NativeMethods.SetWindowRgn(_hwnd, region.HRgn, false);
            }
            else
            {
                // Empty region - set to null to show nothing
                NativeMethods.SetWindowRgn(_hwnd, IntPtr.Zero, false);
            }
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
