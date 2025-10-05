namespace SMTPServiceUpdater.Models;

/// <summary>
/// Represents the result of an update operation
/// </summary>
public class UpdateResult
{
    /// <summary>
    /// Whether the update completed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of files that were replaced
    /// </summary>
    public int FilesReplaced { get; set; }

    /// <summary>
    /// Number of files that were added
    /// </summary>
    public int FilesAdded { get; set; }

    /// <summary>
    /// Number of files that were removed
    /// </summary>
    public int FilesRemoved { get; set; }

    /// <summary>
    /// Path to the backup directory created during update
    /// </summary>
    public string BackupPath { get; set; }

    /// <summary>
    /// List of error messages encountered during update
    /// </summary>
    public List<string> ErrorMessages { get; set; }

    /// <summary>
    /// Initializes a new update result
    /// </summary>
    public UpdateResult()
    {
        Success = false;
        FilesReplaced = 0;
        FilesAdded = 0;
        FilesRemoved = 0;
        BackupPath = string.Empty;
        ErrorMessages = new List<string>();
    }

    /// <summary>
    /// Gets the total number of files modified (added + replaced + removed)
    /// </summary>
    public int TotalFilesModified => FilesAdded + FilesReplaced + FilesRemoved;

    /// <summary>
    /// Whether any errors occurred during the update
    /// </summary>
    public bool HasErrors => ErrorMessages.Count > 0;

    /// <summary>
    /// Adds an error message to the result
    /// </summary>
    /// <param name="errorMessage">Error message to add</param>
    public void AddError(string errorMessage)
    {
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            ErrorMessages.Add(errorMessage);
        }
    }

    /// <summary>
    /// Returns a summary string of the update result
    /// </summary>
    public override string ToString()
    {
        if (Success)
        {
            return $"Update successful: {FilesAdded} added, {FilesReplaced} replaced, {FilesRemoved} removed";
        }
        else
        {
            return $"Update failed with {ErrorMessages.Count} error(s)";
        }
    }
}
