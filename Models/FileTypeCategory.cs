namespace Blueberry.Models;

public enum FileCategory
{
    Image,
    Video,
    Audio,
    Document,
    Archive,
    Unknown
}

public class ConversionOption
{
    public string Extension { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = string.Empty;
    public bool IsTool { get; set; } = false; // e.g. Compress, Extract
    public string ToolAction { get; set; } = string.Empty;

    public ConversionOption(string ext, string name, string desc, string icon = "", bool isTool = false, string toolAction = "")
    {
        Extension = ext;
        DisplayName = name;
        Description = desc;
        IconGlyph = icon;
        IsTool = isTool;
        ToolAction = toolAction;
    }
}
