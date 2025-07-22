using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lorn.OpenAgenticAI.Domain.Models.LLM;

/// <summary>
/// 定价信息特殊价格条目实体
/// 用于存储PricingInfo中的SpecialPricing字典数据
/// </summary>
public class PricingSpecialEntry
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 特殊定价项名称/标识符
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string PricingKey { get; set; } = string.Empty;

    /// <summary>
    /// 特殊价格值
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal Price { get; set; }

    /// <summary>
    /// 价格类型说明（可选）
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用此特殊价格
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
    /// 所属的PricingInfo对象ID（外键关联）
    /// </summary>
    public int PricingInfoId { get; set; }

    /// <summary>
    /// 空构造函数（EF Core需要）
    /// </summary>
    public PricingSpecialEntry()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="pricingKey">特殊定价项名称</param>
    /// <param name="price">价格值</param>
    /// <param name="description">价格类型说明</param>
    public PricingSpecialEntry(string pricingKey, decimal price, string? description = null)
    {
        PricingKey = pricingKey ?? throw new ArgumentNullException(nameof(pricingKey));
        Price = price;
        Description = description;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新价格值
    /// </summary>
    /// <param name="newPrice">新的价格值</param>
    public void UpdatePrice(decimal newPrice)
    {
        Price = newPrice;
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
    /// 启用/禁用此特殊价格
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
