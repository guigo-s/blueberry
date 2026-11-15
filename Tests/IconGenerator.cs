using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Blueberry.Tests;

public static class IconGenerator
{
    public static void GenerateIcon()
    {
        int size = 512;
        var visual = new DrawingVisual();

        string rawEmojiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../apple_blueberry_raw.png");
        BitmapImage? emojiBitmap = null;
        if (File.Exists(rawEmojiPath))
        {
            emojiBitmap = new BitmapImage();
            emojiBitmap.BeginInit();
            emojiBitmap.UriSource = new Uri(Path.GetFullPath(rawEmojiPath));
            emojiBitmap.CacheOption = BitmapCacheOption.OnLoad;
            emojiBitmap.EndInit();
        }

        using (var dc = visual.RenderOpen())
        {
            double margin = 28;
            double squircleSize = size - (margin * 2);
            double cornerRadius = 105;

            var bgRect = new Rect(margin, margin, squircleSize, squircleSize);

            // Sombra do Squircle macOS
            for (int i = 5; i >= 1; i--)
            {
                var shadowBrush = new SolidColorBrush(Color.FromArgb((byte)(12 * (6 - i)), 15, 23, 42));
                dc.DrawRoundedRectangle(shadowBrush, null,
                    new Rect(margin, margin + (i * 3.5), squircleSize, squircleSize), cornerRadius, cornerRadius);
            }

            // Fundo do Squircle (Branco limpo com gradiente Apple imperceptível)
            var bgBrush = new LinearGradientBrush(
                Color.FromRgb(255, 255, 255),
                Color.FromRgb(246, 248, 252),
                new Point(0.5, 0),
                new Point(0.5, 1));

            var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 203, 213, 225)), 1.2);
            dc.DrawRoundedRectangle(bgBrush, borderPen, bgRect, cornerRadius, cornerRadius);

            // Desenha o Emoji Oficial do iPhone
            if (emojiBitmap != null)
            {
                double emojiSize = 310;
                double ex = (size - emojiSize) / 2.0;
                double ey = (size - emojiSize) / 2.0;

                // Sombra suave sob o emoji
                var shadowBrush = new RadialGradientBrush(Color.FromArgb(70, 15, 23, 42), Color.FromArgb(0, 15, 23, 42));
                dc.DrawEllipse(shadowBrush, null, new Point(256, 385), 115, 32);

                dc.DrawImage(emojiBitmap, new Rect(ex, ey - 6, emojiSize, emojiSize));
            }
        }

        var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        string outPng = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../blueberry_mac.png");
        using (var fs = File.Create(outPng))
        {
            encoder.Save(fs);
        }

        Console.WriteLine($"[OK] Ícone Oficial do iPhone gerado em: {outPng}");
    }
}
