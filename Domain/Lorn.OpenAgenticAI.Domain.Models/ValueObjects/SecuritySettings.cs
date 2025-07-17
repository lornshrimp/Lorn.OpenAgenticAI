using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// ��ȫ����ֵ����
/// </summary>
public class SecuritySettings : ValueObject
{
    public string AuthenticationMethod { get; private set; } = string.Empty;
    public int SessionTimeoutMinutes { get; private set; } = 30;
    public bool RequireTwoFactor { get; private set; }
    public DateTime PasswordLastChanged { get; private set; } = DateTime.UtcNow;
    public Dictionary<string, string> AdditionalSettings { get; private set; } = new();

    public SecuritySettings()
    {
        // Ĭ�Ϲ��캯��ʹ�����Գ�ʼ������Ĭ��ֵ
    }

    public SecuritySettings(
        string authenticationMethod,
        int sessionTimeoutMinutes,
        bool requireTwoFactor,
        DateTime passwordLastChanged,
        Dictionary<string, string>? additionalSettings = null)
    {
        AuthenticationMethod = authenticationMethod ?? throw new ArgumentNullException(nameof(authenticationMethod));
        SessionTimeoutMinutes = sessionTimeoutMinutes > 0 ? sessionTimeoutMinutes : throw new ArgumentException("Session timeout must be positive", nameof(sessionTimeoutMinutes));
        RequireTwoFactor = requireTwoFactor;
        PasswordLastChanged = passwordLastChanged;
        AdditionalSettings = additionalSettings ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// ��֤��ȫ�����Ƿ���Ч
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(AuthenticationMethod) && 
               SessionTimeoutMinutes > 0 && 
               SessionTimeoutMinutes <= 1440; // ���24Сʱ
    }

    /// <summary>
    /// ����Ƿ���Ҫ��������
    /// </summary>
    public bool RequiresPasswordChange()
    {
        var daysSinceLastChange = (DateTime.UtcNow - PasswordLastChanged).TotalDays;
        return daysSinceLastChange > 90; // 90����Ҫ��������
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return AuthenticationMethod;
        yield return SessionTimeoutMinutes;
        yield return RequireTwoFactor;
        yield return PasswordLastChanged;
        
        foreach (var kvp in AdditionalSettings)
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
    }
}