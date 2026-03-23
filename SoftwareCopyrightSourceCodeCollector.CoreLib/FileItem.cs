namespace SoftwareCopyrightSourceCodeCollector.CoreLib;

/// <summary>
/// Represents a source file discovered during a folder scan.
/// </summary>
public class FileItem
{
    public string FileName { get; set; } = string.Empty;
    public string CreationDate { get; set; } = string.Empty;
    public int CodeCount { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
}
