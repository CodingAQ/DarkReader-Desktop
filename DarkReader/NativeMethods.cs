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
using System.Windows.Forms;

namespace DarkReader
{
    internal static class NativeMethods
    {
        #region user32.dll

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostThreadMessage(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, KeyModifiers fsModifiers, Keys vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern IntPtr CreateWindowEx(
            int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, [MarshalAs(UnmanagedType.Bool)] bool bRedraw);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        public delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        public const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        public const uint GW_HWNDPREV = 3;
        public const uint GW_HWNDNEXT = 2;
        public const uint GW_OWNER = 4;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        #endregion

        #region gdi32.dll

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRectRgnIndirect(ref RECT lprc);

        [DllImport("gdi32.dll")]
        public static extern int CombineRgn(IntPtr hrgnDst, IntPtr hrgnSrc1, IntPtr hrgnSrc2, int fnCombineMode);

        public const int RGN_AND = 1;
        public const int RGN_OR = 2;
        public const int RGN_XOR = 3;
        public const int RGN_DIFF = 4;
        public const int RGN_COPY = 5;

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern int GetRgnBox(IntPtr hrgn, out RECT lprc);

        [DllImport("gdi32.dll")]
        public static extern int OffsetRgn(IntPtr hrgn, int nXOffset, int nYOffset);

        #endregion

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        #region Magnification.dll

        public const string WC_MAGNIFIER = "Magnifier";

        [DllImport("Magnification.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagInitialize();

        [DllImport("Magnification.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagUninitialize();

        [DllImport("Magnification.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagSetFullscreenColorEffect(ref ColorEffect pEffect);

        [DllImport("Magnification.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagGetFullscreenColorEffect(ref ColorEffect pEffect);

        [DllImport("Magnification.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagGetFullscreenTransform(ref float pMagLevel, ref int pxOffset, ref int pyOffset);

        [DllImport("Magnification.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagSetFullscreenTransform(float magLevel, int xOffset, int yOffset);

        [DllImport("Magnification.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagSetColorEffect(IntPtr hwnd, ref ColorEffect pEffect);

        [DllImport("Magnification.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagGetColorEffect(IntPtr hwnd, ref ColorEffect pEffect);

        [DllImport("Magnification.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagSetWindowSource(IntPtr hwnd, RECT rect);

        [DllImport("Magnification.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagSetWindowTransform(IntPtr hwnd, ref Transformation pTransform);

        #endregion

        #region dwmapi.dll

        [DllImport("dwmapi.dll", PreserveSig = false, SetLastError = true)]
        public static extern bool DwmIsCompositionEnabled();

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        #endregion
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ColorEffect
    {
        public float transform00;
        public float transform01;
        public float transform02;
        public float transform03;
        public float transform04;
        public float transform10;
        public float transform11;
        public float transform12;
        public float transform13;
        public float transform14;
        public float transform20;
        public float transform21;
        public float transform22;
        public float transform23;
        public float transform24;
        public float transform30;
        public float transform31;
        public float transform32;
        public float transform33;
        public float transform34;
        public float transform40;
        public float transform41;
        public float transform42;
        public float transform43;
        public float transform44;

        public ColorEffect(float[,] matrix)
        {
            transform00 = matrix[0, 0];
            transform10 = matrix[1, 0];
            transform20 = matrix[2, 0];
            transform30 = matrix[3, 0];
            transform40 = matrix[4, 0];
            transform01 = matrix[0, 1];
            transform11 = matrix[1, 1];
            transform21 = matrix[2, 1];
            transform31 = matrix[3, 1];
            transform41 = matrix[4, 1];
            transform02 = matrix[0, 2];
            transform12 = matrix[1, 2];
            transform22 = matrix[2, 2];
            transform32 = matrix[3, 2];
            transform42 = matrix[4, 2];
            transform03 = matrix[0, 3];
            transform13 = matrix[1, 3];
            transform23 = matrix[2, 3];
            transform33 = matrix[3, 3];
            transform43 = matrix[4, 3];
            transform04 = matrix[0, 4];
            transform14 = matrix[1, 4];
            transform24 = matrix[2, 4];
            transform34 = matrix[3, 4];
            transform44 = matrix[4, 4];
        }

        public void SetMatrix(float[,] matrix)
        {
            transform00 = matrix[0, 0];
            transform10 = matrix[1, 0];
            transform20 = matrix[2, 0];
            transform30 = matrix[3, 0];
            transform40 = matrix[4, 0];
            transform01 = matrix[0, 1];
            transform11 = matrix[1, 1];
            transform21 = matrix[2, 1];
            transform31 = matrix[3, 1];
            transform41 = matrix[4, 1];
            transform02 = matrix[0, 2];
            transform12 = matrix[1, 2];
            transform22 = matrix[2, 2];
            transform32 = matrix[3, 2];
            transform42 = matrix[4, 2];
            transform03 = matrix[0, 3];
            transform13 = matrix[1, 3];
            transform23 = matrix[2, 3];
            transform33 = matrix[3, 3];
            transform43 = matrix[4, 3];
            transform04 = matrix[0, 4];
            transform14 = matrix[1, 4];
            transform24 = matrix[2, 4];
            transform34 = matrix[3, 4];
            transform44 = matrix[4, 4];
        }

        public float[,] GetMatrix()
        {
            float[,] matrix = new float[5, 5];
            matrix[0, 0] = transform00;
            matrix[1, 0] = transform10;
            matrix[2, 0] = transform20;
            matrix[3, 0] = transform30;
            matrix[4, 0] = transform40;
            matrix[0, 1] = transform01;
            matrix[1, 1] = transform11;
            matrix[2, 1] = transform21;
            matrix[3, 1] = transform31;
            matrix[4, 1] = transform41;
            matrix[0, 2] = transform02;
            matrix[1, 2] = transform12;
            matrix[2, 2] = transform22;
            matrix[3, 2] = transform32;
            matrix[4, 2] = transform42;
            matrix[0, 3] = transform03;
            matrix[1, 3] = transform13;
            matrix[2, 3] = transform23;
            matrix[3, 3] = transform33;
            matrix[4, 3] = transform43;
            matrix[0, 4] = transform04;
            matrix[1, 4] = transform14;
            matrix[2, 4] = transform24;
            matrix[3, 4] = transform34;
            matrix[4, 4] = transform44;
            return matrix;
        }
    }

    [Flags]
    internal enum KeyModifiers : int
    {
        NONE = 0,
        MOD_ALT = 0x0001,
        MOD_CONTROL = 0x0002,
        MOD_SHIFT = 0x0004,
        MOD_WIN = 0x0008,
        MOD_NOREPEAT = 0x4000,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Transformation
    {
        public float m00;
        public float m01;
        public float m02;
        public float m10;
        public float m11;
        public float m12;
        public float m20;
        public float m21;
        public float m22;

        public Transformation(float scale)
        {
            m00 = scale; m01 = 0; m02 = 0;
            m10 = 0; m11 = scale; m12 = 0;
            m20 = 0; m21 = 0; m22 = 1.0f;
        }
    }

    internal static class WindowStyles
    {
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int WS_VISIBLE = 0x10000000;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const int SWP_NOSIZE = 0x0001;
        public const int SWP_NOMOVE = 0x0002;
        public const int SWP_NOACTIVATE = 0x0010;
        public const int SWP_SHOWWINDOW = 0x0040;
        public const int SWP_NOZORDER = 0x0004;
        public const int SWP_NOSENDCHANGING = 0x0400;
    }

    internal enum WindowMessage : int
    {
        WM_APP = 0x8000,
        WM_HOTKEY = 0x312,
    }
}
