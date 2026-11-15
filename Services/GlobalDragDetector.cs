using System.Windows;
using System.Windows.Threading;
using Blueberry.Helpers;
using Blueberry.Views;
using static Blueberry.Helpers.NativeMethods;

namespace Blueberry.Services;

public class GlobalDragDetector
{
    private readonly DispatcherTimer _timer;
    private readonly RadialMenuWindow _radialMenu;

    private bool   _wasLButtonDown   = false;
    private POINT  _dragStartPoint;
    private bool   _menuShown        = false;
    private bool   _lastToolsMode    = false;

    public bool IsEnabled { get; set; } = true;

    public GlobalDragDetector(RadialMenuWindow radialMenu)
    {
        _radialMenu = radialMenu;
        _timer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(25)
        };
        _timer.Tick += OnTick;
    }

    public void Start() => _timer.Start();
    public void Stop()  => _timer.Stop();

    private void OnTick(object? sender, EventArgs e)
    {
        if (!IsEnabled) return;

        bool lBtn     = (GetAsyncKeyState(VK_LBUTTON)  & 0x8000) != 0;
        bool shift    = (GetAsyncKeyState(VK_SHIFT)     & 0x8000) != 0;
        bool ctrl     = (GetAsyncKeyState(VK_CONTROL)   & 0x8000) != 0;

        GetCursorPos(out POINT cur);

        if (lBtn)
        {
            if (!_wasLButtonDown)
            {
                // Início do clique — registra ponto de partida
                _wasLButtonDown = false; // será setado logo abaixo
                _dragStartPoint = cur;
                _menuShown      = false;
            }
            _wasLButtonDown = true;

            int    dx   = cur.X - _dragStartPoint.X;
            int    dy   = cur.Y - _dragStartPoint.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            bool shouldShow = shift && dist > 18;

            if (shouldShow)
            {
                bool wantsTools = ctrl; // Ctrl+Shift = Ferramentas | Shift sozinho = Converter

                if (!_menuShown && !_radialMenu.IsConverting)
                {
                    _menuShown     = true;
                    _lastToolsMode = wantsTools;
                    _radialMenu.ShowAtCursor(null, wantsTools);
                }
                else if (_menuShown && wantsTools != _lastToolsMode && !_radialMenu.IsConverting)
                {
                    // O usuário pressionou/soltou Ctrl enquanto o menu estava aberto
                    _lastToolsMode = wantsTools;
                    _radialMenu.SwitchMode(wantsTools);
                }
            }
            else if (_menuShown && !_radialMenu.IsConverting)
            {
                // Soltou Shift enquanto arrastava → fecha o menu
                _menuShown = false;
                _radialMenu.Hide();
            }
        }
        else
        {
            if (_wasLButtonDown)
            {
                _wasLButtonDown = false;

                if (_menuShown && !_radialMenu.IsConverting)
                {
                    _menuShown = false;
                    // Pequeno delay para o evento Drop da janela processar primeiro
                    Task.Delay(100).ContinueWith(_ =>
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            if (!_radialMenu.IsConverting)
                                _radialMenu.Hide();
                        }));
                }
            }
        }
    }
}
