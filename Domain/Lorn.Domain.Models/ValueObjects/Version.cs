using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.ValueObjects;

namespace Lorn.Domain.Models.ValueObjects;

/// <summary>
/// Version value object for workflow templates
/// </summary>
public class Version : ValueObject
{
    /// <summary>
    /// Gets the major version number
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// Gets the minor version number
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// Gets the patch version number
    /// </summary>
    public int Patch { get; }

    /// <summary>
    /// Gets the version suffix (alpha, beta, rc, etc.)
    /// </summary>
    public string? Suffix { get; }

    /// <summary>
    /// Initializes a new instance of the Version class
    /// </summary>
    /// <param name="major">The major version number</param>
    /// <param name="minor">The minor version number</param>
    /// <param name="patch">The patch version number</param>
    /// <param name="suffix">The version suffix</param>
    public Version(int major, int minor, int patch, string? suffix = null)
    {
        if (major < 0) throw new ArgumentException("Major version cannot be negative", nameof(major));
        if (minor < 0) throw new ArgumentException("Minor version cannot be negative", nameof(minor));
        if (patch < 0) throw new ArgumentException("Patch version cannot be negative", nameof(patch));

        Major = major;
        Minor = minor;
        Patch = patch;
        Suffix = suffix;
    }

    /// <summary>
    /// Returns the string representation of the version
    /// </summary>
    /// <returns>The version string in format "major.minor.patch[-suffix]"</returns>
    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        if (!string.IsNullOrEmpty(Suffix))
            version += $"-{Suffix}";
        return version;
    }

    /// <summary>
    /// Compares this version with another version
    /// </summary>
    /// <param name="other">The version to compare with</param>
    /// <returns>A value indicating the relative order of the versions</returns>
    public int CompareTo(Version? other)
    {
        if (other == null) return 1;

        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;

        var patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0) return patchComparison;

        // Handle suffix comparison
        if (string.IsNullOrEmpty(Suffix) && string.IsNullOrEmpty(other.Suffix))
            return 0;
        
        if (string.IsNullOrEmpty(Suffix))
            return 1; // Release version is higher than pre-release
        
        if (string.IsNullOrEmpty(other.Suffix))
            return -1; // Pre-release is lower than release
        
        return string.Compare(Suffix, other.Suffix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if this version is compatible with another version
    /// </summary>
    /// <param name="other">The version to check compatibility with</param>
    /// <returns>True if compatible, false otherwise</returns>
    public bool IsCompatible(Version other)
    {
        if (other == null) return false;

        // Same major version is generally compatible
        if (Major == other.Major)
            return true;

        // Different major versions are incompatible
        return false;
    }

    /// <summary>
    /// Creates a new version with incremented major number
    /// </summary>
    /// <returns>A new version with incremented major number</returns>
    public Version IncrementMajor()
    {
        return new Version(Major + 1, 0, 0);
    }

    /// <summary>
    /// Creates a new version with incremented minor number
    /// </summary>
    /// <returns>A new version with incremented minor number</returns>
    public Version IncrementMinor()
    {
        return new Version(Major, Minor + 1, 0);
    }

    /// <summary>
    /// Creates a new version with incremented patch number
    /// </summary>
    /// <returns>A new version with incremented patch number</returns>
    public Version IncrementPatch()
    {
        return new Version(Major, Minor, Patch + 1, Suffix);
    }

    /// <summary>
    /// Parses a version string into a Version object
    /// </summary>
    /// <param name="versionString">The version string to parse</param>
    /// <returns>A Version object</returns>
    public static Version Parse(string versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
            throw new ArgumentException("Version string cannot be null or empty", nameof(versionString));

        var parts = versionString.Split('-');
        var versionParts = parts[0].Split('.');
        
        if (versionParts.Length < 3)
            throw new FormatException("Version string must have at least major.minor.patch format");

        if (!int.TryParse(versionParts[0], out var major))
            throw new FormatException("Invalid major version number");
        
        if (!int.TryParse(versionParts[1], out var minor))
            throw new FormatException("Invalid minor version number");
        
        if (!int.TryParse(versionParts[2], out var patch))
            throw new FormatException("Invalid patch version number");

        var suffix = parts.Length > 1 ? parts[1] : null;

        return new Version(major, minor, patch, suffix);
    }

    /// <summary>
    /// Gets the atomic values that make up this value object
    /// </summary>
    /// <returns>The atomic values</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Major;
        yield return Minor;
        yield return Patch;
        yield return Suffix ?? string.Empty;
    }
}