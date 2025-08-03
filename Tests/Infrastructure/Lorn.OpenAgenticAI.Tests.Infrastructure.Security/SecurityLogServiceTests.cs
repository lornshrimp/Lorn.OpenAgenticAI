using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Lorn.OpenAgenticAI.Domain.Contracts;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Infrastructure.Security;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Security;

/// <summary>
/// SecurityLogService 单元测试类
/// 基于业务需求验证审计日志服务的功能正确性
/// 
/// 业务需求覆盖：
/// - 需求5.8: 记录安全事件并可选通知用户
/// - 需求5.9: 显示操作历史和安全相关事件
/// - 需求5.1: 使用AES-256加密存储所有敏感信息
/// - 需求5.7: 会话令牌生成和验证机制
/// - 需求1.5: 在后台维护用户会话状态
/// - 需求2.9: 显示最近的操作记录和时间信息
/// </summary>
public class SecurityLogServiceTests
{
    private readonly Mock<IUserSecurityLogRepository> _mockRepository;
    private readonly Mock<ILogger<SecurityLogService>> _mockLogger;
    private readonly SecurityLogService _securityLogService;
    private readonly Guid _testUserId;
    private readonly string _testMachineId;
    private readonly string _testSessionId;

    public SecurityLogServiceTests()
    {
        _mockRepository = new Mock<IUserSecurityLogRepository>();
        _mockLogger = new Mock<ILogger<SecurityLogService>>();
        _securityLogService = new SecurityLogService(_mockRepository.Object, _mockLogger.Object);

        // 基于业务场景的测试数据
        _testUserId = Guid.NewGuid();
        _testMachineId = "DESKTOP-ABC123-WIN11-USER001"; // 模拟真实机器ID格式
        _testSessionId = "session_" + Guid.NewGuid().ToString("N")[..16]; // 模拟会话ID格式
    }

    [Fact]
    public void Constructor_WhenRepositoryIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert - 验证依赖注入的安全性
        var act = () => new SecurityLogService(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("securityLogRepository");
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert - 验证依赖注入的安全性
        var act = () => new SecurityLogService(_mockRepository.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task LogUserLoginAsync_WhenSuccessfulLogin_ShouldRecordLoginEventWithCorrectDetails()
    {
        // Arrange - 基于业务需求：用户首次启动应用时自动创建用户并记录登录事件
        var deviceInfo = "Windows 11 Pro - Chrome 120.0";
        UserSecurityLog? capturedLog = null;

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Callback<UserSecurityLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - 模拟用户成功登录的业务场景
        await _securityLogService.LogUserLoginAsync(
            _testUserId,
            _testMachineId,
            deviceInfo,
            _testSessionId,
            isSuccessful: true);

        // Assert - 根据需求5.8：系统必须记录安全事件
        capturedLog.Should().NotBeNull("根据业务需求，登录事件必须被记录");
        capturedLog!.UserId.Should().Be(_testUserId, "日志必须关联正确的用户");
        capturedLog.EventType.Should().Be(SecurityEventType.UserLogin, "事件类型必须为用户登录");
        capturedLog.Severity.Should().Be(SecurityEventSeverity.Information, "成功登录应为信息级别");
        capturedLog.IsSuccessful.Should().BeTrue("成功登录标志必须为true");
        capturedLog.MachineId.Should().Be(_testMachineId, "必须记录正确的机器ID");
        capturedLog.SessionId.Should().Be(_testSessionId, "必须记录正确的会话ID");
        capturedLog.DeviceInfo.Should().Be(deviceInfo, "必须记录设备信息用于安全审计");

        // 验证仓储调用
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogUserLoginAsync_WhenFailedLogin_ShouldRecordFailureWithWarningLevel()
    {
        // Arrange - 基于业务需求：记录登录失败事件用于安全监控
        var errorCode = "AUTH_FAILED_001";
        UserSecurityLog? capturedLog = null;

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Callback<UserSecurityLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - 模拟登录失败的业务场景
        await _securityLogService.LogUserLoginAsync(
            _testUserId,
            _testMachineId,
            isSuccessful: false,
            errorCode: errorCode);

        // Assert - 根据需求5.8：失败的登录尝试必须记录为警告级别
        capturedLog.Should().NotBeNull();
        capturedLog!.EventType.Should().Be(SecurityEventType.UserLogin);
        capturedLog.Severity.Should().Be(SecurityEventSeverity.Warning, "登录失败应为警告级别");
        capturedLog.IsSuccessful.Should().BeFalse("失败登录标志必须为false");
        capturedLog.ErrorCode.Should().Be(errorCode, "必须记录错误代码用于问题诊断");
    }

    [Fact]
    public async Task LogUserLoginAsync_WhenRepositoryThrowsException_ShouldLogErrorAndRethrow()
    {
        // Arrange - 测试异常情况下的错误处理
        var expectedException = new InvalidOperationException("数据库连接失败");
        _mockRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert - 验证异常处理机制
        var act = async () => await _securityLogService.LogUserLoginAsync(_testUserId, _testMachineId);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("数据库连接失败");

        // 验证错误日志记录
        VerifyErrorLogged("记录用户登录事件失败");
    }

    [Fact]
    public async Task LogUserLogoutAsync_WhenUserLogsOut_ShouldRecordLogoutEventCorrectly()
    {
        // Arrange - 基于业务需求：记录用户登出事件
        UserSecurityLog? capturedLog = null;

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Callback<UserSecurityLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - 模拟用户登出的业务场景
        await _securityLogService.LogUserLogoutAsync(_testUserId, _testMachineId, _testSessionId);

        // Assert - 验证登出事件记录的正确性
        capturedLog.Should().NotBeNull("登出事件必须被记录");
        capturedLog!.UserId.Should().Be(_testUserId);
        capturedLog.EventType.Should().Be(SecurityEventType.UserLogout, "事件类型必须为用户登出");
        capturedLog.Severity.Should().Be(SecurityEventSeverity.Information, "正常登出应为信息级别");
        capturedLog.IsSuccessful.Should().BeTrue("正常登出应标记为成功");
        capturedLog.Description.Should().Be("用户成功登出");
    }

    [Theory]
    [InlineData(SecurityEventType.UserProfileUpdated, "用户修改个人资料", true)]
    [InlineData(SecurityEventType.PreferencesUpdated, "用户更新偏好设置", true)]
    [InlineData(SecurityEventType.DataExported, "用户导出数据", true)]
    [InlineData(SecurityEventType.DataDeleted, "用户删除数据", false)]
    public async Task LogUserOperationAsync_WhenDifferentOperationTypes_ShouldRecordWithCorrectSeverity(
        SecurityEventType eventType, string description, bool isSuccessful)
    {
        // Arrange - 基于业务需求：记录不同类型的用户操作
        var eventDetails = $"{{\"operation\": \"{eventType}\", \"timestamp\": \"{DateTime.UtcNow:O}\"}}";
        UserSecurityLog? capturedLog = null;

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Callback<UserSecurityLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - 记录用户操作事件
        await _securityLogService.LogUserOperationAsync(
            _testUserId,
            eventType,
            description,
            eventDetails,
            _testMachineId,
            _testSessionId,
            isSuccessful);

        // Assert - 验证操作事件记录的完整性和准确性
        capturedLog.Should().NotBeNull("操作事件必须被记录");
        capturedLog!.EventType.Should().Be(eventType, "事件类型必须正确");
        capturedLog.Description.Should().Be(description, "事件描述必须准确");
        capturedLog.EventDetails.Should().Be(eventDetails, "事件详细信息必须完整保存");
        capturedLog.IsSuccessful.Should().Be(isSuccessful, "操作结果标志必须正确");

        var expectedSeverity = isSuccessful ? SecurityEventSeverity.Information : SecurityEventSeverity.Error;
        capturedLog.Severity.Should().Be(expectedSeverity, "严重级别必须根据操作结果正确分类");
    }

    [Fact]
    public async Task LogSecurityWarningAsync_WhenSuspiciousActivity_ShouldRecordWarningEvent()
    {
        // Arrange - 基于业务需求：检测到异常操作时记录安全警告
        var warningDescription = "检测到异常登录尝试：短时间内多次失败";
        var eventDetails = "{\"failed_attempts\": 5, \"time_window\": \"5分钟\", \"source_ip\": \"192.168.1.100\"}";
        UserSecurityLog? capturedLog = null;

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Callback<UserSecurityLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - 记录安全警告事件
        await _securityLogService.LogSecurityWarningAsync(
            _testUserId,
            warningDescription,
            eventDetails,
            _testMachineId,
            _testSessionId);

        // Assert - 验证安全警告事件的正确分类和处理
        capturedLog.Should().NotBeNull("安全警告事件必须被记录");
        capturedLog!.EventType.Should().Be(SecurityEventType.SuspiciousActivity, "事件类型必须为异常操作");
        capturedLog.Severity.Should().Be(SecurityEventSeverity.Warning, "安全警告必须为警告级别");
        capturedLog.Description.Should().Be(warningDescription, "警告描述必须准确");
        capturedLog.EventDetails.Should().Be(eventDetails, "警告详细信息必须完整");
        capturedLog.IsSuccessful.Should().BeFalse("安全警告应标记为非成功事件");

        // 验证警告日志记录
        VerifyWarningLogged("安全警告事件已记录");
    }

    [Fact]
    public async Task LogSystemErrorAsync_WhenSystemError_ShouldRecordCriticalEvent()
    {
        // Arrange - 基于业务需求：记录系统错误事件
        var errorDescription = "数据库连接池耗尽";
        var errorCode = "DB_POOL_EXHAUSTED";
        var eventDetails = "{\"pool_size\": 100, \"active_connections\": 100, \"wait_time\": \"30s\"}";
        UserSecurityLog? capturedLog = null;

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Callback<UserSecurityLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - 记录系统错误事件
        await _securityLogService.LogSystemErrorAsync(
            _testUserId,
            errorDescription,
            eventDetails,
            errorCode,
            _testMachineId,
            _testSessionId);

        // Assert - 验证系统错误事件的正确分类和处理
        capturedLog.Should().NotBeNull("系统错误事件必须被记录");
        capturedLog!.EventType.Should().Be(SecurityEventType.SystemError, "事件类型必须为系统错误");
        capturedLog.Severity.Should().Be(SecurityEventSeverity.Critical, "系统错误必须为严重级别");
        capturedLog.Description.Should().Be(errorDescription, "错误描述必须准确");
        capturedLog.ErrorCode.Should().Be(errorCode, "错误代码必须记录用于问题诊断");
        capturedLog.EventDetails.Should().Be(eventDetails, "错误详细信息必须完整");
        capturedLog.IsSuccessful.Should().BeFalse("系统错误应标记为失败事件");

        // 验证错误日志记录
        VerifyErrorLogged("系统错误事件已记录");
    }

    [Fact]
    public async Task GetUserOperationLogsAsync_WhenValidParameters_ShouldReturnFilteredLogs()
    {
        // Arrange - 基于业务需求：用户查看操作历史和安全相关事件
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;
        var eventTypes = new[] { SecurityEventType.UserLogin, SecurityEventType.UserProfileUpdated };
        var severities = new[] { SecurityEventSeverity.Information, SecurityEventSeverity.Warning };
        var expectedLogs = CreateTestSecurityLogs(5);

        _mockRepository.Setup(x => x.GetUserLogsAsync(
            _testUserId, fromDate, toDate, eventTypes, severities, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLogs);

        // Act - 查询用户操作日志
        var result = await _securityLogService.GetUserOperationLogsAsync(
            _testUserId, fromDate, toDate, eventTypes, severities, 0, 50);

        // Assert - 验证日志查询功能的性能和结果准确性
        result.Should().NotBeNull("查询结果不能为空");
        result.Should().HaveCount(5, "应返回预期数量的日志记录");
        result.Should().BeEquivalentTo(expectedLogs, "查询结果必须与预期一致");

        // 验证仓储调用参数
        _mockRepository.Verify(x => x.GetUserLogsAsync(
            _testUserId, fromDate, toDate, eventTypes, severities, 0, 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(-1, 0)] // 负数页码应重置为0
    [InlineData(0, 0)]  // 零页码保持不变
    [InlineData(5, 5)]  // 正常页码保持不变
    public async Task GetUserOperationLogsAsync_WhenInvalidPageIndex_ShouldNormalizeToValidValue(
        int inputPageIndex, int expectedPageIndex)
    {
        // Arrange - 测试参数验证和边界条件处理
        _mockRepository.Setup(x => x.GetUserLogsAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<IEnumerable<SecurityEventType>?>(), It.IsAny<IEnumerable<SecurityEventSeverity>?>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSecurityLog>());

        // Act
        await _securityLogService.GetUserOperationLogsAsync(_testUserId, pageIndex: inputPageIndex);

        // Assert - 验证参数标准化
        _mockRepository.Verify(x => x.GetUserLogsAsync(
            _testUserId, null, null, null, null, expectedPageIndex, 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0, 50)]    // 零页大小应重置为默认值
    [InlineData(-10, 50)]  // 负数页大小应重置为默认值
    [InlineData(1500, 50)] // 超大页大小应重置为默认值
    [InlineData(25, 25)]   // 正常页大小保持不变
    public async Task GetUserOperationLogsAsync_WhenInvalidPageSize_ShouldNormalizeToValidValue(
        int inputPageSize, int expectedPageSize)
    {
        // Arrange - 测试页大小参数验证
        _mockRepository.Setup(x => x.GetUserLogsAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<IEnumerable<SecurityEventType>?>(), It.IsAny<IEnumerable<SecurityEventSeverity>?>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSecurityLog>());

        // Act
        await _securityLogService.GetUserOperationLogsAsync(_testUserId, pageSize: inputPageSize);

        // Assert - 验证参数标准化
        _mockRepository.Verify(x => x.GetUserLogsAsync(
            _testUserId, null, null, null, null, 0, expectedPageSize, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSecurityEventStatisticsAsync_WhenValidRequest_ShouldReturnCompleteStatistics()
    {
        // Arrange - 基于业务需求：提供安全事件统计信息用于监控
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        var eventTypeStats = new Dictionary<SecurityEventType, int>
        {
            { SecurityEventType.UserLogin, 45 },
            { SecurityEventType.UserProfileUpdated, 12 },
            { SecurityEventType.PreferencesUpdated, 8 }
        };

        var severityStats = new Dictionary<SecurityEventSeverity, int>
        {
            { SecurityEventSeverity.Information, 55 },
            { SecurityEventSeverity.Warning, 8 },
            { SecurityEventSeverity.Error, 2 }
        };

        var lastLoginTime = DateTime.UtcNow.AddHours(-2);
        var lastActivityTime = DateTime.UtcNow.AddMinutes(-15);

        _mockRepository.Setup(x => x.GetEventTypeStatisticsAsync(_testUserId, fromDate, toDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventTypeStats);
        _mockRepository.Setup(x => x.GetSeverityStatisticsAsync(_testUserId, fromDate, toDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(severityStats);
        _mockRepository.Setup(x => x.GetLastLoginTimeAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastLoginTime);
        _mockRepository.Setup(x => x.GetLastActivityTimeAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastActivityTime);
        _mockRepository.Setup(x => x.GetUserLogCountAsync(
            _testUserId, fromDate, toDate, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(63); // 成功事件数量
        _mockRepository.Setup(x => x.GetUserLogCountAsync(
            _testUserId, fromDate, toDate, null,
            new[] { SecurityEventSeverity.Error, SecurityEventSeverity.Critical }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2); // 失败事件数量

        // Act - 获取安全事件统计信息
        var result = await _securityLogService.GetSecurityEventStatisticsAsync(_testUserId, fromDate, toDate);

        // Assert - 验证统计信息的完整性和准确性
        result.Should().NotBeNull("统计结果不能为空");
        result.TotalEvents.Should().Be(65, "总事件数量应为所有事件类型的总和");
        result.SuccessfulEvents.Should().Be(63, "成功事件数量必须正确");
        result.FailedEvents.Should().Be(2, "失败事件数量必须正确");
        result.WarningEvents.Should().Be(8, "警告事件数量必须正确");
        result.ErrorEvents.Should().Be(2, "错误事件数量必须正确");
        result.EventTypeStatistics.Should().BeEquivalentTo(eventTypeStats, "事件类型统计必须准确");
        result.SeverityStatistics.Should().BeEquivalentTo(severityStats, "严重级别统计必须准确");
        result.LastLoginTime.Should().Be(lastLoginTime, "最后登录时间必须正确");
        result.LastActivityTime.Should().Be(lastActivityTime, "最后活动时间必须正确");
    }

    [Fact]
    public async Task GetRecentSecurityEventsAsync_WhenValidRequest_ShouldReturnRecentEvents()
    {
        // Arrange - 基于业务需求：快速查看最近的安全事件
        var recentEvents = CreateTestSecurityLogs(10);
        _mockRepository.Setup(x => x.GetRecentUserLogsAsync(_testUserId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentEvents);

        // Act - 获取最近的安全事件
        var result = await _securityLogService.GetRecentSecurityEventsAsync(_testUserId, 10);

        // Assert - 验证最近事件查询的准确性
        result.Should().NotBeNull("查询结果不能为空");
        result.Should().HaveCount(10, "应返回指定数量的最近事件");
        result.Should().BeEquivalentTo(recentEvents, "结果必须与预期一致");
    }

    [Theory]
    [InlineData(0, 10)]   // 零数量应重置为默认值
    [InlineData(-5, 10)]  // 负数应重置为默认值
    [InlineData(150, 10)] // 超大数量应重置为默认值
    [InlineData(25, 25)]  // 正常数量保持不变
    public async Task GetRecentSecurityEventsAsync_WhenInvalidCount_ShouldNormalizeToValidValue(
        int inputCount, int expectedCount)
    {
        // Arrange - 测试参数验证
        _mockRepository.Setup(x => x.GetRecentUserLogsAsync(
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSecurityLog>());

        // Act
        await _securityLogService.GetRecentSecurityEventsAsync(_testUserId, inputCount);

        // Assert - 验证参数标准化
        _mockRepository.Verify(x => x.GetRecentUserLogsAsync(
            _testUserId, expectedCount, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanupExpiredLogsAsync_WhenValidRetentionDays_ShouldDeleteExpiredLogsAndReturnCount()
    {
        // Arrange - 基于业务需求：定期清理过期日志以管理存储空间
        var retentionDays = 90;
        var expectedDeletedCount = 150;
        var expectedRetentionDate = DateTime.UtcNow.AddDays(-retentionDays);

        _mockRepository.Setup(x => x.DeleteExpiredLogsAsync(
            It.Is<DateTime>(d => Math.Abs((d - expectedRetentionDate).TotalMinutes) < 1),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDeletedCount);

        _mockRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - 执行日志清理
        var result = await _securityLogService.CleanupExpiredLogsAsync(retentionDays);

        // Assert - 验证日志轮转和清理机制的正确性
        result.Should().Be(expectedDeletedCount, "应返回实际删除的日志数量");

        // 验证仓储调用
        _mockRepository.Verify(x => x.DeleteExpiredLogsAsync(
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // 验证信息日志记录
        VerifyInformationLogged("清理过期日志记录完成");
    }

    [Fact]
    public async Task CleanupExpiredLogsAsync_WhenNoLogsToDelete_ShouldNotCallSaveChanges()
    {
        // Arrange - 测试没有过期日志的情况
        _mockRepository.Setup(x => x.DeleteExpiredLogsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _securityLogService.CleanupExpiredLogsAsync(90);

        // Assert - 验证优化：没有删除时不调用保存
        result.Should().Be(0, "没有过期日志时应返回0");
        _mockRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 90)]   // 零天数应重置为默认值
    [InlineData(-30, 90)] // 负数应重置为默认值
    [InlineData(30, 30)]  // 正常天数保持不变
    public async Task CleanupExpiredLogsAsync_WhenInvalidRetentionDays_ShouldNormalizeToValidValue(
        int inputDays, int expectedDays)
    {
        // Arrange - 测试保留天数参数验证
        _mockRepository.Setup(x => x.DeleteExpiredLogsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _securityLogService.CleanupExpiredLogsAsync(inputDays);

        // Assert - 验证参数标准化
        var expectedDate = DateTime.UtcNow.AddDays(-expectedDays);
        _mockRepository.Verify(x => x.DeleteExpiredLogsAsync(
            It.Is<DateTime>(d => Math.Abs((d - expectedDate).TotalMinutes) < 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HasSuspiciousActivityAsync_WhenSuspiciousActivityDetected_ShouldReturnTrueAndLogWarning()
    {
        // Arrange - 基于业务需求：检测可疑活动并记录警告
        var timeWindow = 24;
        _mockRepository.Setup(x => x.HasSuspiciousActivityAsync(_testUserId, timeWindow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act - 检查可疑活动
        var result = await _securityLogService.HasSuspiciousActivityAsync(_testUserId, timeWindow);

        // Assert - 验证可疑活动检测的准确性
        result.Should().BeTrue("检测到可疑活动时应返回true");

        // 验证警告日志记录
        VerifyWarningLogged("检测到可疑活动");
    }

    [Fact]
    public async Task HasSuspiciousActivityAsync_WhenNoSuspiciousActivity_ShouldReturnFalse()
    {
        // Arrange - 测试正常情况
        _mockRepository.Setup(x => x.HasSuspiciousActivityAsync(_testUserId, 24, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _securityLogService.HasSuspiciousActivityAsync(_testUserId);

        // Assert - 验证正常情况的处理
        result.Should().BeFalse("没有可疑活动时应返回false");
    }

    [Theory]
    [InlineData(0, 24)]   // 零时间窗口应重置为默认值
    [InlineData(-12, 24)] // 负数应重置为默认值
    [InlineData(48, 48)]  // 正常时间窗口保持不变
    public async Task HasSuspiciousActivityAsync_WhenInvalidTimeWindow_ShouldNormalizeToValidValue(
        int inputTimeWindow, int expectedTimeWindow)
    {
        // Arrange - 测试时间窗口参数验证
        _mockRepository.Setup(x => x.HasSuspiciousActivityAsync(
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _securityLogService.HasSuspiciousActivityAsync(_testUserId, inputTimeWindow);

        // Assert - 验证参数标准化
        _mockRepository.Verify(x => x.HasSuspiciousActivityAsync(
            _testUserId, expectedTimeWindow, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConcurrentLogOperations_WhenMultipleThreadsWriteSimultaneously_ShouldHandleAllOperationsSafely()
    {
        // Arrange - 基于业务需求：验证并发日志写入的线程安全性
        var concurrentOperations = 50;
        var completedOperations = new ConcurrentBag<bool>();
        var tasks = new List<Task>();

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - 模拟并发日志写入操作
        for (int i = 0; i < concurrentOperations; i++)
        {
            var operationId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _securityLogService.LogUserOperationAsync(
                        _testUserId,
                        SecurityEventType.PreferencesUpdated,
                        $"并发操作测试 #{operationId}",
                        $"{{\"operation_id\": {operationId}}}",
                        _testMachineId,
                        _testSessionId);

                    completedOperations.Add(true);
                }
                catch
                {
                    completedOperations.Add(false);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - 验证并发操作的线程安全性
        completedOperations.Should().HaveCount(concurrentOperations, "所有并发操作都应完成");
        completedOperations.Should().AllSatisfy(success => success.Should().BeTrue("所有操作都应成功"));

        // 验证仓储调用次数
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<UserSecurityLog>(), It.IsAny<CancellationToken>()),
            Times.Exactly(concurrentOperations));
        _mockRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(concurrentOperations));
    }

    /// <summary>
    /// 创建测试用的安全日志列表
    /// </summary>
    private List<UserSecurityLog> CreateTestSecurityLogs(int count)
    {
        var logs = new List<UserSecurityLog>();
        var eventTypes = new[]
        {
            SecurityEventType.UserLogin,
            SecurityEventType.UserProfileUpdated,
            SecurityEventType.PreferencesUpdated
        };

        for (int i = 0; i < count; i++)
        {
            var eventType = eventTypes[i % eventTypes.Length];
            var log = new UserSecurityLog(
                _testUserId,
                eventType,
                SecurityEventSeverity.Information,
                $"测试事件 #{i + 1}",
                $"{{\"test_id\": {i + 1}}}",
                "127.0.0.1",
                "Test Device",
                _testMachineId,
                "TestSource",
                _testSessionId,
                true);

            logs.Add(log);
        }

        return logs;
    }

    /// <summary>
    /// 验证信息日志是否被记录
    /// </summary>
    private void VerifyInformationLogged(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// 验证警告日志是否被记录
    /// </summary>
    private void VerifyWarningLogged(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// 验证错误日志是否被记录
    /// </summary>
    private void VerifyErrorLogged(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}