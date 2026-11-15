using System.IO;
using System.Windows;
using Blueberry.Services;

namespace Blueberry.Views;

public partial class QuickTrimWindow : Window
{
    private string _filePath = string.Empty;
    private double _totalSeconds = 0;
    private bool _isPlaying = false;

    public QuickTrimWindow()
    {
        InitializeComponent();
    }

    public void LoadMedia(string filePath)
    {
        _filePath = filePath;
        TxtFileName.Text = Path.GetFileName(filePath);
        MediaPreview.Source = new Uri(Path.GetFullPath(filePath));
        MediaPreview.Play();
        MediaPreview.Pause();
    }

    private void MediaPreview_MediaOpened(object sender, RoutedEventArgs e)
    {
        if (MediaPreview.NaturalDuration.HasTimeSpan)
        {
            _totalSeconds = MediaPreview.NaturalDuration.TimeSpan.TotalSeconds;
            SliderStart.Maximum = _totalSeconds;
            SliderEnd.Maximum = _totalSeconds;
            SliderStart.Value = 0;
            SliderEnd.Value = _totalSeconds;

            UpdateSummary();
        }
    }

    private void SliderStart_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SliderEnd != null && SliderStart.Value > SliderEnd.Value)
        {
            SliderEnd.Value = SliderStart.Value;
        }

        TxtStartVal.Text = FormatSeconds(SliderStart.Value);
        MediaPreview.Position = TimeSpan.FromSeconds(SliderStart.Value);
        UpdateSummary();
    }

    private void SliderEnd_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SliderStart != null && SliderEnd.Value < SliderStart.Value)
        {
            SliderStart.Value = SliderEnd.Value;
        }

        TxtEndVal.Text = FormatSeconds(SliderEnd.Value);
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        double duration = Math.Max(0, SliderEnd.Value - SliderStart.Value);
        TxtCutSummary.Text = $"Duração do corte: {FormatSeconds(duration)}";
    }

    private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_isPlaying)
        {
            MediaPreview.Pause();
            BtnPlayPause.Content = "▶ Reproduzir";
            _isPlaying = false;
        }
        else
        {
            MediaPreview.Position = TimeSpan.FromSeconds(SliderStart.Value);
            MediaPreview.Play();
            BtnPlayPause.Content = "⏸ Pausar";
            _isPlaying = true;
        }
    }

    private async void BtnSaveTrim_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
        {
            Close();
            return;
        }

        MediaPreview.Stop();

        double startSec = SliderStart.Value;
        double duration = Math.Max(0.5, SliderEnd.Value - SliderStart.Value);

        await ConversionService.Instance.ExecuteTrimAsync(_filePath, startSec, duration);
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        MediaPreview.Stop();
        Close();
    }

    private static string FormatSeconds(double totalSec)
    {
        var ts = TimeSpan.FromSeconds(totalSec);
        return ts.TotalHours >= 1 ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"mm\:ss");
    }
}
