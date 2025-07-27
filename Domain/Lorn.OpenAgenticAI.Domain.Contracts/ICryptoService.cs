using System;
using System.Threading.Tasks;

namespace Lorn.OpenAgenticAI.Domain.Contracts
{
    /// <summary>
    /// 加密和安全服务接口，提供数据加密、解密、会话令牌管理和数据完整性校验功能
    /// </summary>
    public interface ICryptoService
    {
        /// <summary>
        /// 基于机器ID生成加密密钥
        /// </summary>
        /// <param name="machineId">机器标识符</param>
        /// <param name="salt">盐值，用于增强密钥安全性</param>
        /// <returns>派生的加密密钥</returns>
        byte[] DeriveKeyFromMachineId(string machineId, string salt);

        /// <summary>
        /// 使用AES-256算法加密敏感数据
        /// </summary>
        /// <param name="plainText">待加密的明文数据</param>
        /// <param name="key">加密密钥</param>
        /// <returns>加密后的数据（包含IV）</returns>
        string EncryptData(string plainText, byte[] key);

        /// <summary>
        /// 使用AES-256算法解密数据
        /// </summary>
        /// <param name="encryptedData">加密的数据（包含IV）</param>
        /// <param name="key">解密密钥</param>
        /// <returns>解密后的明文数据</returns>
        string DecryptData(string encryptedData, byte[] key);

        /// <summary>
        /// 生成安全的会话令牌
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="machineId">机器ID</param>
        /// <param name="expirationTime">过期时间</param>
        /// <returns>会话令牌</returns>
        string GenerateSessionToken(string userId, string machineId, DateTime expirationTime);

        /// <summary>
        /// 验证会话令牌的有效性
        /// </summary>
        /// <param name="token">会话令牌</param>
        /// <param name="userId">用户ID</param>
        /// <param name="machineId">机器ID</param>
        /// <returns>令牌验证结果</returns>
        Task<SessionTokenValidationResult> ValidateSessionTokenAsync(string token, string userId, string machineId);

        /// <summary>
        /// 计算数据的完整性校验值
        /// </summary>
        /// <param name="data">待校验的数据</param>
        /// <param name="key">校验密钥</param>
        /// <returns>HMAC校验值</returns>
        string ComputeDataIntegrityHash(string data, byte[] key);

        /// <summary>
        /// 验证数据完整性
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="hash">校验值</param>
        /// <param name="key">校验密钥</param>
        /// <returns>完整性验证结果</returns>
        bool VerifyDataIntegrity(string data, string hash, byte[] key);

        /// <summary>
        /// 安全清理内存中的敏感数据
        /// </summary>
        /// <param name="sensitiveData">敏感数据数组</param>
        void SecureClearMemory(byte[] sensitiveData);

        /// <summary>
        /// 生成安全的随机盐值
        /// </summary>
        /// <param name="length">盐值长度</param>
        /// <returns>随机盐值</returns>
        string GenerateSecureSalt(int length = 32);
    }

    /// <summary>
    /// 会话令牌验证结果
    /// </summary>
    public class SessionTokenValidationResult
    {
        /// <summary>
        /// 验证是否成功
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 令牌是否已过期
        /// </summary>
        public bool IsExpired { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// 机器ID
        /// </summary>
        public string? MachineId { get; set; }

        /// <summary>
        /// 令牌过期时间
        /// </summary>
        public DateTime? ExpirationTime { get; set; }

        /// <summary>
        /// 验证失败的原因
        /// </summary>
        public string? FailureReason { get; set; }
    }
}