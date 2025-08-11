using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lorn.OpenAgenticAI.Domain.Models.UserManagement;

/// <summary>
/// 用户快捷键实体，管理用户自定义的快捷键和快速访问配置
/// </summary>
[Table("UserShortcuts")]
public class UserShortcut
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [Key]
    public Guid Id { get; private set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; private set; }
    /// <summary>
    /// 用户ID
    /// </summary>
    [Required]
    public Guid UserId { get; private set; }

    /// <summary>
    /// 快捷键名称
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// 快捷键组合 (如: Ctrl+Shift+N)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string KeyCombination { get; private set; } = string.Empty;

    /// <summary>
    /// 动作类型 (OpenWorkflow, ExecuteAgent, OpenDialog, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ActionType { get; private set; } = string.Empty;

    /// <summary>
    /// 动作数据 (JSON格式，包含执行动作所需的参数)
    /// </summary>
    [MaxLength(2000)]
    public string ActionData { get; private set; } = string.Empty;

    /// <summary>
    /// 快捷键描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; private set; }

    /// <summary>
    /// 快捷键分类
    /// </summary>
    [MaxLength(100)]
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// 是否全局快捷键（系统级别）
    /// </summary>
    public bool IsGlobal { get; private set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; private set; }

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; private set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    public virtual UserProfile User { get; private set; } = null!;

    /// <summary>
    /// 私有构造函数用于EF Core
    /// </summary>
    private UserShortcut()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsEnabled = true;
        IsGlobal = false;
        UsageCount = 0;
        SortOrder = 0;
    }

    /// <summary>
    /// 创建新的用户快捷键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="name">快捷键名称</param>
    /// <param name="keyCombination">按键组合</param>
    /// <param name="actionType">动作类型</param>
    /// <param name="actionData">动作数据</param>
    /// <param name="description">描述</param>
    /// <param name="category">分类</param>
    /// <param name="isGlobal">是否全局快捷键</param>
    /// <param name="sortOrder">排序顺序</param>
    public UserShortcut(
        Guid userId,
        string name,
        string keyCombination,
        string actionType,
        string actionData = "",
        string? description = null,
        string category = "",
        bool isGlobal = false,
        int sortOrder = 0)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(keyCombination))
            throw new ArgumentException("Key combination cannot be empty", nameof(keyCombination));
        if (string.IsNullOrWhiteSpace(actionType))
            throw new ArgumentException("Action type cannot be empty", nameof(actionType));

        // 容错：部分导入/测试数据可能提供 null 的 actionData 或 category，这里统一归一化为空字符串
        actionData ??= string.Empty;
        category ??= string.Empty;

        Id = Guid.NewGuid();
        UserId = userId;
        Name = name.Trim();
        KeyCombination = keyCombination.Trim();
        ActionType = actionType.Trim();
        ActionData = actionData.Trim();
        Description = description?.Trim();
        Category = category.Trim();
        IsEnabled = true;
        IsGlobal = isGlobal;
        SortOrder = sortOrder;
        CreatedAt = DateTime.UtcNow;
        UsageCount = 0;
    }

    /// <summary>
    /// 更新快捷键信息
    /// </summary>
    /// <param name="name">新名称</param>
    /// <param name="keyCombination">新按键组合</param>
    /// <param name="actionType">新动作类型</param>
    /// <param name="actionData">新动作数据</param>
    /// <param name="description">新描述</param>
    /// <param name="category">新分类</param>
    /// <param name="isGlobal">是否全局快捷键</param>
    public void UpdateShortcut(
        string? name = null,
        string? keyCombination = null,
        string? actionType = null,
        string? actionData = null,
        string? description = null,
        string? category = null,
        bool? isGlobal = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();

        if (!string.IsNullOrWhiteSpace(keyCombination))
            KeyCombination = keyCombination.Trim();

        if (!string.IsNullOrWhiteSpace(actionType))
            ActionType = actionType.Trim();

        if (actionData != null)
            ActionData = actionData.Trim();

        if (description != null)
            Description = description.Trim();

        if (category != null)
            Category = category.Trim();

        if (isGlobal.HasValue)
            IsGlobal = isGlobal.Value;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 启用快捷键
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 禁用快捷键
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新排序顺序
    /// </summary>
    /// <param name="newSortOrder">新的排序顺序</param>
    public void UpdateSortOrder(int newSortOrder)
    {
        SortOrder = newSortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 记录使用
    /// </summary>
    public void RecordUsage()
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取动作数据对象
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <returns>反序列化的动作数据</returns>
    public T? GetActionData<T>() where T : class
    {
        if (string.IsNullOrWhiteSpace(ActionData))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(ActionData);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 设置动作数据对象
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="data">要序列化的数据</param>
    public void SetActionData<T>(T data) where T : class
    {
        if (data == null)
        {
            ActionData = string.Empty;
        }
        else
        {
            ActionData = System.Text.Json.JsonSerializer.Serialize(data);
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 解析按键组合
    /// </summary>
    /// <returns>按键组合的各个部分</returns>
    public KeyCombinationInfo ParseKeyCombination()
    {
        var parts = KeyCombination.Split('+', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(part => part.Trim())
                                  .ToList();

        var modifiers = new List<string>();
        string? mainKey = null;

        foreach (var part in parts)
        {
            var upperPart = part.ToUpperInvariant();
            if (upperPart is "CTRL" or "ALT" or "SHIFT" or "WIN" or "CMD")
            {
                modifiers.Add(upperPart);
            }
            else
            {
                mainKey = part;
            }
        }

        return new KeyCombinationInfo(modifiers, mainKey);
    }
}

/// <summary>
/// 按键组合信息
/// </summary>
/// <param name="Modifiers">修饰键列表</param>
/// <param name="MainKey">主按键</param>
public record KeyCombinationInfo(List<string> Modifiers, string? MainKey);

/// <summary>
/// 快捷键动作类型枚举
/// </summary>
public static class ShortcutActionTypes
{
    public const string OpenWorkflow = "OpenWorkflow";
    public const string ExecuteAgent = "ExecuteAgent";
    public const string OpenDialog = "OpenDialog";
    public const string ExecuteCommand = "ExecuteCommand";
    public const string OpenFavorites = "OpenFavorites";
    public const string QuickSearch = "QuickSearch";
    public const string SwitchUser = "SwitchUser";
    public const string ShowSettings = "ShowSettings";
    public const string ToggleTheme = "ToggleTheme";
    public const string NewWorkflow = "NewWorkflow";
    public const string SaveWorkflow = "SaveWorkflow";
    public const string RunWorkflow = "RunWorkflow";
    public const string StopExecution = "StopExecution";
    public const string ShowHelp = "ShowHelp";
    public const string MinimizeWindow = "MinimizeWindow";
    public const string MaximizeWindow = "MaximizeWindow";
    public const string CloseWindow = "CloseWindow";
}
