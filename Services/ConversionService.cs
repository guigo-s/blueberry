using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Media;
using Blueberry.Models;

namespace Blueberry.Services;

public class ConversionService
{
    private static readonly Lazy<ConversionService> _instance = new(() => new ConversionService());
    public static ConversionService Instance => _instance.Value;

    public event Action<ConversionItem>? ConversionStarted;
    public event Action<ConversionItem>? ConversionCompleted;

    public readonly List<ConversionItem> History = new();

    public async Task<ConversionItem> ConvertAsync(string sourcePath, ConversionOption option)
    {
        var item = new ConversionItem
        {
            SourcePath = sourcePath,
            TargetFormat = option.DisplayName,
            Status = "Processando...",
            OriginalSizeFormatted = FormatFileSize(new FileInfo(sourcePath).Length)
        };

        History.Insert(0, item);
        ConversionStarted?.Invoke(item);

        try
        {
            var targetPath = DetermineOutputPath(sourcePath, option);
            item.TargetPath = targetPath;

            if (option.ToolAction == "zip_file")
            {
                await Task.Run(() => CreateZip(sourcePath, targetPath));
            }
            else if (option.ToolAction == "extract_text")
            {
                await Task.Run(() => ExtractText(sourcePath, targetPath));
            }
            else
            {
                await Task.Run(() => RunFfmpegAction(sourcePath, targetPath, option));
            }

            if (File.Exists(targetPath) || (option.ToolAction == "split_30s"))
            {
                if (File.Exists(targetPath))
                {
                    var targetInfo = new FileInfo(targetPath);
                    item.ConvertedSizeFormatted = FormatFileSize(targetInfo.Length);
                }
                else
                {
                    item.ConvertedSizeFormatted = "Partes geradas";
                }

                item.IsSuccess = true;
                item.Status = "Concluído";

                try { SystemSounds.Asterisk.Play(); } catch { }
            }
            else
            {
                item.IsSuccess = false;
                item.Status = "Falha";
                item.ErrorMessage = "Arquivo final não foi gerado.";
            }
        }
        catch (Exception ex)
        {
            item.IsSuccess = false;
            item.Status = "Erro";
            item.ErrorMessage = ex.Message;
        }

        ConversionCompleted?.Invoke(item);
        return item;
    }

    public async Task<ConversionItem> ExecuteTrimAsync(string sourcePath, double startSec, double durationSec)
    {
        var dir = Path.GetDirectoryName(sourcePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var ext = Path.GetExtension(sourcePath);
        var targetPath = Path.Combine(dir, $"{baseName}_corte{ext}");
        int counter = 1;
        while (File.Exists(targetPath))
        {
            targetPath = Path.Combine(dir, $"{baseName}_corte ({counter}){ext}");
            counter++;
        }

        var item = new ConversionItem
        {
            SourcePath = sourcePath,
            TargetPath = targetPath,
            TargetFormat = "Corte (Trim)",
            Status = "Cortando...",
            OriginalSizeFormatted = FormatFileSize(new FileInfo(sourcePath).Length)
        };

        History.Insert(0, item);
        ConversionStarted?.Invoke(item);

        try
        {
            await Task.Run(() =>
            {
                string sStart = startSec.ToString("0.###", CultureInfo.InvariantCulture);
                string sDur = durationSec.ToString("0.###", CultureInfo.InvariantCulture);
                string args = $"-y -ss {sStart} -i \"{sourcePath}\" -t {sDur} -c copy \"{targetPath}\"";

                RunFfmpegWithArgs(args);
            });

            if (File.Exists(targetPath))
            {
                item.ConvertedSizeFormatted = FormatFileSize(new FileInfo(targetPath).Length);
                item.IsSuccess = true;
                item.Status = "Concluído";
                try { SystemSounds.Asterisk.Play(); } catch { }
            }
            else
            {
                item.IsSuccess = false;
                item.Status = "Falha";
            }
        }
        catch (Exception ex)
        {
            item.IsSuccess = false;
            item.Status = "Erro";
            item.ErrorMessage = ex.Message;
        }

        ConversionCompleted?.Invoke(item);
        return item;
    }

    public async Task<ConversionItem> ExecuteCropRatioAsync(string sourcePath, string ratio, bool isVideo)
    {
        var dir = Path.GetDirectoryName(sourcePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var ext = Path.GetExtension(sourcePath);
        string ratioTag = ratio.Replace(":", "x");
        var targetPath = Path.Combine(dir, $"{baseName}_crop_{ratioTag}{ext}");
        int counter = 1;
        while (File.Exists(targetPath))
        {
            targetPath = Path.Combine(dir, $"{baseName}_crop_{ratioTag} ({counter}){ext}");
            counter++;
        }

        var item = new ConversionItem
        {
            SourcePath = sourcePath,
            TargetPath = targetPath,
            TargetFormat = $"Crop ({ratio})",
            Status = "Cortando...",
            OriginalSizeFormatted = FormatFileSize(new FileInfo(sourcePath).Length)
        };

        History.Insert(0, item);
        ConversionStarted?.Invoke(item);

        try
        {
            string vf = ratio switch
            {
                "9:16" => "crop=w=min(iw\\,ih*9/16):h=min(iw*16/9\\,ih)",
                "16:9" => "crop=w=min(iw\\,ih*16/9):h=min(iw*9/16\\,ih)",
                "4:3" => "crop=w=min(iw\\,ih*4/3):h=min(iw*3/4\\,ih)",
                _ => "crop=min(iw\\,ih):min(iw\\,ih)"
            };

            string args = isVideo
                ? $"-y -i \"{sourcePath}\" -vf \"{vf}\" -c:a copy \"{targetPath}\""
                : $"-y -i \"{sourcePath}\" -vf \"{vf}\" \"{targetPath}\"";

            await Task.Run(() => RunFfmpegWithArgs(args));

            if (File.Exists(targetPath))
            {
                item.ConvertedSizeFormatted = FormatFileSize(new FileInfo(targetPath).Length);
                item.IsSuccess = true;
                item.Status = "Concluído";
                try { SystemSounds.Asterisk.Play(); } catch { }
            }
            else
            {
                item.IsSuccess = false;
                item.Status = "Falha";
            }
        }
        catch (Exception ex)
        {
            item.IsSuccess = false;
            item.Status = "Erro";
            item.ErrorMessage = ex.Message;
        }

        ConversionCompleted?.Invoke(item);
        return item;
    }

    private string DetermineOutputPath(string sourcePath, ConversionOption option)
    {
        var dir = Path.GetDirectoryName(sourcePath);
        if (string.IsNullOrEmpty(dir))
        {
            dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        var originalFileName = Path.GetFileNameWithoutExtension(sourcePath);
        var originalExt = Path.GetExtension(sourcePath);
        var targetExt = "." + option.Extension.TrimStart('.').ToLowerInvariant();

        string baseName = originalFileName;

        if (option.IsTool)
        {
            switch (option.ToolAction)
            {
                case "crop_1x1":
                case "crop_video_1x1":
                    baseName = $"{originalFileName}_quadrado_1x1";
                    break;
                case "crop_9x16":
                case "crop_video_9x16":
                    baseName = $"{originalFileName}_vertical_9x16";
                    break;
                case "crop_16x9":
                    baseName = $"{originalFileName}_paisagem_16x9";
                    break;
                case "snapshot":
                    baseName = $"{originalFileName}_snapshot";
                    targetExt = ".jpg";
                    break;
                case "split_30s":
                    baseName = $"{originalFileName}_parte_%03d";
                    break;
                case "compress_image":
                case "compress_video":
                case "compress_audio":
                    baseName = $"{originalFileName}_otimizado";
                    break;
                case "clean_metadata":
                    baseName = $"{originalFileName}_privacidade";
                    break;
                case "grayscale":
                    baseName = $"{originalFileName}_pb";
                    break;
                case "resize_50":
                    baseName = $"{originalFileName}_50pct";
                    break;
                case "rotate_90":
                    baseName = $"{originalFileName}_rotacionado";
                    break;
                case "mute_video":
                    baseName = $"{originalFileName}_mudo";
                    break;
                case "extract_audio":
                    baseName = $"{originalFileName}_audio";
                    targetExt = ".mp3";
                    break;
                case "speed_2x":
                    baseName = $"{originalFileName}_2x";
                    break;
                case "video_to_gif":
                    baseName = $"{originalFileName}_animado";
                    targetExt = ".gif";
                    break;
                case "normalize_audio":
                    baseName = $"{originalFileName}_normalizado";
                    break;
                case "zip_file":
                    baseName = $"{originalFileName}";
                    targetExt = ".zip";
                    break;
                case "to_pdf":
                    baseName = $"{originalFileName}";
                    targetExt = ".pdf";
                    break;
                case "extract_text":
                    baseName = $"{originalFileName}_texto";
                    targetExt = ".txt";
                    break;
            }
        }

        var candidate = Path.Combine(dir, $"{baseName}{targetExt}");
        int counter = 1;
        while (File.Exists(candidate) && !option.ToolAction.Contains("split"))
        {
            candidate = Path.Combine(dir, $"{baseName} ({counter}){targetExt}");
            counter++;
        }

        return candidate;
    }

    private void CreateZip(string sourcePath, string targetPath)
    {
        using var archive = ZipFile.Open(targetPath, ZipArchiveMode.Create);
        if (File.Exists(sourcePath))
        {
            archive.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath), CompressionLevel.Optimal);
        }
        else if (Directory.Exists(sourcePath))
        {
            var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var relPath = Path.GetRelativePath(sourcePath, file);
                archive.CreateEntryFromFile(file, relPath, CompressionLevel.Optimal);
            }
        }
    }

    private void ExtractText(string sourcePath, string targetPath)
    {
        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (ext == ".txt" || ext == ".md" || ext == ".json" || ext == ".csv" || ext == ".html" || ext == ".xml")
        {
            File.Copy(sourcePath, targetPath, true);
        }
        else
        {
            // Extrai dados e informações gerais do arquivo em texto
            var fi = new FileInfo(sourcePath);
            File.WriteAllText(targetPath, $"Arquivo: {fi.Name}\nTamanho: {FormatFileSize(fi.Length)}\nCriado em: {fi.CreationTime}\nModificado em: {fi.LastWriteTime}\nCaminho: {fi.FullName}\n");
        }
    }

    private void RunFfmpegAction(string sourcePath, string targetPath, ConversionOption option)
    {
        string arguments;

        if (option.IsTool)
        {
            switch (option.ToolAction)
            {
                case "crop_1x1":
                    arguments = $"-y -i \"{sourcePath}\" -vf \"crop=min(iw\\,ih):min(iw\\,ih)\" \"{targetPath}\"";
                    break;
                case "crop_9x16":
                    arguments = $"-y -i \"{sourcePath}\" -vf \"crop=w=min(iw\\,ih*9/16):h=min(iw*16/9\\,ih)\" \"{targetPath}\"";
                    break;
                case "crop_16x9":
                    arguments = $"-y -i \"{sourcePath}\" -vf \"crop=w=min(iw\\,ih*16/9):h=min(iw*9/16\\,ih)\" \"{targetPath}\"";
                    break;
                case "crop_video_9x16":
                    arguments = $"-y -i \"{sourcePath}\" -vf \"crop=w=min(iw\\,ih*9/16):h=min(iw*16/9\\,ih)\" -c:a copy \"{targetPath}\"";
                    break;
                case "crop_video_1x1":
                    arguments = $"-y -i \"{sourcePath}\" -vf \"crop=min(iw\\,ih):min(iw\\,ih)\" -c:a copy \"{targetPath}\"";
                    break;
                case "snapshot":
                    arguments = $"-y -ss 00:00:01 -i \"{sourcePath}\" -vframes 1 -q:v 2 \"{targetPath}\"";
                    break;
                case "split_30s":
                    arguments = $"-y -i \"{sourcePath}\" -c copy -map 0 -segment_time 30 -f segment -reset_timestamps 1 \"{targetPath}\"";
                    break;
                case "compress_image":
                    arguments = $"-y -i \"{sourcePath}\" -q:v 6 \"{targetPath}\"";
                    break;
                case "clean_metadata":
                    arguments = $"-y -i \"{sourcePath}\" -map_metadata -1 -c:v copy \"{targetPath}\"";
                    break;
                case "grayscale":
                    arguments = $"-y -i \"{sourcePath}\" -vf \"hue=s=0\" \"{targetPath}\"";
                    break;
                case "resize_50":
                    arguments = $"-y -i \"{sourcePath}\" -vf \"scale=iw/2:ih/2\" \"{targetPath}\"";
                    break;
                case "rotate_90":
                    arguments = $"-y -i \"{sourcePath}\" -vf \"transpose=1\" \"{targetPath}\"";
                    break;
                case "compress_video":
                    arguments = $"-y -i \"{sourcePath}\" -c:v libx264 -crf 28 -preset fast -c:a aac -b:a 128k \"{targetPath}\"";
                    break;
                case "mute_video":
                    arguments = $"-y -i \"{sourcePath}\" -an -c:v copy \"{targetPath}\"";
                    break;
                case "extract_audio":
                    arguments = $"-y -i \"{sourcePath}\" -vn -c:a libmp3lame -q:a 2 \"{targetPath}\"";
                    break;
                case "speed_2x":
                    arguments = $"-y -i \"{sourcePath}\" -filter_complex \"[0:v]setpts=0.5*PTS[v];[0:a]atempo=2.0[a]\" -map \"[v]\" -map \"[a]\" -c:v libx264 -preset fast \"{targetPath}\"";
                    break;
                case "video_to_gif":
                    arguments = $"-y -i \"{sourcePath}\" -vf \"fps=15,scale=480:-1:flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\" \"{targetPath}\"";
                    break;
                case "compress_audio":
                    arguments = $"-y -i \"{sourcePath}\" -c:a libmp3lame -b:a 96k \"{targetPath}\"";
                    break;
                case "normalize_audio":
                    arguments = $"-y -i \"{sourcePath}\" -af \"loudnorm=I=-16:TP=-1.5:LRA=11\" \"{targetPath}\"";
                    break;
                case "to_mp3":
                    arguments = $"-y -i \"{sourcePath}\" -c:a libmp3lame -b:a 320k \"{targetPath}\"";
                    break;
                case "to_wav":
                    arguments = $"-y -i \"{sourcePath}\" -c:a pcm_s16le \"{targetPath}\"";
                    break;
                case "to_pdf":
                    arguments = $"-y -i \"{sourcePath}\" \"{targetPath}\"";
                    break;
                default:
                    arguments = $"-y -i \"{sourcePath}\" \"{targetPath}\"";
                    break;
            }
        }
        else
        {
            var ext = option.Extension.ToLowerInvariant();
            switch (ext)
            {
                case "webp":
                    arguments = $"-y -i \"{sourcePath}\" -c:v libwebp -quality 85 \"{targetPath}\"";
                    break;
                case "ico":
                    arguments = $"-y -i \"{sourcePath}\" -vf \"scale=256:256\" \"{targetPath}\"";
                    break;
                case "jpg" or "jpeg":
                    arguments = $"-y -i \"{sourcePath}\" -q:v 2 \"{targetPath}\"";
                    break;
                case "png":
                    arguments = $"-y -i \"{sourcePath}\" \"{targetPath}\"";
                    break;
                case "bmp":
                    arguments = $"-y -i \"{sourcePath}\" \"{targetPath}\"";
                    break;
                case "tiff":
                    arguments = $"-y -i \"{sourcePath}\" \"{targetPath}\"";
                    break;
                case "pdf":
                    arguments = $"-y -i \"{sourcePath}\" \"{targetPath}\"";
                    break;
                case "gif":
                    arguments = $"-y -i \"{sourcePath}\" -vf \"fps=15,scale=480:-1:flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\" \"{targetPath}\"";
                    break;
                case "mp3":
                    arguments = $"-y -i \"{sourcePath}\" -c:a libmp3lame -b:a 320k \"{targetPath}\"";
                    break;
                case "wav":
                    arguments = $"-y -i \"{sourcePath}\" -c:a pcm_s16le \"{targetPath}\"";
                    break;
                case "flac":
                    arguments = $"-y -i \"{sourcePath}\" -c:a flac \"{targetPath}\"";
                    break;
                case "ogg":
                    arguments = $"-y -i \"{sourcePath}\" -c:a libvorbis -q:a 5 \"{targetPath}\"";
                    break;
                case "m4a":
                    arguments = $"-y -i \"{sourcePath}\" -c:a aac -b:a 256k \"{targetPath}\"";
                    break;
                case "mp4":
                    arguments = $"-y -i \"{sourcePath}\" -c:v libx264 -crf 22 -preset medium -c:a aac -b:a 192k \"{targetPath}\"";
                    break;
                case "webm":
                    arguments = $"-y -i \"{sourcePath}\" -c:v libvpx-vp9 -crf 30 -b:v 0 -c:a libopus \"{targetPath}\"";
                    break;
                case "mov":
                    arguments = $"-y -i \"{sourcePath}\" -c:v copy -c:a copy \"{targetPath}\"";
                    break;
                case "mkv":
                    arguments = $"-y -i \"{sourcePath}\" -c:v copy -c:a copy \"{targetPath}\"";
                    break;
                case "avi":
                    arguments = $"-y -i \"{sourcePath}\" -c:v mpeg4 -q:v 3 -c:a mp3 \"{targetPath}\"";
                    break;
                default:
                    arguments = $"-y -i \"{sourcePath}\" \"{targetPath}\"";
                    break;
            }
        }

        RunFfmpegWithArgs(arguments);
    }

    private void RunFfmpegWithArgs(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) throw new InvalidOperationException("Não foi possível iniciar o FFmpeg.");

        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"FFmpeg erro (código {process.ExitCode}): {stderr}");
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.#} {sizes[order]}";
    }
}
