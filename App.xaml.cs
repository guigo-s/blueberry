using System.IO;
using System.Windows;
using Blueberry.Helpers;
using Blueberry.Services;
using Blueberry.Tests;
using Blueberry.Views;

namespace Blueberry;

public partial class App : Application
{
    private RadialMenuWindow?  _radialMenu;
    private GlobalDragDetector? _dragDetector;
    private WindowsTrayAgent?  _trayAgent;
    private MainWindow?        _mainWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Captura erros não tratados para não sumir silenciosamente
        DispatcherUnhandledException += (s, args) =>
        {
            File.WriteAllText("crash.log", args.Exception.ToString());
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            File.WriteAllText("crash.log", args.ExceptionObject?.ToString() ?? "?");

        if (e.Args.Contains("--test"))
        {
            await EngineTest.Run();
            Shutdown(0);
            return;
        }

        if (e.Args.Contains("--gen-icon"))
        {
            IconGenerator.GenerateIcon();
            Shutdown(0);
            return;
        }

        // ── Inicialização principal ──────────────────────────────────────────
        _radialMenu   = new RadialMenuWindow();
        _dragDetector = new GlobalDragDetector(_radialMenu);
        _dragDetector.Start();

        _trayAgent = new WindowsTrayAgent();
        _trayAgent.OnOpenRequested += OpenDashboard;
        _trayAgent.OnExitRequested += ExitApp;

        // Abre o painel se pedido via flag (p.ex. via atalho)
        if (e.Args.Contains("--gui"))
            OpenDashboard();
    }

    private void OpenDashboard()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow();
            _mainWindow.Closing += (s, ev) =>
            {
                // Fechar a janela NÃO mata o app — só esconde
                ev.Cancel = true;
                _mainWindow.Hide();
            };
        }
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void ExitApp()
    {
        _dragDetector?.Stop();
        _trayAgent?.Dispose();
        Current.Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _dragDetector?.Stop();
        _trayAgent?.Dispose();
        base.OnExit(e);
    }
}
