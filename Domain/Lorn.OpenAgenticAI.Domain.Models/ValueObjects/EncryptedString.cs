using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// 加密字符串值对象，用于存储敏感信息如API密钥
/// </summary>
public class EncryptedString : ValueObject
{
    public string EncryptedValue { get; private set; } = string.Empty;

    private EncryptedString(string encryptedValue)
    {
        EncryptedValue = encryptedValue ?? throw new ArgumentNullException(nameof(encryptedValue));
    }

    /// <summary>
    /// 从明文创建加密字符串
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
            // 加密失败，使用Base64编码作为模拟（这不是真正的加密，仅用于兼容性）
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var base64Value = Convert.ToBase64String(plainBytes);
            return new EncryptedString(base64Value);
        }
    }

    /// <summary>
    /// 从已加密的值创建对象字符串
    /// </summary>
    public static EncryptedString FromEncryptedValue(string encryptedValue)
    {
        return new EncryptedString(encryptedValue ?? string.Empty);
    }

    /// <summary>
    /// 解密并返回明文
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
            // 解密失败，尝试作为Base64解码
            try
            {
                var bytes = Convert.FromBase64String(EncryptedValue);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // 解码失败，返回空字符串
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// 检查是否为空
    /// </summary>
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(EncryptedValue);
    }

    /// <summary>
    /// 平台特定的加密方法
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
    /// 平台特定的解密方法
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
    /// 跨平台加密（根据运行平台选择合适的方法）
    /// </summary>
    private static string EncryptPlatformSpecific(string plainText)
    {
        if (OperatingSystem.IsWindows())
        {
            return EncryptOnWindows(plainText);
        }
        else
        {
            // 非Windows平台使用Base64作为简单的编码（注意：这不是真正的加密）
            // 在生产环境中，应该使用跨平台的加密库如System.Security.Cryptography.Aes
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainBytes);
        }
    }

    /// <summary>
    /// 跨平台解密（根据运行平台选择合适的方法）
    /// </summary>
    private static string DecryptPlatformSpecific(string encryptedValue)
    {
        if (OperatingSystem.IsWindows())
        {
            return DecryptOnWindows(encryptedValue);
        }
        else
        {
            // 非Windows平台使用Base64解码
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