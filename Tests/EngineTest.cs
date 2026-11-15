using System.IO;
using Blueberry.Models;
using Blueberry.Services;

namespace Blueberry.Tests;

public static class EngineTest
{
    public static async Task Run()
    {
        Console.WriteLine("=== Iniciando Testes do Motor Blueberry ===");

        // Teste de Imagem
        string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../test_sample.png");
        if (File.Exists(imgPath))
        {
            var options = FileTypeService.GetOptionsForFile(imgPath);
            Console.WriteLine($"[OK] Opções encontradas para imagem: {options.Count} opções.");
            foreach (var opt in options)
            {
                Console.WriteLine($"   - {opt.DisplayName} ({opt.Extension}) : {opt.Description}");
            }

            var webpOpt = options.FirstOrDefault(o => o.Extension.ToLower() == "webp");
            if (webpOpt != null)
            {
                var result = await ConversionService.Instance.ConvertAsync(Path.GetFullPath(imgPath), webpOpt);
                Console.WriteLine($"[Resultado Conversão WEBP]: Sucesso={result.IsSuccess}, Saída={result.TargetPath}, Tamanho={result.ConvertedSizeFormatted}");
            }
        }

        // Teste de Vídeo para GIF
        string videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../test_sample.mp4");
        if (File.Exists(videoPath))
        {
            var vOptions = FileTypeService.GetOptionsForFile(videoPath);
            Console.WriteLine($"[OK] Opções encontradas para vídeo: {vOptions.Count} opções.");
            var gifOpt = vOptions.FirstOrDefault(o => o.DisplayName == "GIF");
            if (gifOpt != null)
            {
                var result = await ConversionService.Instance.ConvertAsync(Path.GetFullPath(videoPath), gifOpt);
                Console.WriteLine($"[Resultado Conversão Vídeo->GIF]: Sucesso={result.IsSuccess}, Saída={result.TargetPath}, Tamanho={result.ConvertedSizeFormatted}");
            }
        }

        Console.WriteLine("=== Fim dos Testes ===");
    }
}
