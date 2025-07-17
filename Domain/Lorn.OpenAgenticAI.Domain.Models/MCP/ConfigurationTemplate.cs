using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Domain.Models.MCP;

/// <summary>
/// ����ģ��ʵ��
/// </summary>
public class ConfigurationTemplate
{
    public Guid TemplateId { get; private set; }
    public string TemplateName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public MCPProtocolType ProtocolType { get; set; } = null!;
    public MCPConfiguration DefaultConfiguration { get; set; } = new();
    public List<string> RequiredFields { get; set; } = [];
    public List<string> OptionalFields { get; set; } = [];
    public List<ValidationRule> ValidationRules { get; set; } = [];
    public string UsageExample { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; }
    public int PopularityScore { get; set; }
    public DateTime CreatedTime { get; private set; }
    public Guid? CreatedBy { get; set; }

    // Navigation properties
    public UserProfile? Creator { get; set; }
    public List<MCPConfiguration> GeneratedConfigurations { get; set; } = [];

    public ConfigurationTemplate()
    {
        TemplateId = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// ���ݲ�����������
    /// </summary>
    public MCPConfiguration CreateConfiguration(Dictionary<string, object> parameters)
    {
        var config = new MCPConfiguration
        {
            Name = GetParameterValue<string>(parameters, "Name") ?? "������",
            Description = GetParameterValue<string>(parameters, "Description") ?? "",
            Type = DefaultConfiguration.Type,
            Command = DefaultConfiguration.Command,
            TimeoutSeconds = DefaultConfiguration.TimeoutSeconds,
            IsEnabled = true
        };

        // ����Ĭ�ϲ���
        config.Arguments.AddRange(DefaultConfiguration.Arguments);
        config.EnvironmentVariables.AddRange(DefaultConfiguration.EnvironmentVariables);
        config.Tags.AddRange(DefaultConfiguration.Tags);

        // Ӧ���û��ṩ�Ĳ���
        ApplyUserParameters(config, parameters);

        return config;
    }

    /// <summary>
    /// ��֤����
    /// </summary>
    public ValidationResult ValidateParameters(Dictionary<string, object> parameters)
    {
        var result = new ValidationResult();

        // �������ֶ�
        foreach (var requiredField in RequiredFields)
        {
            if (!parameters.ContainsKey(requiredField) || parameters[requiredField] == null)
            {
                result.AddError(requiredField, $"�����ֶ� {requiredField} ����Ϊ��");
            }
        }

        // Ӧ����֤����
        foreach (var rule in ValidationRules)
        {
            var fieldValidation = rule.ValidateValue(parameters.GetValueOrDefault(rule.FieldName));
            if (!fieldValidation.IsValid)
            {
                result.Errors.AddRange(fieldValidation.Errors);
            }
        }

        return result;
    }

    /// <summary>
    /// �����ܻ�ӭ�̶�
    /// </summary>
    public void IncrementPopularity()
    {
        PopularityScore++;
    }

    /// <summary>
    /// ��ȡ����ֵ
    /// </summary>
    private T? GetParameterValue<T>(Dictionary<string, object> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Ӧ���û�����
    /// </summary>
    private void ApplyUserParameters(MCPConfiguration config, Dictionary<string, object> parameters)
    {
        foreach (var param in parameters)
        {
            switch (param.Key.ToLowerInvariant())
            {
                case "name":
                    config.Name = param.Value?.ToString() ?? config.Name;
                    break;
                case "description":
                    config.Description = param.Value?.ToString() ?? config.Description;
                    break;
                case "timeout":
                    if (param.Value is int timeout)
                        config.TimeoutSeconds = timeout;
                    break;
                case "tags":
                    if (param.Value is List<string> tags)
                    {
                        config.Tags.Clear();
                        config.Tags.AddRange(tags);
                    }
                    break;
            }
        }
    }
}

/// <summary>
/// ��֤����ֵ����
/// </summary>
public class ValidationRule : ValueObject
{
    public string FieldName { get; set; } = string.Empty;
    public ValidationType Type { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
    public List<object> AllowedValues { get; set; } = [];
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsRequired { get; set; }

    /// <summary>
    /// ��ֵ֤
    /// </summary>
    public ValidationResult ValidateValue(object? value)
    {
        var result = new ValidationResult();

        if (IsRequired && value == null)
        {
            result.AddError(FieldName, ErrorMessage.IsNullOrEmpty() ? $"{FieldName} �Ǳ����" : ErrorMessage);
            return result;
        }

        if (value == null)
            return result; // ��ѡ�ֶ�Ϊnull����Ч��

        switch (Type)
        {
            case ValidationType.Required:
                if (value.ToString().IsNullOrEmpty())
                    result.AddError(FieldName, ErrorMessage.IsNullOrEmpty() ? $"{FieldName} ����Ϊ��" : ErrorMessage);
                break;

            case ValidationType.MinLength:
                if (MinValue is int minLen && value.ToString()?.Length < minLen)
                    result.AddError(FieldName, ErrorMessage.IsNullOrEmpty() ? $"{FieldName} ���Ȳ������� {minLen}" : ErrorMessage);
                break;

            case ValidationType.MaxLength:
                if (MaxValue is int maxLen && value.ToString()?.Length > maxLen)
                    result.AddError(FieldName, ErrorMessage.IsNullOrEmpty() ? $"{FieldName} ���Ȳ��ܳ��� {maxLen}" : ErrorMessage);
                break;

            case ValidationType.Pattern:
                if (!Pattern.IsNullOrEmpty() && !System.Text.RegularExpressions.Regex.IsMatch(value.ToString() ?? "", Pattern))
                    result.AddError(FieldName, ErrorMessage.IsNullOrEmpty() ? $"{FieldName} ��ʽ����ȷ" : ErrorMessage);
                break;

            case ValidationType.Range:
                if (!ValidateRange(value))
                    result.AddError(FieldName, ErrorMessage.IsNullOrEmpty() ? $"{FieldName} ֵ������Χ" : ErrorMessage);
                break;

            case ValidationType.Email:
                if (!IsValidEmail(value.ToString()))
                    result.AddError(FieldName, ErrorMessage.IsNullOrEmpty() ? $"{FieldName} ������Ч���ʼ���ַ" : ErrorMessage);
                break;

            case ValidationType.Url:
                if (!IsValidUrl(value.ToString()))
                    result.AddError(FieldName, ErrorMessage.IsNullOrEmpty() ? $"{FieldName} ������Ч��URL" : ErrorMessage);
                break;

            case ValidationType.FilePath:
                if (!IsValidFilePath(value.ToString()))
                    result.AddError(FieldName, ErrorMessage.IsNullOrEmpty() ? $"{FieldName} ������Ч���ļ�·��" : ErrorMessage);
                break;
        }

        // �������ֵ
        if (AllowedValues.Count > 0 && !AllowedValues.Contains(value))
        {
            result.AddError(FieldName, ErrorMessage.IsNullOrEmpty() ? $"{FieldName} ֵ���������б���" : ErrorMessage);
        }

        return result;
    }

    private bool ValidateRange(object value)
    {
        if (value is IComparable comparable)
        {
            if (MinValue is IComparable min && comparable.CompareTo(min) < 0)
                return false;
            if (MaxValue is IComparable max && comparable.CompareTo(max) > 0)
                return false;
        }
        return true;
    }

    private bool IsValidEmail(string? email)
    {
        if (email.IsNullOrEmpty()) return false;
        try
        {
            var addr = new System.Net.Mail.MailAddress(email!);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidUrl(string? url)
    {
        if (url.IsNullOrEmpty()) return false;
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    private bool IsValidFilePath(string? path)
    {
        if (path.IsNullOrEmpty()) return false;
        try
        {
            return System.IO.Path.IsPathFullyQualified(path!);
        }
        catch
        {
            return false;
        }
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return FieldName;
        yield return Type;
        yield return Pattern;
        yield return IsRequired;
        if (MinValue != null) yield return MinValue;
        if (MaxValue != null) yield return MaxValue;
    }
}

/// <summary>
/// ��֤����ö��
/// </summary>
public enum ValidationType
{
    Required,
    MinLength,
    MaxLength,
    Pattern,
    Range,
    Email,
    Url,
    FilePath,
    Custom
}

/// <summary>
/// �ַ�����չ����
/// </summary>
public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? str)
    {
        return string.IsNullOrEmpty(str);
    }
}