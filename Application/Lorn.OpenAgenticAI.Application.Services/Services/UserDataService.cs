using Microsoft.Extensions.Logging;
using Lorn.OpenAgenticAI.Application.Services.Interfaces;
using Lorn.OpenAgenticAI.Domain.Contracts.Repositories;
using Lorn.OpenAgenticAI.Domain.Models.UserManagement;

namespace Lorn.OpenAgenticAI.Application.Services.Services;

/// <summary>
/// 用户数据服务实现（纯数据访问封装）
/// 仅负责调用仓储，不做业务规则/验证/事件。
/// </summary>
public class UserDataService : IUserDataService
{
    private readonly IUserProfileRepository _userRepository;
    private readonly ILogger<UserDataService> _logger;

    public UserDataService(IUserProfileRepository userRepository, ILogger<UserDataService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 用户档案
    public Task<UserProfile?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
        => _userRepository.GetByIdAsync(userId, cancellationToken);

    public Task<UserProfile?> GetUserProfileByMachineIdAsync(string machineId, CancellationToken cancellationToken = default)
        => _userRepository.GetByMachineIdAsync(machineId, cancellationToken);

    public Task<UserProfile?> GetUserProfileByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => _userRepository.GetByUserNameAsync(username, cancellationToken);

    public Task<UserProfile?> GetUserProfileByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _userRepository.GetByEmailAsync(email, cancellationToken);

    public async Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        await _userRepository.AddAsync(userProfile, cancellationToken);
        return userProfile;
    }

    public async Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        await _userRepository.UpdateAsync(userProfile, cancellationToken);
        return userProfile;
    }

    public async Task<bool> DeleteUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (existing == null) return false;
        // 软删除语义保持：调用领域模型方法后更新
        existing.Deactivate();
        await _userRepository.UpdateAsync(existing, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<UserProfile>> GetAllUserProfilesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        IEnumerable<UserProfile> users = includeInactive
            ? await _userRepository.GetAllUsersAsync(cancellationToken)
            : await _userRepository.GetActiveUsersAsync(cancellationToken);
        return users.ToList().AsReadOnly();
    }

    public async Task<UserProfile?> GetDefaultUserProfileAsync(CancellationToken cancellationToken = default)
    {
        var defaults = await _userRepository.GetDefaultUsersAsync(cancellationToken);
        return defaults.FirstOrDefault();
    }

    public Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        => _userRepository.IsUsernameExistsAsync(username, excludeUserId, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        => _userRepository.IsEmailExistsAsync(email, excludeUserId, cancellationToken);
    #endregion

    #region 偏好设置
    public async Task<UserPreferences?> GetUserPreferenceAsync(Guid userId, string category, string key, CancellationToken cancellationToken = default)
    {
        var profile = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return profile?.UserPreferences.FirstOrDefault(p => p.PreferenceCategory == category && p.PreferenceKey == key);
    }

    public async Task<IReadOnlyList<UserPreferences>> GetUserPreferencesAsync(Guid userId, string? category = null, CancellationToken cancellationToken = default)
    {
        var profile = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (profile == null) return Array.Empty<UserPreferences>();
        var query = profile.UserPreferences.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.PreferenceCategory == category);
        return query.ToList().AsReadOnly();
    }

    public async Task<UserPreferences> SaveUserPreferenceAsync(UserPreferences preference, CancellationToken cancellationToken = default)
    {
        var profile = await _userRepository.GetByIdAsync(preference.UserId, cancellationToken) ?? throw new InvalidOperationException("用户不存在");
        var existing = profile.UserPreferences.FirstOrDefault(p => p.PreferenceCategory == preference.PreferenceCategory && p.PreferenceKey == preference.PreferenceKey);
        if (existing != null)
        {
            existing.UpdateValue(preference.PreferenceValue);
            if (!string.IsNullOrWhiteSpace(preference.Description)) existing.UpdateDescription(preference.Description);
        }
        else
        {
            profile.UserPreferences.Add(preference);
        }
        await _userRepository.UpdateAsync(profile, cancellationToken);
        return preference;
    }

    public async Task<int> DeleteUserPreferencesAsync(Guid userId, string category, string? key = null, CancellationToken cancellationToken = default)
    {
        var profile = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (profile == null) return 0;
        var toRemove = profile.UserPreferences.Where(p => p.PreferenceCategory == category && (key == null || p.PreferenceKey == key)).ToList();
        foreach (var p in toRemove) profile.UserPreferences.Remove(p);
        if (toRemove.Count > 0) await _userRepository.UpdateAsync(profile, cancellationToken);
        return toRemove.Count;
    }

    public async Task<int> SaveUserPreferencesBatchAsync(IEnumerable<UserPreferences> preferences, CancellationToken cancellationToken = default)
    {
        if (preferences == null) return 0;
        // 规则：同一 (UserId, Category, Key) 最后一次出现生效；Value == null 或空字符串表示删除该偏好。
        // 先聚合到内存字典以避免同一用户多次往返持久化。
        var aggregated = preferences
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g
                .GroupBy(p => (p.PreferenceCategory, p.PreferenceKey))
                .Select(grp => grp.Last()) // 保留最后一次
                .ToList());

        int affected = 0;
        foreach (var kv in aggregated)
        {
            var profile = await _userRepository.GetByIdAsync(kv.Key, cancellationToken);
            if (profile == null) continue;

            // 建立索引提高查找效率
            var index = profile.UserPreferences
                .GroupBy(p => (p.PreferenceCategory, p.PreferenceKey))
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var pref in kv.Value)
            {
                var key = (pref.PreferenceCategory, pref.PreferenceKey);
                bool isRemove = string.IsNullOrWhiteSpace(pref.PreferenceValue); // 约定：空值表示删除
                if (isRemove)
                {
                    if (index.TryGetValue(key, out var existingRemove))
                    {
                        profile.UserPreferences.Remove(existingRemove);
                        affected++;
                    }
                    continue;
                }

                if (index.TryGetValue(key, out var existing))
                {
                    existing.UpdateValue(pref.PreferenceValue);
                    if (!string.IsNullOrWhiteSpace(pref.Description)) existing.UpdateDescription(pref.Description);
                    affected++;
                }
                else
                {
                    // 复制一份以防外部集合后续被修改
                    var clone = new UserPreferences(pref.UserId, pref.PreferenceCategory, pref.PreferenceKey, pref.PreferenceValue, pref.Description);
                    profile.UserPreferences.Add(clone);
                    affected++;
                }
            }

            await _userRepository.UpdateAsync(profile, cancellationToken);
        }
        return affected;
    }
    #endregion
}
