using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Enumerations;

namespace Lorn.OpenAgenticAI.Domain.Models.Monitoring;

/// <summary>
/// 性能指标记录实体
/// </summary>
public class PerformanceMetricsRecord
{
    public Guid MetricId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ExecutionId { get; private set; }
    public DateTime MetricTimestamp { get; private set; }
    public string MetricType { get; private set; } = string.Empty;
    public string MetricName { get; private set; } = string.Empty;
    public double MetricValue { get; private set; }
    public string MetricUnit { get; private set; } = string.Empty;
    public Dictionary<string, string> Tags { get; private set; } = new();
    public Dictionary<string, object> Context { get; private set; } = new();
    public string? AggregationPeriod { get; private set; }

    // 导航属性
    public virtual UserManagement.UserProfile User { get; private set; } = null!;
    public virtual Execution.TaskExecutionHistory Execution { get; private set; } = null!;

    // 私有构造函数用于EF Core
    private PerformanceMetricsRecord() { }

    public PerformanceMetricsRecord(
        Guid userId,
        Guid executionId,
        string metricType,
        string metricName,
        double metricValue,
        string? metricUnit = null,
        Dictionary<string, string>? tags = null,
        Dictionary<string, object>? context = null,
        string? aggregationPeriod = null)
    {
        MetricId = Guid.NewGuid();
        UserId = userId != Guid.Empty ? userId : throw new ArgumentException("UserId cannot be empty", nameof(userId));
        ExecutionId = executionId != Guid.Empty ? executionId : throw new ArgumentException("ExecutionId cannot be empty", nameof(executionId));
        MetricTimestamp = DateTime.UtcNow;
        MetricType = !string.IsNullOrWhiteSpace(metricType) ? metricType : throw new ArgumentException("MetricType cannot be empty", nameof(metricType));
        MetricName = !string.IsNullOrWhiteSpace(metricName) ? metricName : throw new ArgumentException("MetricName cannot be empty", nameof(metricName));
        MetricValue = metricValue;
        MetricUnit = metricUnit ?? "count";
        Tags = tags ?? new Dictionary<string, string>();
        Context = context ?? new Dictionary<string, object>();
        AggregationPeriod = aggregationPeriod;
    }

    /// <summary>
    /// 添加标签
    /// </summary>
    public void AddTag(string key, string value)
    {
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
        {
            Tags[key] = value;
        }
    }

    /// <summary>
    /// 添加上下文信息
    /// </summary>
    public void AddContext(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key) && value != null)
        {
            Context[key] = value;
        }
    }

    /// <summary>
    /// 检查是否为异常值
    /// </summary>
    public bool IsOutlier(double threshold = 2.0)
    {
        // 这里可以实现更复杂的异常检测逻辑
        // 简化版本：检查值是否为NaN或无穷大
        return double.IsNaN(MetricValue) || double.IsInfinity(MetricValue) || Math.Abs(MetricValue) > threshold * 1000;
    }
}

/// <summary>
/// 错误事件记录实体
/// </summary>
public class ErrorEventRecord
{
    public Guid ErrorEventId { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? ExecutionId { get; private set; }
    public string? StepId { get; private set; }
    public string ErrorType { get; private set; } = string.Empty;
    public string ErrorCode { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;
    public string? StackTrace { get; private set; }
    public string SourceComponent { get; private set; } = string.Empty;
    public string Severity { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public Dictionary<string, object> Environment { get; private set; } = new();
    public string? UserAgent { get; private set; }
    public bool IsResolved { get; private set; }
    public DateTime? ResolutionTime { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public int RecurrenceCount { get; private set; }
    public DateTime FirstOccurrence { get; private set; }
    public DateTime LastOccurrence { get; private set; }

    // 导航属性
    public virtual UserManagement.UserProfile? User { get; private set; }
    public virtual Execution.TaskExecutionHistory? Execution { get; private set; }

    // 私有构造函数用于EF Core
    private ErrorEventRecord() { }

    public ErrorEventRecord(
        string errorType,
        string errorCode,
        string errorMessage,
        string sourceComponent,
        string severity = "Medium",
        Guid? userId = null,
        Guid? executionId = null,
        string? stepId = null,
        string? stackTrace = null,
        string? userAgent = null,
        Dictionary<string, object>? environment = null)
    {
        ErrorEventId = Guid.NewGuid();
        UserId = userId;
        ExecutionId = executionId;
        StepId = stepId;
        ErrorType = !string.IsNullOrWhiteSpace(errorType) ? errorType : throw new ArgumentException("ErrorType cannot be empty", nameof(errorType));
        ErrorCode = !string.IsNullOrWhiteSpace(errorCode) ? errorCode : throw new ArgumentException("ErrorCode cannot be empty", nameof(errorCode));
        ErrorMessage = !string.IsNullOrWhiteSpace(errorMessage) ? errorMessage : throw new ArgumentException("ErrorMessage cannot be empty", nameof(errorMessage));
        StackTrace = stackTrace;
        SourceComponent = !string.IsNullOrWhiteSpace(sourceComponent) ? sourceComponent : throw new ArgumentException("SourceComponent cannot be empty", nameof(sourceComponent));
        Severity = severity;
        Timestamp = DateTime.UtcNow;
        Environment = environment ?? new Dictionary<string, object>();
        UserAgent = userAgent;
        IsResolved = false;
        RecurrenceCount = 1;
        FirstOccurrence = DateTime.UtcNow;
        LastOccurrence = DateTime.UtcNow;
    }

    /// <summary>
    /// 标记为已解决
    /// </summary>
    public void MarkAsResolved(string? resolutionNotes = null)
    {
        IsResolved = true;
        ResolutionTime = DateTime.UtcNow;
        ResolutionNotes = resolutionNotes;
    }

    /// <summary>
    /// 增加重现次数
    /// </summary>
    public void IncrementRecurrence()
    {
        RecurrenceCount++;
        LastOccurrence = DateTime.UtcNow;
    }

    /// <summary>
    /// 检查是否为高频错误
    /// </summary>
    public bool IsHighFrequencyError(int threshold = 10, TimeSpan timeWindow = default)
    {
        if (timeWindow == default)
            timeWindow = TimeSpan.FromHours(1);

        var timeSpan = LastOccurrence - FirstOccurrence;
        return RecurrenceCount >= threshold && timeSpan <= timeWindow;
    }

    /// <summary>
    /// 获取错误摘要
    /// </summary>
    public ErrorSummary GetSummary()
    {
        return new ErrorSummary
        {
            ErrorEventId = ErrorEventId,
            ErrorType = ErrorType,
            ErrorCode = ErrorCode,
            ErrorMessage = ErrorMessage,
            SourceComponent = SourceComponent,
            Severity = Severity,
            Timestamp = Timestamp,
            IsResolved = IsResolved,
            RecurrenceCount = RecurrenceCount,
            Duration = LastOccurrence - FirstOccurrence
        };
    }
}

/// <summary>
/// 错误摘要信息
/// </summary>
public class ErrorSummary
{
    public Guid ErrorEventId { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string SourceComponent { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsResolved { get; set; }
    public int RecurrenceCount { get; set; }
    public TimeSpan Duration { get; set; }
}