using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Blueberry.Models;
using Blueberry.Services;

namespace Blueberry.Views;

public partial class QuickCropWindow : Window
{
    private string _filePath = string.Empty;
    private string _selectedRatio = "1:1";
    private bool _isVideo = false;

    public QuickCropWindow()
    {
        InitializeComponent();
    }

    public void LoadFile(string filePath)
    {
        _filePath = filePath;
        TxtFileName.Text = Path.GetFileName(filePath);
        var cat = FileTypeService.GetCategory(filePath);
        _isVideo = (cat == FileCategory.Video);

        if (_isVideo)
        {
            // Extrai frame do vídeo para preview de corte
            string tempThumb = Path.Combine(Path.GetTempPath(), $"bb_thumb_{Guid.NewGuid():N}.jpg");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-y -ss 00:00:01 -i \"{filePath}\" -vframes 1 -q:v 2 \"{tempThumb}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                p?.WaitForExit();

                if (File.Exists(tempThumb))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(tempThumb);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    ImgPreview.Source = bmp;
                    File.Delete(tempThumb);
                }
            }
            catch { }
        }
        else
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(Path.GetFullPath(filePath));
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                ImgPreview.Source = bmp;
            }
            catch { }
        }

        UpdateCropBox();
    }

    private void PreviewContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateCropBox();
    }

    private void Aspect_Checked(object sender, RoutedEventArgs e)
    {
        if (Rb1x1?.IsChecked == true) _selectedRatio = "1:1";
        else if (Rb9x16?.IsChecked == true) _selectedRatio = "9:16";
        else if (Rb16x9?.IsChecked == true) _selectedRatio = "16:9";
        else if (Rb4x3?.IsChecked == true) _selectedRatio = "4:3";

        if (TxtBoxRatio != null) TxtBoxRatio.Text = _selectedRatio;
        if (TxtDimensions != null) TxtDimensions.Text = $"Proporção de corte: {_selectedRatio}";

        UpdateCropBox();
    }

    private void UpdateCropBox()
    {
        if (PreviewContainer == null || CropBox == null) return;

        double maxW = PreviewContainer.ActualWidth - 40;
        double maxH = PreviewContainer.ActualHeight - 40;
        if (maxW <= 0 || maxH <= 0) return;

        double boxW, boxH;

        switch (_selectedRatio)
        {
            case "9:16":
                boxH = Math.Min(maxH, maxW * (16.0 / 9.0));
                boxW = boxH * (9.0 / 16.0);
                break;
            case "16:9":
                boxW = Math.Min(maxW, maxH * (16.0 / 9.0));
                boxH = boxW * (9.0 / 16.0);
                break;
            case "4:3":
                boxW = Math.Min(maxW, maxH * (4.0 / 3.0));
                boxH = boxW * (3.0 / 4.0);
                break;
            default: // 1:1
                double side = Math.Min(maxW, maxH);
                boxW = side;
                boxH = side;
                break;
        }

        CropBox.Width = boxW;
        CropBox.Height = boxH;
    }

    private async void BtnSaveCrop_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
        {
            Close();
            return;
        }

        Close();
        await ConversionService.Instance.ExecuteCropRatioAsync(_filePath, _selectedRatio, _isVideo);
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
