namespace SMTPServiceUpdater.Models;

/// <summary>
/// Represents version information with parsing and comparison capabilities
/// </summary>
public class VersionInfo : IComparable<VersionInfo>
{
    /// <summary>
    /// Original version string as provided
    /// </summary>
    public string Original { get; }

    /// <summary>
    /// Major version number
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// Minor version number
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// Patch version number
    /// </summary>
    public int Patch { get; }

    /// <summary>
    /// Initializes a new instance of VersionInfo
    /// </summary>
    /// <param name="original">Original version string</param>
    /// <param name="major">Major version number</param>
    /// <param name="minor">Minor version number</param>
    /// <param name="patch">Patch version number</param>
    private VersionInfo(string original, int major, int minor, int patch)
    {
        Original = original;
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    /// <summary>
    /// Parses a version string into a VersionInfo object
    /// </summary>
    /// <param name="versionString">Version string in format "major.minor.patch"</param>
    /// <returns>VersionInfo object</returns>
    /// <exception cref="ArgumentException">Thrown when version string is invalid</exception>
    public static VersionInfo Parse(string versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
        {
            throw new ArgumentException("Version string cannot be null or empty", nameof(versionString));
        }

        // Remove 'v' prefix if present
        var cleanVersion = versionString.TrimStart('v', 'V');

        var parts = cleanVersion.Split('.');
        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid version format: {versionString}. Expected format: major.minor.patch", nameof(versionString));
        }

        if (!int.TryParse(parts[0], out int major))
        {
            throw new ArgumentException($"Invalid major version: {parts[0]}", nameof(versionString));
        }

        if (!int.TryParse(parts[1], out int minor))
        {
            throw new ArgumentException($"Invalid minor version: {parts[1]}", nameof(versionString));
        }

        if (!int.TryParse(parts[2], out int patch))
        {
            throw new ArgumentException($"Invalid patch version: {parts[2]}", nameof(versionString));
        }

        if (major < 0 || minor < 0 || patch < 0)
        {
            throw new ArgumentException("Version numbers cannot be negative", nameof(versionString));
        }

        return new VersionInfo(cleanVersion, major, minor, patch);
    }

    /// <summary>
    /// Tries to parse a version string into a VersionInfo object
    /// </summary>
    /// <param name="versionString">Version string to parse</param>
    /// <param name="versionInfo">Parsed VersionInfo object if successful</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    public static bool TryParse(string versionString, out VersionInfo? versionInfo)
    {
        try
        {
            versionInfo = Parse(versionString);
            return true;
        }
        catch
        {
            versionInfo = null;
            return false;
        }
    }

    /// <summary>
    /// Compares this version to another version
    /// </summary>
    /// <param name="other">Version to compare to</param>
    /// <returns>
    /// Less than 0 if this version is earlier,
    /// 0 if versions are equal,
    /// Greater than 0 if this version is later
    /// </returns>
    public int CompareTo(VersionInfo? other)
    {
        if (other == null)
        {
            return 1;
        }

        // Compare major version
        if (Major != other.Major)
        {
            return Major.CompareTo(other.Major);
        }

        // Compare minor version
        if (Minor != other.Minor)
        {
            return Minor.CompareTo(other.Minor);
        }

        // Compare patch version
        return Patch.CompareTo(other.Patch);
    }

    /// <summary>
    /// Returns the version as a string in format "major.minor.patch"
    /// </summary>
    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }

    /// <summary>
    /// Determines whether this version is equal to another object
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is VersionInfo other)
        {
            return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        }
        return false;
    }

    /// <summary>
    /// Gets hash code for this version
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch);
    }

    // Comparison operators
    public static bool operator <(VersionInfo? left, VersionInfo? right)
    {
        if (left is null)
        {
            return right is not null;
        }
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(VersionInfo? left, VersionInfo? right)
    {
        if (left is null)
        {
            return false;
        }
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(VersionInfo? left, VersionInfo? right)
    {
        if (left is null)
        {
            return true;
        }
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(VersionInfo? left, VersionInfo? right)
    {
        if (left is null)
        {
            return right is null;
        }
        return left.CompareTo(right) >= 0;
    }

    public static bool operator ==(VersionInfo? left, VersionInfo? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(VersionInfo? left, VersionInfo? right)
    {
        return !(left == right);
    }
}
