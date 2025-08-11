using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Domain.Contracts;

namespace Lorn.OpenAgenticAI.Infrastructure.Security;

/// <summary>
/// 加密和安全服务实现，提供基于AES-256的数据加密、会话令牌管理和数据完整性校验
/// </summary>
public class CryptoService : ICryptoService
{
    private readonly ILogger<CryptoService> _logger;
    private const int KeySize = 256; // AES-256
    private const int IvSize = 128; // AES IV size
    private const int SaltSize = 32; // 盐值大小
    private const int KeyDerivationIterations = 100000; // PBKDF2迭代次数

    public CryptoService(ILogger<CryptoService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 基于机器ID生成加密密钥
    /// </summary>
    public byte[] DeriveKeyFromMachineId(string machineId, string salt)
    {
        if (string.IsNullOrWhiteSpace(machineId))
            throw new ArgumentException("机器ID不能为空", nameof(machineId));

        if (string.IsNullOrWhiteSpace(salt))
            throw new ArgumentException("盐值不能为空", nameof(salt));

        try
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                machineId,
                Encoding.UTF8.GetBytes(salt),
                KeyDerivationIterations,
                HashAlgorithmName.SHA256);

            var key = pbkdf2.GetBytes(KeySize / 8); // 256 bits = 32 bytes

            _logger.LogDebug("成功基于机器ID派生加密密钥");
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "基于机器ID派生密钥失败");
            throw new CryptographicException("密钥派生失败", ex);
        }
    }

    /// <summary>
    /// 使用AES-256算法加密敏感数据
    /// </summary>
    public string EncryptData(string plainText, byte[] key)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        if (key == null || key.Length != KeySize / 8)
            throw new ArgumentException("密钥长度必须为32字节（256位）", nameof(key));

        try
        {
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // 将IV和加密数据组合
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            var base64Result = Convert.ToBase64String(result);
            _logger.LogDebug("数据加密成功，长度: {Length}", plainText.Length);

            return base64Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据加密失败");
            throw new CryptographicException("数据加密失败", ex);
        }
    }

    /// <summary>
    /// 使用AES-256算法解密数据
    /// </summary>
    public string DecryptData(string encryptedData, byte[] key)
    {
        if (string.IsNullOrEmpty(encryptedData))
            return string.Empty;

        if (key == null || key.Length != KeySize / 8)
            throw new ArgumentException("密钥长度必须为32字节（256位）", nameof(key));

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedData);

            if (encryptedBytes.Length < IvSize / 8)
                throw new ArgumentException("加密数据格式无效");

            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;

            // 提取IV
            var iv = new byte[IvSize / 8];
            Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            // 提取加密数据
            var cipherBytes = new byte[encryptedBytes.Length - iv.Length];
            Array.Copy(encryptedBytes, iv.Length, cipherBytes, 0, cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            var result = Encoding.UTF8.GetString(decryptedBytes);
            _logger.LogDebug("数据解密成功");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据解密失败");
            throw new CryptographicException("数据解密失败", ex);
        }
    }

    /// <summary>
    /// 生成安全的会话令牌
    /// </summary>
    public string GenerateSessionToken(string userId, string machineId, DateTime expirationTime)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("用户ID不能为空", nameof(userId));

        if (string.IsNullOrWhiteSpace(machineId))
            throw new ArgumentException("机器ID不能为空", nameof(machineId));

        try
        {
            var tokenData = new SessionTokenData
            {
                UserId = userId,
                MachineId = machineId,
                ExpirationTime = expirationTime.ToUniversalTime(),
                IssuedAt = DateTime.UtcNow,
                TokenId = Guid.NewGuid().ToString()
            };

            var jsonData = JsonSerializer.Serialize(tokenData);
            var tokenBytes = Encoding.UTF8.GetBytes(jsonData);

            // 使用HMAC-SHA256签名
            var salt = GenerateSecureSalt();
            var key = DeriveKeyFromMachineId(machineId, salt);

            using var hmac = new HMACSHA256(key);
            var signature = hmac.ComputeHash(tokenBytes);

            var signedToken = new SignedSessionToken
            {
                Data = Convert.ToBase64String(tokenBytes),
                Signature = Convert.ToBase64String(signature),
                Salt = salt
            };

            var signedTokenJson = JsonSerializer.Serialize(signedToken);
            var result = Convert.ToBase64String(Encoding.UTF8.GetBytes(signedTokenJson));

            _logger.LogDebug("会话令牌生成成功，用户: {UserId}", userId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "会话令牌生成失败，用户: {UserId}", userId);
            throw new CryptographicException("会话令牌生成失败", ex);
        }
    }

    /// <summary>
    /// 验证会话令牌的有效性
    /// </summary>
    public Task<SessionTokenValidationResult> ValidateSessionTokenAsync(string token, string userId, string machineId)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(new SessionTokenValidationResult
            {
                IsValid = false,
                FailureReason = "令牌为空"
            });
        }

        try
        {
            // 解码令牌
            var tokenBytes = Convert.FromBase64String(token);
            var tokenJson = Encoding.UTF8.GetString(tokenBytes);
            var signedToken = JsonSerializer.Deserialize<SignedSessionToken>(tokenJson);

            if (signedToken == null)
            {
                return Task.FromResult(new SessionTokenValidationResult
                {
                    IsValid = false,
                    FailureReason = "令牌格式无效"
                });
            }

            // 验证签名
            var dataBytes = Convert.FromBase64String(signedToken.Data);
            var expectedSignature = Convert.FromBase64String(signedToken.Signature);

            var key = DeriveKeyFromMachineId(machineId, signedToken.Salt);
            using var hmac = new HMACSHA256(key);
            var actualSignature = hmac.ComputeHash(dataBytes);

            if (!actualSignature.SequenceEqual(expectedSignature))
            {
                _logger.LogWarning("会话令牌签名验证失败，用户: {UserId}", userId);
                return Task.FromResult(new SessionTokenValidationResult
                {
                    IsValid = false,
                    FailureReason = "令牌签名无效"
                });
            }

            // 解析令牌数据
            var dataJson = Encoding.UTF8.GetString(dataBytes);
            var tokenData = JsonSerializer.Deserialize<SessionTokenData>(dataJson);

            if (tokenData == null)
            {
                return Task.FromResult(new SessionTokenValidationResult
                {
                    IsValid = false,
                    FailureReason = "令牌数据无效"
                });
            }

            // 验证用户ID和机器ID
            if (tokenData.UserId != userId || tokenData.MachineId != machineId)
            {
                _logger.LogWarning("会话令牌用户或机器ID不匹配，期望用户: {ExpectedUserId}, 实际用户: {ActualUserId}",
                    userId, tokenData.UserId);
                return Task.FromResult(new SessionTokenValidationResult
                {
                    IsValid = false,
                    FailureReason = "用户或机器ID不匹配"
                });
            }

            // 检查过期时间
            var isExpired = DateTime.UtcNow > tokenData.ExpirationTime;

            var result = new SessionTokenValidationResult
            {
                IsValid = !isExpired,
                IsExpired = isExpired,
                UserId = tokenData.UserId,
                MachineId = tokenData.MachineId,
                ExpirationTime = tokenData.ExpirationTime,
                FailureReason = isExpired ? "令牌已过期" : null
            };

            _logger.LogDebug("会话令牌验证完成，用户: {UserId}, 有效: {IsValid}, 过期: {IsExpired}",
                userId, result.IsValid, result.IsExpired);

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "会话令牌验证异常，用户: {UserId}", userId);
            return Task.FromResult(new SessionTokenValidationResult
            {
                IsValid = false,
                FailureReason = $"令牌验证异常: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// 计算数据的完整性校验值
    /// </summary>
    public string ComputeDataIntegrityHash(string data, byte[] key)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;

        if (key == null || key.Length == 0)
            throw new ArgumentException("校验密钥不能为空", nameof(key));

        try
        {
            using var hmac = new HMACSHA256(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var hashBytes = hmac.ComputeHash(dataBytes);

            var result = Convert.ToBase64String(hashBytes);
            _logger.LogDebug("数据完整性校验值计算成功");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据完整性校验值计算失败");
            throw new CryptographicException("完整性校验值计算失败", ex);
        }
    }

    /// <summary>
    /// 验证数据完整性
    /// </summary>
    public bool VerifyDataIntegrity(string data, string hash, byte[] key)
    {
        if (string.IsNullOrEmpty(data) && string.IsNullOrEmpty(hash))
            return true;

        if (string.IsNullOrEmpty(hash))
            return false;

        try
        {
            var expectedHash = ComputeDataIntegrityHash(data, key);
            var result = string.Equals(expectedHash, hash, StringComparison.Ordinal);

            _logger.LogDebug("数据完整性验证结果: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据完整性验证失败");
            return false;
        }
    }

    /// <summary>
    /// 安全清理内存中的敏感数据
    /// </summary>
    public void SecureClearMemory(byte[] sensitiveData)
    {
        if (sensitiveData == null || sensitiveData.Length == 0)
            return;

        try
        {
            // 使用随机数据覆盖敏感数据
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(sensitiveData);

            // 再次用零覆盖
            Array.Clear(sensitiveData, 0, sensitiveData.Length);

            _logger.LogDebug("敏感数据内存清理完成，长度: {Length}", sensitiveData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "敏感数据内存清理失败");
        }
    }

    /// <summary>
    /// 生成安全的随机盐值
    /// </summary>
    public string GenerateSecureSalt(int length = 32)
    {
        if (length <= 0)
            throw new ArgumentException("盐值长度必须大于0", nameof(length));

        try
        {
            using var rng = RandomNumberGenerator.Create();
            var saltBytes = new byte[length];
            rng.GetBytes(saltBytes);

            var result = Convert.ToBase64String(saltBytes);
            _logger.LogDebug("安全盐值生成成功，长度: {Length}", length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安全盐值生成失败");
            throw new CryptographicException("盐值生成失败", ex);
        }
    }

    /// <summary>
    /// 会话令牌数据结构
    /// </summary>
    private class SessionTokenData
    {
        public string UserId { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public DateTime ExpirationTime { get; set; }
        public DateTime IssuedAt { get; set; }
        public string TokenId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 签名的会话令牌结构
    /// </summary>
    private class SignedSessionToken
    {
        public string Data { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
    }
}