using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.Enumerations;

/// <summary>
/// 执行状态枚举
/// </summary>
public class ExecutionStatus : Enumeration
{
    public static ExecutionStatus Pending = new(1, nameof(Pending));
    public static ExecutionStatus Running = new(2, nameof(Running));
    public static ExecutionStatus Completed = new(3, nameof(Completed));
    public static ExecutionStatus Failed = new(4, nameof(Failed));
    public static ExecutionStatus Cancelled = new(5, nameof(Cancelled));
    public static ExecutionStatus Timeout = new(6, nameof(Timeout));

    public ExecutionStatus(int id, string name) : base(id, name)
    {
    }

    /// <summary>
    /// 检查是否可以转换到新状态
    /// </summary>
    public bool CanTransitionTo(ExecutionStatus newStatus)
    {
        return (Id, newStatus.Id) switch
        {
            (1, 2) => true, // Pending -> Running
            (1, 5) => true, // Pending -> Cancelled
            (2, 3) => true, // Running -> Completed
            (2, 4) => true, // Running -> Failed
            (2, 5) => true, // Running -> Cancelled
            (2, 6) => true, // Running -> Timeout
            _ => false
        };
    }
}