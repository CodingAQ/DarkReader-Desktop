using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace DarkReader
{
    public static class WindowRegionCalculator
    {
        public static RegionInfo CalculateVisibleRegion(IntPtr targetHwnd, IntPtr excludeHwnd = default)
        {
            var info = new RegionInfo();

            if (!NativeMethods.GetWindowRect(targetHwnd, out RECT targetRect))
            {
                info.IsEmpty = true;
                return info;
            }

            int width = targetRect.right - targetRect.left;
            int height = targetRect.bottom - targetRect.top;

            if (width <= 0 || height <= 0)
            {
                info.IsEmpty = true;
                return info;
            }

            IntPtr hRgn = NativeMethods.CreateRectRgn(targetRect.left, targetRect.top, targetRect.right, targetRect.bottom);
            if (hRgn == IntPtr.Zero)
            {
                info.IsEmpty = true;
                return info;
            }

            IntPtr current = NativeMethods.GetWindow(targetHwnd, NativeMethods.GW_HWNDPREV);
            while (current != IntPtr.Zero)
            {
                // Skip target window itself and the excluded window (e.g., our own overlay)
                if (NativeMethods.IsWindowVisible(current) && current != targetHwnd && current != excludeHwnd)
                {
                    if (NativeMethods.GetWindowRect(current, out RECT coverRect))
                    {
                        int coverW = coverRect.right - coverRect.left;
                        int coverH = coverRect.bottom - coverRect.top;

                        if (coverW > 0 && coverH > 0)
                        {
                            IntPtr hCoverRgn = NativeMethods.CreateRectRgn(
                                coverRect.left, coverRect.top, coverRect.right, coverRect.bottom);

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
