using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Blueberry.Helpers;

/// <summary>
/// Ícone discreto na bandeja do sistema. Clique direito ou duplo-clique
/// abre o painel do Blueberry. Não usa WinForms — 100% WPF + P/Invoke.
/// </summary>
public class WindowsTrayAgent : IDisposable
{
    // ─── Win32 Constants ────────────────────────────────────────────────────────
    private const int NIM_ADD     = 0x0;
    private const int NIM_MODIFY  = 0x1;
    private const int NIM_DELETE  = 0x2;

    private const int NIF_MESSAGE = 0x1;
    private const int NIF_ICON    = 0x2;
    private const int NIF_TIP     = 0x4;
    private const int NIF_INFO    = 0x10;

    private const int WM_USER         = 0x0400;
    private const int WM_TRAYICON     = WM_USER + 101;
    private const int WM_RBUTTONUP    = 0x0205;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_LBUTTONUP    = 0x0202;

    private const int IDI_APPLICATION = 32512;

    // ─── P/Invoke ───────────────────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct NOTIFYICONDATA
    {
        public int    cbSize;
        public IntPtr hWnd;
        public int    uID;
        public int    uFlags;
        public int    uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int    dwState;
        public int    dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int    uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int    dwInfoFlags;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    // ─── State ──────────────────────────────────────────────────────────────────
    private readonly Window   _sinkWindow;
    private          NOTIFYICONDATA _nid;
    private          bool     _disposed;

    private const uint CMD_OPEN = 1001;
    private const uint CMD_EXIT = 1002;
    private const uint MF_STRING = 0x0;
    private const uint MF_SEPARATOR = 0x800;
    private const uint TPM_RETURNCMD = 0x0100;
    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint TPM_LEFTALIGN = 0x0;

    public event Action? OnOpenRequested;
    public event Action? OnExitRequested;

    // ─── Constructor ────────────────────────────────────────────────────────────
    public WindowsTrayAgent()
    {
        // Janela invisível só pra receber mensagens Win32
        _sinkWindow = new Window
        {
            Width = 0, Height = 0,
            WindowStyle     = WindowStyle.None,
            ShowInTaskbar   = false,
            AllowsTransparency = true,
            Background      = System.Windows.Media.Brushes.Transparent,
        };
        _sinkWindow.Show();
        _sinkWindow.Hide();

        var helper = new WindowInteropHelper(_sinkWindow);
        helper.EnsureHandle();
        var src = HwndSource.FromHwnd(helper.Handle);
        src?.AddHook(WndProc);

        string iconFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blueberry.ico");
        IntPtr loadedIcon = IntPtr.Zero;
        if (System.IO.File.Exists(iconFile))
        {
            loadedIcon = LoadImage(IntPtr.Zero, iconFile, 1, 32, 32, 0x00000010);
        }
        if (loadedIcon == IntPtr.Zero)
        {
            loadedIcon = LoadIcon(IntPtr.Zero, (IntPtr)IDI_APPLICATION);
        }

        _nid = new NOTIFYICONDATA
        {
            cbSize          = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd            = helper.Handle,
            uID             = 1,
            uFlags          = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_INFO,
            uCallbackMessage = WM_TRAYICON,
            hIcon           = loadedIcon,
            szTip           = "Blueberry — Shift+Arraste para converter",
            szInfo          = "Blueberry está pronto! Segure Shift ao arrastar qualquer arquivo para converter. Ctrl+Shift para ferramentas.",
            szInfoTitle     = "Blueberry",
            uTimeoutOrVersion = 4000,
            dwInfoFlags     = 1
        };

        Shell_NotifyIcon(NIM_ADD, ref _nid);
    }

    // ─── WndProc ────────────────────────────────────────────────────────────────
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_TRAYICON)
        {
            int evt = lParam.ToInt32() & 0xFFFF;

            if (evt == WM_LBUTTONDBLCLK || evt == WM_LBUTTONUP)
            {
                OnOpenRequested?.Invoke();
                handled = true;
            }
            else if (evt == WM_RBUTTONUP)
            {
                ShowContextMenu(hwnd);
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    private void ShowContextMenu(IntPtr hwnd)
    {
        GetCursorPos(out var pt);
        SetForegroundWindow(hwnd);

        IntPtr hMenu = CreatePopupMenu();
        AppendMenu(hMenu, MF_STRING, CMD_OPEN, "Abrir Blueberry");
        AppendMenu(hMenu, MF_SEPARATOR, 0, "");
        AppendMenu(hMenu, MF_STRING, CMD_EXIT, "Sair");

        int cmd = TrackPopupMenuEx(hMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON | TPM_LEFTALIGN,
                                   pt.X, pt.Y, hwnd, IntPtr.Zero);
        DestroyMenu(hMenu);

        if (cmd == CMD_OPEN) OnOpenRequested?.Invoke();
        if (cmd == CMD_EXIT) OnExitRequested?.Invoke();
    }

    // ─── IDisposable ────────────────────────────────────────────────────────────
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Shell_NotifyIcon(NIM_DELETE, ref _nid);
    }
}
