using System;
using System.IO;

namespace SMTPServiceUpdater.Helpers
{
    /// <summary>
    /// Provides path validation and security checks to prevent directory traversal attacks.
    /// </summary>
    public static class PathValidator
    {
        /// <summary>
        /// Validates that a path is within the allowed root directory and doesn't contain malicious patterns.
        /// </summary>
        /// <param name="path">The path to validate</param>
        /// <param name="rootPath">The root directory that paths must be within</param>
        /// <returns>True if path is valid and safe, false otherwise</returns>
        public static bool IsPathSafe(string path, string rootPath)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(rootPath))
            {
                return false;
            }

            try
            {
                // Normalize paths to full absolute paths
                string fullPath = Path.GetFullPath(path);
                string fullRootPath = Path.GetFullPath(rootPath);

                // Ensure root path ends with directory separator
                if (!fullRootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    fullRootPath += Path.DirectorySeparatorChar;
                }

                // Check if the path is within the root directory
                bool isWithinRoot = fullPath.StartsWith(fullRootPath, StringComparison.OrdinalIgnoreCase);

                if (!isWithinRoot)
                {
                    return false;
                }

                // Check for directory traversal patterns
                if (path.Contains("..") || path.Contains("~"))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                // Any exception during validation means the path is unsafe
                return false;
            }
        }

        /// <summary>
        /// Validates that a file has an allowed extension.
        /// </summary>
        /// <param name="filePath">The file path to validate</param>
        /// <param name="allowedExtensions">Array of allowed extensions (e.g., ".zip", ".json")</param>
        /// <returns>True if file extension is allowed, false otherwise</returns>
        public static bool HasAllowedExtension(string filePath, params string[] allowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            try
            {
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                foreach (string allowed in allowedExtensions)
                {
                    if (extension.Equals(allowed.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates and sanitizes a file name by removing potentially dangerous characters.
        /// </summary>
        /// <param name="fileName">The file name to sanitize</param>
        /// <returns>Sanitized file name, or null if invalid</returns>
        public static string? SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            try
            {
                // Get invalid characters for file names
                char[] invalidChars = Path.GetInvalidFileNameChars();
                
                // Remove invalid characters
                string sanitized = fileName;
                foreach (char c in invalidChars)
                {
                    sanitized = sanitized.Replace(c.ToString(), string.Empty);
                }

                // Remove directory traversal patterns
                sanitized = sanitized.Replace("..", string.Empty);
                sanitized = sanitized.Replace("~", string.Empty);

                // Ensure result is not empty
                if (string.IsNullOrWhiteSpace(sanitized))
                {
                    return null;
                }

                return sanitized;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if sufficient disk space is available for an operation.
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <param name="requiredBytes">Number of bytes required</param>
        /// <returns>True if sufficient space available, false otherwise</returns>
        public static bool HasSufficientDiskSpace(string path, long requiredBytes)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                DriveInfo drive = new DriveInfo(Path.GetPathRoot(path) ?? "C:\\");
                
                // Require at least 100MB buffer beyond the required space
                long bufferBytes = 100 * 1024 * 1024; // 100 MB
                return drive.AvailableFreeSpace >= (requiredBytes + bufferBytes);
            }
            catch
            {
                // If we can't determine disk space, assume there's enough
                // Better to try and fail than to block legitimate operations
                return true;
            }
        }
    }
}
