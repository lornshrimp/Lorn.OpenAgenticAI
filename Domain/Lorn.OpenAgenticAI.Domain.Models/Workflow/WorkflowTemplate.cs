using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.Workflow;

/// <summary>
/// 工作流模板实体（聚合根）
/// </summary>
public class WorkflowTemplate
{
    public Guid TemplateId { get; private set; }
    public Guid UserId { get; private set; }
    public string TemplateName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public bool IsPublic { get; private set; }
    public bool IsSystemTemplate { get; private set; }
    public ValueObjects.Version TemplateVersion { get; private set; } = null!;
    public DateTime CreatedTime { get; private set; }
    public DateTime LastModifiedTime { get; private set; }
    public int UsageCount { get; private set; }
    public double Rating { get; private set; }
    public WorkflowDefinition TemplateDefinition { get; private set; } = null!;
    public List<string> RequiredCapabilities { get; private set; } = new();
    public long EstimatedExecutionTime { get; private set; }
    public List<string> Tags { get; private set; } = new();
    public string? IconUrl { get; private set; }
    public byte[]? ThumbnailData { get; private set; }

    // 导航属性
    public virtual UserManagement.UserProfile User { get; private set; } = null!;
    public virtual ICollection<WorkflowTemplateStep> TemplateSteps { get; private set; } = new List<WorkflowTemplateStep>();

    // 私有构造函数供EF Core
    private WorkflowTemplate() { }

    public WorkflowTemplate(
        Guid userId,
        string templateName,
        string description,
        string category,
        WorkflowDefinition templateDefinition,
        bool isPublic = false,
        bool isSystemTemplate = false,
        List<string>? requiredCapabilities = null,
        List<string>? tags = null,
        string? iconUrl = null)
    {
        TemplateId = Guid.NewGuid();
        UserId = userId != Guid.Empty ? userId : throw new ArgumentException("UserId cannot be empty", nameof(userId));
        TemplateName = !string.IsNullOrWhiteSpace(templateName) ? templateName : throw new ArgumentException("TemplateName cannot be empty", nameof(templateName));
        Description = description ?? string.Empty;
        Category = category ?? "General";
        IsPublic = isPublic;
        IsSystemTemplate = isSystemTemplate;
        TemplateVersion = new ValueObjects.Version(1, 0, 0);
        CreatedTime = DateTime.UtcNow;
        LastModifiedTime = DateTime.UtcNow;
        UsageCount = 0;
        Rating = 0.0;
        TemplateDefinition = templateDefinition ?? throw new ArgumentNullException(nameof(templateDefinition));
        RequiredCapabilities = requiredCapabilities ?? new List<string>();
        EstimatedExecutionTime = 0;
        Tags = tags ?? new List<string>();
        IconUrl = iconUrl;
    }

    /// <summary>
    /// 增加使用次数
    /// </summary>
    public void IncrementUsageCount()
    {
        UsageCount++;
        LastModifiedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新评分
    /// </summary>
    public void UpdateRating(double newRating)
    {
        if (newRating < 0 || newRating > 5)
            throw new ArgumentException("Rating must be between 0 and 5", nameof(newRating));

        Rating = newRating;
        LastModifiedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 克隆模板
    /// </summary>
    public WorkflowTemplate Clone(Guid newUserId, string? newTemplateName = null)
    {
        var clonedTemplate = new WorkflowTemplate(
            newUserId,
            newTemplateName ?? $"{TemplateName} (Copy)",
            Description,
            Category,
            TemplateDefinition,
            false, // 克隆的模板默认为私有
            false, // 克隆的模板不是系统模板
            new List<string>(RequiredCapabilities),
            new List<string>(Tags),
            IconUrl
        );

        // 克隆步骤
        foreach (var step in TemplateSteps)
        {
            clonedTemplate.AddStep(
                step.StepOrder,
                step.StepType,
                step.StepName,
                step.StepDescription,
                step.RequiredCapability,
                step.Parameters,
                step.DependsOnSteps?.ToList(),
                step.IsOptional,
                step.TimeoutSeconds
            );
        }

        return clonedTemplate;
    }

    /// <summary>
    /// 验证模板
    /// </summary>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            result.AddError("TemplateName", "Template name is required");
        }

        if (TemplateDefinition == null)
        {
            result.AddError("TemplateDefinition", "Template definition is required");
        }
        else
        {
            var definitionValidation = TemplateDefinition.Validate();
            if (!definitionValidation.IsValid)
            {
                foreach (var error in definitionValidation.Errors)
                {
                    result.AddError($"TemplateDefinition.{error.PropertyName}", error.ErrorMessage);
                }
            }
        }

        // 验证步骤顺序
        var stepValidation = ValidateStepOrder();
        if (!stepValidation.IsValid)
        {
            foreach (var error in stepValidation.Errors)
            {
                result.AddError(error.PropertyName, error.ErrorMessage);
            }
        }

        return result;
    }

    /// <summary>
    /// 验证步骤顺序和依赖关系
    /// </summary>
    private ValidationResult ValidateStepOrder()
    {
        var result = new ValidationResult();

        var steps = TemplateSteps.OrderBy(s => s.StepOrder).ToList();
        var stepIds = steps.Select(s => s.StepId).ToHashSet();

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];

            // 检查步骤顺序是否连续
            if (step.StepOrder != i)
            {
                result.AddError($"Steps[{i}].StepOrder", $"Step order should be {i} but is {step.StepOrder}");
            }

            // 检查依赖关系
            if (step.DependsOnSteps?.Any() == true)
            {
                foreach (var dependencyId in step.DependsOnSteps)
                {
                    if (!stepIds.Contains(dependencyId))
                    {
                        result.AddError($"Steps[{i}].DependsOnSteps", $"Dependency step '{dependencyId}' not found");
                    }
                    else
                    {
                        var dependencyStep = steps.FirstOrDefault(s => s.StepId == dependencyId);
                        if (dependencyStep != null && dependencyStep.StepOrder >= step.StepOrder)
                        {
                            result.AddError($"Steps[{i}].DependsOnSteps", $"Dependency step '{dependencyId}' must come before current step");
                        }
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 添加步骤
    /// </summary>
    public void AddStep(
        int stepOrder,
        string stepType,
        string stepName,
        string stepDescription,
        string requiredCapability,
        StepParameters? parameters = null,
        List<Guid>? dependsOnSteps = null,
        bool isOptional = false,
        int timeoutSeconds = 30)
    {
        var step = new WorkflowTemplateStep(
            TemplateId,
            stepOrder,
            stepType,
            stepName,
            stepDescription,
            requiredCapability,
            parameters,
            dependsOnSteps ?? new List<Guid>(),
            isOptional,
            timeoutSeconds
        );

        TemplateSteps.Add(step);
        LastModifiedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 移除步骤
    /// </summary>
    public void RemoveStep(Guid stepId)
    {
        var step = TemplateSteps.FirstOrDefault(s => s.StepId == stepId);
        if (step != null)
        {
            TemplateSteps.Remove(step);
            LastModifiedTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 更新模板信息
    /// </summary>
    public void UpdateTemplate(
        string? templateName = null,
        string? description = null,
        string? category = null,
        bool? isPublic = null,
        List<string>? requiredCapabilities = null,
        List<string>? tags = null,
        string? iconUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(templateName))
            TemplateName = templateName;

        if (description != null)
            Description = description;

        if (!string.IsNullOrWhiteSpace(category))
            Category = category;

        if (isPublic.HasValue)
            IsPublic = isPublic.Value;

        if (requiredCapabilities != null)
            RequiredCapabilities = requiredCapabilities;

        if (tags != null)
            Tags = tags;

        if (iconUrl != null)
            IconUrl = iconUrl;

        LastModifiedTime = DateTime.UtcNow;
        
        // 更新版本号
        TemplateVersion = new ValueObjects.Version(TemplateVersion.Major, TemplateVersion.Minor, TemplateVersion.Patch + 1);
    }

    /// <summary>
    /// 设置缩略图
    /// </summary>
    public void SetThumbnail(byte[]? thumbnailData)
    {
        ThumbnailData = thumbnailData;
        LastModifiedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加标签
    /// </summary>
    public void AddTag(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
        {
            Tags.Add(tag);
            LastModifiedTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 移除标签
    /// </summary>
    public void RemoveTag(string tag)
    {
        if (Tags.Remove(tag))
        {
            LastModifiedTime = DateTime.UtcNow;
        }
    }
}