using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lorn.OpenAgenticAI.Domain.Models.UserManagement;

/// <summary>
/// 用户收藏实体，管理用户收藏的工作流、模板和Agent等内容
/// </summary>
[Table("UserFavorites")]
public class UserFavorite
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
    /// 收藏项类型 (Workflow, Template, Agent, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ItemType { get; private set; } = string.Empty;

    /// <summary>
    /// 收藏项ID
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string ItemId { get; private set; } = string.Empty;

    /// <summary>
    /// 收藏项名称
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string ItemName { get; private set; } = string.Empty;

    /// <summary>
    /// 收藏分类
    /// </summary>
    [MaxLength(100)]
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// 标签（JSON格式存储，用于搜索和过滤）
    /// </summary>
    [MaxLength(1000)]
    public string Tags { get; private set; } = string.Empty;

    /// <summary>
    /// 收藏描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; private set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime LastAccessedAt { get; private set; }

    /// <summary>
    /// 访问次数
    /// </summary>
    public int AccessCount { get; private set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    public virtual UserProfile User { get; private set; } = null!;

    /// <summary>
    /// 私有构造函数用于EF Core
    /// </summary>
    private UserFavorite()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        LastAccessedAt = DateTime.UtcNow;
        IsEnabled = true;
        AccessCount = 0;
        SortOrder = 0;
    }

    /// <summary>
    /// 创建新的用户收藏
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="itemType">收藏项类型</param>
    /// <param name="itemId">收藏项ID</param>
    /// <param name="itemName">收藏项名称</param>
    /// <param name="category">分类</param>
    /// <param name="tags">标签</param>
    /// <param name="description">描述</param>
    /// <param name="sortOrder">排序顺序</param>
    public UserFavorite(
        Guid userId,
        string itemType,
        string itemId,
        string itemName,
        string category = "",
        string tags = "",
        string? description = null,
        int sortOrder = 0)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(itemType))
            throw new ArgumentException("Item type cannot be empty", nameof(itemType));
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("Item ID cannot be empty", nameof(itemId));
        if (string.IsNullOrWhiteSpace(itemName))
            throw new ArgumentException("Item name cannot be empty", nameof(itemName));

        Id = Guid.NewGuid();
        UserId = userId;
        ItemType = itemType.Trim();
        ItemId = itemId.Trim();
        ItemName = itemName.Trim();
        Category = category.Trim();
        Tags = tags.Trim();
        Description = description?.Trim();
        SortOrder = sortOrder;
        CreatedAt = DateTime.UtcNow;
        LastAccessedAt = DateTime.UtcNow;
        IsEnabled = true;
        AccessCount = 0;
    }

    /// <summary>
    /// 更新收藏信息
    /// </summary>
    /// <param name="itemName">新的收藏项名称</param>
    /// <param name="category">新的分类</param>
    /// <param name="tags">新的标签</param>
    /// <param name="description">新的描述</param>
    public void UpdateFavorite(string? itemName = null, string? category = null, string? tags = null, string? description = null)
    {
        if (!string.IsNullOrWhiteSpace(itemName))
            ItemName = itemName.Trim();

        if (category != null)
            Category = category.Trim();

        if (tags != null)
            Tags = tags.Trim();

        if (description != null)
            Description = description.Trim();

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
    /// 记录访问
    /// </summary>
    public void RecordAccess()
    {
        AccessCount++;
        LastAccessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 启用收藏
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 禁用收藏
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取标签列表
    /// </summary>
    /// <returns>标签列表</returns>
    public List<string> GetTagsList()
    {
        if (string.IsNullOrWhiteSpace(Tags))
            return new List<string>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(Tags) ?? new List<string>();
        }
        catch
        {
            // 如果JSON解析失败，尝试按逗号分割
            return Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(tag => tag.Trim())
                      .Where(tag => !string.IsNullOrEmpty(tag))
                      .ToList();
        }
    }

    /// <summary>
    /// 设置标签列表
    /// </summary>
    /// <param name="tags">标签列表</param>
    public void SetTags(IEnumerable<string> tags)
    {
        var validTags = tags?.Where(tag => !string.IsNullOrWhiteSpace(tag))
                             .Select(tag => tag.Trim())
                             .Distinct()
                             .ToList() ?? new List<string>();

        Tags = System.Text.Json.JsonSerializer.Serialize(validTags);
        UpdatedAt = DateTime.UtcNow;
    }
}
