using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// �����ַ���ֵ�������ڴ洢������Ϣ��API��Կ
/// </summary>
public class EncryptedString : ValueObject
{
    public string EncryptedValue { get; private set; } = string.Empty;

    private EncryptedString(string encryptedValue)
    {
        EncryptedValue = encryptedValue ?? throw new ArgumentNullException(nameof(encryptedValue));
    }

    /// <summary>
    /// �����Ĵ��������ַ���
    /// </summary>
    public static EncryptedString FromPlainText(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return new EncryptedString(string.Empty);

        try
        {
            var encryptedValue = EncryptPlatformSpecific(plainText);
            return new EncryptedString(encryptedValue);
        }
        catch (Exception)
        {
            // ����ʧ�ܣ�ʹ��Base64������Ϊģ�⣨�ⲻ�������ļ��ܣ������ڼ����ԣ�
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var base64Value = Convert.ToBase64String(plainBytes);
            return new EncryptedString(base64Value);
        }
    }

    /// <summary>
    /// ���Ѽ��ܵ�ֵ���������ַ���
    /// </summary>
    public static EncryptedString FromEncryptedValue(string encryptedValue)
    {
        return new EncryptedString(encryptedValue ?? string.Empty);
    }

    /// <summary>
    /// ���ܲ���������
    /// </summary>
    public string Decrypt()
    {
        if (string.IsNullOrEmpty(EncryptedValue))
            return string.Empty;

        try
        {
            return DecryptPlatformSpecific(EncryptedValue);
        }
        catch (Exception)
        {
            // ����ʧ�ܣ�������ΪBase64����
            try
            {
                var bytes = Convert.FromBase64String(EncryptedValue);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // ����ʧ�ܣ����ؿ��ַ���
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// ����Ƿ�Ϊ��
    /// </summary>
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(EncryptedValue);
    }

    /// <summary>
    /// ƽ̨�ض��ļ��ܷ���
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static string EncryptOnWindows(string plainText)
    {
        var encryptedBytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plainText),
            null,
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// ƽ̨�ض��Ľ��ܷ���
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static string DecryptOnWindows(string encryptedValue)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedValue);
        var decryptedBytes = ProtectedData.Unprotect(
            encryptedBytes,
            null,
            DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    /// <summary>
    /// ��ƽ̨���ܣ���������ƽ̨ѡ����ʵķ�����
    /// </summary>
    private static string EncryptPlatformSpecific(string plainText)
    {
        if (OperatingSystem.IsWindows())
        {
            return EncryptOnWindows(plainText);
        }
        else
        {
            // ��Windowsƽ̨ʹ��Base64��Ϊ�򵥵ı��루ע�⣺�ⲻ�������ļ��ܣ�
            // �����������У�Ӧ��ʹ�ÿ�ƽ̨�ļ��ܿ���System.Security.Cryptography.Aes
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainBytes);
        }
    }

    /// <summary>
    /// ��ƽ̨���ܣ���������ƽ̨ѡ����ʵķ�����
    /// </summary>
    private static string DecryptPlatformSpecific(string encryptedValue)
    {
        if (OperatingSystem.IsWindows())
        {
            return DecryptOnWindows(encryptedValue);
        }
        else
        {
            // ��Windowsƽ̨ʹ��Base64����
            var bytes = Convert.FromBase64String(encryptedValue);
            return Encoding.UTF8.GetString(bytes);
        }
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return EncryptedValue;
    }

    public override string ToString()
    {
        return IsEmpty() ? "[Empty]" : "[Encrypted]";
    }
}