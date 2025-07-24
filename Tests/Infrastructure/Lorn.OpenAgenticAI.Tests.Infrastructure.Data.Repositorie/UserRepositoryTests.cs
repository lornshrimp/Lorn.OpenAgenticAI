using Lorn.OpenAgenticAI.Domain.Models.UserManagement;
using Lorn.OpenAgenticAI.Infrastructure.Data.Repositorie;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

public class UserRepositoryTests : RepositoryTestBase
{
    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        var repository = new UserRepository(DbContext, GetMockLogger<UserRepository>().Object);
        var user = CreateTestUser();

        // Act
        var result = await repository.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Assert
        var savedUser = await DbContext.UserProfiles.FirstOrDefaultAsync(u => u.UserId == user.UserId);
        Assert.NotNull(savedUser);
        Assert.Equal(user.Username, savedUser.Username);
        Assert.Equal(user.Email, savedUser.Email);
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result.UserId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCorrectUser()
    {
        // Arrange
        var repository = new UserRepository(DbContext, GetMockLogger<UserRepository>().Object);
        var user = CreateTestUser();
        await repository.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(user.UserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetByUsernameAsync_ShouldReturnCorrectUser()
    {
        // Arrange
        var repository = new UserRepository(DbContext, GetMockLogger<UserRepository>().Object);
        var user = CreateTestUser();
        await repository.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetByUsernameAsync(user.Username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnCorrectUser()
    {
        // Arrange
        var repository = new UserRepository(DbContext, GetMockLogger<UserRepository>().Object);
        var user = CreateTestUser();
        await repository.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetByEmailAsync(user.Email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingUser()
    {
        // Arrange
        var repository = new UserRepository(DbContext, GetMockLogger<UserRepository>().Object);
        var user = CreateTestUser();
        await repository.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Modify user (only modifiable properties)
        user.Email = "updated@example.com";

        // Act
        var result = await repository.UpdateAsync(user);
        await DbContext.SaveChangesAsync();

        // Assert
        var updatedUser = await repository.GetByIdAsync(user.UserId);
        Assert.NotNull(updatedUser);
        Assert.Equal("updated@example.com", updatedUser.Email);
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result.UserId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUserFromDatabase()
    {
        // Arrange
        var repository = new UserRepository(DbContext, GetMockLogger<UserRepository>().Object);
        var user = CreateTestUser();
        await repository.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await repository.DeleteAsync(user.UserId);
        await DbContext.SaveChangesAsync();

        // Assert
        var deletedUser = await repository.GetByIdAsync(user.UserId);
        Assert.Null(deletedUser);
        Assert.True(result);
    }

    [Fact]
    public async Task IsUsernameExistsAsync_ShouldReturnTrueForExistingUsername()
    {
        // Arrange
        var repository = new UserRepository(DbContext, GetMockLogger<UserRepository>().Object);
        var user = CreateTestUser();
        await repository.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Act
        var exists = await repository.IsUsernameExistsAsync(user.Username);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task IsUsernameExistsAsync_ShouldReturnFalseForNewUsername()
    {
        // Arrange
        var repository = new UserRepository(DbContext, GetMockLogger<UserRepository>().Object);

        // Act
        var exists = await repository.IsUsernameExistsAsync("newusername");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task IsEmailExistsAsync_ShouldReturnTrueForExistingEmail()
    {
        // Arrange
        var repository = new UserRepository(DbContext, GetMockLogger<UserRepository>().Object);
        var user = CreateTestUser();
        await repository.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Act
        var exists = await repository.IsEmailExistsAsync(user.Email);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task IsEmailExistsAsync_ShouldReturnFalseForNewEmail()
    {
        // Arrange
        var repository = new UserRepository(DbContext, GetMockLogger<UserRepository>().Object);

        // Act
        var exists = await repository.IsEmailExistsAsync("newemail@example.com");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetActiveUsersAsync_ShouldReturnOnlyActiveUsers()
    {
        // Arrange
        var repository = new UserRepository(DbContext, GetMockLogger<UserRepository>().Object);
        var user1 = CreateTestUser("user1", "user1@example.com");
        var user2 = CreateTestUser("user2", "user2@example.com");

        await repository.AddAsync(user1);
        await repository.AddAsync(user2);

        // Soft delete one user
        await repository.SoftDeleteAsync(user2.UserId);
        await DbContext.SaveChangesAsync();

        // Act
        var activeUsers = await repository.GetActiveUsersAsync();

        // Assert
        Assert.Single(activeUsers);
        Assert.Equal(user1.UserId, activeUsers.First().UserId);
    }

    [Fact]
    public async Task GetUserCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var repository = new UserRepository(DbContext, GetMockLogger<UserRepository>().Object);
        var user1 = CreateTestUser("user1", "user1@example.com");
        var user2 = CreateTestUser("user2", "user2@example.com");

        await repository.AddAsync(user1);
        await repository.AddAsync(user2);
        await DbContext.SaveChangesAsync();

        // Act
        var count = await repository.GetUserCountAsync();

        // Assert
        Assert.Equal(2, count);
    }
}
