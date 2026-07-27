using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace DarkReader
{
    public static class WindowRegionCalculator
    {
        /// <summary>
        /// Get the visible window rectangle (excluding invisible extended frame).
        /// Uses DwmGetWindowAttribute for accurate bounds on Windows 10/11.
        /// </summary>
        private static Rectangle GetVisibleWindowRect(IntPtr hwnd)
        {
            // Try DWM extended frame bounds first (accurate visible bounds)
            RECT rect;
            int hr = NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT)));
            if (hr >= 0)
            {
                return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
            }

            // Fallback to GetWindowRect
            if (NativeMethods.GetWindowRect(hwnd, out rect))
            {
                return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
            }

            return Rectangle.Empty;
        }
        /// <summary>
        /// Check if a window is cloaked by DWM (e.g., on another virtual desktop, behind secure desktop).
        /// </summary>
        private static bool IsDwmCloaked(IntPtr hwnd)
        {
            try
            {
                int hr = NativeMethods.DwmGetWindowAttributeInt(hwnd, NativeMethods.DWMWA_CLOAKED, out int cloaked, sizeof(int));
                return hr >= 0 && cloaked != 0;
            }
            catch
            {
                return false; // If cloaking detection fails, assume the window is not cloaked
            }
        }

        public static RegionInfo CalculateVisibleRegion(IntPtr targetHwnd, IntPtr excludeHwnd = default, bool skipTopmost = false)
        {
            var info = new RegionInfo();

            // Get target window visible rectangle (excluding invisible extended frame)
            Rectangle targetRect = GetVisibleWindowRect(targetHwnd);
            if (targetRect.IsEmpty)
            {
                info.IsEmpty = true;
                return info;
            }

            int width = targetRect.Width;
            int height = targetRect.Height;

            if (width <= 0 || height <= 0)
            {
                info.IsEmpty = true;
                return info;
            }

            IntPtr hRgn = NativeMethods.CreateRectRgn(targetRect.Left, targetRect.Top, targetRect.Right, targetRect.Bottom);
            if (hRgn == IntPtr.Zero)
            {
                info.IsEmpty = true;
                return info;
            }

            IntPtr current = NativeMethods.GetWindow(targetHwnd, NativeMethods.GW_HWNDPREV);
            while (current != IntPtr.Zero)
            {
                // Skip: target itself, our overlay, minimized windows, cloaked windows (other desktops)
                if (NativeMethods.IsWindowVisible(current) && !NativeMethods.IsIconic(current) && !IsDwmCloaked(current) && current != targetHwnd && current != excludeHwnd)
                {
                    // Skip always-on-top windows (e.g., fullscreen games) when requested
                    if (skipTopmost)
                    {
                        IntPtr exStyle = NativeMethods.GetWindowLongPtr(current, NativeMethods.GWL_EXSTYLE);
                        if ((exStyle.ToInt64() & NativeMethods.WS_EX_TOPMOST) != 0)
                        {
                            current = NativeMethods.GetWindow(current, NativeMethods.GW_HWNDPREV);
                            continue;
                        }
                    }

                    Rectangle coverRect = GetVisibleWindowRect(current);
                    if (!coverRect.IsEmpty)
                    {
                        int coverW = coverRect.Width;
                        int coverH = coverRect.Height;

                        if (coverW > 0 && coverH > 0)
                        {
                            // Only subtract if this window actually overlaps the current region bounds
                            NativeMethods.GetRgnBox(hRgn, out RECT currentBounds);
                            if (coverRect.Right <= currentBounds.left || coverRect.Bottom <= currentBounds.top ||
                                coverRect.Left >= currentBounds.right || coverRect.Top >= currentBounds.bottom)
                            {
                                current = NativeMethods.GetWindow(current, NativeMethods.GW_HWNDPREV);
                                continue; // No overlap — skip this window
                            }

                            IntPtr hCoverRgn = NativeMethods.CreateRectRgn(
                                coverRect.Left, coverRect.Top, coverRect.Right, coverRect.Bottom);

                            if (hCoverRgn != IntPtr.Zero)
                            {
                                NativeMethods.CombineRgn(hRgn, hRgn, hCoverRgn, NativeMethods.RGN_DIFF);
                                NativeMethods.DeleteObject(hCoverRgn);

                                NativeMethods.GetRgnBox(hRgn, out RECT box);
                                if (box.right - box.left <= 0 || box.bottom - box.top <= 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                current = NativeMethods.GetWindow(current, NativeMethods.GW_HWNDPREV);
            }

            NativeMethods.GetRgnBox(hRgn, out RECT bounds);
            int boundsW = bounds.right - bounds.left;
            int boundsH = bounds.bottom - bounds.top;

            if (boundsW <= 0 || boundsH <= 0)
            {
                NativeMethods.DeleteObject(hRgn);
                info.IsEmpty = true;
                return info;
            }

            info.HRgn = hRgn;
            info.Bounds = new Rectangle(bounds.left, bounds.top, boundsW, boundsH);
            info.IsEmpty = false;
            return info;
        }

        public static void ReleaseRegion(IntPtr hRgn)
        {
            if (hRgn != IntPtr.Zero)
            {
                NativeMethods.DeleteObject(hRgn);
            }
        }

        public static bool RegionEquals(RegionInfo a, RegionInfo b)
        {
            if (a.IsEmpty && b.IsEmpty) return true;
            if (a.IsEmpty != b.IsEmpty) return false;
            return a.Bounds == b.Bounds;
        }
    }

    public struct RegionInfo
    {
        public IntPtr HRgn;

        public Rectangle Bounds;

        public bool IsEmpty;
    }
}
