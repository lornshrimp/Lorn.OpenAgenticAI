using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Infrastructure.Security;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Security;

/// <summary>
/// CryptoService 单元测试类
/// </summary>
public class CryptoServiceTests
{
    private readonly Mock<ILogger<CryptoService>> _mockLogger;
    private readonly CryptoService _cryptoService;

    public CryptoServiceTests()
    {
        _mockLogger = new Mock<ILogger<CryptoService>>();
        _cryptoService = new CryptoService(_mockLogger.Object);
    }

    #region 密钥派生测试

    [Fact]
    public void DeriveKeyFromMachineId_WithValidInputs_ShouldReturnConsistentKey()
    {
        // Arrange
        var machineId = "TEST-MACHINE-001";
        var salt = "test-salt-value";

        // Act
        var key1 = _cryptoService.DeriveKeyFromMachineId(machineId, salt);
        var key2 = _cryptoService.DeriveKeyFromMachineId(machineId, salt);

        // Assert
        key1.Should().NotBeNull();
        key1.Length.Should().Be(32); // 256 bits = 32 bytes
        key1.Should().BeEquivalentTo(key2); // 相同输入应产生相同密钥
    }

    [Fact]
    public void DeriveKeyFromMachineId_WithDifferentMachineIds_ShouldReturnDifferentKeys()
    {
        // Arrange
        var machineId1 = "TEST-MACHINE-001";
        var machineId2 = "TEST-MACHINE-002";
        var salt = "test-salt-value";

        // Act
        var key1 = _cryptoService.DeriveKeyFromMachineId(machineId1, salt);
        var key2 = _cryptoService.DeriveKeyFromMachineId(machineId2, salt);

        // Assert
        key1.Should().NotBeEquivalentTo(key2); // 不同机器ID应产生不同密钥
    }

    [Fact]
    public void DeriveKeyFromMachineId_WithDifferentSalts_ShouldReturnDifferentKeys()
    {
        // Arrange
        var machineId = "TEST-MACHINE-001";
        var salt1 = "test-salt-value-1";
        var salt2 = "test-salt-value-2";

        // Act
        var key1 = _cryptoService.DeriveKeyFromMachineId(machineId, salt1);
        var key2 = _cryptoService.DeriveKeyFromMachineId(machineId, salt2);

        // Assert
        key1.Should().NotBeEquivalentTo(key2); // 不同盐值应产生不同密钥
    }

    [Theory]
    [InlineData(null, "salt")]
    [InlineData("", "salt")]
    [InlineData("   ", "salt")]
    [InlineData("machine", null)]
    [InlineData("machine", "")]
    [InlineData("machine", "   ")]
    public void DeriveKeyFromMachineId_WithInvalidInputs_ShouldThrowArgumentException(string machineId, string salt)
    {
        // Act & Assert
        var action = () => _cryptoService.DeriveKeyFromMachineId(machineId, salt);
        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region 数据加密解密测试

    [Fact]
    public void EncryptData_WithValidData_ShouldReturnEncryptedString()
    {
        // Arrange
        var plainText = "这是需要加密的敏感数据";
        var key = GenerateTestKey();

        // Act
        var encryptedData = _cryptoService.EncryptData(plainText, key);

        // Assert
        encryptedData.Should().NotBeNullOrEmpty();
        encryptedData.Should().NotBe(plainText);

        // 验证是否为有效的Base64字符串
        var action = () => Convert.FromBase64String(encryptedData);
        action.Should().NotThrow();
    }

    [Fact]
    public void DecryptData_WithValidEncryptedData_ShouldReturnOriginalText()
    {
        // Arrange
        var plainText = "这是需要加密的敏感数据";
        var key = GenerateTestKey();
        var encryptedData = _cryptoService.EncryptData(plainText, key);

        // Act
        var decryptedText = _cryptoService.DecryptData(encryptedData, key);

        // Assert
        decryptedText.Should().Be(plainText);
    }

    [Fact]
    public void EncryptData_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var plainText = "";
        var key = GenerateTestKey();

        // Act
        var encryptedData = _cryptoService.EncryptData(plainText, key);

        // Assert
        encryptedData.Should().Be("");
    }

    [Fact]
    public void DecryptData_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var encryptedData = "";
        var key = GenerateTestKey();

        // Act
        var decryptedText = _cryptoService.DecryptData(encryptedData, key);

        // Assert
        decryptedText.Should().Be("");
    }

    [Fact]
    public void EncryptData_WithInvalidKey_ShouldThrowArgumentException()
    {
        // Arrange
        var plainText = "test data";
        var invalidKey = new byte[16]; // 错误的密钥长度

        // Act & Assert
        var action = () => _cryptoService.EncryptData(plainText, invalidKey);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DecryptData_WithInvalidKey_ShouldThrowArgumentException()
    {
        // Arrange
        var encryptedData = "dGVzdA=="; // 有效的Base64
        var invalidKey = new byte[16]; // 错误的密钥长度

        // Act & Assert
        var action = () => _cryptoService.DecryptData(encryptedData, invalidKey);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DecryptData_WithWrongKey_ShouldThrowCryptographicException()
    {
        // Arrange
        var plainText = "test data";
        var key1 = GenerateTestKey();
        var key2 = GenerateTestKey();
        var encryptedData = _cryptoService.EncryptData(plainText, key1);

        // Act & Assert
        var action = () => _cryptoService.DecryptData(encryptedData, key2);
        action.Should().Throw<CryptographicException>();
    }

    #endregion

    #region 会话令牌测试

    [Fact]
    public void GenerateSessionToken_WithValidInputs_ShouldReturnValidToken()
    {
        // Arrange
        var userId = "user123";
        var machineId = "machine456";
        var expirationTime = DateTime.UtcNow.AddHours(1);

        // Act
        var token = _cryptoService.GenerateSessionToken(userId, machineId, expirationTime);

        // Assert
        token.Should().NotBeNullOrEmpty();

        // 验证是否为有效的Base64字符串
        var action = () => Convert.FromBase64String(token);
        action.Should().NotThrow();
    }

    [Fact]
    public void GenerateSessionToken_WithSameInputs_ShouldReturnDifferentTokens()
    {
        // Arrange
        var userId = "user123";
        var machineId = "machine456";
        var expirationTime = DateTime.UtcNow.AddHours(1);

        // Act
        var token1 = _cryptoService.GenerateSessionToken(userId, machineId, expirationTime);
        var token2 = _cryptoService.GenerateSessionToken(userId, machineId, expirationTime);

        // Assert
        token1.Should().NotBe(token2); // 每次生成的令牌应该不同（因为包含TokenId）
    }

    [Theory]
    [InlineData(null, "machine")]
    [InlineData("", "machine")]
    [InlineData("   ", "machine")]
    [InlineData("user", null)]
    [InlineData("user", "")]
    [InlineData("user", "   ")]
    public void GenerateSessionToken_WithInvalidInputs_ShouldThrowArgumentException(string userId, string machineId)
    {
        // Arrange
        var expirationTime = DateTime.UtcNow.AddHours(1);

        // Act & Assert
        var action = () => _cryptoService.GenerateSessionToken(userId, machineId, expirationTime);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task ValidateSessionTokenAsync_WithValidToken_ShouldReturnValidResult()
    {
        // Arrange
        var userId = "user123";
        var machineId = "machine456";
        var expirationTime = DateTime.UtcNow.AddHours(1);
        var token = _cryptoService.GenerateSessionToken(userId, machineId, expirationTime);

        // Act
        var result = await _cryptoService.ValidateSessionTokenAsync(token, userId, machineId);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.IsExpired.Should().BeFalse();
        result.UserId.Should().Be(userId);
        result.MachineId.Should().Be(machineId);
        result.ExpirationTime.Should().BeCloseTo(expirationTime, TimeSpan.FromSeconds(1));
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task ValidateSessionTokenAsync_WithExpiredToken_ShouldReturnExpiredResult()
    {
        // Arrange
        var userId = "user123";
        var machineId = "machine456";
        var expirationTime = DateTime.UtcNow.AddMilliseconds(-100); // 已过期
        var token = _cryptoService.GenerateSessionToken(userId, machineId, expirationTime);

        // Act
        var result = await _cryptoService.ValidateSessionTokenAsync(token, userId, machineId);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.IsExpired.Should().BeTrue();
        result.FailureReason.Should().Be("令牌已过期");
    }

    [Fact]
    public async Task ValidateSessionTokenAsync_WithWrongUserId_ShouldReturnInvalidResult()
    {
        // Arrange
        var userId = "user123";
        var wrongUserId = "user456";
        var machineId = "machine456";
        var expirationTime = DateTime.UtcNow.AddHours(1);
        var token = _cryptoService.GenerateSessionToken(userId, machineId, expirationTime);

        // Act
        var result = await _cryptoService.ValidateSessionTokenAsync(token, wrongUserId, machineId);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be("用户或机器ID不匹配");
    }

    [Fact]
    public async Task ValidateSessionTokenAsync_WithWrongMachineId_ShouldReturnInvalidResult()
    {
        // Arrange
        var userId = "user123";
        var machineId = "machine456";
        var wrongMachineId = "machine789";
        var expirationTime = DateTime.UtcNow.AddHours(1);
        var token = _cryptoService.GenerateSessionToken(userId, machineId, expirationTime);

        // Act
        var result = await _cryptoService.ValidateSessionTokenAsync(token, userId, wrongMachineId);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be("令牌签名无效"); // 机器ID不匹配会导致签名验证失败
    }

    [Fact]
    public async Task ValidateSessionTokenAsync_WithEmptyToken_ShouldReturnInvalidResult()
    {
        // Arrange
        var token = "";
        var userId = "user123";
        var machineId = "machine456";

        // Act
        var result = await _cryptoService.ValidateSessionTokenAsync(token, userId, machineId);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be("令牌为空");
    }

    [Fact]
    public async Task ValidateSessionTokenAsync_WithInvalidToken_ShouldReturnInvalidResult()
    {
        // Arrange
        var token = "invalid-token";
        var userId = "user123";
        var machineId = "machine456";

        // Act
        var result = await _cryptoService.ValidateSessionTokenAsync(token, userId, machineId);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Contain("令牌验证异常");
    }

    #endregion

    #region 数据完整性校验测试

    [Fact]
    public void ComputeDataIntegrityHash_WithValidData_ShouldReturnConsistentHash()
    {
        // Arrange
        var data = "这是需要校验完整性的数据";
        var key = GenerateTestKey();

        // Act
        var hash1 = _cryptoService.ComputeDataIntegrityHash(data, key);
        var hash2 = _cryptoService.ComputeDataIntegrityHash(data, key);

        // Assert
        hash1.Should().NotBeNullOrEmpty();
        hash1.Should().Be(hash2); // 相同数据和密钥应产生相同哈希值

        // 验证是否为有效的Base64字符串
        var action = () => Convert.FromBase64String(hash1);
        action.Should().NotThrow();
    }

    [Fact]
    public void ComputeDataIntegrityHash_WithDifferentData_ShouldReturnDifferentHashes()
    {
        // Arrange
        var data1 = "数据1";
        var data2 = "数据2";
        var key = GenerateTestKey();

        // Act
        var hash1 = _cryptoService.ComputeDataIntegrityHash(data1, key);
        var hash2 = _cryptoService.ComputeDataIntegrityHash(data2, key);

        // Assert
        hash1.Should().NotBe(hash2); // 不同数据应产生不同哈希值
    }

    [Fact]
    public void ComputeDataIntegrityHash_WithDifferentKeys_ShouldReturnDifferentHashes()
    {
        // Arrange
        var data = "测试数据";
        var key1 = GenerateTestKey();
        var key2 = GenerateTestKey();

        // Act
        var hash1 = _cryptoService.ComputeDataIntegrityHash(data, key1);
        var hash2 = _cryptoService.ComputeDataIntegrityHash(data, key2);

        // Assert
        hash1.Should().NotBe(hash2); // 不同密钥应产生不同哈希值
    }

    [Fact]
    public void ComputeDataIntegrityHash_WithEmptyData_ShouldReturnEmptyString()
    {
        // Arrange
        var data = "";
        var key = GenerateTestKey();

        // Act
        var hash = _cryptoService.ComputeDataIntegrityHash(data, key);

        // Assert
        hash.Should().Be("");
    }

    [Fact]
    public void ComputeDataIntegrityHash_WithNullKey_ShouldThrowArgumentException()
    {
        // Arrange
        var data = "test data";
        byte[] key = null;

        // Act & Assert
        var action = () => _cryptoService.ComputeDataIntegrityHash(data, key);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ComputeDataIntegrityHash_WithEmptyKey_ShouldThrowArgumentException()
    {
        // Arrange
        var data = "test data";
        var key = new byte[0];

        // Act & Assert
        var action = () => _cryptoService.ComputeDataIntegrityHash(data, key);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VerifyDataIntegrity_WithValidHash_ShouldReturnTrue()
    {
        // Arrange
        var data = "这是需要验证完整性的数据";
        var key = GenerateTestKey();
        var hash = _cryptoService.ComputeDataIntegrityHash(data, key);

        // Act
        var result = _cryptoService.VerifyDataIntegrity(data, hash, key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyDataIntegrity_WithInvalidHash_ShouldReturnFalse()
    {
        // Arrange
        var data = "测试数据";
        var key = GenerateTestKey();
        var invalidHash = "invalid-hash-value";

        // Act
        var result = _cryptoService.VerifyDataIntegrity(data, invalidHash, key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyDataIntegrity_WithModifiedData_ShouldReturnFalse()
    {
        // Arrange
        var originalData = "原始数据";
        var modifiedData = "修改后的数据";
        var key = GenerateTestKey();
        var hash = _cryptoService.ComputeDataIntegrityHash(originalData, key);

        // Act
        var result = _cryptoService.VerifyDataIntegrity(modifiedData, hash, key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyDataIntegrity_WithEmptyDataAndEmptyHash_ShouldReturnTrue()
    {
        // Arrange
        var data = "";
        var hash = "";
        var key = GenerateTestKey();

        // Act
        var result = _cryptoService.VerifyDataIntegrity(data, hash, key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyDataIntegrity_WithDataButEmptyHash_ShouldReturnFalse()
    {
        // Arrange
        var data = "test data";
        var hash = "";
        var key = GenerateTestKey();

        // Act
        var result = _cryptoService.VerifyDataIntegrity(data, hash, key);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region 安全内存清理测试

    [Fact]
    public void SecureClearMemory_WithValidData_ShouldClearMemory()
    {
        // Arrange
        var sensitiveData = Encoding.UTF8.GetBytes("敏感数据内容");
        var originalData = new byte[sensitiveData.Length];
        Array.Copy(sensitiveData, originalData, sensitiveData.Length);

        // Act
        _cryptoService.SecureClearMemory(sensitiveData);

        // Assert
        sensitiveData.Should().NotBeEquivalentTo(originalData); // 数据应该被清理
        sensitiveData.All(b => b == 0).Should().BeTrue(); // 最终应该全部为零
    }

    [Fact]
    public void SecureClearMemory_WithNullData_ShouldNotThrow()
    {
        // Arrange
        byte[] sensitiveData = null;

        // Act & Assert
        var action = () => _cryptoService.SecureClearMemory(sensitiveData);
        action.Should().NotThrow();
    }

    [Fact]
    public void SecureClearMemory_WithEmptyData_ShouldNotThrow()
    {
        // Arrange
        var sensitiveData = new byte[0];

        // Act & Assert
        var action = () => _cryptoService.SecureClearMemory(sensitiveData);
        action.Should().NotThrow();
    }

    #endregion

    #region 安全盐值生成测试

    [Fact]
    public void GenerateSecureSalt_WithDefaultLength_ShouldReturnValidSalt()
    {
        // Act
        var salt = _cryptoService.GenerateSecureSalt();

        // Assert
        salt.Should().NotBeNullOrEmpty();

        // 验证是否为有效的Base64字符串
        var action = () => Convert.FromBase64String(salt);
        action.Should().NotThrow();

        // 验证长度（Base64编码后的长度会比原始字节数组长）
        var saltBytes = Convert.FromBase64String(salt);
        saltBytes.Length.Should().Be(32); // 默认长度
    }

    [Fact]
    public void GenerateSecureSalt_WithCustomLength_ShouldReturnCorrectLength()
    {
        // Arrange
        var length = 16;

        // Act
        var salt = _cryptoService.GenerateSecureSalt(length);

        // Assert
        salt.Should().NotBeNullOrEmpty();
        var saltBytes = Convert.FromBase64String(salt);
        saltBytes.Length.Should().Be(length);
    }

    [Fact]
    public void GenerateSecureSalt_MultipleCalls_ShouldReturnDifferentSalts()
    {
        // Act
        var salt1 = _cryptoService.GenerateSecureSalt();
        var salt2 = _cryptoService.GenerateSecureSalt();

        // Assert
        salt1.Should().NotBe(salt2); // 每次生成的盐值应该不同
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void GenerateSecureSalt_WithInvalidLength_ShouldThrowArgumentException(int length)
    {
        // Act & Assert
        var action = () => _cryptoService.GenerateSecureSalt(length);
        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region 性能和安全边界测试

    [Fact]
    public void EncryptDecrypt_WithLargeData_ShouldHandleCorrectly()
    {
        // Arrange
        var largeData = new string('A', 10000); // 10KB数据
        var key = GenerateTestKey();

        // Act
        var encrypted = _cryptoService.EncryptData(largeData, key);
        var decrypted = _cryptoService.DecryptData(encrypted, key);

        // Assert
        decrypted.Should().Be(largeData);
    }

    [Fact]
    public void KeyDerivation_WithLongMachineId_ShouldHandleCorrectly()
    {
        // Arrange
        var longMachineId = new string('M', 1000); // 很长的机器ID
        var salt = "test-salt";

        // Act
        var key1 = _cryptoService.DeriveKeyFromMachineId(longMachineId, salt);
        var key2 = _cryptoService.DeriveKeyFromMachineId(longMachineId, salt);

        // Assert
        key1.Should().BeEquivalentTo(key2);
        key1.Length.Should().Be(32);
    }

    [Fact]
    public async Task SessionToken_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var userId = "用户123";
        var machineId = "机器456";
        var expirationTime = DateTime.UtcNow.AddHours(1);

        // Act
        var token = _cryptoService.GenerateSessionToken(userId, machineId, expirationTime);
        var result = await _cryptoService.ValidateSessionTokenAsync(token, userId, machineId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be(userId);
        result.MachineId.Should().Be(machineId);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 生成测试用的32字节密钥
    /// </summary>
    private byte[] GenerateTestKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var key = new byte[32]; // 256 bits
        rng.GetBytes(key);
        return key;
    }

    #endregion
}