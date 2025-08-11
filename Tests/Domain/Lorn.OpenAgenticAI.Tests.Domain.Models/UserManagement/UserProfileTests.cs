using FluentAssertions;
using NUnit.Framework;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;
using System.Reflection;

namespace Lorn.OpenAgenticAI.Tests.Domain.Models.UserManagement;

/// <summary>
/// UserProfile 领域模型单元测试
/// 验证静默认证、状态转换、数据完整性检查和智能默认值生成功能
/// 根据需求文档全面测试业务逻辑正确性、边界条件、不变量和异常处理
/// </summary>
[TestFixture]
public class UserProfileTests
{
    private SecuritySettings _defaultSecuritySettings = null!;
    private SecuritySettings _invalidSecuritySettings = null!;

    [SetUp]
    public void SetUp()
    {
        _defaultSecuritySettings = new SecuritySettings(
            authenticationMethod: "Silent",
            sessionTimeoutMinutes: 30,
            requireTwoFactor: false,
            passwordLastChanged: DateTime.UtcNow
        );

        // 创建无效的安全设置用于测试 - 使用有效的构造参数但设置为无效状态
        _invalidSecuritySettings = new SecuritySettings(
            authenticationMethod: "Silent",
            sessionTimeoutMinutes: 30,
            requireTwoFactor: false,
            passwordLastChanged: DateTime.UtcNow
        );

        // 通过反射设置无效状态来模拟无效的安全设置
        // 这样可以避免构造函数验证，但仍然测试IsValid()方法
        SetPrivateProperty(_invalidSecuritySettings, "AuthenticationMethod", "");
        SetPrivateProperty(_invalidSecuritySettings, "SessionTimeoutMinutes", -1);
    }

    /// <summary>
    /// 使用反射设置私有属性值，用于测试
    /// </summary>
    private static void SetPrivateProperty(object obj, string propertyName, object? value)
    {
        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null && property.CanWrite)
        {
            property.SetValue(obj, value);
        }
        else
        {
            // 如果属性是只读的，尝试设置对应的私有字段
            var field = obj.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
    }

    #region 构造函数测试

    [Test]
    public void Constructor_WithValidParameters_ShouldCreateUserProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var email = "test@example.com";

        // Act
        var userProfile = new UserProfile(userId, username, email, _defaultSecuritySettings);

        // Assert
        userProfile.UserId.Should().Be(userId);
        userProfile.Username.Should().Be(username);
        userProfile.Email.Should().Be(email);
        userProfile.IsActive.Should().BeTrue();
        userProfile.Status.Should().Be(UserStatus.Active);
        userProfile.ProfileVersion.Should().Be(1);
        userProfile.SecuritySettings.Should().NotBeNull();
        userProfile.CreatedTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        userProfile.LastLoginTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        userProfile.LastActiveTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Constructor_WithEmptyGuid_ShouldGenerateNewUserId()
    {
        // Arrange
        var username = "testuser";
        var email = "test@example.com";

        // Act
        var userProfile = new UserProfile(Guid.Empty, username, email, _defaultSecuritySettings);

        // Assert
        userProfile.UserId.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void Constructor_WithEmptyUsername_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act & Assert
        var act = () => new UserProfile(userId, "", email, _defaultSecuritySettings);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Username cannot be empty*");
    }

    [Test]
    public void Constructor_WithNullSecuritySettings_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var email = "test@example.com";

        // Act & Assert
        var act = () => new UserProfile(userId, username, email, null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("securitySettings");
    }

    [Test]
    public void SilentAuthConstructor_WithValidMachineId_ShouldCreateUserProfile()
    {
        // Arrange
        var machineId = "MACHINE123456";
        var displayName = "Test User";

        // Act
        var userProfile = new UserProfile(machineId, displayName, isDefault: true);

        // Assert
        userProfile.MachineId.Should().Be(machineId);
        userProfile.DisplayName.Should().Be(displayName);
        userProfile.IsDefault.Should().BeTrue();
        userProfile.Status.Should().Be(UserStatus.Active);
        userProfile.Username.Should().StartWith("User_");
        userProfile.Description.Should().Be("通过静默认证自动创建的用户");
        userProfile.SecuritySettings.Should().NotBeNull();
        userProfile.Email.Should().BeEmpty();
        userProfile.IsActive.Should().BeTrue();
        userProfile.ProfileVersion.Should().Be(1);
    }

    [Test]
    public void SilentAuthConstructor_WithNullDisplayName_ShouldGenerateDefaultDisplayName()
    {
        // Arrange
        var machineId = "MACHINE123456";

        // Act
        var userProfile = new UserProfile(machineId);

        // Assert
        userProfile.DisplayName.Should().NotBeEmpty();
        userProfile.DisplayName.Should().Contain("@");
    }

    [Test]
    public void SilentAuthConstructor_WithEmptyMachineId_ShouldThrowException()
    {
        // Act & Assert
        var act = () => new UserProfile(string.Empty);
        act.Should().Throw<ArgumentException>()
           .WithMessage("MachineId cannot be empty*");
    }

    [Test]
    public void SilentAuthConstructor_WithWhitespaceMachineId_ShouldThrowException()
    {
        // Act & Assert
        var act = () => new UserProfile("   ");
        act.Should().Throw<ArgumentException>()
           .WithMessage("MachineId cannot be empty*");
    }

    [Test]
    public void SilentAuthConstructor_WithNullMachineId_ShouldThrowException()
    {
        // Act & Assert
        var act = () => new UserProfile(null!);
        act.Should().Throw<ArgumentException>()
           .WithMessage("MachineId cannot be empty*");
    }

    #endregion

    #region 静默认证业务方法测试

    [Test]
    public void ValidateMachineId_WithMatchingId_ShouldReturnTrue()
    {
        // Arrange
        var machineId = "MACHINE123456";
        var userProfile = new UserProfile(machineId);

        // Act
        var result = userProfile.ValidateMachineId(machineId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void ValidateMachineId_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        var machineId = "MACHINE123456";
        var userProfile = new UserProfile(machineId);

        // Act
        var result = userProfile.ValidateMachineId("DIFFERENT123");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void ValidateMachineId_WithCaseInsensitiveMatch_ShouldReturnTrue()
    {
        // Arrange
        var machineId = "MACHINE123456";
        var userProfile = new UserProfile(machineId);

        // Act
        var result = userProfile.ValidateMachineId("machine123456");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void ValidateMachineId_WithEmptyMachineId_ShouldReturnFalse()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var result = userProfile.ValidateMachineId("");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void ValidateMachineId_WithNullMachineId_ShouldReturnFalse()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var result = userProfile.ValidateMachineId(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void UpdateMachineId_WithValidId_ShouldUpdateMachineIdAndVersion()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var originalVersion = userProfile.ProfileVersion;
        var newMachineId = "NEWMACHINE789";

        // Act
        userProfile.UpdateMachineId(newMachineId);

        // Assert
        userProfile.MachineId.Should().Be(newMachineId);
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
    }

    [Test]
    public void UpdateMachineId_WithEmptyId_ShouldThrowArgumentException()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act & Assert
        var act = () => userProfile.UpdateMachineId("");
        act.Should().Throw<ArgumentException>()
           .WithMessage("MachineId cannot be empty*");
    }

    [Test]
    public void UpdateMachineId_WithNullId_ShouldThrowArgumentException()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act & Assert
        var act = () => userProfile.UpdateMachineId(null!);
        act.Should().Throw<ArgumentException>()
           .WithMessage("MachineId cannot be empty*");
    }

    [Test]
    public void PerformSilentLogin_WithActiveUser_ShouldUpdateLoginTime()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var originalLoginTime = userProfile.LastLoginTime;
        var originalActiveTime = userProfile.LastActiveTime;
        var originalVersion = userProfile.ProfileVersion;
        Thread.Sleep(10); // 确保时间差异

        // Act
        userProfile.PerformSilentLogin();

        // Assert
        userProfile.LastLoginTime.Should().BeAfter(originalLoginTime);
        userProfile.LastActiveTime.Should().BeAfter(originalActiveTime);
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
    }

    [Test]
    public void PerformSilentLogin_WithInactiveUser_ShouldThrowException()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.TransitionTo(UserStatus.Suspended);

        // Act & Assert
        var act = () => userProfile.PerformSilentLogin();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("无法对状态为 Suspended 的用户执行静默登录");
    }

    [Test]
    public void PerformSilentLogin_WithDeletedUser_ShouldThrowException()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.TransitionTo(UserStatus.Deleted);

        // Act & Assert
        var act = () => userProfile.PerformSilentLogin();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("无法对状态为 Deleted 的用户执行静默登录");
    }

    [Test]
    public void UpdateActivity_WithActiveUser_ShouldUpdateActiveTime()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var originalActiveTime = userProfile.LastActiveTime;
        var originalVersion = userProfile.ProfileVersion;
        Thread.Sleep(10);

        // Act
        userProfile.UpdateActivity();

        // Assert
        userProfile.LastActiveTime.Should().BeAfter(originalActiveTime);
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
        userProfile.Status.Should().Be(UserStatus.Active);
        userProfile.IsActive.Should().BeTrue();
    }

    [Test]
    public void UpdateActivity_WithInactiveUser_ShouldActivateUser()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.TransitionTo(UserStatus.Inactive);
        var originalActiveTime = userProfile.LastActiveTime;
        Thread.Sleep(10);

        // Act
        userProfile.UpdateActivity();

        // Assert
        userProfile.Status.Should().Be(UserStatus.Active);
        userProfile.IsActive.Should().BeTrue();
        userProfile.LastActiveTime.Should().BeAfter(originalActiveTime);
    }

    [Test]
    public void SetAsDefault_ShouldSetDefaultFlagAndIncrementVersion()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var originalVersion = userProfile.ProfileVersion;

        // Act
        userProfile.SetAsDefault();

        // Assert
        userProfile.IsDefault.Should().BeTrue();
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
    }

    [Test]
    public void UnsetAsDefault_ShouldUnsetDefaultFlagAndIncrementVersion()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456", isDefault: true);
        var originalVersion = userProfile.ProfileVersion;

        // Act
        userProfile.UnsetAsDefault();

        // Assert
        userProfile.IsDefault.Should().BeFalse();
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
    }

    #endregion

    #region 状态转换业务规则测试

    [Test]
    public void CanTransitionTo_FromActiveToValidStates_ShouldReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act & Assert
        userProfile.CanTransitionTo(UserStatus.Inactive).Should().BeTrue();
        userProfile.CanTransitionTo(UserStatus.Suspended).Should().BeTrue();
        userProfile.CanTransitionTo(UserStatus.Deleted).Should().BeTrue();
    }

    [Test]
    public void CanTransitionTo_FromActiveToInvalidStates_ShouldReturnFalse()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act & Assert
        userProfile.CanTransitionTo(UserStatus.Initializing).Should().BeFalse();
        userProfile.CanTransitionTo(UserStatus.Active).Should().BeFalse(); // 已经是Active状态
    }

    [Test]
    public void CanTransitionTo_FromInactiveToValidStates_ShouldReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.TransitionTo(UserStatus.Inactive);

        // Act & Assert
        userProfile.CanTransitionTo(UserStatus.Active).Should().BeTrue();
        userProfile.CanTransitionTo(UserStatus.Suspended).Should().BeTrue();
        userProfile.CanTransitionTo(UserStatus.Deleted).Should().BeTrue();
    }

    [Test]
    public void CanTransitionTo_FromSuspendedToValidStates_ShouldReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.TransitionTo(UserStatus.Suspended);

        // Act & Assert
        userProfile.CanTransitionTo(UserStatus.Active).Should().BeTrue();
        userProfile.CanTransitionTo(UserStatus.Deleted).Should().BeTrue();
    }

    [Test]
    public void CanTransitionTo_FromDeletedToInitializing_ShouldReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.TransitionTo(UserStatus.Deleted);

        // Act & Assert
        userProfile.CanTransitionTo(UserStatus.Initializing).Should().BeTrue();
        userProfile.CanTransitionTo(UserStatus.Deleted).Should().BeFalse(); // 已经是Deleted状态
    }

    [Test]
    public void CanTransitionTo_FromInitializingToActive_ShouldReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.TransitionTo(UserStatus.Deleted);
        userProfile.TransitionTo(UserStatus.Initializing);

        // Act & Assert
        userProfile.CanTransitionTo(UserStatus.Active).Should().BeTrue();
    }

    [Test]
    public void TransitionTo_FromActiveToInactive_ShouldUpdateStatusAndProperties()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var originalVersion = userProfile.ProfileVersion;

        // Act
        userProfile.TransitionTo(UserStatus.Inactive);

        // Assert
        userProfile.Status.Should().Be(UserStatus.Inactive);
        userProfile.IsActive.Should().BeFalse();
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
    }

    [Test]
    public void TransitionTo_FromInactiveToActive_ShouldUpdateStatusAndProperties()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.TransitionTo(UserStatus.Inactive);
        var originalVersion = userProfile.ProfileVersion;
        var originalActiveTime = userProfile.LastActiveTime;
        Thread.Sleep(10);

        // Act
        userProfile.TransitionTo(UserStatus.Active);

        // Assert
        userProfile.Status.Should().Be(UserStatus.Active);
        userProfile.IsActive.Should().BeTrue();
        userProfile.LastActiveTime.Should().BeAfter(originalActiveTime);
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
    }

    [Test]
    public void TransitionTo_ToSuspended_ShouldSetInactiveFlag()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        userProfile.TransitionTo(UserStatus.Suspended);

        // Assert
        userProfile.Status.Should().Be(UserStatus.Suspended);
        userProfile.IsActive.Should().BeFalse();
    }

    [Test]
    public void TransitionTo_ToDeleted_ShouldSetInactiveFlag()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        userProfile.TransitionTo(UserStatus.Deleted);

        // Assert
        userProfile.Status.Should().Be(UserStatus.Deleted);
        userProfile.IsActive.Should().BeFalse();
    }

    [Test]
    public void TransitionTo_ToInitializing_ShouldSetInactiveFlag()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.TransitionTo(UserStatus.Deleted);

        // Act
        userProfile.TransitionTo(UserStatus.Initializing);

        // Assert
        userProfile.Status.Should().Be(UserStatus.Initializing);
        userProfile.IsActive.Should().BeFalse();
    }

    [Test]
    public void TransitionTo_InvalidTransition_ShouldThrowException()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act & Assert
        var act = () => userProfile.TransitionTo(UserStatus.Initializing);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("无法从状态 Active 转换到 Initializing");
    }

    [Test]
    public void TransitionTo_WithReason_ShouldStillWork()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        userProfile.TransitionTo(UserStatus.Inactive, "测试原因");

        // Assert
        userProfile.Status.Should().Be(UserStatus.Inactive);
    }

    [Test]
    public void CheckAndUpdateInactiveStatus_WithLongInactivity_ShouldTransitionToInactive()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        // 使用反射设置 LastActiveTime 为31天前
        SetPrivateProperty(userProfile, "LastActiveTime", DateTime.UtcNow.AddDays(-31));

        // Act
        var result = userProfile.CheckAndUpdateInactiveStatus(30);

        // Assert
        result.Should().BeTrue();
        userProfile.Status.Should().Be(UserStatus.Inactive);
    }

    [Test]
    public void CheckAndUpdateInactiveStatus_WithRecentActivity_ShouldNotChange()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var result = userProfile.CheckAndUpdateInactiveStatus(30);

        // Assert
        result.Should().BeFalse();
        userProfile.Status.Should().Be(UserStatus.Active);
    }

    [Test]
    public void CheckAndUpdateInactiveStatus_WithNonActiveStatus_ShouldReturnFalse()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.TransitionTo(UserStatus.Suspended);
        SetPrivateProperty(userProfile, "LastActiveTime", DateTime.UtcNow.AddDays(-31));

        // Act
        var result = userProfile.CheckAndUpdateInactiveStatus(30);

        // Assert
        result.Should().BeFalse();
        userProfile.Status.Should().Be(UserStatus.Suspended);
    }

    [Test]
    public void CheckAndUpdateInactiveStatus_WithExactInactiveDays_ShouldNotTransition()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        // 设置为恰好30天前，但要确保不会因为毫秒差异而超过30天
        var exactlyThirtyDaysAgo = DateTime.UtcNow.AddDays(-30).AddMilliseconds(1);
        SetPrivateProperty(userProfile, "LastActiveTime", exactlyThirtyDaysAgo);

        // Act
        var result = userProfile.CheckAndUpdateInactiveStatus(30);

        // Assert
        result.Should().BeFalse();
        userProfile.Status.Should().Be(UserStatus.Active);
    }

    #endregion

    #region 数据完整性检查测试

    [Test]
    public void PerformIntegrityCheck_WithValidData_ShouldReturnNoIssues()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var issues = userProfile.PerformIntegrityCheck();

        // Assert
        issues.Should().BeEmpty();
    }

    [Test]
    public void PerformIntegrityCheck_WithEmptyUsername_ShouldReturnUsernameIssue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "Username", string.Empty);

        // Act
        var issues = userProfile.PerformIntegrityCheck();

        // Assert
        issues.Should().NotBeEmpty();
        issues.Should().Contain("用户名不能为空");
    }

    [Test]
    public void PerformIntegrityCheck_WithEmptyUserId_ShouldReturnUserIdIssue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "UserId", Guid.Empty);

        // Act
        var issues = userProfile.PerformIntegrityCheck();

        // Assert
        issues.Should().Contain("用户ID无效");
    }

    [Test]
    public void PerformIntegrityCheck_WithNullSecuritySettings_ShouldReturnSecurityIssue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "SecuritySettings", null);

        // Act
        var issues = userProfile.PerformIntegrityCheck();

        // Assert
        issues.Should().Contain("安全设置不能为空");
    }

    [Test]
    public void PerformIntegrityCheck_WithInvalidSecuritySettings_ShouldReturnSecurityIssue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "SecuritySettings", _invalidSecuritySettings);

        // Act
        var issues = userProfile.PerformIntegrityCheck();

        // Assert
        issues.Should().Contain("安全设置无效");
    }

    [Test]
    public void PerformIntegrityCheck_WithInvalidLoginTime_ShouldReturnTimeIssue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "LastLoginTime", userProfile.CreatedTime.AddDays(-1));

        // Act
        var issues = userProfile.PerformIntegrityCheck();

        // Assert
        issues.Should().Contain("最后登录时间不能早于创建时间");
    }

    [Test]
    public void PerformIntegrityCheck_WithInvalidActiveTime_ShouldReturnTimeIssue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "LastActiveTime", userProfile.CreatedTime.AddDays(-1));

        // Act
        var issues = userProfile.PerformIntegrityCheck();

        // Assert
        issues.Should().Contain("最后活跃时间不能早于创建时间");
    }

    [Test]
    public void PerformIntegrityCheck_WithInconsistentActiveStatus_ShouldReturnStatusIssue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.TransitionTo(UserStatus.Deleted);
        SetPrivateProperty(userProfile, "IsActive", true);

        // Act
        var issues = userProfile.PerformIntegrityCheck();

        // Assert
        issues.Should().Contain("用户活跃标志与状态不一致");
    }

    [Test]
    public void PerformIntegrityCheck_WithDeletedDefaultUser_ShouldReturnDefaultUserIssue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456", isDefault: true);
        userProfile.TransitionTo(UserStatus.Deleted);

        // Act
        var issues = userProfile.PerformIntegrityCheck();

        // Assert
        issues.Should().Contain("已删除的用户不能是默认用户");
    }

    [Test]
    public void AutoRepairIntegrityIssues_WithEmptyUsername_ShouldRepairAndReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var originalVersion = userProfile.ProfileVersion;
        SetPrivateProperty(userProfile, "Username", string.Empty);

        // Act
        var hasRepairs = userProfile.AutoRepairIntegrityIssues();

        // Assert
        hasRepairs.Should().BeTrue();
        userProfile.Username.Should().NotBeEmpty();
        userProfile.Username.Should().StartWith("User_");
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
    }

    [Test]
    public void AutoRepairIntegrityIssues_WithEmptyUsernameAndNoMachineId_ShouldGenerateUserIdBasedUsername()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "Username", string.Empty);
        SetPrivateProperty(userProfile, "MachineId", string.Empty);

        // Act
        var hasRepairs = userProfile.AutoRepairIntegrityIssues();

        // Assert
        hasRepairs.Should().BeTrue();
        userProfile.Username.Should().StartWith("User_");
        userProfile.Username.Should().Contain(userProfile.UserId.ToString("N"));
    }

    [Test]
    public void AutoRepairIntegrityIssues_WithEmptyDisplayName_ShouldRepairAndReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "DisplayName", string.Empty);

        // Act
        var hasRepairs = userProfile.AutoRepairIntegrityIssues();

        // Assert
        hasRepairs.Should().BeTrue();
        userProfile.DisplayName.Should().NotBeEmpty();
        userProfile.DisplayName.Should().Contain("@");
    }

    [Test]
    public void AutoRepairIntegrityIssues_WithNullSecuritySettings_ShouldRepairAndReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "SecuritySettings", null);

        // Act
        var hasRepairs = userProfile.AutoRepairIntegrityIssues();

        // Assert
        hasRepairs.Should().BeTrue();
        userProfile.SecuritySettings.Should().NotBeNull();
    }

    [Test]
    public void AutoRepairIntegrityIssues_WithInvalidLoginTime_ShouldRepairAndReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "LastLoginTime", userProfile.CreatedTime.AddDays(-1));

        // Act
        var hasRepairs = userProfile.AutoRepairIntegrityIssues();

        // Assert
        hasRepairs.Should().BeTrue();
        userProfile.LastLoginTime.Should().Be(userProfile.CreatedTime);
    }

    [Test]
    public void AutoRepairIntegrityIssues_WithInvalidActiveTime_ShouldRepairAndReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "LastActiveTime", userProfile.CreatedTime.AddDays(-1));

        // Act
        var hasRepairs = userProfile.AutoRepairIntegrityIssues();

        // Assert
        hasRepairs.Should().BeTrue();
        userProfile.LastActiveTime.Should().Be(userProfile.CreatedTime);
    }

    [Test]
    public void AutoRepairIntegrityIssues_WithInconsistentActiveStatus_ShouldRepairAndReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.TransitionTo(UserStatus.Deleted);
        SetPrivateProperty(userProfile, "IsActive", true);

        // Act
        var hasRepairs = userProfile.AutoRepairIntegrityIssues();

        // Assert
        hasRepairs.Should().BeTrue();
        userProfile.IsActive.Should().BeFalse();
    }

    [Test]
    public void AutoRepairIntegrityIssues_WithActiveStatusButInactiveFlag_ShouldRepairAndReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "IsActive", false);

        // Act
        var hasRepairs = userProfile.AutoRepairIntegrityIssues();

        // Assert
        hasRepairs.Should().BeTrue();
        userProfile.Status.Should().Be(UserStatus.Inactive);
    }

    [Test]
    public void AutoRepairIntegrityIssues_WithDeletedDefaultUser_ShouldRepairAndReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456", isDefault: true);
        userProfile.TransitionTo(UserStatus.Deleted);

        // Act
        var hasRepairs = userProfile.AutoRepairIntegrityIssues();

        // Assert
        hasRepairs.Should().BeTrue();
        userProfile.IsDefault.Should().BeFalse();
    }

    [Test]
    public void AutoRepairIntegrityIssues_WithValidData_ShouldReturnFalse()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var originalVersion = userProfile.ProfileVersion;

        // Act
        var hasRepairs = userProfile.AutoRepairIntegrityIssues();

        // Assert
        hasRepairs.Should().BeFalse();
        userProfile.ProfileVersion.Should().Be(originalVersion);
    }

    [Test]
    public void AutoRepairIntegrityIssues_WithMultipleIssues_ShouldRepairAllAndReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "Username", string.Empty);
        SetPrivateProperty(userProfile, "DisplayName", string.Empty);
        SetPrivateProperty(userProfile, "LastLoginTime", userProfile.CreatedTime.AddDays(-1));

        // Act
        var hasRepairs = userProfile.AutoRepairIntegrityIssues();

        // Assert
        hasRepairs.Should().BeTrue();
        userProfile.Username.Should().NotBeEmpty();
        userProfile.DisplayName.Should().NotBeEmpty();
        userProfile.LastLoginTime.Should().Be(userProfile.CreatedTime);
    }

    #endregion

    #region 智能默认值生成测试

    [Test]
    public void GetIntelligentDefaultPreferences_ShouldReturnCompletePreferences()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var preferences = userProfile.GetIntelligentDefaultPreferences();

        // Assert
        preferences.Should().ContainKeys("Interface", "Operation", "Security");

        // 验证界面偏好
        preferences["Interface"].Should().ContainKeys("Theme", "FontSize", "Language", "Layout", "ShowTips", "AutoSave");
        preferences["Interface"]["Theme"].Should().BeOfType<string>();
        preferences["Interface"]["FontSize"].Should().BeOfType<int>();
        preferences["Interface"]["Language"].Should().BeOfType<string>();
        preferences["Interface"]["Layout"].Should().Be("Standard");
        preferences["Interface"]["ShowTips"].Should().Be(true);
        preferences["Interface"]["AutoSave"].Should().Be(true);

        // 验证操作偏好
        preferences["Operation"].Should().ContainKeys("DefaultLLMModel", "TaskTimeout", "MaxConcurrentTasks", "AutoRetry", "RetryCount", "EnableNotifications");
        preferences["Operation"]["DefaultLLMModel"].Should().Be("gpt-3.5-turbo");
        preferences["Operation"]["TaskTimeout"].Should().Be(300);
        preferences["Operation"]["MaxConcurrentTasks"].Should().BeOfType<int>();
        preferences["Operation"]["AutoRetry"].Should().Be(true);
        preferences["Operation"]["RetryCount"].Should().Be(3);
        preferences["Operation"]["EnableNotifications"].Should().Be(true);

        // 验证安全偏好
        preferences["Security"].Should().ContainKeys("SessionTimeout", "AutoLock", "AuditLogging", "DataEncryption", "BackupEnabled", "BackupFrequency");
        preferences["Security"]["SessionTimeout"].Should().Be(30);
        preferences["Security"]["AutoLock"].Should().Be(false);
        preferences["Security"]["AuditLogging"].Should().Be(true);
        preferences["Security"]["DataEncryption"].Should().Be(true);
        preferences["Security"]["BackupEnabled"].Should().Be(true);
        preferences["Security"]["BackupFrequency"].Should().Be("Daily");
    }

    [Test]
    public void GetIntelligentDefaultPreferences_MaxConcurrentTasks_ShouldMatchProcessorCount()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var preferences = userProfile.GetIntelligentDefaultPreferences();

        // Assert
        preferences["Operation"]["MaxConcurrentTasks"].Should().Be(Environment.ProcessorCount);
    }

    [Test]
    public void GetIntelligentDefaultPreferences_Theme_ShouldBeValidTheme()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var preferences = userProfile.GetIntelligentDefaultPreferences();

        // Assert
        var theme = preferences["Interface"]["Theme"].ToString();
        theme.Should().BeOneOf("Light", "Dark");
    }

    [Test]
    public void GetIntelligentDefaultPreferences_FontSize_ShouldBeReasonable()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var preferences = userProfile.GetIntelligentDefaultPreferences();

        // Assert
        var fontSize = (int)preferences["Interface"]["FontSize"];
        fontSize.Should().BeInRange(10, 20);
    }

    [Test]
    public void GetIntelligentDefaultPreferences_Language_ShouldBeValidCulture()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var preferences = userProfile.GetIntelligentDefaultPreferences();

        // Assert
        var language = preferences["Interface"]["Language"].ToString();
        language.Should().BeOneOf("zh-CN", "en-US");
    }

    [Test]
    public void GetIntelligentDefaultPreferences_TaskTimeout_ShouldBePositive()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var preferences = userProfile.GetIntelligentDefaultPreferences();

        // Assert
        var timeout = (int)preferences["Operation"]["TaskTimeout"];
        timeout.Should().BePositive();
    }

    [Test]
    public void GetIntelligentDefaultPreferences_SessionTimeout_ShouldBePositive()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var preferences = userProfile.GetIntelligentDefaultPreferences();

        // Assert
        var sessionTimeout = (int)preferences["Security"]["SessionTimeout"];
        sessionTimeout.Should().BePositive();
    }

    [Test]
    public void GetIntelligentDefaultPreferences_RetryCount_ShouldBeReasonable()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var preferences = userProfile.GetIntelligentDefaultPreferences();

        // Assert
        var retryCount = (int)preferences["Operation"]["RetryCount"];
        retryCount.Should().BeInRange(1, 10);
    }

    [Test]
    public void GetIntelligentDefaultPreferences_BackupFrequency_ShouldBeValidOption()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var preferences = userProfile.GetIntelligentDefaultPreferences();

        // Assert
        var backupFrequency = preferences["Security"]["BackupFrequency"].ToString();
        backupFrequency.Should().BeOneOf("Daily", "Weekly", "Monthly");
    }

    #endregion

    #region 基础功能测试

    [Test]
    public void ValidateProfile_WithValidProfile_ShouldReturnTrue()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        var isValid = userProfile.ValidateProfile();

        // Assert
        isValid.Should().BeTrue();
    }

    [Test]
    public void ValidateProfile_WithEmptyUsername_ShouldReturnFalse()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "Username", string.Empty);

        // Act
        var isValid = userProfile.ValidateProfile();

        // Assert
        isValid.Should().BeFalse();
    }

    [Test]
    public void ValidateProfile_WithNullSecuritySettings_ShouldReturnFalse()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        SetPrivateProperty(userProfile, "SecuritySettings", null);

        // Act
        var isValid = userProfile.ValidateProfile();

        // Assert
        isValid.Should().BeFalse();
    }

    [Test]
    public void ValidateProfile_WithInactiveUser_ShouldReturnFalse()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.Deactivate();

        // Act
        var isValid = userProfile.ValidateProfile();

        // Assert
        isValid.Should().BeFalse();
    }

    [Test]
    public void UpdateLastLogin_ShouldUpdateTime()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var originalTime = userProfile.LastLoginTime;
        Thread.Sleep(10);

        // Act
        userProfile.UpdateLastLogin();

        // Assert
        userProfile.LastLoginTime.Should().BeAfter(originalTime);
    }

    [Test]
    public void IncrementVersion_ShouldIncreaseVersion()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var originalVersion = userProfile.ProfileVersion;

        // Act
        userProfile.IncrementVersion();

        // Assert
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
    }

    [Test]
    public void UpdateEmail_WithValidEmail_ShouldUpdateEmailAndVersion()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var originalVersion = userProfile.ProfileVersion;
        var newEmail = "newemail@example.com";

        // Act
        userProfile.UpdateEmail(newEmail);

        // Assert
        userProfile.Email.Should().Be(newEmail);
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
    }

    [Test]
    public void UpdateEmail_WithNullEmail_ShouldSetEmptyEmailAndVersion()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var originalVersion = userProfile.ProfileVersion;

        // Act
        userProfile.UpdateEmail(null);

        // Assert
        userProfile.Email.Should().BeEmpty();
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
    }

    [Test]
    public void UpdateSecuritySettings_WithValidSettings_ShouldUpdateAndIncrementVersion()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var originalVersion = userProfile.ProfileVersion;
        var newSettings = new SecuritySettings(
            authenticationMethod: "TwoFactor",
            sessionTimeoutMinutes: 60,
            requireTwoFactor: true,
            passwordLastChanged: DateTime.UtcNow
        );

        // Act
        userProfile.UpdateSecuritySettings(newSettings);

        // Assert
        userProfile.SecuritySettings.Should().Be(newSettings);
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
    }

    [Test]
    public void UpdateSecuritySettings_WithNullSettings_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act & Assert
        var act = () => userProfile.UpdateSecuritySettings(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("securitySettings");
    }

    [Test]
    public void Activate_ShouldSetActiveStatusAndIncrementVersion()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        userProfile.Deactivate();
        var originalVersion = userProfile.ProfileVersion;

        // Act
        userProfile.Activate();

        // Assert
        userProfile.IsActive.Should().BeTrue();
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
    }

    [Test]
    public void Deactivate_ShouldSetInactiveStatusAndIncrementVersion()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var originalVersion = userProfile.ProfileVersion;

        // Act
        userProfile.Deactivate();

        // Assert
        userProfile.IsActive.Should().BeFalse();
        userProfile.ProfileVersion.Should().Be(originalVersion + 1);
    }

    #endregion

    #region 领域模型不变量和业务约束测试

    [Test]
    public void UserProfile_ShouldMaintainIdImmutability()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userProfile = new UserProfile(userId, "testuser", "test@example.com", _defaultSecuritySettings);

        // Act & Assert
        userProfile.Id.Should().Be(userId);
        userProfile.UserId.Should().Be(userId);
        // UserId 应该是只读的，无法从外部修改
    }

    [Test]
    public void UserProfile_ShouldMaintainVersionConsistency()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");
        var initialVersion = userProfile.ProfileVersion;

        // Act - 执行多个会增加版本的操作
        userProfile.UpdateEmail("new@example.com");
        userProfile.SetAsDefault();
        userProfile.UpdateActivity();

        // Assert
        userProfile.ProfileVersion.Should().Be(initialVersion + 3);
    }

    [Test]
    public void UserProfile_ShouldMaintainTimeConsistency()
    {
        // Arrange & Act
        var userProfile = new UserProfile("MACHINE123456");

        // Assert
        userProfile.CreatedTime.Should().BeOnOrBefore(userProfile.LastLoginTime);
        userProfile.CreatedTime.Should().BeOnOrBefore(userProfile.LastActiveTime);
        userProfile.LastLoginTime.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
        userProfile.LastActiveTime.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Test]
    public void UserProfile_ShouldEnforceBusinessRuleForDefaultUser()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456", isDefault: true);

        // Act
        userProfile.TransitionTo(UserStatus.Deleted);

        // Assert - 删除状态的用户不应该是默认用户（通过完整性检查验证）
        var issues = userProfile.PerformIntegrityCheck();
        issues.Should().Contain("已删除的用户不能是默认用户");
    }

    [Test]
    public void UserProfile_ShouldEnforceBusinessRuleForActiveStatus()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act
        userProfile.TransitionTo(UserStatus.Suspended);

        // Assert
        userProfile.IsActive.Should().BeFalse();
        userProfile.Status.Should().Be(UserStatus.Suspended);
    }

    #endregion

    #region 异常处理和错误恢复测试

    [Test]
    public void UserProfile_ShouldHandleExceptionInDefaultPreferencesGeneration()
    {
        // Arrange & Act
        var userProfile = new UserProfile("MACHINE123456");

        // Assert - 即使在异常情况下，GetIntelligentDefaultPreferences 也应该返回有效的默认值
        var act = () => userProfile.GetIntelligentDefaultPreferences();
        act.Should().NotThrow();

        var preferences = userProfile.GetIntelligentDefaultPreferences();
        preferences.Should().NotBeNull();
        preferences.Should().ContainKeys("Interface", "Operation", "Security");
    }

    [Test]
    public void UserProfile_ShouldRecoverFromCorruptedState()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // 模拟数据损坏
        SetPrivateProperty(userProfile, "Username", string.Empty);
        SetPrivateProperty(userProfile, "DisplayName", string.Empty);
        SetPrivateProperty(userProfile, "SecuritySettings", null);

        // Act
        var hasRepairs = userProfile.AutoRepairIntegrityIssues();

        // Assert
        hasRepairs.Should().BeTrue();
        userProfile.ValidateProfile().Should().BeTrue();
    }

    [Test]
    public void UserProfile_ShouldHandleEdgeCaseInStatusTransition()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act & Assert - 尝试转换到相同状态应该被拒绝
        var act = () => userProfile.TransitionTo(UserStatus.Active);
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void UserProfile_ShouldHandleNullParametersGracefully()
    {
        // Arrange
        var userProfile = new UserProfile("MACHINE123456");

        // Act & Assert - 各种null参数应该被正确处理
        var act1 = () => userProfile.ValidateMachineId(null!);
        act1.Should().NotThrow();
        userProfile.ValidateMachineId(null!).Should().BeFalse();

        var act2 = () => userProfile.UpdateEmail(null);
        act2.Should().NotThrow();
        userProfile.Email.Should().BeEmpty();
    }

    #endregion


}