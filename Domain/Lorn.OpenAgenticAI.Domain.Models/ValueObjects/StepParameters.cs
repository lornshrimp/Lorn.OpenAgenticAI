using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.Workflow;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// 步骤参数值对象
/// </summary>
[ValueObject]
public class StepParameters : ValueObject
{
    public Guid Id { get; private set; }

    // 导航属性 - 指向实际的参数条目
    public virtual ICollection<StepParameterEntry> ParameterEntries { get; private set; } = new List<StepParameterEntry>();

    // EF Core 需要的无参数构造函数
    private StepParameters()
    {
        Id = Guid.NewGuid();
        ParameterEntries = new List<StepParameterEntry>();
    }

    public StepParameters(
        Dictionary<string, object>? inputParameters = null,
        Dictionary<string, object>? outputParameters = null,
        Dictionary<string, string>? parameterMappings = null)
    {
        Id = Guid.NewGuid();
        ParameterEntries = new List<StepParameterEntry>();

        // 将Dictionary参数转换为ParameterEntry
        if (inputParameters != null)
        {
            foreach (var kvp in inputParameters)
            {
                ParameterEntries.Add(new StepParameterEntry(Id, "Input", kvp.Key, kvp.Value));
            }
        }

        if (outputParameters != null)
        {
            foreach (var kvp in outputParameters)
            {
                ParameterEntries.Add(new StepParameterEntry(Id, "Output", kvp.Key, kvp.Value));
            }
        }

        if (parameterMappings != null)
        {
            foreach (var kvp in parameterMappings)
            {
                ParameterEntries.Add(new StepParameterEntry(Id, "Mapping", kvp.Key, kvp.Value));
            }
        }
    }

    /// <summary>
    /// 获取输入参数字典（用于向后兼容）
    /// </summary>
    public Dictionary<string, object> GetInputParameters()
    {
        return ParameterEntries
            .Where(e => e.ParameterType == "Input")
            .ToDictionary(e => e.Key, e => e.GetValue() ?? new object());
    }

    /// <summary>
    /// 获取输出参数字典（用于向后兼容）
    /// </summary>
    public Dictionary<string, object> GetOutputParameters()
    {
        return ParameterEntries
            .Where(e => e.ParameterType == "Output")
            .ToDictionary(e => e.Key, e => e.GetValue() ?? new object());
    }

    /// <summary>
    /// 获取参数映射字典（用于向后兼容）
    /// </summary>
    public Dictionary<string, string> GetParameterMappings()
    {
        return ParameterEntries
            .Where(e => e.ParameterType == "Mapping")
            .ToDictionary(e => e.Key, e => e.GetValue<string>() ?? string.Empty);
    }

    /// <summary>
    /// 获取指定类型的参数
    /// </summary>
    public T? GetParameter<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return default;

        var entry = ParameterEntries.FirstOrDefault(e => e.Key == key && e.ParameterType == "Input");
        if (entry == null)
            return default;

        try
        {
            return entry.GetValue<T>();
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 设置输入参数
    /// </summary>
    public void SetInputParameter(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        // 查找现有的参数条目
        var existingEntry = ParameterEntries.FirstOrDefault(e => e.Key == key && e.ParameterType == "Input");
        if (existingEntry != null)
        {
            existingEntry.UpdateValue(value);
        }
        else
        {
            ParameterEntries.Add(new StepParameterEntry(Id, "Input", key, value));
        }
    }

    /// <summary>
    /// 设置输出参数
    /// </summary>
    public void SetOutputParameter(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        // 查找现有的参数条目
        var existingEntry = ParameterEntries.FirstOrDefault(e => e.Key == key && e.ParameterType == "Output");
        if (existingEntry != null)
        {
            existingEntry.UpdateValue(value);
        }
        else
        {
            ParameterEntries.Add(new StepParameterEntry(Id, "Output", key, value));
        }
    }

    /// <summary>
    /// 添加参数映射
    /// </summary>
    public void AddParameterMapping(string sourceKey, string targetKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey) || string.IsNullOrWhiteSpace(targetKey))
            throw new ArgumentException("Keys cannot be empty");

        // 查找现有的映射条目
        var existingEntry = ParameterEntries.FirstOrDefault(e => e.Key == sourceKey && e.ParameterType == "Mapping");
        if (existingEntry != null)
        {
            existingEntry.UpdateValue(targetKey);
        }
        else
        {
            ParameterEntries.Add(new StepParameterEntry(Id, "Mapping", sourceKey, targetKey));
        }
    }

    /// <summary>
    /// 验证参数
    /// </summary>
    public ValidationResult ValidateParameters()
    {
        var result = new ValidationResult();

        var inputParams = GetInputParameters();
        var mappings = GetParameterMappings();

        // 验证参数映射
        foreach (var mapping in mappings)
        {
            if (!inputParams.ContainsKey(mapping.Key))
            {
                result.AddError($"InputParameters.{mapping.Key}", $"Required parameter '{mapping.Key}' is missing");
            }
        }

        return result;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Id;

        foreach (var entry in ParameterEntries.OrderBy(e => e.ParameterType).ThenBy(e => e.Key))
        {
            yield return entry.ParameterType;
            yield return entry.Key;
            yield return entry.ValueJson;
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

        var parts = versionString.Split('.');
        if (parts.Length < 3)
            throw new ArgumentException("Version string must have at least major.minor.patch format", nameof(versionString));

        if (!int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor) ||
            !int.TryParse(parts[2], out var patch))
        {
            throw new ArgumentException("Invalid version format", nameof(versionString));
        }

        string? suffix = parts.Length > 3 ? string.Join(".", parts[3..]) : null;
        return new Version(major, minor, patch, suffix);
    }

    /// <summary>
    /// 比较版本
    /// </summary>
    public int CompareTo(Version? other)
    {
        if (other == null) return 1;

        int majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;

        int minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;

        int patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0) return patchComparison;

        return string.Compare(Suffix, other.Suffix, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        return string.IsNullOrEmpty(Suffix) ? version : $"{version}.{Suffix}";
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Major;
        yield return Minor;
        yield return Patch;
        yield return Suffix ?? string.Empty;
    }
}