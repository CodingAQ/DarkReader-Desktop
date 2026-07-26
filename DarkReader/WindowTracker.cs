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
    /// Tracks multiple target windows' positions and sizes, invoking callbacks when they change.
    /// Polls at configurable interval for smooth following.
    /// </summary>
    public class WindowTracker : IDisposable
    {
        private readonly Dictionary<IntPtr, TrackedWindow> _trackedWindows = new Dictionary<IntPtr, TrackedWindow>();
        private Thread _pollThread;
        private bool _running;
        private bool _disposed;
        private readonly object _lock = new object();
        private Action _onWindowChanged;
        private Action<IntPtr> _onWindowClosed;
        private int _intervalMs;

        public bool IsTracking => _running;
        public int Count => _trackedWindows.Count;

        public WindowTracker(int intervalMs = 100)
        {
            _intervalMs = intervalMs;
        }

        /// <summary>
        /// Update the polling interval. Takes effect on next poll cycle.
        /// </summary>
        public void UpdateInterval(int intervalMs)
        {
            _intervalMs = intervalMs;
        }

        /// <summary>
        /// Start tracking multiple windows by their handles.
        /// </summary>
        public void StartTracking(IEnumerable<IntPtr> hwnds, Action onWindowChanged, Action<IntPtr> onWindowClosed = null)
        {
            if (_running) StopTracking();

            _onWindowChanged = onWindowChanged;
            _onWindowClosed = onWindowClosed;

            foreach (var hwnd in hwnds)
            {
                AddWindowInternal(hwnd);
            }

            _running = true;
            _pollThread = new Thread(PollLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.Normal,
                Name = "DarkReader WindowTracker"
            };
            _pollThread.Start();

            // Fire callback immediately so overlay appears immediately
            if (_trackedWindows.Count > 0)
            {
                _onWindowChanged?.Invoke();
            }
        }

        /// <summary>
        /// Add a window to tracking. Must be called when tracker is already running.
        /// </summary>
        public void AddWindow(IntPtr hwnd)
        {
            lock (_lock)
            {
                AddWindowInternal(hwnd);
            }
        }

        /// <summary>
        /// Remove a window from tracking.
        /// </summary>
        public void RemoveWindow(IntPtr hwnd)
        {
            lock (_lock)
            {
                _trackedWindows.Remove(hwnd);
            }
        }

        private void AddWindowInternal(IntPtr hwnd)
        {
            if (_trackedWindows.ContainsKey(hwnd)) return;

            var tw = new TrackedWindow { Hwnd = hwnd };
            if (NativeMethods.GetWindowRect(hwnd, out RECT rect))
            {
                tw.LastRect = new Rectangle(rect.left, rect.top,
                    rect.right - rect.left, rect.bottom - rect.top);
            }
            _trackedWindows[hwnd] = tw;
        }

        public void StopTracking()
        {
            _running = false;
            if (_pollThread != null)
            {
                _pollThread.Join(200);
                _pollThread = null;
            }
            _trackedWindows.Clear();
        }

        private void PollLoop()
        {
            while (_running && !_disposed)
            {
                lock (_lock)
                {
                    var closedWindows = new List<IntPtr>();

                    foreach (var kvp in _trackedWindows)
                    {
                        var tw = kvp.Value;

                        if (!NativeMethods.IsWindow(tw.Hwnd))
                        {
                            closedWindows.Add(tw.Hwnd);
                            continue;
                        }

                        if (NativeMethods.GetWindowRect(tw.Hwnd, out RECT rect))
                        {
                            var newRect = new Rectangle(rect.left, rect.top,
                                rect.right - rect.left, rect.bottom - rect.top);

                            if (newRect != tw.LastRect)
                            {
                                tw.LastRect = newRect;
                                tw.HasChanged = true;
                            }
                        }
                    }

                    // Fire closed callbacks
                    foreach (var hwnd in closedWindows)
                    {
                        _trackedWindows.Remove(hwnd);
                        _onWindowClosed?.Invoke(hwnd);
                    }

                    // Fire changed callback if any window changed
                    foreach (var kvp in _trackedWindows)
                    {
                        if (kvp.Value.HasChanged)
                        {
                            kvp.Value.HasChanged = false;
                            _onWindowChanged?.Invoke();
                            break;
                        }
                    }
                }

                Thread.Sleep(_intervalMs);
            }
        }

        private class TrackedWindow
        {
            public IntPtr Hwnd;
            public Rectangle LastRect;
            public bool HasChanged;
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
