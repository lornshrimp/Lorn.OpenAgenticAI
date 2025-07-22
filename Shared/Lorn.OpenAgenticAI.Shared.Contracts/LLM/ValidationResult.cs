namespace Lorn.OpenAgenticAI.Shared.Contracts.LLM;

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误列表
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告列表
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 创建成功的验证结果
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// 创建失败的验证结果
    /// </summary>
    /// <param name="errors">错误信息</param>
    public static ValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };

    /// <summary>
    /// 创建带警告的成功验证结果
    /// </summary>
    /// <param name="warnings">警告信息</param>
    public static ValidationResult SuccessWithWarnings(params string[] warnings) => new()
    {
        IsValid = true,
        Warnings = warnings.ToList()
    };
}
