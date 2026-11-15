using System.IO;
using Blueberry.Models;

namespace Blueberry.Services;

public class FileTypeService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".ico", ".bmp", ".gif", ".tiff", ".tif", ".svg", ".avif", ".heic"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".mkv", ".avi", ".webm", ".flv", ".wmv", ".m4v", ".ts", ".3gp"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".m4a", ".aac", ".flac", ".ogg", ".opus", ".wma", ".aiff"
    };

    private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".doc", ".txt", ".md", ".rtf", ".html", ".csv", ".json", ".xml"
    };

    public static FileCategory GetCategory(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(ext)) return FileCategory.Unknown;

        if (ImageExtensions.Contains(ext)) return FileCategory.Image;
        if (VideoExtensions.Contains(ext)) return FileCategory.Video;
        if (AudioExtensions.Contains(ext)) return FileCategory.Audio;
        if (DocumentExtensions.Contains(ext)) return FileCategory.Document;

        return FileCategory.Unknown;
    }

    public static List<ConversionOption> GetOptionsForFile(string filePath, bool isToolsMode = false)
    {
        return isToolsMode ? GetToolOptions(filePath) : GetConversionOptions(filePath);
    }

    public static List<ConversionOption> GetConversionOptions(string filePath)
    {
        var category = GetCategory(filePath);
        var currentExt = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
        var list = new List<ConversionOption>();

        switch (category)
        {
            case FileCategory.Image:
                AddIfDifferent(list, currentExt, "jpg", "JPG", "Foto Universal", "");
                AddIfDifferent(list, currentExt, "png", "PNG", "Sem Perda / Alpha", "");
                AddIfDifferent(list, currentExt, "webp", "WEBP", "Ultra Leve / Web", "");
                AddIfDifferent(list, currentExt, "pdf", "PDF", "Documento Portatil", "");
                AddIfDifferent(list, currentExt, "ico", "ICO", "Icone de App", "");
                AddIfDifferent(list, currentExt, "gif", "GIF", "Animacao Web", "");
                AddIfDifferent(list, currentExt, "bmp", "BMP", "Bitmap Puro", "");
                AddIfDifferent(list, currentExt, "tiff", "TIFF", "Alta Fidelidade", "");
                break;

            case FileCategory.Video:
                AddIfDifferent(list, currentExt, "mp4", "MP4", "H.264 Universal", "");
                list.Add(new ConversionOption("gif", "GIF", "GIF de Alta Definicao", "", true, "video_to_gif"));
                list.Add(new ConversionOption("mp3", "MP3", "Extrair Audio", "", true, "extract_audio"));
                AddIfDifferent(list, currentExt, "webm", "WEBM", "Otimizado Web", "");
                AddIfDifferent(list, currentExt, "mov", "MOV", "Apple QuickTime", "");
                AddIfDifferent(list, currentExt, "mkv", "MKV", "Matroska Multi-track", "");
                AddIfDifferent(list, currentExt, "avi", "AVI", "Formato Classico", "");
                break;

            case FileCategory.Audio:
                AddIfDifferent(list, currentExt, "mp3", "MP3", "Universal 320kbps", "");
                AddIfDifferent(list, currentExt, "wav", "WAV", "Estudio Sem Perdas", "");
                AddIfDifferent(list, currentExt, "m4a", "AAC", "Qualidade Apple", "");
                AddIfDifferent(list, currentExt, "flac", "FLAC", "Lossless Audio", "");
                AddIfDifferent(list, currentExt, "ogg", "OGG", "Audio Web/Jogos", "");
                break;

            case FileCategory.Document:
                AddIfDifferent(list, currentExt, "pdf", "PDF", "Documento Portatil", "");
                AddIfDifferent(list, currentExt, "txt", "TXT", "Texto Puro", "");
                list.Add(new ConversionOption("zip", "ZIP", "Compactar em ZIP", "", true, "zip_file"));
                break;

            default:
                list.Add(new ConversionOption("zip", "ZIP", "Compactar em ZIP", "", true, "zip_file"));
                break;
        }

        return list;
    }

    public static List<ConversionOption> GetToolOptions(string filePath)
    {
        var category = GetCategory(filePath);
        var currentExt = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
        var list = new List<ConversionOption>();

        switch (category)
        {
            case FileCategory.Image:
                list.Add(new ConversionOption(currentExt, "CORTAR", "Cortar enquadramento", "", true, "crop_interactive"));
                list.Add(new ConversionOption(currentExt, "COMPRIMIR", "Otimizar peso", "", true, "compress_image"));
                list.Add(new ConversionOption(currentExt, "LIMPAR EXIF", "Remover GPS e metadados", "", true, "clean_metadata"));
                list.Add(new ConversionOption(currentExt, "P&B", "Preto e Branco", "", true, "grayscale"));
                list.Add(new ConversionOption(currentExt, "REDUZIR 50%", "Metade da resolucao", "", true, "resize_50"));
                list.Add(new ConversionOption(currentExt, "GIRAR 90°", "Rotacionar 90 graus", "", true, "rotate_90"));
                list.Add(new ConversionOption("pdf", "GERAR PDF", "Criar documento PDF", "", true, "to_pdf"));
                break;

            case FileCategory.Video:
                list.Add(new ConversionOption("mp4", "CORTAR TEMPO", "Cortar trecho inicio e fim", "", true, "trim_interactive"));
                list.Add(new ConversionOption("mp4", "CORTAR TAMANHO", "Cortar enquadramento 9:16 / 1:1", "", true, "crop_interactive"));
                list.Add(new ConversionOption("jpg", "SNAPSHOT", "Capturar frame de capa", "", true, "snapshot"));
                list.Add(new ConversionOption("mp4", "COMPRIMIR", "Otimizar p/ Discord / WhatsApp", "", true, "compress_video"));
                list.Add(new ConversionOption("mp4", "MUDO", "Remover faixa de audio", "", true, "mute_video"));
                list.Add(new ConversionOption("mp3", "EXTRAIR MP3", "Audio isolado 320k", "", true, "extract_audio"));
                list.Add(new ConversionOption("mp4", "VELOCIDADE 2X", "Acelerar video em 2x", "", true, "speed_2x"));
                list.Add(new ConversionOption("gif", "CRIAR GIF", "Transformar em GIF", "", true, "video_to_gif"));
                break;

            case FileCategory.Audio:
                list.Add(new ConversionOption(currentExt, "CORTAR TEMPO", "Cortar inicio e fim", "", true, "trim_interactive"));
                list.Add(new ConversionOption(currentExt, "NORMALIZAR", "Nivelar ganho e volume", "", true, "normalize_audio"));
                list.Add(new ConversionOption("mp3", "COMPRIMIR", "Otimizar tamanho (96k)", "", true, "compress_audio"));
                list.Add(new ConversionOption("mp3", "CONVERTER MP3", "Universal 320kbps", "", true, "to_mp3"));
                list.Add(new ConversionOption("wav", "ESTUDIO WAV", "Sem compressao estudio", "", true, "to_wav"));
                break;

            case FileCategory.Document:
                list.Add(new ConversionOption("zip", "COMPACTAR ZIP", "Comprimir em .zip", "", true, "zip_file"));
                list.Add(new ConversionOption("pdf", "GERAR PDF", "Converter em PDF", "", true, "to_pdf"));
                list.Add(new ConversionOption("txt", "EXTRAIR TEXTO", "Extrair texto puro", "", true, "extract_text"));
                break;

            default:
                list.Add(new ConversionOption("zip", "COMPACTAR ZIP", "Comprimir em .zip", "", true, "zip_file"));
                break;
        }

        return list;
    }

    private static void AddIfDifferent(List<ConversionOption> list, string currentExt, string targetExt, string name, string desc, string icon)
    {
        if (!string.Equals(currentExt, targetExt, StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new ConversionOption(targetExt, name, desc, icon));
        }
    }
}
