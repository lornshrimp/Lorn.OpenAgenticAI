using System;
using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Common;
using Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

namespace Lorn.OpenAgenticAI.Domain.Models.Workflow;

/// <summary>
/// ������ģ�岽��ʵ��
/// </summary>
public class WorkflowTemplateStep
{
    public Guid StepId { get; private set; }
    public Guid TemplateId { get; private set; }
    public int StepOrder { get; private set; }
    public string StepType { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string StepDescription { get; set; } = string.Empty;
    public string RequiredCapability { get; set; } = string.Empty;
    public StepParameters Parameters { get; set; } = new();
    public List<Guid> DependsOnSteps { get; set; } = [];
    public bool IsOptional { get; private set; }
    public int TimeoutSeconds { get; private set; }

    // ��������
    public virtual WorkflowTemplate Template { get; set; } = null!;

    // ˽�й��캯������EF Core
    private WorkflowTemplateStep()
    {
        StepId = Guid.NewGuid();
    }

    public WorkflowTemplateStep(
        Guid templateId,
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
        StepId = Guid.NewGuid();
        TemplateId = templateId != Guid.Empty ? templateId : throw new ArgumentException("TemplateId cannot be empty", nameof(templateId));
        StepOrder = stepOrder >= 0 ? stepOrder : throw new ArgumentException("StepOrder must be non-negative", nameof(stepOrder));
        StepType = !string.IsNullOrWhiteSpace(stepType) ? stepType : throw new ArgumentException("StepType cannot be empty", nameof(stepType));
        StepName = !string.IsNullOrWhiteSpace(stepName) ? stepName : throw new ArgumentException("StepName cannot be empty", nameof(stepName));
        StepDescription = stepDescription ?? string.Empty;
        RequiredCapability = !string.IsNullOrWhiteSpace(requiredCapability) ? requiredCapability : throw new ArgumentException("RequiredCapability cannot be empty", nameof(requiredCapability));
        Parameters = parameters ?? new StepParameters();
        DependsOnSteps = dependsOnSteps ?? [];
        IsOptional = isOptional;
        TimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : 30;
    }

    /// <summary>
    /// ��֤����
    /// </summary>
    public ValidationResult ValidateStep()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(StepName))
        {
            result.AddError("StepName", "Step name is required");
        }

        if (string.IsNullOrWhiteSpace(StepType))
        {
            result.AddError("StepType", "Step type is required");
        }

        if (string.IsNullOrWhiteSpace(RequiredCapability))
        {
            result.AddError("RequiredCapability", "Required capability is required");
        }

        if (TimeoutSeconds <= 0)
        {
            result.AddError("TimeoutSeconds", "Timeout must be positive");
        }

        if (Parameters != null)
        {
            var parameterValidation = Parameters.ValidateParameters();
            if (!parameterValidation.IsValid)
            {
                foreach (var error in parameterValidation.Errors)
                {
                    result.AddError($"Parameters.{error.PropertyName}", error.ErrorMessage);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// ����Ƿ���������ϵ
    /// </summary>
    public bool HasDependencies()
    {
        return DependsOnSteps?.Count > 0;
    }

    /// <summary>
    /// ���²�����Ϣ
    /// </summary>
    public void UpdateStep(
        string? stepName = null,
        string? stepDescription = null,
        string? requiredCapability = null,
        StepParameters? parameters = null,
        List<Guid>? dependsOnSteps = null,
        bool? isOptional = null,
        int? timeoutSeconds = null)
    {
        if (!string.IsNullOrWhiteSpace(stepName))
            StepName = stepName;

        if (stepDescription != null)
            StepDescription = stepDescription;

        if (!string.IsNullOrWhiteSpace(requiredCapability))
            RequiredCapability = requiredCapability;

        if (parameters != null)
            Parameters = parameters;

        if (dependsOnSteps != null)
            DependsOnSteps = dependsOnSteps;

        if (isOptional.HasValue)
            IsOptional = isOptional.Value;

        if (timeoutSeconds.HasValue && timeoutSeconds.Value > 0)
            TimeoutSeconds = timeoutSeconds.Value;
    }

    /// <summary>
    /// �����������
    /// </summary>
    public void AddDependency(Guid stepId)
    {
        if (stepId != Guid.Empty && !DependsOnSteps.Contains(stepId))
        {
            DependsOnSteps.Add(stepId);
        }
    }

    /// <summary>
    /// �Ƴ���������
    /// </summary>
    public void RemoveDependency(Guid stepId)
    {
        DependsOnSteps.Remove(stepId);
    }

    /// <summary>
    /// ���²���˳��
    /// </summary>
    public void UpdateStepOrder(int newOrder)
    {
        if (newOrder >= 0)
        {
            StepOrder = newOrder;
        }
    }

    /// <summary>
    /// ���ò���
    /// </summary>
    public void SetParameter(string key, object value)
    {
        Parameters.SetInputParameter(key, value);
    }

    /// <summary>
    /// ��ȡ����
    /// </summary>
    public T? GetParameter<T>(string key)
    {
        return Parameters.GetParameter<T>(key);
    }
}