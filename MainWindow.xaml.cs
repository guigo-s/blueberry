using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Blueberry.Models;
using Blueberry.Services;
using Blueberry.Views;
using Microsoft.Win32;

namespace Blueberry;

public partial class MainWindow : Window
{
    public ObservableCollection<ConversionItem> HistoryItems { get; } = new();

    public MainWindow()
    {
        InitializeComponent();

        HistoryListBox.ItemsSource = HistoryItems;

        // Carrega histórico atual do serviço
        foreach (var item in ConversionService.Instance.History)
        {
            HistoryItems.Add(item);
        }

        // Assina eventos de conversão
        ConversionService.Instance.ConversionStarted += OnConversionUpdated;
        ConversionService.Instance.ConversionCompleted += OnConversionUpdated;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void BtnClose_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Hide();
    }

    private void BtnMinimize_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnConversionUpdated(ConversionItem item)
    {
        Dispatcher.Invoke(() =>
        {
            if (!HistoryItems.Contains(item))
            {
                HistoryItems.Insert(0, item);
            }
            else
            {
                HistoryListBox.Items.Refresh();
            }
        });
    }

    private void BtnToggleIsland_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "O Blueberry agora opera de forma 100% invisível em segundo plano!\n\n" +
            "Para utilizá-lo:\n" +
            "1. Arraste qualquer arquivo no Windows Explorer ou na Área de Trabalho.\n" +
            "2. Segure a tecla SHIFT enquanto arrasta.\n" +
            "3. O menu radial azul surge exatamente no seu cursor para você soltar!",
            "Como Usar o Blueberry 🫐",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Blueberry v1.0 - Instruções de Interatividade:\n\n" +
            "• Segure SHIFT enquanto arrasta arquivos no Windows.\n" +
            "• Mova o cursor em direção ao formato desejado (o formato acenderá em azul neon).\n" +
            "• Solte o botão do mouse: o arquivo é convertido e salvo imediatamente ao lado do original.\n" +
            "• Se soltar o SHIFT antes de soltar o mouse, o menu fecha e você move o arquivo normalmente.",
            "Blueberry 🫐 - Ajustes & Dicas",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Selecione um arquivo para converter com Blueberry",
            Multiselect = true
        };

        if (ofd.ShowDialog() == true && ofd.FileNames.Length > 0)
        {
            var radial = new RadialMenuWindow();
            radial.ShowAtCursor(ofd.FileNames[0]);
        }
    }

    private void DropZone_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        BtnSelectFile_Click(sender, e);
    }

    private void DropZone_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Copy;
        DropZoneBorder.Background = new SolidColorBrush(Color.FromRgb(30, 58, 138));
        DropZoneBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248));
        if (DropZoneBorder.Effect is DropShadowEffect shadow)
        {
            shadow.Color = Color.FromRgb(56, 189, 248);
            shadow.BlurRadius = 30;
            shadow.Opacity = 0.8;
        }
        TxtDropTitle.Text = "Solte para abrir o menu de opções!";
    }

    private void DropZone_DragLeave(object sender, DragEventArgs e)
    {
        ResetDropZoneVisual();
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        ResetDropZoneVisual();

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                var radial = new RadialMenuWindow();
                radial.ShowAtCursor(files[0]);
            }
        }
    }

    private void ResetDropZoneVisual()
    {
        DropZoneBorder.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
        DropZoneBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
        if (DropZoneBorder.Effect is DropShadowEffect shadow)
        {
            shadow.Color = Color.FromRgb(30, 64, 175);
            shadow.BlurRadius = 20;
            shadow.Opacity = 0.3;
        }
        TxtDropTitle.Text = "Arraste e solte qualquer arquivo aqui";
    }

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Copy;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        DropZone_Drop(sender, e);
    }

    private void BtnClearHistory_Click(object sender, RoutedEventArgs e)
    {
        HistoryItems.Clear();
        ConversionService.Instance.History.Clear();
    }

    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string path && !string.IsNullOrEmpty(path))
        {
            if (File.Exists(path))
            {
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else
            {
                var dir = Path.GetDirectoryName(path);
                if (Directory.Exists(dir))
                {
                    Process.Start("explorer.exe", $"\"{dir}\"");
                }
            }
        }
    }
}
