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
/// 基于业务需求验证加密和安全服务的功能正确性
/// 
/// 业务需求覆盖：
/// - 需求5.1: 使用AES-256加密存储所有敏感信息
/// - 需求5.2: 基于机器ID派生加密密钥
/// - 需求5.6: 数据完整性校验功能
/// - 需求5.7: 会话令牌生成和验证机制
/// - 需求1.2: 基于机器ID生成用户标识和会话令牌
/// - 需求1.5: 在后台维护用户会话状态
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

    #region 基于机器ID的密钥派生测试 - 验证需求5.2

    [Fact]
    public void DeriveKeyFromMachineId_WhenUserFirstStartsApplication_ShouldGenerateConsistentEncryptionKey()
    {
        // Arrange - 基于业务需求：用户首次启动应用时需要基于机器ID生成加密密钥
        var machineId = CreateBusinessMachineId(); // 模拟真实的机器ID格式
        var salt = CreateBusinessSalt(); // 模拟业务场景中的盐值

        // Act - 模拟系统为新用户生成密钥的过程
        var key1 = _cryptoService.DeriveKeyFromMachineId(machineId, salt);
        var key2 = _cryptoService.DeriveKeyFromMachineId(machineId, salt);

        // Assert - 根据需求5.2：基于机器ID派生的密钥必须一致且符合AES-256要求
        key1.Should().NotBeNull("根据业务需求，密钥派生不能失败");
        key1.Length.Should().Be(32, "根据需求5.1，必须支持AES-256加密（32字节密钥）");
        key1.Should().BeEquivalentTo(key2, "根据业务需求，相同机器ID和盐值必须产生相同密钥以确保数据一致性");

        // 验证密钥的随机性和安全性
        key1.All(b => b == 0).Should().BeFalse("密钥不应全为零，确保加密安全性");
    }

    [Fact]
    public void DeriveKeyFromMachineId_WhenApplicationRunsOnDifferentMachines_ShouldGenerateUniqueKeysForDataIsolation()
    {
        // Arrange - 基于业务需求：不同机器上的用户数据必须完全隔离
        var machineId1 = CreateBusinessMachineId("DESKTOP-USER001");
        var machineId2 = CreateBusinessMachineId("LAPTOP-USER001");
        var salt = CreateBusinessSalt();

        // Act - 模拟同一用户在不同设备上使用应用
        var key1 = _cryptoService.DeriveKeyFromMachineId(machineId1, salt);
        var key2 = _cryptoService.DeriveKeyFromMachineId(machineId2, salt);

        // Assert - 根据需求6.7：确保用户数据完全隔离和安全
        key1.Should().NotBeEquivalentTo(key2, "根据业务需求，不同机器必须产生不同密钥以确保数据隔离");

        // 验证密钥差异的显著性（安全性要求）
        var differentBytes = key1.Zip(key2, (b1, b2) => b1 != b2).Count(diff => diff);
        differentBytes.Should().BeGreaterThan(16, "密钥差异应该足够显著以确保安全性");
    }

    [Fact]
    public void DeriveKeyFromMachineId_WhenSystemUsesEnhancedSecurity_ShouldGenerateDifferentKeysWithDifferentSalts()
    {
        // Arrange - 基于业务需求：系统使用盐值增强密钥安全性
        var machineId = CreateBusinessMachineId();
        var userSalt = CreateBusinessSalt("user-specific");
        var systemSalt = CreateBusinessSalt("system-wide");

        // Act - 模拟系统为不同用途生成不同密钥的场景
        var userKey = _cryptoService.DeriveKeyFromMachineId(machineId, userSalt);
        var systemKey = _cryptoService.DeriveKeyFromMachineId(machineId, systemSalt);

        // Assert - 根据安全设计：不同盐值必须产生不同密钥以增强安全性
        userKey.Should().NotBeEquivalentTo(systemKey, "根据安全设计，不同盐值必须产生不同密钥");

        // 验证两个密钥都符合AES-256要求
        userKey.Length.Should().Be(32, "用户密钥必须符合AES-256要求");
        systemKey.Length.Should().Be(32, "系统密钥必须符合AES-256要求");
    }

    [Theory]
    [InlineData("", "valid-salt", "机器ID为空字符串时")]
    [InlineData("   ", "valid-salt", "机器ID仅包含空格时")]
    [InlineData("valid-machine", "", "盐值为空字符串时")]
    [InlineData("valid-machine", "   ", "盐值仅包含空格时")]
    public void DeriveKeyFromMachineId_WhenSystemReceivesInvalidInput_ShouldProvideBusinessFriendlyErrorHandling(
        string machineId, string salt, string scenario)
    {
        // Act & Assert - 根据业务需求：系统必须优雅处理无效输入
        var action = () => _cryptoService.DeriveKeyFromMachineId(machineId, salt);
        action.Should().Throw<ArgumentException>($"根据业务规则，{scenario}系统必须拒绝密钥生成")
              .WithMessage("*不能为空*", "错误信息必须清晰说明问题原因");

        // 验证日志记录 - 确保异常情况被正确记录用于问题诊断
        // 注意：在实际业务场景中，这类错误应该被记录但不应暴露给最终用户
    }

    [Fact]
    public void DeriveKeyFromMachineId_WhenMachineIdIsNull_ShouldThrowArgumentException()
    {
        // Arrange - 基于业务需求：系统必须验证机器ID的有效性
        string? nullMachineId = null;
        var validSalt = CreateBusinessSalt();

        // Act & Assert - 根据安全设计：null机器ID必须被拒绝
        var action = () => _cryptoService.DeriveKeyFromMachineId(nullMachineId!, validSalt);
        action.Should().Throw<ArgumentException>("根据安全设计，null机器ID必须被拒绝以防止安全漏洞");
    }

    [Fact]
    public void DeriveKeyFromMachineId_WhenSaltIsNull_ShouldThrowArgumentException()
    {
        // Arrange - 基于业务需求：系统必须验证盐值的有效性
        var validMachineId = CreateBusinessMachineId();
        string? nullSalt = null;

        // Act & Assert - 根据安全设计：null盐值必须被拒绝
        var action = () => _cryptoService.DeriveKeyFromMachineId(validMachineId, nullSalt!);
        action.Should().Throw<ArgumentException>("根据安全设计，null盐值必须被拒绝以防止安全漏洞");
    }

    #endregion

    #region AES-256敏感数据加密测试 - 验证需求5.1

    [Fact]
    public void EncryptData_WhenUserStoresSensitivePreferences_ShouldSecurelyEncryptWithAES256()
    {
        // Arrange - 基于业务需求：用户的敏感偏好设置需要加密存储
        var sensitiveUserData = CreateBusinessSensitiveData();
        var encryptionKey = CreateBusinessEncryptionKey();

        // Act - 模拟系统加密用户敏感数据的过程
        var encryptedData = _cryptoService.EncryptData(sensitiveUserData, encryptionKey);

        // Assert - 根据需求5.1：使用AES-256加密存储所有敏感信息
        encryptedData.Should().NotBeNullOrEmpty("根据业务需求，敏感数据必须被成功加密");
        encryptedData.Should().NotBe(sensitiveUserData, "加密后的数据不能与原始数据相同");

        // 验证加密结果的格式和安全性
        var action = () => Convert.FromBase64String(encryptedData);
        action.Should().NotThrow("加密结果必须是有效的Base64格式以便存储");

        // 验证加密强度 - 确保没有明显的模式
        encryptedData.Should().NotContain(sensitiveUserData.Substring(0, Math.Min(10, sensitiveUserData.Length)),
            "加密结果不应包含原始数据的任何片段");
    }

    [Fact]
    public void DecryptData_WhenUserAccessesStoredPreferences_ShouldCorrectlyRestoreOriginalData()
    {
        // Arrange - 基于业务需求：用户重新启动应用时需要解密并恢复偏好设置
        var originalUserPreferences = CreateBusinessSensitiveData();
        var userEncryptionKey = CreateBusinessEncryptionKey();
        var encryptedPreferences = _cryptoService.EncryptData(originalUserPreferences, userEncryptionKey);

        // Act - 模拟系统启动时解密用户偏好设置的过程
        var restoredPreferences = _cryptoService.DecryptData(encryptedPreferences, userEncryptionKey);

        // Assert - 根据业务需求：解密后的数据必须与原始数据完全一致
        restoredPreferences.Should().Be(originalUserPreferences,
            "根据业务需求，解密后的用户偏好设置必须与原始设置完全一致");

        // 验证数据完整性 - 确保没有数据丢失或损坏
        restoredPreferences.Length.Should().Be(originalUserPreferences.Length,
            "解密后的数据长度必须与原始数据一致");
    }

    [Fact]
    public void EncryptData_WhenUserHasNoSensitiveDataToStore_ShouldHandleEmptyInputGracefully()
    {
        // Arrange - 基于业务需求：用户可能没有敏感数据需要加密
        var emptyUserData = "";
        var encryptionKey = CreateBusinessEncryptionKey();

        // Act - 模拟系统处理空数据的场景
        var encryptedData = _cryptoService.EncryptData(emptyUserData, encryptionKey);

        // Assert - 根据产品设计：空数据应该被优雅处理而不是报错
        encryptedData.Should().Be("", "根据产品设计，空数据应该返回空字符串而不进行不必要的加密操作");

        // 验证性能优化 - 空数据不应消耗加密资源
        // 这符合客户端应用的性能要求
    }

    [Fact]
    public void DecryptData_WhenUserHasNoStoredSensitiveData_ShouldHandleEmptyInputConsistently()
    {
        // Arrange - 基于业务需求：新用户可能没有存储的敏感数据
        var emptyEncryptedData = "";
        var userKey = CreateBusinessEncryptionKey();

        // Act - 模拟系统尝试解密空数据的场景
        var decryptedData = _cryptoService.DecryptData(emptyEncryptedData, userKey);

        // Assert - 根据产品设计：空加密数据应该返回空结果
        decryptedData.Should().Be("", "根据产品设计，空加密数据应该返回空字符串以保持一致性");

        // 验证与加密操作的对称性
        var reEncrypted = _cryptoService.EncryptData(decryptedData, userKey);
        reEncrypted.Should().Be("", "加密和解密操作必须保持对称性");
    }

    [Fact]
    public void EncryptData_WhenSystemUsesInvalidKey_ShouldProvideSecurityErrorGuidance()
    {
        // Arrange - 基于业务需求：系统必须验证密钥的有效性以确保安全
        var userSensitiveData = CreateBusinessSensitiveData();
        var invalidKey = new byte[16]; // 模拟错误的密钥长度（应该是32字节）

        // Act & Assert - 根据安全设计：无效密钥必须被拒绝
        var action = () => _cryptoService.EncryptData(userSensitiveData, invalidKey);
        action.Should().Throw<ArgumentException>("根据安全设计，无效密钥必须被拒绝以防止安全漏洞")
              .WithMessage("*密钥长度必须为32字节*", "错误信息必须明确指出密钥长度要求");

        // 验证安全边界 - 确保系统不会使用弱密钥进行加密
        // 这符合需求5.1中AES-256的安全要求
    }

    [Fact]
    public void DecryptData_WhenSystemUsesInvalidKey_ShouldPreventUnauthorizedDataAccess()
    {
        // Arrange - 基于业务需求：系统必须防止使用无效密钥访问敏感数据
        var validEncryptedData = "dGVzdA=="; // 模拟有效的加密数据格式
        var invalidKey = new byte[16]; // 模拟错误的密钥长度

        // Act & Assert - 根据安全设计：无效密钥不能用于解密操作
        var action = () => _cryptoService.DecryptData(validEncryptedData, invalidKey);
        action.Should().Throw<ArgumentException>("根据安全设计，无效密钥必须被拒绝以防止未授权访问")
              .WithMessage("*密钥长度必须为32字节*", "错误信息必须明确指出安全要求");

        // 验证数据保护 - 确保敏感数据不会被错误解密
        // 这符合需求5.6中数据完整性和安全性的要求
    }

    [Fact]
    public void DecryptData_WhenUnauthorizedUserAttemptsAccess_ShouldPreventDataBreach()
    {
        // Arrange - 基于业务需求：不同用户的数据必须完全隔离
        var userASensitiveData = CreateBusinessSensitiveData("用户A的敏感偏好设置");
        var userAKey = CreateBusinessEncryptionKey("UserA");
        var userBKey = CreateBusinessEncryptionKey("UserB"); // 不同用户的密钥
        var userAEncryptedData = _cryptoService.EncryptData(userASensitiveData, userAKey);

        // Act & Assert - 根据需求6.7：确保用户数据完全隔离和安全
        var action = () => _cryptoService.DecryptData(userAEncryptedData, userBKey);
        action.Should().Throw<CryptographicException>("根据业务需求，用户B不能访问用户A的加密数据")
              .WithMessage("*解密失败*", "必须提供明确的解密失败信息");

        // 验证数据隔离 - 确保跨用户数据访问被阻止
        // 这是多用户支持的核心安全要求
    }

    #endregion

    #region 会话令牌管理测试 - 验证需求5.7和1.2

    [Fact]
    public void GenerateSessionToken_WhenUserStartsApplication_ShouldCreateSecureSessionForStaticAuthentication()
    {
        // Arrange - 基于业务需求：用户启动应用时系统自动创建会话令牌
        var userId = CreateBusinessUserId();
        var machineId = CreateBusinessMachineId();
        var sessionDuration = TimeSpan.FromHours(24); // 业务规则：会话持续24小时
        var expirationTime = DateTime.UtcNow.Add(sessionDuration);

        // Act - 模拟静默认证过程中的令牌生成
        var sessionToken = _cryptoService.GenerateSessionToken(userId, machineId, expirationTime);

        // Assert - 根据需求1.2：基于机器ID生成用户标识和会话令牌
        sessionToken.Should().NotBeNullOrEmpty("根据业务需求，会话令牌生成不能失败");

        // 验证令牌格式和安全性
        var action = () => Convert.FromBase64String(sessionToken);
        action.Should().NotThrow("会话令牌必须是有效的Base64格式以便传输和存储");

        // 验证令牌的唯一性和安全性
        var tokenBytes = Convert.FromBase64String(sessionToken);
        tokenBytes.Length.Should().BeGreaterThan(50, "会话令牌必须有足够的长度以确保安全性");
    }

    [Fact]
    public void GenerateSessionToken_WhenUserRestartsApplication_ShouldCreateUniqueTokensForSecurityReasons()
    {
        // Arrange - 基于业务需求：用户每次重启应用都应获得新的会话令牌
        var userId = CreateBusinessUserId();
        var machineId = CreateBusinessMachineId();
        var sessionExpiration = DateTime.UtcNow.AddHours(24);

        // Act - 模拟用户多次启动应用的场景
        var firstSessionToken = _cryptoService.GenerateSessionToken(userId, machineId, sessionExpiration);
        var secondSessionToken = _cryptoService.GenerateSessionToken(userId, machineId, sessionExpiration);

        // Assert - 根据安全设计：每次生成的令牌必须唯一
        firstSessionToken.Should().NotBe(secondSessionToken,
            "根据安全设计，每次生成的会话令牌必须唯一以防止重放攻击");

        // 验证令牌的随机性和不可预测性
        var token1Bytes = Convert.FromBase64String(firstSessionToken);
        var token2Bytes = Convert.FromBase64String(secondSessionToken);

        // 由于令牌包含相同的用户ID和机器ID，我们主要验证令牌ID部分的随机性
        // 这里我们简单验证令牌不相同即可，因为每个令牌都包含唯一的TokenId
        firstSessionToken.Should().NotBe(secondSessionToken,
            "每次生成的会话令牌必须唯一以防止重放攻击");

        // 验证令牌结构长度差异在安全可接受范围（可能因时间序列化或内部实现微小差异导致）
        Math.Abs(token1Bytes.Length - token2Bytes.Length)
            .Should().BeLessOrEqualTo(8, "令牌结构长度差异应保持在可接受范围内，不影响验证逻辑");
    }

    [Fact]
    public async Task GenerateSessionToken_StructureAndSignature_ShouldBeValid()
    {
        var userId = CreateBusinessUserId();
        var machineId = CreateBusinessMachineId();
        var sessionExpiration = DateTime.UtcNow.AddHours(1);

        var token = _cryptoService.GenerateSessionToken(userId, machineId, sessionExpiration);
        token.Should().NotBeNullOrWhiteSpace();

        // 一级 Base64 解码
        var outerBytes = Convert.FromBase64String(token);
        var outerJson = Encoding.UTF8.GetString(outerBytes);
        using var doc = JsonDocument.Parse(outerJson);
        doc.RootElement.TryGetProperty("Data", out var dataProp).Should().BeTrue();
        doc.RootElement.TryGetProperty("Signature", out var sigProp).Should().BeTrue();
        doc.RootElement.TryGetProperty("Salt", out var saltProp).Should().BeTrue();

        var innerDataB64 = dataProp.GetString();
        innerDataB64.Should().NotBeNull();
        var innerBytes = Convert.FromBase64String(innerDataB64!);
        var innerJson = Encoding.UTF8.GetString(innerBytes);
        using var innerDoc = JsonDocument.Parse(innerJson);
        innerDoc.RootElement.TryGetProperty("UserId", out var userIdProp).Should().BeTrue();
        innerDoc.RootElement.TryGetProperty("MachineId", out var machineIdProp).Should().BeTrue();
        innerDoc.RootElement.TryGetProperty("ExpirationTime", out var expProp).Should().BeTrue();
        innerDoc.RootElement.TryGetProperty("IssuedAt", out var issuedProp).Should().BeTrue();
        innerDoc.RootElement.TryGetProperty("TokenId", out var tokenIdProp).Should().BeTrue();

        userIdProp.GetString().Should().Be(userId);
        machineIdProp.GetString().Should().Be(machineId);
        DateTime.Parse(expProp.GetString()!).Should().BeCloseTo(sessionExpiration, TimeSpan.FromSeconds(5));
        DateTime.Parse(issuedProp.GetString()!).Should().BeBefore(DateTime.UtcNow.AddSeconds(5));
        tokenIdProp.GetString().Should().NotBeNullOrWhiteSpace();

        // 重算签名
        var signatureB64 = sigProp.GetString();
        signatureB64.Should().NotBeNull();
        var salt = saltProp.GetString();
        salt.Should().NotBeNull();

        // 使用反射访问 DeriveKeyFromMachineId (保持测试层级不改变生产可见性)
        var deriveMethod = typeof(CryptoService).GetMethod("DeriveKeyFromMachineId");
        deriveMethod.Should().NotBeNull();
        var key = (byte[])deriveMethod!.Invoke(_cryptoService, new object[] { machineId, salt! })!;

        using var hmac = new HMACSHA256(key);
        var recomputed = hmac.ComputeHash(innerBytes);
        Convert.FromBase64String(signatureB64!).SequenceEqual(recomputed).Should().BeTrue("签名必须匹配以保证完整性");

        // 验证 Validate API 一致
        var validateResult = await _cryptoService.ValidateSessionTokenAsync(token, userId, machineId);
        validateResult.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "DESKTOP-ABC123", "用户ID为空字符串时")]
    [InlineData("   ", "DESKTOP-ABC123", "用户ID仅包含空格时")]
    [InlineData("user-001", "", "机器ID为空字符串时")]
    [InlineData("user-001", "   ", "机器ID仅包含空格时")]
    public void GenerateSessionToken_WhenSystemReceivesInvalidAuthenticationData_ShouldPreventInsecureTokenGeneration(
        string userId, string machineId, string scenario)
    {
        // Arrange - 基于业务需求：系统必须验证认证数据的有效性
        var validExpirationTime = DateTime.UtcNow.AddHours(24);

        // Act & Assert - 根据安全设计：无效的认证数据不能生成会话令牌
        var action = () => _cryptoService.GenerateSessionToken(userId, machineId, validExpirationTime);
        action.Should().Throw<ArgumentException>($"根据安全设计，{scenario}系统必须拒绝令牌生成")
              .WithMessage("*不能为空*", "错误信息必须明确指出数据验证失败的原因");

        // 验证安全边界 - 确保不会生成无效的会话令牌
        // 这符合静默认证的安全要求
    }

    [Fact]
    public void GenerateSessionToken_WhenUserIdIsNull_ShouldThrowArgumentException()
    {
        // Arrange - 基于业务需求：系统必须验证用户ID的有效性
        string? nullUserId = null;
        var validMachineId = CreateBusinessMachineId();
        var validExpirationTime = DateTime.UtcNow.AddHours(24);

        // Act & Assert - 根据安全设计：null用户ID必须被拒绝
        var action = () => _cryptoService.GenerateSessionToken(nullUserId!, validMachineId, validExpirationTime);
        action.Should().Throw<ArgumentException>("根据安全设计，null用户ID必须被拒绝");
    }

    [Fact]
    public void GenerateSessionToken_WhenMachineIdIsNull_ShouldThrowArgumentException()
    {
        // Arrange - 基于业务需求：系统必须验证机器ID的有效性
        var validUserId = CreateBusinessUserId();
        string? nullMachineId = null;
        var validExpirationTime = DateTime.UtcNow.AddHours(24);

        // Act & Assert - 根据安全设计：null机器ID必须被拒绝
        var action = () => _cryptoService.GenerateSessionToken(validUserId, nullMachineId!, validExpirationTime);
        action.Should().Throw<ArgumentException>("根据安全设计，null机器ID必须被拒绝");
    }

    [Fact]
    public async Task ValidateSessionTokenAsync_WhenUserContinuesUsingApplication_ShouldMaintainValidSession()
    {
        // Arrange - 基于业务需求：用户在会话期间继续使用应用时会话应保持有效
        var userId = CreateBusinessUserId();
        var machineId = CreateBusinessMachineId();
        var sessionDuration = TimeSpan.FromHours(24);
        var expirationTime = DateTime.UtcNow.Add(sessionDuration);
        var activeSessionToken = _cryptoService.GenerateSessionToken(userId, machineId, expirationTime);

        // Act - 模拟系统验证活跃用户会话的过程
        var validationResult = await _cryptoService.ValidateSessionTokenAsync(activeSessionToken, userId, machineId);

        // Assert - 根据需求1.5：在后台维护用户会话状态
        validationResult.Should().NotBeNull("会话验证结果不能为空");
        validationResult.IsValid.Should().BeTrue("根据业务需求，有效会话必须通过验证");
        validationResult.IsExpired.Should().BeFalse("活跃会话不应被标记为过期");
        validationResult.UserId.Should().Be(userId, "会话必须正确识别用户身份");
        validationResult.MachineId.Should().Be(machineId, "会话必须正确绑定机器ID");
        validationResult.ExpirationTime.Should().BeCloseTo(expirationTime, TimeSpan.FromSeconds(1),
            "会话过期时间必须准确");
        validationResult.FailureReason.Should().BeNull("有效会话不应有失败原因");

        // 验证会话状态的完整性
        validationResult.UserId.Should().NotBeNullOrEmpty("用户ID必须被正确解析");
        validationResult.MachineId.Should().NotBeNullOrEmpty("机器ID必须被正确解析");
    }

    [Fact]
    public async Task ValidateSessionTokenAsync_WhenUserSessionExpires_ShouldRequireReauthentication()
    {
        // Arrange - 基于业务需求：过期的会话必须被识别并要求重新认证
        var userId = CreateBusinessUserId();
        var machineId = CreateBusinessMachineId();
        var pastExpirationTime = DateTime.UtcNow.AddMilliseconds(-100); // 模拟已过期的会话
        var expiredSessionToken = _cryptoService.GenerateSessionToken(userId, machineId, pastExpirationTime);

        // Act - 模拟系统检查过期会话的过程
        var validationResult = await _cryptoService.ValidateSessionTokenAsync(expiredSessionToken, userId, machineId);

        // Assert - 根据安全设计：过期会话必须被拒绝
        validationResult.Should().NotBeNull("过期会话验证结果不能为空");
        validationResult.IsValid.Should().BeFalse("根据安全设计，过期会话必须被标记为无效");
        validationResult.IsExpired.Should().BeTrue("过期标志必须被正确设置");
        validationResult.FailureReason.Should().Be("令牌已过期", "必须提供明确的过期原因");

        // 验证过期会话的安全处理
        validationResult.UserId.Should().Be(userId, "即使过期，用户ID仍应被正确解析用于审计");
        validationResult.MachineId.Should().Be(machineId, "即使过期，机器ID仍应被正确解析用于审计");

        // 验证业务逻辑：过期会话应触发重新认证流程
        // 在实际业务中，这会导致静默认证服务创建新的会话
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

    #region 数据完整性校验测试 - 验证需求5.6

    [Fact]
    public void ComputeDataIntegrityHash_WhenSystemStoresUserData_ShouldEnsureDataIntegrityVerification()
    {
        // Arrange - 基于业务需求：系统存储用户数据时必须计算完整性校验值
        var userConfigurationData = CreateBusinessSensitiveData();
        var integrityKey = CreateBusinessEncryptionKey("integrity");

        // Act - 模拟系统为用户数据计算完整性校验值的过程
        var integrityHash1 = _cryptoService.ComputeDataIntegrityHash(userConfigurationData, integrityKey);
        var integrityHash2 = _cryptoService.ComputeDataIntegrityHash(userConfigurationData, integrityKey);

        // Assert - 根据需求5.6：数据完整性校验功能的准确性
        integrityHash1.Should().NotBeNullOrEmpty("根据业务需求，完整性校验值不能为空");
        integrityHash1.Should().Be(integrityHash2, "相同数据和密钥必须产生相同的校验值以确保一致性");

        // 验证校验值的格式和安全性
        var action = () => Convert.FromBase64String(integrityHash1);
        action.Should().NotThrow("完整性校验值必须是有效的Base64格式以便存储");

        // 验证校验值的长度和强度
        var hashBytes = Convert.FromBase64String(integrityHash1);
        hashBytes.Length.Should().Be(32, "HMAC-SHA256校验值必须是32字节以确保安全性");
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
    public void ComputeDataIntegrityHash_WhenKeyIsNull_ShouldPreventInsecureHashGeneration()
    {
        // Arrange - 基于业务需求：系统必须验证校验密钥的有效性
        var userData = CreateBusinessSensitiveData();
        byte[]? nullKey = null;

        // Act & Assert - 根据安全设计：null密钥必须被拒绝
        var action = () => _cryptoService.ComputeDataIntegrityHash(userData, nullKey!);
        action.Should().Throw<ArgumentException>("根据安全设计，null校验密钥必须被拒绝以防止不安全的校验值生成");
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
    public void VerifyDataIntegrity_WhenSystemLoadsUserData_ShouldDetectDataCorruption()
    {
        // Arrange - 基于业务需求：系统加载用户数据时必须验证数据完整性
        var originalUserData = CreateBusinessSensitiveData();
        var integrityKey = CreateBusinessEncryptionKey("integrity");
        var storedIntegrityHash = _cryptoService.ComputeDataIntegrityHash(originalUserData, integrityKey);

        // Act - 模拟系统启动时验证用户数据完整性的过程
        var integrityCheckResult = _cryptoService.VerifyDataIntegrity(originalUserData, storedIntegrityHash, integrityKey);

        // Assert - 根据需求5.6：验证数据完整性校验功能的准确性
        integrityCheckResult.Should().BeTrue("根据业务需求，未被篡改的数据必须通过完整性验证");

        // 验证业务逻辑：完整性验证通过意味着数据可以安全使用
        // 在实际业务中，这确保了用户偏好设置的可靠性
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
    public void VerifyDataIntegrity_WhenUserDataIsCorrupted_ShouldDetectTamperingAndProtectUser()
    {
        // Arrange - 基于业务需求：系统必须检测数据篡改以保护用户
        var originalUserPreferences = CreateBusinessSensitiveData();
        var corruptedUserPreferences = originalUserPreferences.Replace("\"theme\": \"dark\"", "\"theme\": \"hacked\"");
        var integrityKey = CreateBusinessEncryptionKey("integrity");
        var originalIntegrityHash = _cryptoService.ComputeDataIntegrityHash(originalUserPreferences, integrityKey);

        // Act - 模拟系统检测被篡改数据的过程
        var integrityCheckResult = _cryptoService.VerifyDataIntegrity(corruptedUserPreferences, originalIntegrityHash, integrityKey);

        // Assert - 根据安全设计：被篡改的数据必须被检测出来
        integrityCheckResult.Should().BeFalse("根据安全设计，被篡改的数据必须被检测并拒绝");

        // 验证安全保护：在实际业务中，这会触发数据恢复或重新初始化流程
        // 确保用户不会使用被篡改的配置数据
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

    #region 安全内存清理测试 - 验证内存中敏感数据的安全清理机制

    [Fact]
    public void SecureClearMemory_WhenProcessingUserCredentials_ShouldPreventMemoryLeakage()
    {
        // Arrange - 基于业务需求：处理用户敏感信息后必须安全清理内存
        var sensitiveUserCredentials = Encoding.UTF8.GetBytes("用户API密钥和敏感配置信息");
        var originalCredentials = new byte[sensitiveUserCredentials.Length];
        Array.Copy(sensitiveUserCredentials, originalCredentials, sensitiveUserCredentials.Length);

        // Act - 模拟系统处理完敏感数据后的安全清理过程
        _cryptoService.SecureClearMemory(sensitiveUserCredentials);

        // Assert - 根据安全设计：敏感数据必须被安全清理以防止内存泄露
        sensitiveUserCredentials.Should().NotBeEquivalentTo(originalCredentials,
            "根据安全设计，敏感数据必须被清理以防止内存中的数据泄露");
        sensitiveUserCredentials.All(b => b == 0).Should().BeTrue(
            "内存清理后所有字节必须为零以确保敏感信息无法恢复");

        // 验证安全清理的彻底性
        var hasNonZeroBytes = sensitiveUserCredentials.Any(b => b != 0);
        hasNonZeroBytes.Should().BeFalse("确保没有敏感数据残留在内存中");
    }

    [Fact]
    public void SecureClearMemory_WhenNoSensitiveDataExists_ShouldHandleGracefully()
    {
        // Arrange - 基于业务需求：系统必须优雅处理空的敏感数据清理请求
        byte[]? noSensitiveData = null;

        // Act & Assert - 根据产品设计：空数据清理请求不应导致系统错误
        var action = () => _cryptoService.SecureClearMemory(noSensitiveData);
        action.Should().NotThrow("根据产品设计，空数据清理请求必须被优雅处理");

        // 验证系统稳定性：即使没有数据需要清理，系统也应正常运行
        // 这符合客户端应用的稳定性要求
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

    #region 性能和安全边界测试 - 验证客户端性能要求

    [Fact]
    public void EncryptDecrypt_WhenProcessingLargeUserConfiguration_ShouldMeetPerformanceRequirements()
    {
        // Arrange - 基于业务需求：客户端必须能够处理大型用户配置文件
        var largeUserConfiguration = new string('A', 10000); // 模拟10KB的用户配置数据
        var userEncryptionKey = CreateBusinessEncryptionKey();

        // Act - 模拟处理大型用户配置的加密解密过程
        var startTime = DateTime.UtcNow;
        var encryptedConfiguration = _cryptoService.EncryptData(largeUserConfiguration, userEncryptionKey);
        var decryptedConfiguration = _cryptoService.DecryptData(encryptedConfiguration, userEncryptionKey);
        var processingTime = DateTime.UtcNow - startTime;

        // Assert - 根据性能要求：界面操作响应时间 < 200ms
        decryptedConfiguration.Should().Be(largeUserConfiguration,
            "根据业务需求，大型配置文件必须被正确处理");
        processingTime.TotalMilliseconds.Should().BeLessThan(200,
            "根据性能要求，大型数据处理必须在200ms内完成以确保用户体验");

        // 验证数据完整性
        decryptedConfiguration.Length.Should().Be(largeUserConfiguration.Length,
            "处理大型数据时不能有数据丢失");
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
    public async Task SessionToken_WhenSystemSupportsInternationalization_ShouldHandleUnicodeUserData()
    {
        // Arrange - 基于业务需求：系统必须支持国际化用户数据
        var chineseUserId = "用户-张三-001";
        var chineseMachineId = "办公电脑-北京-001";
        var sessionExpiration = DateTime.UtcNow.AddHours(24);

        // Act - 模拟国际化环境下的会话管理
        var unicodeSessionToken = _cryptoService.GenerateSessionToken(chineseUserId, chineseMachineId, sessionExpiration);
        var validationResult = await _cryptoService.ValidateSessionTokenAsync(unicodeSessionToken, chineseUserId, chineseMachineId);

        // Assert - 根据产品设计：系统必须正确处理Unicode字符
        validationResult.IsValid.Should().BeTrue("根据产品设计，系统必须支持Unicode用户数据");
        validationResult.UserId.Should().Be(chineseUserId, "Unicode用户ID必须被正确处理");
        validationResult.MachineId.Should().Be(chineseMachineId, "Unicode机器ID必须被正确处理");

        // 验证国际化支持的完整性
        unicodeSessionToken.Should().NotBeNullOrEmpty("Unicode数据的会话令牌必须被成功生成");

        // 验证字符编码的正确性
        var tokenBytes = Convert.FromBase64String(unicodeSessionToken);
        tokenBytes.Length.Should().BeGreaterThan(0, "Unicode令牌必须包含有效数据");
    }

    #endregion

    #region 综合业务场景测试 - 验证端到端加密安全流程

    [Fact]
    public async Task CryptoService_WhenUserFirstStartsApplication_ShouldProvideCompleteSecurityWorkflow()
    {
        // Arrange - 基于业务需求：用户首次启动应用的完整安全流程
        var newUserId = CreateBusinessUserId("new-user");
        var userMachineId = CreateBusinessMachineId();
        var userSensitivePreferences = CreateBusinessSensitiveData();

        // Act - 模拟完整的用户安全初始化流程

        // 1. 基于机器ID派生用户专用密钥
        var userSalt = _cryptoService.GenerateSecureSalt();
        var userEncryptionKey = _cryptoService.DeriveKeyFromMachineId(userMachineId, userSalt);

        // 2. 加密用户敏感偏好设置
        var encryptedPreferences = _cryptoService.EncryptData(userSensitivePreferences, userEncryptionKey);

        // 3. 计算数据完整性校验值
        var integrityHash = _cryptoService.ComputeDataIntegrityHash(userSensitivePreferences, userEncryptionKey);

        // 4. 生成用户会话令牌
        var sessionExpiration = DateTime.UtcNow.AddHours(24);
        var sessionToken = _cryptoService.GenerateSessionToken(newUserId, userMachineId, sessionExpiration);

        // 5. 验证会话令牌
        var sessionValidation = await _cryptoService.ValidateSessionTokenAsync(sessionToken, newUserId, userMachineId);

        // 6. 解密并验证用户数据
        var decryptedPreferences = _cryptoService.DecryptData(encryptedPreferences, userEncryptionKey);
        var integrityCheck = _cryptoService.VerifyDataIntegrity(decryptedPreferences, integrityHash, userEncryptionKey);

        // Assert - 验证完整的安全工作流程

        // 验证密钥派生
        userEncryptionKey.Should().NotBeNull("用户加密密钥必须成功生成");
        userEncryptionKey.Length.Should().Be(32, "必须符合AES-256要求");

        // 验证数据加密
        encryptedPreferences.Should().NotBeNullOrEmpty("用户偏好设置必须被成功加密");
        encryptedPreferences.Should().NotBe(userSensitivePreferences, "加密后数据不能与原始数据相同");

        // 验证完整性保护
        integrityHash.Should().NotBeNullOrEmpty("完整性校验值必须被成功计算");

        // 验证会话管理
        sessionToken.Should().NotBeNullOrEmpty("会话令牌必须被成功生成");
        sessionValidation.IsValid.Should().BeTrue("会话验证必须成功");

        // 验证数据恢复
        decryptedPreferences.Should().Be(userSensitivePreferences, "解密后的数据必须与原始数据一致");
        integrityCheck.Should().BeTrue("数据完整性验证必须通过");

        // 验证端到端安全性
        // 这个测试覆盖了需求5.1, 5.2, 5.6, 5.7, 1.2的综合场景
    }

    [Fact]
    public void CryptoService_WhenMultipleUsersShareDevice_ShouldEnsureDataIsolation()
    {
        // Arrange - 基于业务需求：共享设备上的多用户数据隔离
        var user1Id = CreateBusinessUserId("user1");
        var user2Id = CreateBusinessUserId("user2");
        var sharedMachineId = CreateBusinessMachineId("SHARED-DEVICE");

        var user1Data = CreateBusinessSensitiveData("用户1的个人偏好设置");
        var user2Data = CreateBusinessSensitiveData("用户2的个人偏好设置");

        // Act - 模拟多用户环境下的数据处理

        // 为每个用户生成独立的加密密钥
        var user1Salt = _cryptoService.GenerateSecureSalt();
        var user2Salt = _cryptoService.GenerateSecureSalt();
        var user1Key = _cryptoService.DeriveKeyFromMachineId($"{sharedMachineId}-{user1Id}", user1Salt);
        var user2Key = _cryptoService.DeriveKeyFromMachineId($"{sharedMachineId}-{user2Id}", user2Salt);

        // 分别加密两个用户的数据
        var user1EncryptedData = _cryptoService.EncryptData(user1Data, user1Key);
        var user2EncryptedData = _cryptoService.EncryptData(user2Data, user2Key);

        // Assert - 验证多用户数据隔离

        // 验证密钥隔离
        user1Key.Should().NotBeEquivalentTo(user2Key, "不同用户必须有不同的加密密钥");

        // 验证数据隔离
        user1EncryptedData.Should().NotBe(user2EncryptedData, "不同用户的加密数据必须不同");

        // 验证跨用户访问保护
        var attemptCrossUserDecryption = () => _cryptoService.DecryptData(user1EncryptedData, user2Key);
        attemptCrossUserDecryption.Should().Throw<CryptographicException>(
            "用户2不能解密用户1的数据，确保数据隔离");

        // 验证正确用户可以访问自己的数据
        var user1DecryptedData = _cryptoService.DecryptData(user1EncryptedData, user1Key);
        var user2DecryptedData = _cryptoService.DecryptData(user2EncryptedData, user2Key);

        user1DecryptedData.Should().Be(user1Data, "用户1必须能正确访问自己的数据");
        user2DecryptedData.Should().Be(user2Data, "用户2必须能正确访问自己的数据");

        // 这个测试覆盖了需求6.7：确保用户数据完全隔离和安全
    }

    #endregion

    #region 业务场景测试数据构建方法

    /// <summary>
    /// 创建符合业务场景的机器ID
    /// </summary>
    private string CreateBusinessMachineId(string deviceName = "DESKTOP-USER001")
    {
        // 模拟真实的Windows机器ID格式
        return $"{deviceName}-{Environment.UserName}-{DateTime.Now:yyyyMMdd}";
    }

    /// <summary>
    /// 创建符合业务场景的用户ID
    /// </summary>
    private string CreateBusinessUserId(string userPrefix = "user")
    {
        // 模拟静默认证生成的用户ID格式
        return $"{userPrefix}-{Guid.NewGuid():N}";
    }

    /// <summary>
    /// 创建符合业务场景的盐值
    /// </summary>
    private string CreateBusinessSalt(string context = "default")
    {
        // 模拟业务场景中的盐值生成
        return $"salt-{context}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    /// <summary>
    /// 创建符合业务场景的敏感数据
    /// </summary>
    private string CreateBusinessSensitiveData(string content = null)
    {
        // 模拟用户的敏感偏好设置数据
        return content ?? @"{
            ""theme"": ""dark"",
            ""language"": ""zh-CN"",
            ""defaultLLMModel"": ""gpt-4"",
            ""apiKeys"": {
                ""openai"": ""sk-xxx"",
                ""baidu"": ""xxx""
            },
            ""personalInfo"": {
                ""displayName"": ""张三"",
                ""email"": ""zhangsan@example.com""
            }
        }";
    }

    /// <summary>
    /// 创建符合业务场景的加密密钥
    /// </summary>
    private byte[] CreateBusinessEncryptionKey(string userContext = "default")
    {
        // 基于业务场景生成密钥（实际业务中会基于机器ID派生）
        var machineId = CreateBusinessMachineId();
        var salt = CreateBusinessSalt(userContext);
        return _cryptoService.DeriveKeyFromMachineId(machineId, salt);
    }

    /// <summary>
    /// 生成测试用的32字节密钥（保留用于技术测试）
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