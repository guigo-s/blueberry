using System.Runtime.InteropServices;
using System.Windows;

namespace Blueberry.Helpers;

public static class NativeMethods
{
    public const int VK_LBUTTON = 0x01;
    public const int VK_SHIFT = 0x10;
    public const int VK_CONTROL = 0x11;
    public const int VK_MENU = 0x12; // Alt key

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    public static Point GetCurrentMousePosition()
    {
        if (GetCursorPos(out POINT pt))
        {
            return new Point(pt.X, pt.Y);
        }
        return new Point(0, 0);
    }
}
