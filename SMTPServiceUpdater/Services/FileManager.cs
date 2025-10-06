using SMTPServiceUpdater.Models;
using System.IO.Compression;
using System.Security.Cryptography;

namespace SMTPServiceUpdater.Services;

/// <summary>
/// Manages file operations for the update process including extraction, analysis, backup, and application of changes
/// </summary>
public class FileManager
{
    private readonly UpdateLogger _logger;
    private readonly List<string> _excludedFiles;
    private readonly List<string> _excludedFolders;

    /// <summary>
    /// Initializes a new instance of FileManager
    /// </summary>
    /// <param name="logger">Logger instance for operation tracking</param>
    public FileManager(UpdateLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize excluded files list
        _excludedFiles = new List<string>
        {
            "smtp-config.json",
            "smtp-config.json.backup",
            "smtp-config.json.pre-migration.backup",
            "smtp.json",
            "smtp.json.backup",
            "user.json",
            "user.json.backup",
            "graph.json",
            "graph.json.backup",
            "git.json",
            "git.json.backup"
        };

        // Initialize excluded folders list
        _excludedFolders = new List<string>
        {
            "logs",
            "stats",
            "updates",
            "backup"
        };
    }

    /// <summary>
    /// Determines if a file or folder should be excluded from update operations
    /// </summary>
    /// <param name="relativePath">Relative path from root (e.g., "Config/smtp-config.json")</param>
    /// <returns>True if should be excluded, false otherwise</returns>
    public bool ShouldExclude(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        // Normalize path separators
        var normalizedPath = relativePath.Replace('\\', '/');
        var parts = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return false;
        }

        // Check if first part is an excluded folder
        if (_excludedFolders.Contains(parts[0], StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if filename matches excluded files
        var fileName = Path.GetFileName(relativePath);
        if (_excludedFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Extracts a ZIP file to a specified location
    /// </summary>
    /// <param name="zipPath">Path to ZIP file</param>
    /// <param name="extractPath">Path where files should be extracted</param>
    /// <exception cref="FileNotFoundException">Thrown when ZIP file doesn't exist</exception>
    /// <exception cref="InvalidDataException">Thrown when ZIP file is corrupted</exception>
    public void ExtractZip(string zipPath, string extractPath)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
        {
            throw new ArgumentException("ZIP path cannot be null or empty", nameof(zipPath));
        }

        if (string.IsNullOrWhiteSpace(extractPath))
        {
            throw new ArgumentException("Extract path cannot be null or empty", nameof(extractPath));
        }

        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException($"ZIP file not found: {zipPath}");
        }

        _logger.WriteLog($"Extracting ZIP: {zipPath}", LogLevel.Info);
        _logger.WriteLog($"Destination: {extractPath}", LogLevel.Info);

        try
        {
            // Create extraction directory if it doesn't exist
            if (!Directory.Exists(extractPath))
            {
                Directory.CreateDirectory(extractPath);
            }

            // Extract ZIP file
            ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);

            _logger.WriteLog("ZIP extraction completed successfully", LogLevel.Success);
        }
        catch (InvalidDataException ex)
        {
            _logger.WriteLog($"ZIP file is corrupted: {ex.Message}", LogLevel.Error);
            throw;
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Failed to extract ZIP: {ex.Message}", LogLevel.Error);
            throw;
        }
    }

    /// <summary>
    /// Analyzes files from extraction against current installation
    /// </summary>
    /// <param name="extractPath">Path where update was extracted</param>
    /// <param name="rootPath">Current installation root path</param>
    /// <returns>List of file operations to perform</returns>
    public List<FileOperation> AnalyzeFiles(string extractPath, string rootPath)
    {
        var operations = new List<FileOperation>();

        _logger.WriteLog("Analyzing files for update...", LogLevel.Info);

        if (!Directory.Exists(extractPath))
        {
            _logger.WriteLog($"Extract path not found: {extractPath}", LogLevel.Error);
            return operations;
        }

        // Get all files from extracted update
        var extractedFiles = Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories);

        foreach (var extractedFile in extractedFiles)
        {
            // Get relative path from extract folder
            var relativePath = Path.GetRelativePath(extractPath, extractedFile);

            // Check if should be excluded
            if (ShouldExclude(relativePath))
            {
                operations.Add(new FileOperation(
                    relativePath,
                    extractedFile,
                    Path.Combine(rootPath, relativePath),
                    OperationType.Skip
                ));
                continue;
            }

            // Build destination path
            var destPath = Path.Combine(rootPath, relativePath);
            var sourcePath = extractedFile;

            // Determine operation type
            if (!File.Exists(destPath))
            {
                // New file - Add
                operations.Add(new FileOperation(relativePath, sourcePath, destPath, OperationType.Add));
            }
            else
            {
                // File exists - check if identical or needs replacement
                if (AreFilesIdentical(sourcePath, destPath))
                {
                    operations.Add(new FileOperation(relativePath, sourcePath, destPath, OperationType.Identical));
                }
                else
                {
                    operations.Add(new FileOperation(relativePath, sourcePath, destPath, OperationType.Replace));
                }
            }
        }

        _logger.WriteLog($"Analysis complete: {operations.Count} files analyzed", LogLevel.Success);

        return operations;
    }

    /// <summary>
    /// Detects files in current installation that are not in the update (orphaned files)
    /// </summary>
    /// <param name="extractPath">Path where update was extracted</param>
    /// <param name="rootPath">Current installation root path</param>
    /// <returns>List of file operations for orphaned files (marked for deletion)</returns>
    public List<FileOperation> DetectOrphanedFiles(string extractPath, string rootPath)
    {
        var operations = new List<FileOperation>();

        _logger.WriteLog("Detecting orphaned files...", LogLevel.Info);

        if (!Directory.Exists(rootPath))
        {
            _logger.WriteLog($"Root path not found: {rootPath}", LogLevel.Error);
            return operations;
        }

        // Get all files from current installation
        var currentFiles = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories);

        foreach (var currentFile in currentFiles)
        {
            var relativePath = Path.GetRelativePath(rootPath, currentFile);

            // Skip excluded files/folders
            if (ShouldExclude(relativePath))
            {
                continue;
            }

            // Check if file exists in extracted update
            var extractedFilePath = Path.Combine(extractPath, relativePath);

            if (!File.Exists(extractedFilePath))
            {
                // File doesn't exist in update - mark for deletion
                operations.Add(new FileOperation(
                    relativePath,
                    string.Empty,
                    currentFile,
                    OperationType.Delete
                ));
            }
        }

        _logger.WriteLog($"Found {operations.Count} orphaned files", operations.Count > 0 ? LogLevel.Warning : LogLevel.Info);

        return operations;
    }

    /// <summary>
    /// Creates a backup of the current installation
    /// </summary>
    /// <param name="rootPath">Root path of current installation</param>
    /// <param name="version">Version being installed (for backup naming)</param>
    /// <returns>BackupInfo with details of created backup</returns>
    public BackupInfo CreateBackup(string rootPath, string version)
    {
        _logger.WriteLog("Creating backup of current installation...", LogLevel.Info);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFolderName = $"backup_{version}_{timestamp}";
        var backupPath = Path.Combine(rootPath, "backup", backupFolderName);

        try
        {
            // Ensure backup directory exists
            if (!Directory.Exists(Path.Combine(rootPath, "backup")))
            {
                Directory.CreateDirectory(Path.Combine(rootPath, "backup"));
            }

            // Create backup folder
            Directory.CreateDirectory(backupPath);

            // Copy all files except excluded folders
            CopyDirectory(rootPath, backupPath, rootPath);

            var now = DateTime.Now;
            var backupInfo = new BackupInfo(backupPath, now, version);

            _logger.WriteLog($"Backup created: {backupPath}", LogLevel.Success);

            return backupInfo;
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Failed to create backup: {ex.Message}", LogLevel.Critical);
            throw;
        }
    }

    /// <summary>
    /// Maintains backup folder by keeping only the latest N backups
    /// </summary>
    /// <param name="backupFolder">Path to backup folder</param>
    /// <param name="maxBackups">Maximum number of backups to keep (default: 20)</param>
    public void MaintainBackups(string backupFolder, int maxBackups = 20)
    {
        _logger.WriteLog($"Maintaining backups (keeping latest {maxBackups})...", LogLevel.Info);

        if (!Directory.Exists(backupFolder))
        {
            _logger.WriteLog("Backup folder doesn't exist, nothing to maintain", LogLevel.Info);
            return;
        }

        try
        {
            // Get all backup directories
            var backupDirs = Directory.GetDirectories(backupFolder)
                .Select(d => new DirectoryInfo(d))
                .OrderByDescending(d => d.CreationTime)
                .ToList();

            // Delete old backups if we exceed the limit
            if (backupDirs.Count > maxBackups)
            {
                var backupsToDelete = backupDirs.Skip(maxBackups).ToList();

                _logger.WriteLog($"Cleaning up {backupsToDelete.Count} old backup(s):", LogLevel.Info);
                foreach (var backup in backupsToDelete)
                {
                    _logger.WriteLog($"  Deleting: {backup.FullName}", LogLevel.Warning);
                    Directory.Delete(backup.FullName, recursive: true);
                    _logger.WriteLog($"  Deleted: {backup.Name}", LogLevel.Success);
                }

                _logger.WriteLog($"Deleted {backupsToDelete.Count} old backup(s)", LogLevel.Success);
            }
            else
            {
                _logger.WriteLog($"Only {backupDirs.Count} backup(s) exist, no cleanup needed", LogLevel.Info);
            }
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Error maintaining backups: {ex.Message}", LogLevel.Warning);
            // Non-critical error, continue
        }
    }

    /// <summary>
    /// Applies file operations (copy/delete) from the operation list
    /// </summary>
    /// <param name="operations">List of file operations to execute</param>
    /// <returns>True if all operations succeeded, false otherwise</returns>
    public bool ApplyFileOperations(List<FileOperation> operations)
    {
        _logger.WriteLog("Applying file operations...", LogLevel.Info);

        var successCount = 0;
        var errorCount = 0;

        foreach (var operation in operations)
        {
            try
            {
                switch (operation.Operation)
                {
                    case OperationType.Add:
                    case OperationType.Replace:
                        // Ensure destination directory exists
                        var destDir = Path.GetDirectoryName(operation.DestPath);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }

                        // Copy file
                        File.Copy(operation.SourcePath, operation.DestPath, overwrite: true);
                        _logger.WriteLog($"[{operation.Operation}] {operation.DestPath}", LogLevel.Info);
                        successCount++;
                        break;

                    case OperationType.Delete:
                        if (File.Exists(operation.DestPath))
                        {
                            File.Delete(operation.DestPath);
                            _logger.WriteLog($"[DELETE] {operation.DestPath}", LogLevel.Warning);
                            successCount++;
                        }
                        break;

                    case OperationType.Skip:
                    case OperationType.Identical:
                        // No action needed
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Failed to apply operation [{operation.Operation}] {operation.DestPath}: {ex.Message}", LogLevel.Error);
                errorCount++;
            }
        }

        _logger.WriteLog($"File operations complete: {successCount} succeeded, {errorCount} failed", 
            errorCount > 0 ? LogLevel.Warning : LogLevel.Success);

        return errorCount == 0;
    }

    /// <summary>
    /// Recursively copies a directory, excluding specified folders
    /// </summary>
    private void CopyDirectory(string sourceDir, string destDir, string rootPath)
    {
        // Get relative path from root to check exclusions
        var relativePath = Path.GetRelativePath(rootPath, sourceDir);

        // Skip if this is an excluded folder
        if (ShouldExclude(relativePath))
        {
            return;
        }

        // Create destination directory
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        // Copy files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var relativeFilePath = Path.GetRelativePath(rootPath, file);

            // Skip excluded files
            if (ShouldExclude(relativeFilePath))
            {
                continue;
            }

            var destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, overwrite: true);
        }

        // Recurse into subdirectories
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(subDir);
            var destSubDir = Path.Combine(destDir, dirName);
            CopyDirectory(subDir, destSubDir, rootPath);
        }
    }

    /// <summary>
    /// Compares two files using SHA256 hash to determine if they are identical
    /// </summary>
    private bool AreFilesIdentical(string file1, string file2)
    {
        try
        {
            // Quick check: file sizes must match
            var file1Info = new FileInfo(file1);
            var file2Info = new FileInfo(file2);

            if (file1Info.Length != file2Info.Length)
            {
                return false;
            }

            // Compare file hashes
            using var sha256 = SHA256.Create();
            
            byte[] hash1;
            using (var stream1 = File.OpenRead(file1))
            {
                hash1 = sha256.ComputeHash(stream1);
            }

            byte[] hash2;
            using (var stream2 = File.OpenRead(file2))
            {
                hash2 = sha256.ComputeHash(stream2);
            }

            return hash1.SequenceEqual(hash2);
        }
        catch
        {
            // If comparison fails, assume files are different to be safe
            return false;
        }
    }
}
