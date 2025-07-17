using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Domain.Models.MCP;

/// <summary>
/// MCP配置聚合根
/// </summary>
public class MCPConfiguration
{
    public Guid ConfigurationId { get; private set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MCPProtocolType Type { get; set; } = null!;
    public string Command { get; set; } = string.Empty;
    public List<ArgumentItem> Arguments { get; set; } = [];
    public List<EnvironmentVariable> EnvironmentVariables { get; set; } = [];
    public int? TimeoutSeconds { get; set; }
    public ProviderInfo? ProviderInfo { get; set; }
    public List<string> Tags { get; set; } = [];
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedTime { get; private set; }
    public DateTime UpdatedTime { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime? LastUsedTime { get; set; }
    public ProtocolAdapterConfiguration? AdapterConfiguration { get; set; }

    // Navigation properties
    public List<ConfigurationTemplate> Templates { get; set; } = [];

    public MCPConfiguration()
    {
        ConfigurationId = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
        UpdatedTime = CreatedTime;
    }

    /// <summary>
    /// 验证配置
    /// </summary>
    public ValidationResult ValidateConfiguration()
    {
        var result = new ValidationResult();
        
        if (string.IsNullOrWhiteSpace(Name))
            result.AddError("Name", "配置名称不能为空");
            
        if (string.IsNullOrWhiteSpace(Command))
            result.AddError("Command", "命令不能为空");
            
        return result;
    }

    /// <summary>
    /// 构建命令行
    /// </summary>
    public string BuildCommandLine()
    {
        var commandParts = new List<string> { Command };
        
        foreach (var arg in Arguments)
        {
            if (!string.IsNullOrWhiteSpace(arg.Value))
            {
                if (string.IsNullOrWhiteSpace(arg.Key))
                    commandParts.Add(arg.Value);
                else
                    commandParts.Add($"{arg.Key} {arg.Value}");
            }
        }
        
        return string.Join(" ", commandParts);
    }

    /// <summary>
    /// 测试连接
    /// </summary>
    public ConnectionTestResult TestConnection()
    {
        // TODO: 实现连接测试逻辑
        return new ConnectionTestResult
        {
            IsSuccessful = true,
            Message = "连接测试成功",
            TestedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 检查协议兼容性
    /// </summary>
    public bool IsCompatibleWith(MCPProtocolType type)
    {
        return Type == type;
    }

    /// <summary>
    /// 更新最后使用时间
    /// </summary>
    public void UpdateLastUsedTime()
    {
        LastUsedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }
}

/// <summary>
/// 参数项值对象
/// </summary>
public class ArgumentItem : ValueObject
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string Description { get; set; } = string.Empty;
    public ArgumentType Type { get; set; }
    public List<string> AllowedValues { get; set; } = [];

    /// <summary>
    /// 验证参数
    /// </summary>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();
        
        if (IsRequired && string.IsNullOrWhiteSpace(Value))
            result.AddError("Value", "必需参数的值不能为空");
            
        if (AllowedValues.Count > 0 && !AllowedValues.Contains(Value))
            result.AddError("Value", "参数值不在允许的值列表中");
            
        return result;
    }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Key) ? Value : $"{Key} {Value}";
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Key;
        yield return Value;
        yield return IsRequired;
        yield return Type;
    }
}

/// <summary>
/// 环境变量值对象
/// </summary>
public class EnvironmentVariable : ValueObject
{
    public string Key { get; set; } = string.Empty;
    public EncryptedString Value { get; set; } = EncryptedString.FromPlainText("");
    public bool IsSecure { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; }

    /// <summary>
    /// 获取解密后的值
    /// </summary>
    public string GetDecryptedValue()
    {
        return Value.Decrypt();
    }

    /// <summary>
    /// 设置加密值
    /// </summary>
    public void SetEncryptedValue(string value)
    {
        Value = EncryptedString.FromPlainText(value);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Key;
        yield return Value;
        yield return IsSecure;
        yield return IsRequired;
    }
}

/// <summary>
/// 提供商信息值对象
/// </summary>
public class ProviderInfo : ValueObject
{
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderURL { get; set; } = string.Empty;
    public string LogoURL { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = string.Empty;
    public bool IsVerified { get; set; }

    /// <summary>
    /// 验证提供商信息
    /// </summary>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();
        
        if (string.IsNullOrWhiteSpace(ProviderName))
            result.AddError("ProviderName", "提供商名称不能为空");
            
        return result;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return ProviderName;
        yield return ProviderURL;
        yield return Version;
        yield return IsVerified;
    }
}

/// <summary>
/// 参数类型枚举
/// </summary>
public enum ArgumentType
{
    String,
    Integer,
    Boolean,
    FilePath,
    DirectoryPath,
    Url,
    Email,
    Json
}

/// <summary>
/// 连接测试结果
/// </summary>
public class ConnectionTestResult
{
    public bool IsSuccessful { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TestedAt { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public Dictionary<string, string> Details { get; set; } = [];
}