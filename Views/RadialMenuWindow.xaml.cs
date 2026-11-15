using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using Blueberry.Helpers;
using Blueberry.Models;
using Blueberry.Services;
using DragEventArgs    = System.Windows.DragEventArgs;
using KeyEventArgs     = System.Windows.Input.KeyEventArgs;
using DataFormats      = System.Windows.DataFormats;
using DragDropEffects  = System.Windows.DragDropEffects;
using Path             = System.IO.Path;
using WPath            = System.Windows.Shapes.Path;

namespace Blueberry.Views;

public partial class RadialMenuWindow : Window
{
    // ── State ────────────────────────────────────────────────────────────────────
    private readonly List<string> _files = new();
    private List<ConversionOption> _options = new();
    private readonly List<(WPath petal, TextBlock label)> _petals = new();
    private int  _hoveredIdx  = -1;
    private bool _isToolsMode = false;
    private string _cachedFileSize = "–";
    private readonly DispatcherTimer _cursorTrackingTimer;
    public  bool IsConverting { get; private set; }

    // ── Design constants ─────────────────────────────────────────────────────────
    private const double CX = 250, CY = 250;        // Centro do Canvas 500x500
    private const double InnerR  = 72;               // Raio da área central (cancelamento)
    private const double OuterR  = 194;              // Raio das pétalas
    private const double PetalGap = 4.0;             // Espaço entre pétalas (graus)

    // Cores estilo Apple Liquid Glass (Vidro Fosco do iPhone)
    private static readonly Color TextIdle      = Color.FromRgb(30, 41, 59);           // Slate escuro Apple
    private static readonly Color TextHover     = Color.FromRgb(15, 23, 42);           // Preto suave Apple

    // ── Init ─────────────────────────────────────────────────────────────────────
    public RadialMenuWindow()
    {
        InitializeComponent();

        // Rastreamento contínuo compatível com DPI em 60 FPS
        _cursorTrackingTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _cursorTrackingTimer.Tick += CursorTrackingTimer_Tick;
    }

    private void CursorTrackingTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsVisible || IsConverting) return;

        try
        {
            var mouse = NativeMethods.GetCurrentMousePosition();
            var localPos = PointFromScreen(mouse);
            UpdateHoverByAngle(localPos);
        }
        catch { }
    }

    // ── Public API ───────────────────────────────────────────────────────────────
    public void ShowAtCursor(string? file = null, bool toolsMode = false)
    {
        if (IsConverting) return;

        _isToolsMode = toolsMode;
        if (!string.IsNullOrEmpty(file))
        {
            _files.Clear();
            _files.Add(file);
        }

        PositionAtMouse();
        RefreshAll();

        StatusOverlay.Visibility = Visibility.Collapsed;
        Show();
        Activate();

        _cursorTrackingTimer.Start();
    }

    public void SwitchMode(bool toolsMode)
    {
        if (IsConverting || toolsMode == _isToolsMode) return;
        _isToolsMode = toolsMode;
        RefreshAll();
    }

    public new void Hide()
    {
        _cursorTrackingTimer.Stop();
        base.Hide();
        SetHovered(-1);
    }

    // ── Positioning ──────────────────────────────────────────────────────────────
    private void PositionAtMouse()
    {
        var m  = NativeMethods.GetCurrentMousePosition();
        var wa = SystemParameters.WorkArea;
        Left = Math.Clamp(m.X - Width  / 2.0, wa.Left, wa.Right  - Width);
        Top  = Math.Clamp(m.Y - Height / 2.0, wa.Top,  wa.Bottom - Height);
    }

    // ── Rebuild ──────────────────────────────────────────────────────────────────
    private void RefreshAll()
    {
        string src = _files.FirstOrDefault() ?? string.Empty;
        _options = _isToolsMode
            ? FileTypeService.GetToolOptions(src)
            : FileTypeService.GetConversionOptions(src);

        UpdateCenterInfo(src);

        ModeBadge.Background = new SolidColorBrush(_isToolsMode
            ? Color.FromArgb(220, 126, 34, 206)
            : Color.FromArgb(220, 37, 99, 235));
        TxtModeBadge.Text = _isToolsMode ? "FERRAMENTAS" : "CONVERTER";

        BuildPetals();
    }

    private void UpdateCenterInfo(string src)
    {
        _cachedFileSize = "–";

        if (string.IsNullOrEmpty(src))
        {
            ImgCenterIcon.Visibility = Visibility.Visible;
            TxtFileIcon.Visibility = Visibility.Collapsed;
            TxtFileSize.Text = "–";
            return;
        }

        var cat = FileTypeService.GetCategory(src);
        ImgCenterIcon.Visibility = Visibility.Collapsed;
        TxtFileIcon.Visibility = Visibility.Visible;
        TxtFileIcon.Text = cat switch
        {
            FileCategory.Image    => "🖼️",
            FileCategory.Video    => "🎬",
            FileCategory.Audio    => "🎵",
            FileCategory.Document => "📄",
            _                     => "📦"
        };

        try
        {
            var fi = new FileInfo(src);
            if (fi.Exists)
            {
                _cachedFileSize = FormatSize(fi.Length);
                TxtFileSize.Text = _cachedFileSize;
            }
        }
        catch { TxtFileSize.Text = "–"; }
    }

    // ── Petal Builder (Efeito Liquid Glass Apple & Tipografia Limpa) ─────────────
    private void BuildPetals()
    {
        PetalCanvas.Children.Clear();
        _petals.Clear();
        _hoveredIdx = -1;

        int n = _options.Count;
        if (n == 0) return;

        double sliceDeg = 360.0 / n;

        for (int i = 0; i < n; i++)
        {
            var opt = _options[i];

            double centerDeg = -90.0 + (sliceDeg * i);
            double halfSlice = sliceDeg / 2.0;

            double startDeg = centerDeg - halfSlice + (PetalGap / 2.0);
            double endDeg   = centerDeg + halfSlice - (PetalGap / 2.0);

            var (petal, label) = CreatePetal(opt, centerDeg, startDeg, endDeg, i);
            _petals.Add((petal, label));
            PetalCanvas.Children.Add(petal);
            PetalCanvas.Children.Add(label);
        }
    }

    private (WPath petal, TextBlock label) CreatePetal(
        ConversionOption opt, double centerDeg, double startDeg, double endDeg, int index)
    {
        var geo = BuildSectorGeometry(CX, CY, InnerR, OuterR, startDeg, endDeg);

        // Fundo Liquid Glass Translúcido com borda especular de reflexo
        var glassBrush = CreateGlassBrush(false);
        var glassBorderBrush = CreateGlassBorderBrush(false);

        var petal = new WPath
        {
            Data            = geo,
            Fill            = glassBrush,
            Stroke          = glassBorderBrush,
            StrokeThickness = 1.2,
            Tag             = index,
            Effect          = new DropShadowEffect
            {
                Color       = Color.FromArgb(40, 15, 23, 42),
                BlurRadius  = 14,
                ShadowDepth = 3,
                Direction   = 270,
                Opacity     = 0.22
            }
        };

        // Posição centralizada para o texto do formato (LIMPO, SEM SUBTÍTULO)
        double midR = (InnerR + OuterR) / 2.0;
        double rad  = centerDeg * Math.PI / 180.0;
        double lx   = CX + midR * Math.Cos(rad);
        double ly   = CY + midR * Math.Sin(rad);

        var label = new TextBlock
        {
            Text                = opt.DisplayName,
            FontSize            = 14,
            FontWeight          = FontWeights.Bold,
            Foreground          = new SolidColorBrush(TextIdle),
            FontFamily          = new FontFamily("SF Pro Display, Segoe UI Variable Display, Segoe UI"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment       = TextAlignment.Center,
            IsHitTestVisible    = false,
            Tag                 = index
        };

        label.Measure(new Size(110, 40));
        Canvas.SetLeft(label, lx - label.DesiredSize.Width  / 2.0);
        Canvas.SetTop( label, ly - label.DesiredSize.Height / 2.0);

        return (petal, label);
    }

    private Brush CreateGlassBrush(bool isHover)
    {
        if (isHover)
        {
            // Vidro fosco ativado: branco leitoso puro com brilho
            var b = new LinearGradientBrush
            {
                StartPoint = new Point(0.3, 0),
                EndPoint = new Point(0.7, 1)
            };
            b.GradientStops.Add(new GradientStop(Color.FromArgb(245, 255, 255, 255), 0));
            b.GradientStops.Add(new GradientStop(Color.FromArgb(220, 240, 248, 255), 1));
            return b;
        }
        else
        {
            // Vidro fosco idle do iPhone: translúcido com reflexo gelo/azul suave
            var b = new LinearGradientBrush
            {
                StartPoint = new Point(0.2, 0),
                EndPoint = new Point(0.8, 1)
            };
            if (_isToolsMode)
            {
                b.GradientStops.Add(new GradientStop(Color.FromArgb(215, 250, 245, 255), 0));
                b.GradientStops.Add(new GradientStop(Color.FromArgb(175, 235, 220, 255), 1));
            }
            else
            {
                b.GradientStops.Add(new GradientStop(Color.FromArgb(215, 248, 252, 255), 0));
                b.GradientStops.Add(new GradientStop(Color.FromArgb(175, 220, 238, 255), 1));
            }
            return b;
        }
    }

    private Brush CreateGlassBorderBrush(bool isHover)
    {
        var b = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };
        if (isHover)
        {
            b.GradientStops.Add(new GradientStop(Color.FromArgb(255, 255, 255, 255), 0));
            b.GradientStops.Add(new GradientStop(Color.FromArgb(180, 56, 189, 248), 1));
        }
        else
        {
            b.GradientStops.Add(new GradientStop(Color.FromArgb(190, 255, 255, 255), 0));
            b.GradientStops.Add(new GradientStop(Color.FromArgb(80, 180, 215, 255), 1));
        }
        return b;
    }

    private static Geometry BuildSectorGeometry(
        double cx, double cy,
        double innerR, double outerR,
        double startDeg, double endDeg)
    {
        double s1 = startDeg * Math.PI / 180.0;
        double e1 = endDeg   * Math.PI / 180.0;

        var innerStart = new Point(cx + innerR * Math.Cos(s1), cy + innerR * Math.Sin(s1));
        var innerEnd   = new Point(cx + innerR * Math.Cos(e1), cy + innerR * Math.Sin(e1));
        var outerStart = new Point(cx + outerR * Math.Cos(s1), cy + outerR * Math.Sin(s1));
        var outerEnd   = new Point(cx + outerR * Math.Cos(e1), cy + outerR * Math.Sin(e1));

        bool isLarge = (endDeg - startDeg) > 180;

        var fig = new PathFigure { StartPoint = innerStart, IsClosed = true };
        fig.Segments.Add(new ArcSegment(innerEnd, new Size(innerR, innerR), 0, isLarge, SweepDirection.Clockwise, true));
        fig.Segments.Add(new LineSegment(outerEnd, true));
        fig.Segments.Add(new ArcSegment(outerStart, new Size(outerR, outerR), 0, isLarge, SweepDirection.Counterclockwise, true));

        return new PathGeometry(new[] { fig });
    }

    // ── Seleção Direcional Sem Piscar / Área Neutra Central ─────────────────────
    private void UpdateHoverByAngle(Point pos)
    {
        if (_options.Count == 0 || IsConverting) return;

        double dx = pos.X - CX, dy = pos.Y - CY;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        // Se o cursor estiver na área central (raio neutro):
        if (dist < InnerR - 4)
        {
            if (_hoveredIdx != -1)
            {
                SetHovered(-1);
            }
            return;
        }

        double mouseAngle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        double diff = (mouseAngle - (-90.0) + 360.0) % 360.0;
        double sliceDeg = 360.0 / _options.Count;

        int idx = (int)Math.Floor((diff + (sliceDeg / 2.0)) / sliceDeg) % _options.Count;
        if (idx != _hoveredIdx)
        {
            SetHovered(idx);
        }
    }

    private void SetHovered(int idx)
    {
        _hoveredIdx = idx;

        if (idx == -1)
        {
            CenterPill.Background = new SolidColorBrush(Color.FromRgb(71, 85, 105));
            TxtFileSize.Text = "Solte p/ cancelar";
            TxtFileSize.Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249));
        }
        else
        {
            CenterPill.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
            TxtFileSize.Text = _cachedFileSize;
            TxtFileSize.Foreground = Brushes.White;
        }

        for (int i = 0; i < _petals.Count; i++)
        {
            var (petal, label) = _petals[i];
            bool active = (i == idx);

            petal.Fill = CreateGlassBrush(active);
            petal.Stroke = CreateGlassBorderBrush(active);

            if (active)
            {
                petal.StrokeThickness = 1.8;
                petal.Effect = new DropShadowEffect
                {
                    Color       = Color.FromArgb(70, 56, 189, 248),
                    BlurRadius  = 22,
                    ShadowDepth = 5,
                    Direction   = 270,
                    Opacity     = 0.55
                };
                label.Foreground = new SolidColorBrush(TextHover);
                label.FontSize   = 15.2;
                label.FontWeight = FontWeights.Bold;
            }
            else
            {
                petal.StrokeThickness = 1.2;
                petal.Effect = new DropShadowEffect
                {
                    Color       = Color.FromArgb(40, 15, 23, 42),
                    BlurRadius  = 14,
                    ShadowDepth = 3,
                    Direction   = 270,
                    Opacity     = 0.22
                };
                label.Foreground = new SolidColorBrush(TextIdle);
                label.FontSize   = 14;
                label.FontWeight = FontWeights.SemiBold;
            }
        }
    }

    // ── Drag & Drop Events ───────────────────────────────────────────────────────
    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Copy;
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0)
            {
                _files.Clear();
                _files.AddRange(files);
                RefreshAll();
            }
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
        UpdateHoverByAngle(e.GetPosition(RootGrid));
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (_hoveredIdx < 0 || _hoveredIdx >= _options.Count)
        {
            Hide();
            return;
        }

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (dropped?.Length > 0)
            {
                _files.Clear();
                _files.AddRange(dropped);
            }
        }

        if (_files.Count == 0 || _options.Count == 0)
        {
            Hide();
            return;
        }

        await RunConversion(_files.ToList(), _options[_hoveredIdx]);
    }

    // ── Conversion & Telinhas Interativas ───────────────────────────────────────
    private async Task RunConversion(List<string> files, ConversionOption opt)
    {
        // 1. Telinha Interativa de Corte de Tamanho (Crop)
        if (opt.ToolAction == "crop_interactive" && files.Count > 0)
        {
            Hide();
            var cropWin = new QuickCropWindow();
            cropWin.LoadFile(files[0]);
            cropWin.Show();
            cropWin.Activate();
            return;
        }

        // 2. Telinha Interativa de Corte de Duração (Trim)
        if (opt.ToolAction == "trim_interactive" && files.Count > 0)
        {
            Hide();
            var trimWin = new QuickTrimWindow();
            trimWin.LoadMedia(files[0]);
            trimWin.Show();
            trimWin.Activate();
            return;
        }

        IsConverting = true;
        _cursorTrackingTimer.Stop();

        ModeBadge.Visibility     = Visibility.Collapsed;
        StatusOverlay.Visibility = Visibility.Visible;
        TxtStatusIcon.Text       = "⏳";
        TxtStatusText.Text       = files.Count > 1 ? $"{files.Count} arquivos" : "Processando...";

        bool ok = false;
        foreach (var f in files)
        {
            var r = await ConversionService.Instance.ConvertAsync(f, opt);
            if (r.IsSuccess) ok = true;
        }

        TxtStatusIcon.Text = ok ? "✓" : "✕";
        TxtStatusText.Text = ok ? "Pronto!" : "Falhou";

        await Task.Delay(ok ? 550 : 1200);

        IsConverting = false;
        ModeBadge.Visibility = Visibility.Visible;
        Hide();
        SetHovered(-1);
    }

    // ── Keyboard ─────────────────────────────────────────────────────────────────
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Hide();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────
    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)        return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes / (1024.0 * 1024):0.#} MB";
    }
}
