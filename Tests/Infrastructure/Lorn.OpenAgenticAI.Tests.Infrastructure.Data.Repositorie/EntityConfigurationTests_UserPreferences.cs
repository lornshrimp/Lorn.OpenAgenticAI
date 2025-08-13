using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

public class EntityConfigurationTests_UserPreferences : EfSqliteTestBase
{
    [Fact]
    public async Task UserPreferences_BasicMapping_CreatesAndReads()
    {
        var user = new UserProfile(
                Guid.NewGuid(),
                "u1",
                "u1@x.com",
                new Lorn.OpenAgenticAI.Domain.Models.ValueObjects.SecuritySettings(
                    authenticationMethod: "Pwd",
                    sessionTimeoutMinutes: 30,
                    requireTwoFactor: false,
                    passwordLastChanged: DateTime.UtcNow,
                    additionalSettings: new System.Collections.Generic.Dictionary<string, string>())
        );
        var pref = new UserPreferences(user.UserId, "UI", "Theme", "dark", "String");

        using (var ctx = CreateContext())
        {
            ctx.UserProfiles.Add(user);
            ctx.UserPreferences.Add(pref);
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CreateContext())
        {
            var loaded = await ctx.UserPreferences.Include(p => p.User).FirstAsync();
            Assert.Equal("UI", loaded.PreferenceCategory);
            Assert.Equal("Theme", loaded.PreferenceKey);
            Assert.Equal("dark", loaded.PreferenceValue);
            Assert.Equal(user.UserId, loaded.UserId);
            Assert.NotNull(loaded.User);
        }
    }

    [Fact]
    public async Task UserPreferences_UniqueIndex_Enforced()
    {
        var user = new UserProfile(
                Guid.NewGuid(),
                "u2",
                "u2@x.com",
                new Lorn.OpenAgenticAI.Domain.Models.ValueObjects.SecuritySettings(
                    authenticationMethod: "Pwd",
                    sessionTimeoutMinutes: 30,
                    requireTwoFactor: false,
                    passwordLastChanged: DateTime.UtcNow,
                    additionalSettings: new System.Collections.Generic.Dictionary<string, string>())
        );
        using (var ctx = CreateContext())
        {
            ctx.UserProfiles.Add(user);
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CreateContext())
        {
            ctx.UserPreferences.Add(new UserPreferences(user.UserId, "UI", "Theme", "dark", "String"));
            ctx.UserPreferences.Add(new UserPreferences(user.UserId, "UI", "Theme", "light", "String"));
            await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
        }
    }
}
