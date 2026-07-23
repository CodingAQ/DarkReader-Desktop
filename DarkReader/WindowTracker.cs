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
using System.Threading;

namespace DarkReader
{
    /// <summary>
    /// Tracks a target window's position and size, invoking a callback when it changes.
    /// Polls at 10fps for smooth following.
    /// </summary>
    public class WindowTracker : IDisposable
    {
        private IntPtr _targetHwnd;
        private Thread _pollThread;
        private bool _running;
        private bool _disposed;
        private Rectangle _lastRect;
        private readonly object _lock = new object();
        private Action _onWindowChanged;
        Action _onWindowClosed;

        public bool IsTracking => _running;
        public IntPtr TargetHandle => _targetHwnd;

        /// <summary>
        /// Start tracking a window by its handle.
        /// </summary>
        public void StartTracking(IntPtr hwnd, Action onWindowChanged, Action onWindowClosed = null)
        {
            if (_running) StopTracking();

            _targetHwnd = hwnd;
            _onWindowChanged = onWindowChanged;
            _onWindowClosed = onWindowClosed;
            _running = true;

            // Get initial rect
            if (NativeMethods.GetWindowRect(hwnd, out RECT rect))
            {
                _lastRect = new Rectangle(rect.left, rect.top,
                    rect.right - rect.left, rect.bottom - rect.top);
            }

            _pollThread = new Thread(PollLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.Normal,
                Name = "DarkReader WindowTracker"
            };
            _pollThread.Start();

            // Fire callback immediately so overlay appears immediately
            if (_lastRect.Width > 0 && _lastRect.Height > 0)
            {
                _onWindowChanged?.Invoke();
            }
        }

        /// <summary>
        /// Start tracking using a window title match (partial match).
        /// </summary>
        public bool StartTrackingByTitle(string titleSubstring, Action onWindowChanged, Action onWindowClosed = null)
        {
            var hwnd = FindWindowByTitle(titleSubstring);
            if (hwnd == IntPtr.Zero) return false;
            StartTracking(hwnd, onWindowChanged, onWindowClosed);
            return true;
        }

        public void StopTracking()
        {
            _running = false;
            if (_pollThread != null)
            {
                _pollThread.Join(200);
                _pollThread = null;
            }
            _targetHwnd = IntPtr.Zero;
        }

        private void PollLoop()
        {
            while (_running && !_disposed)
            {
                if (!NativeMethods.IsWindow(_targetHwnd))
                {
                    // Window was closed
                    _running = false;
                    _onWindowClosed?.Invoke();
                    break;
                }

                if (NativeMethods.GetWindowRect(_targetHwnd, out RECT rect))
                {
                    var newRect = new Rectangle(rect.left, rect.top,
                        rect.right - rect.left, rect.bottom - rect.top);

                    lock (_lock)
                    {
                        if (newRect != _lastRect)
                        {
                            _lastRect = newRect;
                            _onWindowChanged?.Invoke();
                        }
                    }
                }

                Thread.Sleep(100); // 10fps polling - enough for window tracking
            }
        }

        /// <summary>
        /// Find a window handle by partial title match.
        /// </summary>
        public static IntPtr FindWindowByTitle(string titleSubstring)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hwnd, lParam) =>
            {
                if (!NativeMethods.IsWindow(hwnd)) return true;
                var sb = new System.Text.StringBuilder(512);
                NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
                string title = sb.ToString();
                if (!string.IsNullOrEmpty(title) &&
                    title.IndexOf(titleSubstring, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = hwnd;
                    return false; // stop enumerating
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                StopTracking();
            }
        }
    }
}
