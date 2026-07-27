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
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DarkReader
{
    internal class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip contextMenu;
        private bool effectActive = false;
        private int currentMode = 0;
        private Thread controlThread;
        private readonly object controlLock = new object();
        private bool exiting = false;
        private float[,] currentMatrix;
        private RegionOverlay _regionOverlay;

        // Region restriction
        private Rectangle? _region;
        private bool _useRegion = false;
        private Rectangle _lastAppliedRegion;
        private bool _lastShowDecision = false; // last shouldShow state
        private IntPtr _winEventHook = IntPtr.Zero;
        private NativeMethods.WinEventProc _winEventProc; // keep reference to prevent GC
        private RegionInfo _currentRegionInfo; // current visible region (for window mode)

        // Window targeting
        private WindowTracker _windowTracker;
        private Dictionary<IntPtr, string> _targetWindows = new Dictionary<IntPtr, string>();
        private HashSet<string> _closedWindowTitles = new HashSet<string>();
        private bool _useWindow = false;
        private System.Threading.Timer _closedWindowRescanTimer;
        private const int ClosedWindowRescanIntervalMs = 1000;

        // Hotkey IDs
        private const int HOTKEY_TOGGLE = 1;
        private const int HOTKEY_MODE0 = 9;
        private const int HOTKEY_MODE1 = 2;
        private const int HOTKEY_MODE2 = 3;
        private const int HOTKEY_MODE3 = 4;
        private const int HOTKEY_MODE4 = 5;
        private const int HOTKEY_MODE5 = 6;
        private const int HOTKEY_MODE6 = 7;
        private const int HOTKEY_REGION = 8;
        private const int HOTKEY_EXIT = 99;

        public MainForm()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing;

            InitializeTrayIcon();
            RegisterHotKeys();
            StartControlLoop();
            InstallForegroundHook();

            // Periodically check whether a previously closed target window has
            // reopened, so dark mode re-applies automatically without requiring
            // the user to open the "Select Window" menu.
            _closedWindowRescanTimer = new System.Threading.Timer(
                RescanClosedWindows, null, ClosedWindowRescanIntervalMs, ClosedWindowRescanIntervalMs);
        }

        private void InitializeTrayIcon()
        {
            contextMenu = new ContextMenuStrip();

            var toggleItem = new ToolStripMenuItem("Toggle", null, OnToggleClick);
            contextMenu.Items.Add(toggleItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            var mode1Item = new ToolStripMenuItem("Default", null, (s, e) => SetMode(0));
            var mode2Item = new ToolStripMenuItem("Preset 1", null, (s, e) => SetMode(1));
            var mode3Item = new ToolStripMenuItem("Preset 2", null, (s, e) => SetMode(2));
            var mode4Item = new ToolStripMenuItem("Preset 3", null, (s, e) => SetMode(3));
            var mode5Item = new ToolStripMenuItem("Preset 4", null, (s, e) => SetMode(4));
            var mode6Item = new ToolStripMenuItem("Preset 5", null, (s, e) => SetMode(5));
            var mode7Item = new ToolStripMenuItem("Grayscale", null, (s, e) => SetMode(6));

            contextMenu.Items.Add(mode1Item);
            contextMenu.Items.Add(mode2Item);
            contextMenu.Items.Add(mode3Item);
            contextMenu.Items.Add(mode4Item);
            contextMenu.Items.Add(mode5Item);
            contextMenu.Items.Add(mode6Item);
            contextMenu.Items.Add(mode7Item);
            contextMenu.Items.Add(new ToolStripSeparator());

            // Region restriction menu
            var selectRegionItem = new ToolStripMenuItem("Select Region", null, OnSelectRegionClick);
            var clearRegionItem = new ToolStripMenuItem("Clear Region", null, OnClearRegionClick);
            contextMenu.Items.Add(selectRegionItem);
            contextMenu.Items.Add(clearRegionItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            // Window targeting menu
            var selectWindowItem = new ToolStripMenuItem("Select Window");
            var clearWindowItem = new ToolStripMenuItem("Clear Window Target", null, OnClearWindowClick);
            var startupItem = new ToolStripMenuItem("Active On Startup", null, OnToggleStartup) { Checked = Settings.Current.ActiveOnStartup };
            contextMenu.Items.Add(selectWindowItem);
            contextMenu.Items.Add(clearWindowItem);
            contextMenu.Items.Add(startupItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            // Populate window list when dropdown opens
            selectWindowItem.DropDownOpening += (s, e) => PopulateWindowList(selectWindowItem);

            // Frame rate
            var fpsItem = new ToolStripMenuItem("Frame Rate", null, OnFrameRateClick);

            var exitItem = new ToolStripMenuItem("Exit", null, OnExitClick);
            contextMenu.Items.Add(fpsItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            contextMenu.Opening += (s, e) =>
            {
                UpdateMenuCheckmarks(toggleItem, mode1Item, mode2Item, mode3Item, mode4Item, mode5Item, mode6Item, mode7Item);
                selectRegionItem.Checked = _useRegion && !_useWindow;
                selectRegionItem.Text = (_useRegion && !_useWindow) ? $"Region: {RegionText}" : "Select Region...";

                string windowText;
                bool windowChecked;
                bool windowEnabled;
                lock (controlLock)
                {
                    windowChecked = _useWindow;
                    windowEnabled = _useWindow;
                    windowText = _useWindow
                        ? $"Windows: {(_targetWindows.Count > 0 ? string.Join(", ", _targetWindows.Values.Take(2)) + (_targetWindows.Count > 2 ? $" +{_targetWindows.Count - 2}" : "") : "none")}"
                        : "Select Window...";
                }

                selectWindowItem.Checked = windowChecked;
                selectWindowItem.Text = windowText;
                clearWindowItem.Enabled = windowEnabled;
                startupItem.Checked = Settings.Current.ActiveOnStartup;
            };

            trayIcon = new NotifyIcon
            {
                Icon = CreateDefaultIcon(),
                Text = "DarkReader",
                Visible = true,
                ContextMenuStrip = contextMenu
            };
            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) Toggle();
            };
        }

        private string RegionText => _region.HasValue
            ? $"{_region.Value.Width}x{_region.Value.Height} @ ({_region.Value.X},{_region.Value.Y})"
            : "None";

        private void UpdateMenuCheckmarks(ToolStripMenuItem toggle, params ToolStripMenuItem[] modes)
        {
            toggle.Checked = effectActive;
            for (int i = 0; i < modes.Length; i++)
            {
                modes[i].Checked = effectActive && currentMode == i;
            }
        }

        private void RegisterHotKeys()
        {
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_TOGGLE, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.N);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_MODE0, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.D0);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_MODE1, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.D1);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_MODE2, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.D2);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_MODE3, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.D3);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_MODE4, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.D4);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_MODE5, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.D5);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_MODE6, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.D6);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_REGION, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.R);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_EXIT, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.H);
        }

        private void UnregisterHotKeys()
        {
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_TOGGLE);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_MODE0);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_MODE1);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_MODE2);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_MODE3);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_MODE4);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_MODE5);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_MODE6);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_REGION);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_EXIT);
        }

        private void InstallForegroundHook()
        {
            _winEventProc = new NativeMethods.WinEventProc(OnWinEvent);
            // Hook LOCATIONCHANGE to detect any window move/resize/Z-order change
            _winEventHook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
                NativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
                IntPtr.Zero,
                _winEventProc,
                0, 0,
                NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);
        }

        private void RemoveForegroundHook()
        {
            if (_winEventHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_winEventHook);
                _winEventHook = IntPtr.Zero;
            }
        }

        private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // Only respond to window objects (not menus, cursors, etc.)
            if (idObject != 0) return; // OBJID_WINDOW = 0

            // Any window moved/resized/reordered - visible region may have changed
            if (_useWindow && hwnd != IntPtr.Zero)
            {
                lock (controlLock)
                {
                    Monitor.Pulse(controlLock);
                }
            }
        }

        private void StartControlLoop()
        {
            currentMatrix = BuiltinMatrices.Identity;
            controlThread = new Thread(ControlLoop);
            controlThread.SetApartmentState(ApartmentState.STA);
            controlThread.Start();
        }

        private void ControlLoop()
        {
            while (!exiting)
            {
                lock (controlLock)
                {
                    if (!effectActive)
                    {
                        Monitor.Wait(controlLock, Settings.Current.UpdateIntervalMs);
                        continue;
                    }
                }

                if (!NativeMethods.MagInitialize())
                {
                    Thread.Sleep(Settings.Current.UpdateIntervalMs);
                    continue;
                }

                bool shouldUninit = false;
                while (!exiting)
                {
                    // Wait with timeout so Pulse can wake us immediately
                    lock (controlLock)
                    {
                        Monitor.Wait(controlLock, Settings.Current.UpdateIntervalMs);
                    }

                    if (exiting) break;

                    lock (controlLock)
                    {
                        if (!effectActive)
                        {
                            shouldUninit = true;
                            break;
                        }
                    }

                    try { ApplyCurrentEffect(); } catch { }
                }

                if (shouldUninit)
                {
                    CleanupEffect();
                    NativeMethods.MagUninitialize();
                }
            }
        }

        private void ApplyCurrentEffect()
        {
            if (_useWindow)
            {
                // Window tracking mode - calculate visible region for all targets
                int windowCount;
                lock (controlLock)
                {
                    windowCount = _targetWindows.Count;
                }

                if (windowCount == 0)
                {
                    EnsureRegionOverlayDestroyed();
                    if (_lastShowDecision)
                    {
                        BuiltinMatrices.ApplyMatrix(BuiltinMatrices.Identity);
                        _lastShowDecision = false;
                    }
                    return;
                }

                // Calculate union of visible regions for all target windows
                IntPtr overlayHwnd = _regionOverlay?.WindowHandle ?? IntPtr.Zero;
                RegionInfo region = CalculateMultiWindowRegion(overlayHwnd);

                if (region.IsEmpty)
                {
                    // Fully covered - set region to empty to hide overlay
                    if (_lastShowDecision)
                    {
                        if (_regionOverlay != null && _regionOverlay.IsCreated)
                        {
                            var emptyRegion = new RegionInfo { IsEmpty = true };
                            _regionOverlay.UpdateRegion(emptyRegion);
                        }
                        BuiltinMatrices.ApplyMatrix(BuiltinMatrices.Identity);
                        _lastShowDecision = false;
                        _currentRegionInfo = region;
                    }
                }
                else
                {
                    // Partially or fully visible - show overlay with region shape
                    // Clear stale fullscreen effect: double-applying inverts the inversion
                    BuiltinMatrices.ApplyMatrix(BuiltinMatrices.Identity);
                    EnsureRegionOverlay(region);
                    _regionOverlay.ApplyColorEffect(currentMatrix);
                    _currentRegionInfo = region;
                    _lastShowDecision = true;
                }
            }
            else if (_useRegion && _region.HasValue)
            {
                // Manual region mode - use rectangle
                // Clear stale fullscreen effect: double-applying inverts the inversion
                BuiltinMatrices.ApplyMatrix(BuiltinMatrices.Identity);
                bool regionChanged = _region.Value != _lastAppliedRegion;
                var regionInfo = new RegionInfo
                {
                    HRgn = IntPtr.Zero,
                    Bounds = _region.Value,
                    IsEmpty = false
                };
                EnsureRegionOverlay(regionInfo);
                _regionOverlay.ApplyColorEffect(currentMatrix);
                if (regionChanged || !_lastShowDecision)
                    _regionOverlay.UpdateRegion(regionInfo);
                _lastAppliedRegion = _region.Value;
                _lastShowDecision = true;
            }
            else
            {
                // Fullscreen mode
                EnsureRegionOverlayDestroyed();
                BuiltinMatrices.ApplyMatrix(currentMatrix);
                _lastShowDecision = true;
            }
        }

        private void CleanupEffect()
        {
            try
            {
                EnsureRegionOverlayDestroyed();
                BuiltinMatrices.ApplyMatrix(BuiltinMatrices.Identity);
                _lastShowDecision = false;
            }
            catch { }
        }

        /// <summary>
        /// Calculate the union of visible regions for all target windows.
        /// </summary>
        private RegionInfo CalculateMultiWindowRegion(IntPtr overlayHwnd)
        {
            IntPtr combinedRgn = IntPtr.Zero;
            Rectangle totalBounds = Rectangle.Empty;
            bool anyVisible = false;

            IntPtr[] hwnds;
            lock (controlLock)
            {
                hwnds = _targetWindows.Keys.ToArray();
            }

            foreach (var hwnd in hwnds)
            {
                if (!NativeMethods.IsWindow(hwnd))
                    continue;

                var region = WindowRegionCalculator.CalculateVisibleRegion(hwnd, overlayHwnd, skipTopmost: true);

                if (region.IsEmpty)
                {
                    WindowRegionCalculator.ReleaseRegion(region.HRgn);
                    continue;
                }

                if (combinedRgn == IntPtr.Zero)
                {
                    // First visible region - create a copy
                    combinedRgn = NativeMethods.CreateRectRgn(0, 0, 0, 0);
                    NativeMethods.CombineRgn(combinedRgn, region.HRgn, IntPtr.Zero, NativeMethods.RGN_COPY);
                    totalBounds = region.Bounds;
                }
                else
                {
                    // Union with existing region
                    NativeMethods.CombineRgn(combinedRgn, combinedRgn, region.HRgn, NativeMethods.RGN_OR);
                    totalBounds = Rectangle.Union(totalBounds, region.Bounds);
                }

                anyVisible = true;
                WindowRegionCalculator.ReleaseRegion(region.HRgn);
            }

            if (!anyVisible)
            {
                return new RegionInfo { IsEmpty = true };
            }

            return new RegionInfo
            {
                HRgn = combinedRgn,
                Bounds = totalBounds,
                IsEmpty = false
            };
        }

        private void EnsureRegionOverlay(RegionInfo region)
        {
            if (_regionOverlay == null)
                _regionOverlay = new RegionOverlay();

            if (!_regionOverlay.IsCreated && !region.IsEmpty)
            {
                _regionOverlay.Show(region);
            }
            else if (_regionOverlay.IsCreated && !region.IsEmpty)
            {
                _regionOverlay.UpdateRegion(region);
            }
        }

        private void EnsureRegionOverlay()
        {
            if (_regionOverlay == null)
                _regionOverlay = new RegionOverlay();

            if (!_regionOverlay.IsCreated && _region.HasValue)
            {
                var regionInfo = new RegionInfo
                {
                    HRgn = IntPtr.Zero,
                    Bounds = _region.Value,
                    IsEmpty = false
                };
                _regionOverlay.Show(regionInfo);
            }
            else if (_regionOverlay.IsCreated && _region.HasValue)
            {
                var regionInfo = new RegionInfo
                {
                    HRgn = IntPtr.Zero,
                    Bounds = _region.Value,
                    IsEmpty = false
                };
                _regionOverlay.UpdateRegion(regionInfo);
            }
        }

        private void EnsureRegionOverlayDestroyed()
        {
            _regionOverlay?.Dispose();
            _regionOverlay = null;
        }

        private float[,] GetMatrixForMode(int mode)
        {
            return mode switch
            {
                0 => BuiltinMatrices.SimpleInversion,
                1 => BuiltinMatrices.SmartInversion1,
                2 => BuiltinMatrices.SmartInversion2,
                3 => BuiltinMatrices.SmartInversion3,
                4 => BuiltinMatrices.SmartInversion4,
                5 => BuiltinMatrices.SmartInversion5,
                6 => BuiltinMatrices.Grayscale,
                _ => BuiltinMatrices.Identity
            };
        }

        public void Toggle()
        {
            lock (controlLock)
            {
                effectActive = !effectActive;
                if (effectActive)
                    Monitor.Pulse(controlLock);
            }
            UpdateTrayTip();
        }

        public void SetMode(int mode)
        {
            lock (controlLock)
            {
                currentMode = mode;
                currentMatrix = GetMatrixForMode(mode);
                if (!effectActive)
                {
                    effectActive = true;
                    Monitor.Pulse(controlLock);
                }
            }
            // Invalidate overlay matrix so it re-applies
            _regionOverlay?.InvalidateMatrix();
            Settings.Current.ActiveMode = mode;
            Settings.Save();
            UpdateTrayTip();
        }

        private void OnSelectRegionClick(object sender, EventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                // Stop window tracking if active
                StopWindowTracking();

                using var selector = new RegionSelectorForm();
                if (selector.ShowDialog() == DialogResult.OK && !selector.Cancelled)
                {
                    _region = selector.SelectedRegion;
                    _useRegion = true;
                    _useWindow = false;

                    Settings.Current.UseRegion = true;
                    Settings.Current.UseWindow = false;
                    Settings.Current.RegionX = _region.Value.X;
                    Settings.Current.RegionY = _region.Value.Y;
                    Settings.Current.RegionWidth = _region.Value.Width;
                    Settings.Current.RegionHeight = _region.Value.Height;
                    Settings.Save();

                    lock (controlLock) { Monitor.Pulse(controlLock); }
                    UpdateTrayTip();
                }
            }));
        }

        private void OnClearRegionClick(object sender, EventArgs e)
        {
            StopWindowTracking();
            _useRegion = false;
            _useWindow = false;
            _region = null;
            Settings.Current.UseRegion = false;
            Settings.Current.UseWindow = false;
            Settings.Save();

            lock (controlLock) { Monitor.Pulse(controlLock); }
            UpdateTrayTip();
        }

        private void PopulateWindowList(ToolStripMenuItem selectWindowItem)
        {
            try
            {
                selectWindowItem.DropDownItems.Clear();

                // Self-heal: move any target window whose handle is no longer valid into
                // the closed list before deciding what counts as "reopened" below. This
                // avoids depending on the timing of the tracker's async close
                // notification (which is delivered via BeginInvoke and could otherwise
                // race with the menu being opened right around the time a window closes).
                lock (controlLock)
                {
                    foreach (var deadHwnd in _targetWindows.Keys.Where(h => !NativeMethods.IsWindow(h)).ToList())
                    {
                        string deadTitle = _targetWindows[deadHwnd];
                        _targetWindows.Remove(deadHwnd);
                        _closedWindowTitles.Add(deadTitle);
                    }
                }

                var windows = new List<(IntPtr hwnd, string title)>();

                NativeMethods.EnumWindows((hWnd, lParam) =>
                {
                    if (!NativeMethods.IsWindowVisible(hWnd))
                        return true;

                    var sb = new StringBuilder(512);
                    NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                    string title = sb.ToString();

                    if (string.IsNullOrWhiteSpace(title))
                        return true;

                    // Skip our own window
                    if (hWnd == this.Handle)
                        return true;

                    // Skip tiny/zero-size windows
                    if (!NativeMethods.GetWindowRect(hWnd, out RECT rect))
                        return true;
                    int w = rect.right - rect.left;
                    int h = rect.bottom - rect.top;
                    if (w < 10 || h < 10)
                        return true;

                    windows.Add((hWnd, title));
                    return true;
                }, IntPtr.Zero);

                // Build set of live window titles for closed-window detection
                var liveWindowTitles = new HashSet<string>(windows.Select(w => w.title));

                // Get tracked closed titles that are not in the live window list
                List<string> closedTitles;
                lock (controlLock)
                {
                    closedTitles = _closedWindowTitles
                        .Where(title => !liveWindowTitles.Contains(title))
                        .ToList();
                }

                if (windows.Count == 0 && closedTitles.Count == 0)
                {
                    selectWindowItem.DropDownItems.Add("(no windows found)").Enabled = false;
                    return;
                }

                // Add live windows
                bool anyReconnected = false;
                foreach (var (hwnd, title) in windows)
                {
                    bool isTracked;
                    lock (controlLock)
                    {
                        isTracked = _targetWindows.ContainsKey(hwnd);
                    }

                    // Match by title for reopened windows (hwnd changes on recreation)
                    bool reconnected = !isTracked && TryReconnectClosedWindow(hwnd, title);
                    if (reconnected)
                        anyReconnected = true;

                    string displayTitle = title.Length > 60 ? title.Substring(0, 57) + "..." : title;

                    var item = new ToolStripMenuItem(displayTitle);
                    item.ToolTipText = title;
                    item.Checked = isTracked || reconnected;
                    item.CheckOnClick = true;

                    item.CheckedChanged += (s, e) => OnWindowItemCheckedChanged(hwnd, title, item);

                    selectWindowItem.DropDownItems.Add(item);
                }

                if (anyReconnected)
                {
                    SaveWindowSettings();
                    lock (controlLock) { Monitor.Pulse(controlLock); }
                    UpdateTrayTip();
                }

                // Add closed windows as synthetic entries
                foreach (var title in closedTitles)
                {
                    string displayTitle = title.Length > 60 ? title.Substring(0, 57) + "..." : title;
                    displayTitle += " (Closed)";

                    var item = new ToolStripMenuItem(displayTitle);
                    item.ToolTipText = title;
                    item.Checked = true;
                    item.CheckOnClick = true;
                    item.ForeColor = Color.Gray;

                    // For closed windows, route through removal flow
                    item.CheckedChanged += (s, e) =>
                    {
                        try
                        {
                            if (!item.Checked)
                            {
                                // Unchecking a closed window - remove it from closed titles
                                lock (controlLock)
                                {
                                    _closedWindowTitles.Remove(title);
                                }

                                SaveWindowSettings();

                                // Remove from dropdown
                                if (item.Owner is ToolStripDropDownMenu parent)
                                {
                                    parent.Items.Remove(item);
                                }

                                UpdateTrayTip();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"DarkReader: failed to remove closed window entry: {ex}");
                        }
                    };

                    selectWindowItem.DropDownItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                // Never let a failure here take down the whole application - worst
                // case the window list is temporarily empty/stale.
                Debug.WriteLine($"DarkReader: failed to populate window list: {ex}");
            }
        }

        private void OnWindowItemCheckedChanged(IntPtr hwnd, string title, ToolStripMenuItem item)
        {
            try
            {
                if (item.Checked)
                {
                    // Window checked - check if this title was in closed list and remove it
                    lock (controlLock)
                    {
                        // Remove from closed titles if present (rebinding to new HWND)
                        _closedWindowTitles.Remove(title);

                        // Add to tracking
                        _targetWindows[hwnd] = title;
                    }

                    IEnumerable<IntPtr> hwndsToTrack;
                    lock (controlLock)
                    {
                        hwndsToTrack = _targetWindows.Keys.ToArray();
                    }

                    // Start tracker if not running
                    if (_windowTracker == null || !_windowTracker.IsTracking)
                    {
                        StartTracker(hwndsToTrack);
                    }
                    else
                    {
                        _windowTracker.AddWindow(hwnd);
                    }
                }
                else
                {
                    // Window unchecked - remove from tracking
                    int remainingCount;
                    lock (controlLock)
                    {
                        _targetWindows.Remove(hwnd);
                        remainingCount = _targetWindows.Count;
                    }
                    _windowTracker?.RemoveWindow(hwnd);

                    // If no windows remain, stop tracker but keep _useWindow true
                    if (remainingCount == 0)
                    {
                        _windowTracker?.Dispose();
                        _windowTracker = null;
                    }
                }

                // Save settings
                SaveWindowSettings();

                lock (controlLock) { Monitor.Pulse(controlLock); }
                UpdateTrayTip();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DarkReader: failed to handle window selection change: {ex}");
            }
        }

        private void StartTracker(IEnumerable<IntPtr> hwnds)
        {
            _windowTracker?.Dispose();
            _windowTracker = new WindowTracker(Settings.Current.UpdateIntervalMs);
            _windowTracker.StartTracking(hwnds, OnWindowChanged, OnTargetWindowClosed);
            _useWindow = true;
            _useRegion = false;
        }

        private void SaveWindowSettings()
        {
            lock (controlLock)
            {
                Settings.Current.UseWindow = _useWindow;
                Settings.Current.UseRegion = false;
                Settings.Current.TargetWindowTitles = new List<string>(_targetWindows.Values);
                Settings.Current.ClosedWindowTitles = new List<string>(_closedWindowTitles);
            }
            Settings.Save();
        }

        private void OnClearWindowClick(object sender, EventArgs e)
        {
            StopWindowTracking();
            UpdateTrayTip();
        }

        private void OnToggleStartup(object sender, EventArgs e)
        {
            Settings.Current.ActiveOnStartup = !Settings.Current.ActiveOnStartup;
            Settings.Save();
            if (sender is ToolStripMenuItem item)
                item.Checked = Settings.Current.ActiveOnStartup;
        }

        private void SetFrameRate(int intervalMs)
        {
            Settings.Current.UpdateIntervalMs = intervalMs;
            Settings.Save();
            _windowTracker?.UpdateInterval(intervalMs);
            lock (controlLock)
            {
                Monitor.Pulse(controlLock);
            }
        }

        private void OnFrameRateClick(object sender, EventArgs e)
        {
            using var form = new Form
            {
                Text = "Frame Rate",
                Size = new Size(240, 120),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lbl = new Label
            {
                Text = "FPS (5-60):",
                Location = new Point(15, 18),
                AutoSize = true
            };

            var nud = new NumericUpDown
            {
                Location = new Point(100, 15),
                Size = new Size(100, 23),
                Minimum = 5,
                Maximum = 60,
                Value = Math.Clamp(1000 / Settings.Current.UpdateIntervalMs, 5, 60)
            };

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(80, 55),
                Size = new Size(60, 26)
            };

            form.Controls.AddRange(new Control[] { lbl, nud, btnOk });
            form.AcceptButton = btnOk;

            if (form.ShowDialog() == DialogResult.OK)
            {
                int fps = (int)nud.Value;
                int intervalMs = Math.Max(1, 1000 / fps);
                SetFrameRate(intervalMs);
            }
        }

        private void RestoreMultiWindowTracking()
        {
            lock (controlLock)
            {
                _targetWindows.Clear();
                _closedWindowTitles.Clear();

                foreach (var title in Settings.Current.TargetWindowTitles)
                {
                    var hwnd = WindowTracker.FindWindowByTitle(title);
                    if (hwnd != IntPtr.Zero)
                    {
                        // Found a live window with this title
                        _targetWindows[hwnd] = title;
                    }
                    else
                    {
                        // Window not currently open - preserve in closed titles
                        _closedWindowTitles.Add(title);
                    }
                }

                foreach (var title in Settings.Current.ClosedWindowTitles)
                {
                    // Don't duplicate titles already in TargetWindowTitles
                    if (Settings.Current.TargetWindowTitles.Contains(title))
                        continue;

                    var hwnd = WindowTracker.FindWindowByTitle(title);
                    if (hwnd == IntPtr.Zero)
                    {
                        // Still closed - preserve it
                        _closedWindowTitles.Add(title);
                    }
                    // If found live, it wasn't in TargetWindowTitles, so don't track it
                }

                // Enable window tracking mode even if no windows are currently found
                _useWindow = true;
                _useRegion = false;
            }

            IEnumerable<IntPtr> hwndsToTrack;
            lock (controlLock)
            {
                hwndsToTrack = _targetWindows.Keys.ToArray();
            }

            // Start tracker if we have any live windows
            if (hwndsToTrack.Any())
            {
                _windowTracker = new WindowTracker(Settings.Current.UpdateIntervalMs);
                _windowTracker.StartTracking(hwndsToTrack, OnWindowChanged, OnTargetWindowClosed);
            }

            Settings.Current.UseWindow = true;
            Settings.Current.UseRegion = false;
            Settings.Save();

            lock (controlLock) { Monitor.Pulse(controlLock); }
            UpdateTrayTip();
        }

        private void StopWindowTracking()
        {
            _windowTracker?.Dispose();
            _windowTracker = null;
            lock (controlLock)
            {
                _targetWindows.Clear();
                _closedWindowTitles.Clear();
                _useWindow = false;
            }
        }

        private void OnWindowChanged()
        {
            // Called from tracker thread when window position/size changes
            // Wake control loop to recalculate visible region
            lock (controlLock)
            {
                Monitor.Pulse(controlLock);
            }
        }

        private void OnTargetWindowClosed(IntPtr hwnd)
        {
            // A target window was closed - gray it out in the list
            // Check if handle is created and form is not disposing
            if (!IsHandleCreated || IsDisposed || Disposing)
                return;

            try
            {
                this.BeginInvoke(new Action(() =>
                {
                    string title;
                    int remainingCount;

                    lock (controlLock)
                    {
                        title = _targetWindows.ContainsKey(hwnd) ? _targetWindows[hwnd] : $"Window (0x{hwnd.ToInt64():X})";

                        // Move from active to closed
                        _targetWindows.Remove(hwnd);
                        _closedWindowTitles.Add(title);
                        remainingCount = _targetWindows.Count;
                    }

                    // Save settings
                    SaveWindowSettings();

                    // If no active windows remain, keep tracking mode but with empty overlay
                    if (remainingCount == 0)
                    {
                        _region = null;
                        _currentRegionInfo = default;
                        _lastShowDecision = false;
                    }

                    lock (controlLock) { Monitor.Pulse(controlLock); }
                    UpdateTrayTip();
                }));
            }
            catch (InvalidOperationException)
            {
                // Handle destroyed between check and invocation - ignore
            }
        }

        /// <summary>
        /// Attempts to bind a live window handle to a previously tracked title that is
        /// currently recorded as closed. Must be called on the UI thread.
        /// Returns true if the window was reconnected and tracking was (re)started for it.
        /// </summary>
        private bool TryReconnectClosedWindow(IntPtr hwnd, string title)
        {
            lock (controlLock)
            {
                // Already tracked under this handle, or no longer recorded as closed
                // (e.g. a concurrent rescan already reconnected it) - nothing to do.
                if (_targetWindows.ContainsKey(hwnd)) return false;
                if (!_closedWindowTitles.Remove(title)) return false;

                _targetWindows[hwnd] = title;
            }

            if (_windowTracker == null || !_windowTracker.IsTracking)
            {
                IEnumerable<IntPtr> hwndsToTrack;
                lock (controlLock) { hwndsToTrack = _targetWindows.Keys.ToArray(); }
                StartTracker(hwndsToTrack);
            }
            else
            {
                _windowTracker.AddWindow(hwnd);
            }

            return true;
        }

        /// <summary>
        /// Periodically checks whether any closed target window has reopened (matched by
        /// title) and automatically resumes tracking it, so dark mode re-applies without
        /// requiring the user to open the "Select Window" menu.
        /// </summary>
        private void RescanClosedWindows(object state)
        {
            try
            {
                if (!IsHandleCreated || IsDisposed || Disposing)
                    return;

                List<string> closedSnapshot;
                lock (controlLock)
                {
                    if (!_useWindow || _closedWindowTitles.Count == 0)
                        return;
                    closedSnapshot = new List<string>(_closedWindowTitles);
                }

                var matches = new List<(IntPtr hwnd, string title)>();
                NativeMethods.EnumWindows((hWnd, lParam) =>
                {
                    if (!NativeMethods.IsWindowVisible(hWnd))
                        return true;

                    // Skip our own window
                    if (hWnd == this.Handle)
                        return true;

                    var sb = new StringBuilder(512);
                    NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                    string title = sb.ToString();

                    if (!string.IsNullOrWhiteSpace(title) && closedSnapshot.Contains(title))
                        matches.Add((hWnd, title));

                    return true;
                }, IntPtr.Zero);

                if (matches.Count == 0)
                    return;

                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        bool anyReconnected = false;
                        foreach (var (hwnd, title) in matches)
                        {
                            if (TryReconnectClosedWindow(hwnd, title))
                                anyReconnected = true;
                        }

                        if (!anyReconnected)
                            return;

                        SaveWindowSettings();
                        lock (controlLock) { Monitor.Pulse(controlLock); }
                        UpdateTrayTip();
                    }
                    catch (Exception ex)
                    {
                        // Form may have been disposed/closed between the check above and
                        // this callback running - never let a background timer callback
                        // take down the whole application.
                        Debug.WriteLine($"DarkReader: closed-window reconnect failed: {ex}");
                    }
                }));
            }
            catch (Exception ex)
            {
                // Same rationale as above: this runs on a thread-pool timer thread, so an
                // unhandled exception here would crash the whole process.
                Debug.WriteLine($"DarkReader: closed-window rescan failed: {ex}");
            }
        }

        private void UpdateTrayTip()
        {
            if (trayIcon != null)
            {
                string mode = effectActive ? $"Mode {currentMode}" : "Off";
                string target;

                lock (controlLock)
                {
                    if (_useWindow && _targetWindows.Count > 0)
                    {
                        string titles = string.Join(", ", _targetWindows.Values.Take(2));
                        if (_targetWindows.Count > 2) titles += $" +{_targetWindows.Count - 2}";
                        target = $" [Windows: {titles}]";
                    }
                    else if (_useWindow)
                        target = " [Windows: none]";
                    else if (_useRegion)
                        target = $" [{RegionText}]";
                    else
                        target = " [Fullscreen]";
                }

                trayIcon.Text = $"DarkReader - {mode}{target}";
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)WindowMessage.WM_HOTKEY)
            {
                int id = (int)m.WParam;
                switch (id)
                {
                    case HOTKEY_TOGGLE: Toggle(); return;
                    case HOTKEY_MODE0: SetMode(0); return;
                    case HOTKEY_MODE1: SetMode(1); return;
                    case HOTKEY_MODE2: SetMode(2); return;
                    case HOTKEY_MODE3: SetMode(3); return;
                    case HOTKEY_MODE4: SetMode(4); return;
                     case HOTKEY_MODE5: SetMode(5); return;
                     case HOTKEY_MODE6: SetMode(6); return;
                     case HOTKEY_REGION: OnSelectRegionClick(null, null); return;
                     case HOTKEY_EXIT: ExitApp(); return;
                }
            }
            else if (m.Msg == (int)WindowMessage.WM_APP + 1)
            {
                Toggle();
                return;
            }
            base.WndProc(ref m);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Hide();

            // Restore region settings
            if (Settings.Current.UseRegion && Settings.Current.RegionWidth > 0 && Settings.Current.RegionHeight > 0)
            {
                _region = new Rectangle(
                    Settings.Current.RegionX, Settings.Current.RegionY,
                    Settings.Current.RegionWidth, Settings.Current.RegionHeight);
                _useRegion = true;
            }

            // Restore window tracking for multiple windows
            if (Settings.Current.UseWindow && Settings.Current.TargetWindowTitles.Count > 0)
            {
                RestoreMultiWindowTracking();
            }

            // Restore saved mode (only activate if Active On Startup is enabled)
            if (Settings.Current.ActiveOnStartup && Settings.Current.ActiveMode >= 0)
            {
                SetMode(Settings.Current.ActiveMode);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ExitApp();
        }

        private void OnToggleClick(object sender, EventArgs e) => Toggle();
        private void OnExitClick(object sender, EventArgs e) => ExitApp();

        private void ExitApp()
        {
            exiting = true;
            lock (controlLock)
            {
                effectActive = false;
                Monitor.Pulse(controlLock);
            }
            RemoveForegroundHook();
            UnregisterHotKeys();
            _closedWindowRescanTimer?.Dispose();
            StopWindowTracking();
            EnsureRegionOverlayDestroyed();
            trayIcon?.Dispose();
            Application.Exit();
        }

        private Icon CreateDefaultIcon()
        {
            using var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(45, 45, 48));
                using var pen = new Pen(Color.FromArgb(0, 122, 204), 2);
                g.DrawRectangle(pen, 2, 2, 12, 12);
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _closedWindowRescanTimer?.Dispose();
                _windowTracker?.Dispose();
                _regionOverlay?.Dispose();
                trayIcon?.Dispose();
                contextMenu?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
