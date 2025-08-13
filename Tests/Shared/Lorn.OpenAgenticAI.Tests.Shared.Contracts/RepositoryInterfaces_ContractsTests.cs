using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using Moq;
using Lorn.OpenAgenticAI.Shared.Contracts.Repositories;
using Lorn.OpenAgenticAI.Domain.Contracts.Repositories;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Domain.Models.Execution;

namespace Lorn.OpenAgenticAI.Tests.Shared.Contracts;

public class RepositoryInterfaces_ContractsTests
{
    [Fact]
    public void GenericInterfaces_Should_Exist_And_Have_Constraints()
    {
        var irepo = typeof(IRepository<>);
        var iasync = typeof(IAsyncRepository<>);
        Assert.True(irepo.IsInterface);
        Assert.True(iasync.IsInterface);
        Assert.True(irepo.GetGenericArguments().Length == 1);
        Assert.True(iasync.GetGenericArguments().Length == 1);
        // 约束：class
        var repoConstraint = irepo.GetGenericArguments()[0].GenericParameterAttributes;
        var asyncConstraint = iasync.GetGenericArguments()[0].GenericParameterAttributes;
        Assert.True(repoConstraint.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        Assert.True(asyncConstraint.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
    }

    [Fact]
    public void DomainRepositories_Should_Inherit_GenericInterfaces()
    {
        typeof(IUserProfileRepository).GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRepository<>));
        typeof(IUserProfileRepository).GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncRepository<>));

        typeof(ITaskExecutionRepository).GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRepository<>));
        typeof(ITaskExecutionRepository).GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncRepository<>));
    }

    [Fact]
    public void DomainRepositories_Methods_Should_Have_Expected_Signatures()
    {
        // IUserProfileRepository
        var upr = typeof(IUserProfileRepository);
        var getByUserName = upr.GetMethod("GetByUserName");
        Assert.NotNull(getByUserName);
        Assert.Equal(typeof(UserProfile), getByUserName!.ReturnType);
        Assert.Single(getByUserName!.GetParameters());

        var getByUserNameAsync = upr.GetMethod("GetByUserNameAsync");
        Assert.NotNull(getByUserNameAsync);
        Assert.True(getByUserNameAsync!.ReturnType.IsGenericType);

        // ITaskExecutionRepository
        var ter = typeof(ITaskExecutionRepository);
        var getByRequestId = ter.GetMethod("GetByRequestId");
        Assert.NotNull(getByRequestId);
        Assert.Equal(typeof(TaskExecutionHistory), getByRequestId!.ReturnType);
        Assert.Single(getByRequestId!.GetParameters());

        var listStepsAsync = ter.GetMethod("ListStepsAsync");
        Assert.NotNull(listStepsAsync);
        Assert.True(listStepsAsync!.ReturnType.IsGenericType);
    }

    [Fact]
    public void Interfaces_Should_Be_DI_Compatible()
    {
        var services = new ServiceCollection();
        var userRepoMock = new Mock<IUserProfileRepository>();
        var taskRepoMock = new Mock<ITaskExecutionRepository>();
        services.AddSingleton<IUserProfileRepository>(userRepoMock.Object);
        services.AddSingleton<ITaskExecutionRepository>(taskRepoMock.Object);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IUserProfileRepository>());
        Assert.NotNull(provider.GetService<ITaskExecutionRepository>());
    }
}
