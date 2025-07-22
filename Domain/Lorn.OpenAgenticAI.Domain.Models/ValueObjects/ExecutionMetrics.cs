using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.Execution;

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

    /// <summary>
    /// 步骤执行时间条目（导航属性）
    /// </summary>
    [NotMapped]
    public virtual ICollection<StepExecutionTimeEntry> StepExecutionTimeEntries { get; set; } = new List<StepExecutionTimeEntry>();

    /// <summary>
    /// 资源利用率条目（导航属性）
    /// </summary>
    [NotMapped]
    public virtual ICollection<ResourceUtilizationEntry> ResourceUtilizationEntries { get; set; } = new List<ResourceUtilizationEntry>();

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
        StepExecutionTimeEntries = new List<StepExecutionTimeEntry>();
        ResourceUtilizationEntries = new List<ResourceUtilizationEntry>();

        // 如果提供了步骤执行时间字典，转换为实体对象
        if (stepExecutionTimes != null)
        {
            foreach (var kvp in stepExecutionTimes)
            {
                StepExecutionTimeEntries.Add(new StepExecutionTimeEntry(kvp.Key, kvp.Value));
            }
        }
    }

    /// <summary>
    /// 获取步骤执行时间字典（向后兼容）
    /// </summary>
    public Dictionary<string, long> GetStepExecutionTimes()
    {
        return StepExecutionTimeEntries
            .Where(e => e.IsEnabled)
            .ToDictionary(e => e.StepName, e => e.ExecutionTimeMs);
    }

    /// <summary>
    /// 设置步骤执行时间（向后兼容）
    /// </summary>
    public void SetStepExecutionTime(string stepName, long executionTime)
    {
        var existing = StepExecutionTimeEntries.FirstOrDefault(e => e.StepName == stepName);
        if (existing != null)
        {
            existing.UpdateExecutionTime(executionTime);
            existing.SetEnabled(true);
        }
        else
        {
            StepExecutionTimeEntries.Add(new StepExecutionTimeEntry(stepName, executionTime));
        }
    }

    /// <summary>
    /// 获取资源利用率字典（向后兼容）
    /// </summary>
    public Dictionary<string, double> GetResourceUtilization()
    {
        return ResourceUtilizationEntries
            .Where(e => e.IsEnabled)
            .ToDictionary(e => e.ResourceName, e => e.UtilizationRate);
    }

    /// <summary>
    /// 设置资源利用率（向后兼容）
    /// </summary>
    public void SetResourceUtilization(string resourceName, double utilizationRate)
    {
        var existing = ResourceUtilizationEntries.FirstOrDefault(e => e.ResourceName == resourceName);
        if (existing != null)
        {
            existing.UpdateUtilizationRate(utilizationRate);
            existing.SetEnabled(true);
        }
        else
        {
            ResourceUtilizationEntries.Add(new ResourceUtilizationEntry(resourceName, utilizationRate));
        }
    }

    /// <summary>
    /// 更新资源利用率
    /// </summary>
    public ExecutionMetrics UpdateResourceUtilization(Dictionary<string, double> resourceUtilization)
    {
        var newMetrics = new ExecutionMetrics(
            TotalExecutionTime,
            LlmProcessingTime,
            AgentExecutionTime,
            TokenUsage,
            GetStepExecutionTimes());

        // 添加资源利用率数据
        if (resourceUtilization != null)
        {
            foreach (var kvp in resourceUtilization)
            {
                newMetrics.SetResourceUtilization(kvp.Key, kvp.Value);
            }
        }

        return newMetrics;
    }

    /// <summary>
    /// 添加步骤执行时间
    /// </summary>
    public ExecutionMetrics AddStepExecutionTime(string stepName, long executionTime)
    {
        var newStepTimes = GetStepExecutionTimes();
        newStepTimes[stepName] = executionTime;

        var newMetrics = new ExecutionMetrics(
            TotalExecutionTime,
            LlmProcessingTime,
            AgentExecutionTime,
            TokenUsage,
            newStepTimes);

        // 复制资源利用率数据
        var resourceUtilization = GetResourceUtilization();
        foreach (var kvp in resourceUtilization)
        {
            newMetrics.SetResourceUtilization(kvp.Key, kvp.Value);
        }

        return newMetrics;
    }

    /// <summary>
    /// 计算平均步骤执行时间
    /// </summary>
    public double GetAverageStepExecutionTime()
    {
        var stepTimes = GetStepExecutionTimes();
        if (stepTimes.Count == 0)
            return 0;

        return stepTimes.Values.Sum() / (double)stepTimes.Count;
    }

    /// <summary>
    /// 获取最长执行时间的步骤
    /// </summary>
    public KeyValuePair<string, long> GetLongestStep()
    {
        var stepTimes = GetStepExecutionTimes();
        if (stepTimes.Count == 0)
            return new KeyValuePair<string, long>(string.Empty, 0);

        return stepTimes.OrderByDescending(x => x.Value).First();
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return TotalExecutionTime;
        yield return LlmProcessingTime;
        yield return AgentExecutionTime;
        yield return TokenUsage;

        foreach (var stepTime in GetStepExecutionTimes().OrderBy(x => x.Key))
        {
            yield return stepTime.Key;
            yield return stepTime.Value;
        }

        foreach (var resource in GetResourceUtilization().OrderBy(x => x.Key))
        {
            yield return resource.Key;
            yield return resource.Value;
        }
    }
}
