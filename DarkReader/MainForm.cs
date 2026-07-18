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

        // Window targeting
        private WindowTracker _windowTracker;
        private IntPtr? _targetWindow;
        private string _targetWindowTitle;
        private bool _useWindow = false;
        private bool _pauseWhenNotInForeground = true;

        // Hotkey IDs
        private const int HOTKEY_TOGGLE = 1;
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
        }

        private void InitializeTrayIcon()
        {
            contextMenu = new ContextMenuStrip();

            var toggleItem = new ToolStripMenuItem("Toggle", null, OnToggleClick);
            contextMenu.Items.Add(toggleItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            var mode1Item = new ToolStripMenuItem("Default", null, (s, e) => SetMode(1));
            var mode2Item = new ToolStripMenuItem("Preset 1", null, (s, e) => SetMode(2));
            var mode3Item = new ToolStripMenuItem("Preset 2", null, (s, e) => SetMode(3));
            var mode4Item = new ToolStripMenuItem("Preset 3", null, (s, e) => SetMode(4));
            var mode5Item = new ToolStripMenuItem("Preset 4", null, (s, e) => SetMode(5));
            var mode6Item = new ToolStripMenuItem("Preset 5", null, (s, e) => SetMode(6));
            var mode7Item = new ToolStripMenuItem("Grayscale", null, (s, e) => SetMode(7));

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
            var pauseCheckItem = new ToolStripMenuItem("Pause When Not Foreground", null, OnTogglePauseCheck) { Checked = true };
            var startupItem = new ToolStripMenuItem("Active On Startup", null, OnToggleStartup) { Checked = Settings.Current.ActiveOnStartup };
            contextMenu.Items.Add(selectWindowItem);
            contextMenu.Items.Add(clearWindowItem);
            contextMenu.Items.Add(pauseCheckItem);
            contextMenu.Items.Add(startupItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            // Populate window list when dropdown opens
            selectWindowItem.DropDownOpening += (s, e) => PopulateWindowList(selectWindowItem);

            var exitItem = new ToolStripMenuItem("Exit", null, OnExitClick);
            contextMenu.Items.Add(exitItem);

            contextMenu.Opening += (s, e) =>
            {
                UpdateMenuCheckmarks(toggleItem, mode1Item, mode2Item, mode3Item, mode4Item, mode5Item, mode6Item, mode7Item);
                selectRegionItem.Checked = _useRegion && !_useWindow;
                selectRegionItem.Text = (_useRegion && !_useWindow) ? $"Region: {RegionText}" : "Select Region...";
                selectWindowItem.Checked = _useWindow;
                selectWindowItem.Text = _useWindow ? $"Window: {_targetWindowTitle}" : "Select Window...";
                clearWindowItem.Enabled = _useWindow;
                pauseCheckItem.Checked = _pauseWhenNotInForeground;
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
                modes[i].Checked = effectActive && currentMode == i + 1;
            }
        }

        private void RegisterHotKeys()
        {
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_TOGGLE, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.N);
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
            _winEventHook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
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
            // Foreground window changed - wake control loop to re-evaluate
            if (_useWindow && _pauseWhenNotInForeground)
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
                        Monitor.Wait(controlLock, 100);
                        continue;
                    }
                }

                if (!NativeMethods.MagInitialize())
                {
                    Thread.Sleep(100);
                    continue;
                }

                bool shouldUninit = false;
                while (!exiting)
                {
                    // Wait with timeout so Pulse can wake us immediately
                    lock (controlLock)
                    {
                        Monitor.Wait(controlLock, 100);
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
            // Determine if effect should be visible
            bool shouldShow = true;
            if (_useWindow && _pauseWhenNotInForeground && _targetWindow.HasValue)
            {
                var fg = NativeMethods.GetForegroundWindow();
                if (fg != _targetWindow.Value)
                    shouldShow = false;
            }

            if (!shouldShow)
            {
                // Effect should be hidden - clear if currently shown
                if (_lastShowDecision)
                {
                    EnsureRegionOverlayDestroyed();
                    BuiltinMatrices.ApplyMatrix(BuiltinMatrices.Identity);
                    _lastShowDecision = false;
                }
                return;
            }

            // Effect should be visible - always re-apply (DWM may have cleared it)
            if ((_useRegion && _region.HasValue) || _useWindow)
            {
                // Region/window mode
                bool regionChanged = _region.Value != _lastAppliedRegion;
                EnsureRegionOverlay();
                _regionOverlay.ApplyColorEffect(currentMatrix);
                if (regionChanged || !_lastShowDecision)
                    _regionOverlay.UpdateRegion(_region.Value);
                _lastAppliedRegion = _region.Value;
            }
            else
            {
                // Fullscreen mode
                EnsureRegionOverlayDestroyed();
                BuiltinMatrices.ApplyMatrix(currentMatrix);
            }
            _lastShowDecision = true;
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

        private void EnsureRegionOverlay()
        {
            if (_regionOverlay == null)
                _regionOverlay = new RegionOverlay();

            if (!_regionOverlay.IsCreated && _region.HasValue)
            {
                _regionOverlay.Show(_region.Value);
            }
            else if (_regionOverlay.IsCreated && _region.HasValue)
            {
                _regionOverlay.UpdateRegion(_region.Value);
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
            selectWindowItem.DropDownItems.Clear();

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

            if (windows.Count == 0)
            {
                selectWindowItem.DropDownItems.Add("(no windows found)").Enabled = false;
                return;
            }

            foreach (var (hwnd, title) in windows)
            {
                string displayTitle = title.Length > 60 ? title.Substring(0, 57) + "..." : title;
                var item = new ToolStripMenuItem(displayTitle, null, (s, e) => StartWindowTracking(hwnd));
                item.ToolTipText = title;

                // Highlight currently selected window
                if (_useWindow && _targetWindow == hwnd)
                    item.Checked = true;

                selectWindowItem.DropDownItems.Add(item);
            }
        }

        private void OnClearWindowClick(object sender, EventArgs e)
        {
            StopWindowTracking();
            UpdateTrayTip();
        }

        private void OnTogglePauseCheck(object sender, EventArgs e)
        {
            _pauseWhenNotInForeground = !_pauseWhenNotInForeground;
            Settings.Current.PauseWhenNotInForeground = _pauseWhenNotInForeground;
            Settings.Save();
        }

        private void OnToggleStartup(object sender, EventArgs e)
        {
            Settings.Current.ActiveOnStartup = !Settings.Current.ActiveOnStartup;
            Settings.Save();
            if (sender is ToolStripMenuItem item)
                item.Checked = Settings.Current.ActiveOnStartup;
        }

        private void StartWindowTracking(IntPtr hwnd)
        {
            // Stop any existing tracking
            StopWindowTracking();

            _targetWindow = hwnd;
            _useWindow = true;
            _useRegion = false;

            // Get window title
            var sb = new StringBuilder(512);
            NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
            _targetWindowTitle = sb.ToString();
            if (string.IsNullOrEmpty(_targetWindowTitle))
                _targetWindowTitle = $"Window (0x{hwnd.ToInt64():X})";

            // Start tracker
            _windowTracker = new WindowTracker();
            _windowTracker.StartTracking(hwnd, OnWindowRectChanged, OnTargetWindowClosed);

            // Save settings
            Settings.Current.UseWindow = true;
            Settings.Current.UseRegion = false;
            Settings.Current.TargetWindowTitle = _targetWindowTitle;
            Settings.Save();

            lock (controlLock) { Monitor.Pulse(controlLock); }
            UpdateTrayTip();
        }

        private void StopWindowTracking()
        {
            _windowTracker?.Dispose();
            _windowTracker = null;
            _targetWindow = null;
            _targetWindowTitle = null;
            _useWindow = false;
        }

        private void OnWindowRectChanged(Rectangle newRect)
        {
            // Called from tracker thread - update region
            _region = newRect;

            // Update overlay on control thread
            lock (controlLock)
            {
                Monitor.Pulse(controlLock);
            }
        }

        private void OnTargetWindowClosed()
        {
            // Target window was closed - stop tracking
            this.BeginInvoke(new Action(() =>
            {
                StopWindowTracking();
                _region = null;
                _lastShowDecision = false;
                Settings.Current.UseWindow = false;
                Settings.Save();
                UpdateTrayTip();
            }));
        }

        private void UpdateTrayTip()
        {
            if (trayIcon != null)
            {
                string mode = effectActive ? $"Mode {currentMode}" : "Off";
                string target;
                if (_useWindow)
                    target = $" [Window: {_targetWindowTitle}]";
                else if (_useRegion)
                    target = $" [{RegionText}]";
                else
                    target = " [Fullscreen]";
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
                    case HOTKEY_MODE1: SetMode(1); return;
                    case HOTKEY_MODE2: SetMode(2); return;
                    case HOTKEY_MODE3: SetMode(3); return;
                    case HOTKEY_MODE4: SetMode(4); return;
                     case HOTKEY_MODE5: SetMode(5); return;
                     case HOTKEY_MODE6: SetMode(7); return;
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

            // Restore pause setting
            _pauseWhenNotInForeground = Settings.Current.PauseWhenNotInForeground;

            // Restore window tracking
            if (Settings.Current.UseWindow && !string.IsNullOrEmpty(Settings.Current.TargetWindowTitle))
            {
                var hwnd = WindowTracker.FindWindowByTitle(Settings.Current.TargetWindowTitle);
                if (hwnd != IntPtr.Zero)
                {
                    StartWindowTracking(hwnd);
                }
            }

            // Restore saved mode (activates effect)
            if (Settings.Current.ActiveMode > 0)
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
                _windowTracker?.Dispose();
                _regionOverlay?.Dispose();
                trayIcon?.Dispose();
                contextMenu?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
