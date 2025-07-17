using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// 步骤参数值对象
/// </summary>
public class StepParameters : ValueObject
{
    public Dictionary<string, object> InputParameters { get; private set; } = [];
    public Dictionary<string, object> OutputParameters { get; private set; } = [];
    public Dictionary<string, string> ParameterMappings { get; private set; } = [];

    public StepParameters(
        Dictionary<string, object>? inputParameters = null,
        Dictionary<string, object>? outputParameters = null,
        Dictionary<string, string>? parameterMappings = null)
    {
        InputParameters = inputParameters ?? [];
        OutputParameters = outputParameters ?? [];
        ParameterMappings = parameterMappings ?? [];
    }

    /// <summary>
    /// 获取指定类型的参数
    /// </summary>
    public T? GetParameter<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !InputParameters.ContainsKey(key))
            return default;

        try
        {
            var value = InputParameters[key];
            if (value is T directValue)
                return directValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 设置参数
    /// </summary>
    public void SetParameter(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        InputParameters[key] = value;
    }

    /// <summary>
    /// 设置输出参数
    /// </summary>
    public void SetOutputParameter(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        OutputParameters[key] = value;
    }

    /// <summary>
    /// 添加参数映射
    /// </summary>
    public void AddParameterMapping(string sourceKey, string targetKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
            throw new ArgumentException("Source key cannot be empty", nameof(sourceKey));
        if (string.IsNullOrWhiteSpace(targetKey))
            throw new ArgumentException("Target key cannot be empty", nameof(targetKey));

        ParameterMappings[sourceKey] = targetKey;
    }

    /// <summary>
    /// 验证参数
    /// </summary>
    public ValidationResult ValidateParameters()
    {
        var result = new ValidationResult();

        // 验证参数映射
        foreach (var mapping in ParameterMappings)
        {
            if (!InputParameters.ContainsKey(mapping.Key))
            {
                result.AddError($"InputParameters.{mapping.Key}", $"Required parameter '{mapping.Key}' is missing");
            }
        }

        return result;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        foreach (var kvp in InputParameters)
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
        
        foreach (var kvp in OutputParameters)
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
        
        foreach (var kvp in ParameterMappings)
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
    }
}

/// <summary>
/// 版本值对象
/// </summary>
public class Version : ValueObject, IComparable<Version>
{
    public int Major { get; private set; }
    public int Minor { get; private set; }
    public int Patch { get; private set; }
    public string? Suffix { get; private set; }

    public Version(int major, int minor, int patch, string? suffix = null)
    {
        Major = major >= 0 ? major : throw new ArgumentException("Major version must be non-negative", nameof(major));
        Minor = minor >= 0 ? minor : throw new ArgumentException("Minor version must be non-negative", nameof(minor));
        Patch = patch >= 0 ? patch : throw new ArgumentException("Patch version must be non-negative", nameof(patch));
        Suffix = suffix;
    }

    /// <summary>
    /// 从字符串解析版本
    /// </summary>
    public static Version Parse(string versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
            throw new ArgumentException("Version string cannot be empty", nameof(versionString));

        var parts = versionString.Split('-');
        var versionPart = parts[0];
        var suffix = parts.Length > 1 ? parts[1] : null;

        var versionNumbers = versionPart.Split('.');
        if (versionNumbers.Length < 3)
            throw new ArgumentException("Version string must have at least 3 parts (major.minor.patch)", nameof(versionString));

        if (!int.TryParse(versionNumbers[0], out int major) ||
            !int.TryParse(versionNumbers[1], out int minor) ||
            !int.TryParse(versionNumbers[2], out int patch))
        {
            throw new ArgumentException("Invalid version format", nameof(versionString));
        }

        return new Version(major, minor, patch, suffix);
    }

    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        return string.IsNullOrWhiteSpace(Suffix) ? version : $"{version}-{Suffix}";
    }

    public int CompareTo(Version? other)
    {
        if (other == null) return 1;

        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;

        var patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0) return patchComparison;

        // 主版本相同，比较后缀
        return string.Compare(Suffix, other.Suffix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 检查是否兼容一个版本号
    /// </summary>
    public bool IsCompatible(Version other)
    {
        if (other == null) return false;

        // 主版本号必须相同
        if (Major != other.Major) return false;

        // 当前版本的次版本号必须大于或等于目标版本
        return Minor >= other.Minor;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Major;
        yield return Minor;
        yield return Patch;
        if (Suffix != null) yield return Suffix;
    }
}