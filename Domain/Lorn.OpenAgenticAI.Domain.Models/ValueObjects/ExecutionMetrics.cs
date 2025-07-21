using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// 执行指标值对象
/// </summary>
public class ExecutionMetrics : ValueObject
{
    public long TotalExecutionTime { get; private set; }
    public long LlmProcessingTime { get; private set; }
    public long AgentExecutionTime { get; private set; }
    public int TokenUsage { get; private set; }
    public Dictionary<string, long> StepExecutionTimes { get; private set; } = new();
    public Dictionary<string, double> ResourceUtilization { get; private set; } = new();

    public ExecutionMetrics(
        long totalExecutionTime,
        long llmProcessingTime,
        long agentExecutionTime,
        int tokenUsage,
        Dictionary<string, long>? stepExecutionTimes = null)
    {
        TotalExecutionTime = totalExecutionTime >= 0 ? totalExecutionTime : 0;
        LlmProcessingTime = llmProcessingTime >= 0 ? llmProcessingTime : 0;
        AgentExecutionTime = agentExecutionTime >= 0 ? agentExecutionTime : 0;
        TokenUsage = tokenUsage >= 0 ? tokenUsage : 0;
        StepExecutionTimes = stepExecutionTimes ?? new Dictionary<string, long>();
        ResourceUtilization = new Dictionary<string, double>();
    }

    /// <summary>
    /// 更新资源利用率
    /// </summary>
    public ExecutionMetrics UpdateResourceUtilization(Dictionary<string, double> resourceUtilization)
    {
        return new ExecutionMetrics(
            TotalExecutionTime,
            LlmProcessingTime,
            AgentExecutionTime,
            TokenUsage,
            StepExecutionTimes)
        {
            ResourceUtilization = resourceUtilization ?? new Dictionary<string, double>()
        };
    }

    /// <summary>
    /// 添加步骤执行时间
    /// </summary>
    public ExecutionMetrics AddStepExecutionTime(string stepName, long executionTime)
    {
        var newStepTimes = new Dictionary<string, long>(StepExecutionTimes)
        {
            [stepName] = executionTime
        };

        return new ExecutionMetrics(
            TotalExecutionTime,
            LlmProcessingTime,
            AgentExecutionTime,
            TokenUsage,
            newStepTimes)
        {
            ResourceUtilization = ResourceUtilization
        };
    }

    /// <summary>
    /// 计算平均步骤执行时间
    /// </summary>
    public double GetAverageStepExecutionTime()
    {
        if (StepExecutionTimes.Count == 0)
            return 0;

        return StepExecutionTimes.Values.Sum() / (double)StepExecutionTimes.Count;
    }

    /// <summary>
    /// 获取最长执行时间的步骤
    /// </summary>
    public KeyValuePair<string, long> GetLongestStep()
    {
        if (StepExecutionTimes.Count == 0)
            return new KeyValuePair<string, long>(string.Empty, 0);

        return StepExecutionTimes.OrderByDescending(x => x.Value).First();
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return TotalExecutionTime;
        yield return LlmProcessingTime;
        yield return AgentExecutionTime;
        yield return TokenUsage;

        foreach (var stepTime in StepExecutionTimes.OrderBy(x => x.Key))
        {
            yield return stepTime.Key;
            yield return stepTime.Value;
        }

        foreach (var resource in ResourceUtilization.OrderBy(x => x.Key))
        {
            yield return resource.Key;
            yield return resource.Value;
        }
    }
}
