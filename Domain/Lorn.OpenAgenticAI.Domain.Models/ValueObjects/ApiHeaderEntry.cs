using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// API自定义头部条目实体
/// 用于存储ApiConfiguration中的CustomHeaders字典数据
/// </summary>
public class ApiHeaderEntry
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 头部名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string HeaderName { get; set; } = string.Empty;

    /// <summary>
    /// 头部值
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string HeaderValue { get; set; } = string.Empty;

    /// <summary>
    /// 头部描述
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用此头部
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 所属的ApiConfiguration对象ID（外键关联）
    /// </summary>
    public int ApiConfigurationId { get; set; }

    /// <summary>
    /// 空构造函数（EF Core需要）
    /// </summary>
    public ApiHeaderEntry()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="headerName">头部名称</param>
    /// <param name="headerValue">头部值</param>
    /// <param name="description">描述</param>
    public ApiHeaderEntry(string headerName, string headerValue, string? description = null)
    {
        HeaderName = headerName ?? throw new ArgumentNullException(nameof(headerName));
        HeaderValue = headerValue ?? throw new ArgumentNullException(nameof(headerValue));
        Description = description;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新头部值
    /// </summary>
    /// <param name="newValue">新的头部值</param>
    public void UpdateValue(string newValue)
    {
        HeaderValue = newValue ?? throw new ArgumentNullException(nameof(newValue));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新描述
    /// </summary>
    /// <param name="newDescription">新的描述</param>
    public void UpdateDescription(string? newDescription)
    {
        Description = newDescription;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 启用/禁用此头部
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
