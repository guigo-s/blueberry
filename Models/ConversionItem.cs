namespace Blueberry.Models;

public class ConversionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SourcePath { get; set; } = string.Empty;
    public string FileName => System.IO.Path.GetFileName(SourcePath);
    public string TargetPath { get; set; } = string.Empty;
    public string TargetFormat { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Pendente";
    public bool IsSuccess { get; set; } = false;
    public string ErrorMessage { get; set; } = string.Empty;
    public string OriginalSizeFormatted { get; set; } = string.Empty;
    public string ConvertedSizeFormatted { get; set; } = string.Empty;
}
