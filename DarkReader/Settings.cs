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
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DarkReader
{
    public class Settings
    {
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DarkReader");
        private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

        public static Settings Current { get; private set; } = new Settings();

        public int ActiveMode { get; set; } = -1;
        public bool ActiveOnStartup { get; set; } = false;
        public bool SmoothTransitions { get; set; } = true;
        private int _updateIntervalMs = 100;
        public int UpdateIntervalMs
        {
            get => _updateIntervalMs;
            set => _updateIntervalMs = Math.Clamp(value, 16, 200); // 5-60 fps
        }

        // Region restriction settings
        public bool UseRegion { get; set; } = false;
        public int RegionX { get; set; } = 0;
        public int RegionY { get; set; } = 0;
        public int RegionWidth { get; set; } = 0;
        public int RegionHeight { get; set; } = 0;

        // Window targeting settings
        public bool UseWindow { get; set; } = false;
        public List<string> TargetWindowTitles { get; set; } = new List<string>();
        public List<string> ClosedWindowTitles { get; set; } = new List<string>();
        public bool PauseWhenNotInForeground { get; set; } = true;

        // Legacy single-window setting (for migration)
        public string TargetWindowTitle { get; set; } = null;

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    Current = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();

                    // Replace null lists with empty lists
                    if (Current.TargetWindowTitles == null)
                        Current.TargetWindowTitles = new List<string>();
                    if (Current.ClosedWindowTitles == null)
                        Current.ClosedWindowTitles = new List<string>();

                    // Migrate legacy TargetWindowTitle to TargetWindowTitles
                    if (!string.IsNullOrEmpty(Current.TargetWindowTitle) &&
                        !Current.TargetWindowTitles.Contains(Current.TargetWindowTitle))
                    {
                        Current.TargetWindowTitles.Add(Current.TargetWindowTitle);
                        Current.TargetWindowTitle = null; // Clear after migration
                        Save();
                    }
                }
                else
                {
                    Current = new Settings();
                    Save();
                }
            }
            catch
            {
                Current = new Settings();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Best-effort save
            }
        }
    }
}
