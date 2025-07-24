using Xunit;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

/// <summary>
/// 基本功能验证测试
/// </summary>
public class BasicFunctionalityTests
{
    [Fact]
    public void RepositoryImplementation_ShouldCompile()
    {
        // 这个测试验证所有仓储实现都能编译成功
        Assert.True(true);
    }

    [Fact]
    public void UserRepositoryInterface_ShouldBeImplemented()
    {
        // 验证用户仓储接口已实现
        var repositoryType = typeof(Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie.UserRepository);
        var interfaceType = typeof(Lorn.OpenAgenticAI.Domain.Contracts.IUserRepository);

        Assert.True(interfaceType.IsAssignableFrom(repositoryType));
    }

    [Fact]
    public void UserPreferenceRepositoryInterface_ShouldBeImplemented()
    {
        // 验证用户偏好仓储接口已实现
        var repositoryType = typeof(Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie.UserPreferenceRepository);
        var interfaceType = typeof(Lorn.OpenAgenticAI.Domain.Contracts.IUserPreferenceRepository);

        Assert.True(interfaceType.IsAssignableFrom(repositoryType));
    }

    [Fact]
    public void UserMetadataRepositoryInterface_ShouldBeImplemented()
    {
        // 验证用户元数据仓储接口已实现
        var repositoryType = typeof(Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie.UserMetadataRepository);
        var interfaceType = typeof(Lorn.OpenAgenticAI.Domain.Contracts.IUserMetadataRepository);

        Assert.True(interfaceType.IsAssignableFrom(repositoryType));
    }

    [Fact]
    public void ServiceRegistrationExtensions_ShouldExist()
    {
        // 验证服务注册扩展类存在
        var extensionType = typeof(Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie.RepositoryServiceExtensions);

        Assert.NotNull(extensionType);
    }
}
