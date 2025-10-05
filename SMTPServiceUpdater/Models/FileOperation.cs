namespace SMTPServiceUpdater.Models;

/// <summary>
/// Represents the type of file operation to be performed during update
/// </summary>
public enum OperationType
{
    /// <summary>
    /// File is new and will be added to installation
    /// </summary>
    Add,

    /// <summary>
    /// File exists and will be replaced with new version
    /// </summary>
    Replace,

    /// <summary>
    /// File exists in current installation but not in update - will be deleted
    /// </summary>
    Delete,

    /// <summary>
    /// File is excluded from operations (config files, logs, etc.)
    /// </summary>
    Skip,

    /// <summary>
    /// File is identical in both current and new installation - no action needed
    /// </summary>
    Identical
}

/// <summary>
/// Represents a file operation to be performed during the update process
/// </summary>
public class FileOperation
{
    /// <summary>
    /// Relative path of the file (from root)
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Full source path (where file is being copied from)
    /// </summary>
    public string SourcePath { get; }

    /// <summary>
    /// Full destination path (where file will be copied to)
    /// </summary>
    public string DestPath { get; }

    /// <summary>
    /// Type of operation to perform
    /// </summary>
    public OperationType Operation { get; }

    /// <summary>
    /// Initializes a new file operation
    /// </summary>
    /// <param name="path">Relative path from root</param>
    /// <param name="sourcePath">Full source path</param>
    /// <param name="destPath">Full destination path</param>
    /// <param name="operation">Type of operation</param>
    public FileOperation(string path, string sourcePath, string destPath, OperationType operation)
    {
        Path = path ?? string.Empty;
        SourcePath = sourcePath ?? string.Empty;
        DestPath = destPath ?? string.Empty;
        Operation = operation;
    }

    /// <summary>
    /// Returns a string representation of the file operation
    /// </summary>
    public override string ToString()
    {
        return $"[{Operation}] {Path}";
    }
}
