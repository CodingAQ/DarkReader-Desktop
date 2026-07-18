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
using System.IO;
using System.Windows.Forms;

namespace DarkReader
{
    internal static class Program
    {
        private const string MutexName = "DarkReader_SingleInstance_Mutex";

        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                MessageBox.Show(e.ExceptionObject.ToString(), "DarkReader Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            // Single-instance enforcement
            using var mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                ActivateExistingInstance();
                return;
            }

            // Check Windows version (Windows 7+ = 6.1+)
            if (Environment.OSVersion.Version < new Version(6, 1))
            {
                MessageBox.Show("DarkReader requires Windows 7 or later.", "DarkReader", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // DPI awareness
            NativeMethods.SetProcessDPIAware();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Load settings
            Settings.Load();

            // Check DWM composition
            if (!NativeMethods.DwmIsCompositionEnabled())
            {
                var result = MessageBox.Show(
                    "Windows Aero/DWM composition is not enabled. DarkReader may not work correctly.",
                    "DarkReader", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (result != DialogResult.OK) return;
            }

            // Create the main form (hidden, hosts message loop for hotkeys)
            using var mainForm = new MainForm();
            Application.Run(mainForm);
        }

        private static void ActivateExistingInstance()
        {
            // Find the existing process and post to its main window thread
            var existing = Process.GetProcessesByName("DarkReader");
            foreach (var proc in existing)
            {
                if (proc.Id != Process.GetCurrentProcess().Id && proc.MainWindowHandle != IntPtr.Zero)
                {
                    // Post to all threads as we don't know which is the main thread
                    foreach (ProcessThread thread in proc.Threads)
                    {
                        NativeMethods.PostThreadMessage((uint)thread.Id, (uint)WindowMessage.WM_APP + 1, IntPtr.Zero, IntPtr.Zero);
                    }
                    break;
                }
            }
        }
    }
}
