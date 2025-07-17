using System;

namespace Lorn.OpenAgenticAI.Domain.Models.UserManagement;

/// <summary>
/// �û�ƫ������ʵ��
/// </summary>
public class UserPreferences
{
    public Guid PreferenceId { get; private set; }
    public Guid UserId { get; private set; }
    public string PreferenceCategory { get; set; } = string.Empty;
    public string PreferenceKey { get; set; } = string.Empty;
    public string PreferenceValue { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public DateTime LastUpdatedTime { get; private set; }
    public bool IsSystemDefault { get; private set; }
    public string Description { get; set; } = string.Empty;

    // ��������
    public virtual UserProfile User { get; set; } = null!;

    // ˽�й��캯������EF Core
    private UserPreferences() 
    {
        PreferenceId = Guid.NewGuid();
        LastUpdatedTime = DateTime.UtcNow;
    }

    public UserPreferences(
        Guid userId,
        string preferenceCategory,
        string preferenceKey,
        string preferenceValue,
        string valueType,
        bool isSystemDefault = false,
        string? description = null)
    {
        PreferenceId = Guid.NewGuid();
        UserId = userId != Guid.Empty ? userId : throw new ArgumentException("UserId cannot be empty", nameof(userId));
        PreferenceCategory = !string.IsNullOrWhiteSpace(preferenceCategory) ? preferenceCategory : throw new ArgumentException("PreferenceCategory cannot be empty", nameof(preferenceCategory));
        PreferenceKey = !string.IsNullOrWhiteSpace(preferenceKey) ? preferenceKey : throw new ArgumentException("PreferenceKey cannot be empty", nameof(preferenceKey));
        PreferenceValue = preferenceValue ?? string.Empty;
        ValueType = !string.IsNullOrWhiteSpace(valueType) ? valueType : "String";
        LastUpdatedTime = DateTime.UtcNow;
        IsSystemDefault = isSystemDefault;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// ��ȡ���ͻ���ֵ
    /// </summary>
    public T? GetTypedValue<T>()
    {
        if (string.IsNullOrEmpty(PreferenceValue))
            return default(T);

        try
        {
            return ValueType switch
            {
                "Boolean" => (T)(object)bool.Parse(PreferenceValue),
                "Integer" => (T)(object)int.Parse(PreferenceValue),
                "Double" => (T)(object)double.Parse(PreferenceValue),
                "DateTime" => (T)(object)DateTime.Parse(PreferenceValue),
                "JSON" => System.Text.Json.JsonSerializer.Deserialize<T>(PreferenceValue),
                _ => (T)(object)PreferenceValue
            };
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// �������ͻ���ֵ
    /// </summary>
    public void SetTypedValue<T>(T? value)
    {
        if (value == null)
        {
            PreferenceValue = string.Empty;
            return;
        }

        PreferenceValue = value switch
        {
            bool b => b.ToString(),
            int i => i.ToString(),
            double d => d.ToString(),
            DateTime dt => dt.ToString("O"), // ISO 8601 format
            string s => s,
            _ => System.Text.Json.JsonSerializer.Serialize(value)
        };

        ValueType = value switch
        {
            bool => "Boolean",
            int => "Integer",
            double => "Double",
            DateTime => "DateTime",
            string => "String",
            _ => "JSON"
        };

        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// ����ƫ��ֵ
    /// </summary>
    public void UpdateValue(string? value, string? valueType = null)
    {
        PreferenceValue = value ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(valueType))
        {
            ValueType = valueType;
        }
        LastUpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description ?? string.Empty;
        LastUpdatedTime = DateTime.UtcNow;
    }
}