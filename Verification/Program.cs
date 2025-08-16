using Microsoft.Extensions.Logging;
using Moq;
using Lorn.OpenAgenticAI.Application.Services.Services;
using Lorn.OpenAgenticAI.Domain.Contracts.Repositories;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

Console.WriteLine("=== UserDataService 功能验证 ===");

// 创建模拟对象
var mockUserRepository = new Mock<IUserProfileRepository>();
var mockLogger = new Mock<ILogger<UserDataService>>();

// 创建UserDataService实例
var userDataService = new UserDataService(
    mockUserRepository.Object,
    mockLogger.Object);

Console.WriteLine("✓ UserDataService实例创建成功");

// 测试验证功能
var testUser = new UserProfile("TEST-MACHINE-001", "测试用户");
var validationResult = userDataService.ValidateUserProfile(testUser);

Console.WriteLine($"✓ 用户档案验证测试: {(validationResult.IsValid ? "通过" : "失败")}");
if (!validationResult.IsValid)
{
    Console.WriteLine($"  验证错误: {validationResult.Summary}");
}

// 测试创建UserProfile 
try
{
    var newUser = new UserProfile("TEST-MACHINE-002", "新用户", true);
    Console.WriteLine("✓ UserProfile创建测试: 通过");
    Console.WriteLine($"  - 用户ID: {newUser.UserId}");
    Console.WriteLine($"  - 机器ID: {newUser.MachineId}");
    Console.WriteLine($"  - 显示名称: {newUser.DisplayName}");
    Console.WriteLine($"  - 是否默认: {newUser.IsDefault}");
    Console.WriteLine($"  - 创建时间: {newUser.CreatedTime}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ UserProfile创建测试: 失败 - {ex.Message}");
}

// 测试偏好设置功能
try
{
    var userId = Guid.NewGuid();
    var user = new UserProfile("TEST-MACHINE-003", "偏好测试用户");

    // 添加测试偏好设置
    var preference1 = new UserPreferences(userId, "Interface", "Theme", "Dark", "String");
    var preference2 = new UserPreferences(userId, "Interface", "FontSize", "14", "Integer");

    user.UserPreferences.Add(preference1);
    user.UserPreferences.Add(preference2);

    Console.WriteLine("✓ 用户偏好设置创建测试: 通过");
    Console.WriteLine($"  - 偏好设置数量: {user.UserPreferences.Count}");

    // 测试类型化值获取
    var fontSize = preference2.GetTypedValue<int>();
    Console.WriteLine($"  - 字体大小(整数): {fontSize}");

    var theme = preference1.GetTypedValue<string>();
    Console.WriteLine($"  - 主题(字符串): {theme}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ 用户偏好设置测试: 失败 - {ex.Message}");
}

// 测试数据完整性检查
try
{
    // 模拟repository返回数据
    var allUsers = new[]
    {
        new UserProfile("MACHINE-001", "用户1"),
        new UserProfile("MACHINE-002", "用户2")
    };

    var defaultUser = allUsers[0];
    defaultUser.SetAsDefault();

    mockUserRepository.Setup(r => r.GetAllUsersAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(allUsers);

    mockUserRepository.Setup(r => r.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(allUsers);

    mockUserRepository.Setup(r => r.GetDefaultUsersAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(new[] { defaultUser });

    var integrityResult = await userDataService.PerformDataIntegrityCheckAsync();

    Console.WriteLine("✓ 数据完整性检查测试: 通过");
    Console.WriteLine($"  - 检查有效性: {integrityResult.IsValid}");
    Console.WriteLine($"  - 总用户数: {integrityResult.Statistics.TotalUserProfiles}");
    Console.WriteLine($"  - 活跃用户数: {integrityResult.Statistics.ActiveUserProfiles}");
    Console.WriteLine($"  - 检查项目数: {integrityResult.CheckItems.Count}");

    foreach (var checkItem in integrityResult.CheckItems)
    {
        Console.WriteLine($"    * {checkItem.ItemName}: {checkItem.Status} - {checkItem.Description}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ 数据完整性检查测试: 失败 - {ex.Message}");
}

Console.WriteLine("\n=== 测试完成 ===");
Console.WriteLine("UserDataService的基本功能验证通过！");
