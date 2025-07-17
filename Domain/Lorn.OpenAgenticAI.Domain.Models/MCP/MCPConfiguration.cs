using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Domain.Models.MCP;

/// <summary>
/// MCP���þۺϸ�
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
    /// ��֤����
    /// </summary>
    public ValidationResult ValidateConfiguration()
    {
        var result = new ValidationResult();
        
        if (string.IsNullOrWhiteSpace(Name))
            result.AddError("Name", "�������Ʋ���Ϊ��");
            
        if (string.IsNullOrWhiteSpace(Command))
            result.AddError("Command", "�����Ϊ��");
            
        return result;
    }

    /// <summary>
    /// ����������
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
    /// ��������
    /// </summary>
    public ConnectionTestResult TestConnection()
    {
        // TODO: ʵ�����Ӳ����߼�
        return new ConnectionTestResult
        {
            IsSuccessful = true,
            Message = "���Ӳ��Գɹ�",
            TestedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// ���Э�������
    /// </summary>
    public bool IsCompatibleWith(MCPProtocolType type)
    {
        return Type == type;
    }

    /// <summary>
    /// �������ʹ��ʱ��
    /// </summary>
    public void UpdateLastUsedTime()
    {
        LastUsedTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }
}

/// <summary>
/// ������ֵ����
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
    /// ��֤����
    /// </summary>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();
        
        if (IsRequired && string.IsNullOrWhiteSpace(Value))
            result.AddError("Value", "���������ֵ����Ϊ��");
            
        if (AllowedValues.Count > 0 && !AllowedValues.Contains(Value))
            result.AddError("Value", "����ֵ���������ֵ�б���");
            
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
/// ��������ֵ����
/// </summary>
public class EnvironmentVariable : ValueObject
{
    public string Key { get; set; } = string.Empty;
    public EncryptedString Value { get; set; } = EncryptedString.FromPlainText("");
    public bool IsSecure { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; }

    /// <summary>
    /// ��ȡ���ܺ��ֵ
    /// </summary>
    public string GetDecryptedValue()
    {
        return Value.Decrypt();
    }

    /// <summary>
    /// ���ü���ֵ
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
/// �ṩ����Ϣֵ����
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
    /// ��֤�ṩ����Ϣ
    /// </summary>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();
        
        if (string.IsNullOrWhiteSpace(ProviderName))
            result.AddError("ProviderName", "�ṩ�����Ʋ���Ϊ��");
            
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
/// ��������ö��
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
/// ���Ӳ��Խ��
/// </summary>
public class ConnectionTestResult
{
    public bool IsSuccessful { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TestedAt { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public Dictionary<string, string> Details { get; set; } = [];
}