using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.ValueObjects;

namespace Lorn.Domain.Models.Workflow;

/// <summary>
/// Workflow template step entity
/// </summary>
public class WorkflowTemplateStep : BaseEntity
{
    /// <summary>
    /// Gets the step identifier
    /// </summary>
    public Guid StepId { get; private set; }

    /// <summary>
    /// Gets the template identifier
    /// </summary>
    public Guid TemplateId { get; private set; }

    /// <summary>
    /// Gets the step order
    /// </summary>
    public int StepOrder { get; private set; }

    /// <summary>
    /// Gets the step type
    /// </summary>
    public string StepType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the step name
    /// </summary>
    public string StepName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the step description
    /// </summary>
    public string StepDescription { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the required capability
    /// </summary>
    public string RequiredCapability { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the step parameters
    /// </summary>
    public StepParameters Parameters { get; private set; }

    /// <summary>
    /// Gets the list of step IDs this step depends on
    /// </summary>
    public List<Guid> DependsOnSteps { get; private set; } = new();

    /// <summary>
    /// Gets whether this step is optional
    /// </summary>
    public bool IsOptional { get; private set; }

    /// <summary>
    /// Gets the timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; private set; }

    /// <summary>
    /// Gets the workflow template
    /// </summary>
    public WorkflowTemplate Template { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the WorkflowTemplateStep class
    /// </summary>
    /// <param name="stepId">The step identifier</param>
    /// <param name="templateId">The template identifier</param>
    /// <param name="stepOrder">The step order</param>
    /// <param name="stepType">The step type</param>
    /// <param name="stepName">The step name</param>
    /// <param name="stepDescription">The step description</param>
    /// <param name="requiredCapability">The required capability</param>
    /// <param name="parameters">The step parameters</param>
    /// <param name="isOptional">Whether the step is optional</param>
    /// <param name="timeoutSeconds">The timeout in seconds</param>
    public WorkflowTemplateStep(
        Guid stepId,
        Guid templateId,
        int stepOrder,
        string stepType,
        string stepName,
        string stepDescription,
        string requiredCapability,
        StepParameters? parameters = null,
        bool isOptional = false,
        int timeoutSeconds = 300)
    {
        StepId = stepId;
        TemplateId = templateId;
        StepOrder = stepOrder;
        StepType = stepType ?? throw new ArgumentNullException(nameof(stepType));
        StepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
        StepDescription = stepDescription ?? throw new ArgumentNullException(nameof(stepDescription));
        RequiredCapability = requiredCapability ?? throw new ArgumentNullException(nameof(requiredCapability));
        Parameters = parameters ?? new StepParameters();
        IsOptional = isOptional;
        TimeoutSeconds = timeoutSeconds;
        
        Id = stepId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private WorkflowTemplateStep() 
    {
        Parameters = new StepParameters();
    }

    /// <summary>
    /// Updates the step information
    /// </summary>
    /// <param name="stepName">The new step name</param>
    /// <param name="stepDescription">The new step description</param>
    /// <param name="requiredCapability">The new required capability</param>
    public void UpdateStep(string stepName, string stepDescription, string requiredCapability)
    {
        if (string.IsNullOrWhiteSpace(stepName))
            throw new ArgumentException("Step name cannot be empty", nameof(stepName));

        StepName = stepName;
        StepDescription = stepDescription ?? string.Empty;
        RequiredCapability = requiredCapability ?? string.Empty;
        UpdateVersion();
    }

    /// <summary>
    /// Updates the step order
    /// </summary>
    /// <param name="stepOrder">The new step order</param>
    public void UpdateStepOrder(int stepOrder)
    {
        if (stepOrder < 0)
            throw new ArgumentException("Step order cannot be negative", nameof(stepOrder));

        StepOrder = stepOrder;
        UpdateVersion();
    }

    /// <summary>
    /// Updates the step parameters
    /// </summary>
    /// <param name="parameters">The new parameters</param>
    public void UpdateParameters(StepParameters parameters)
    {
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        UpdateVersion();
    }

    /// <summary>
    /// Sets whether the step is optional
    /// </summary>
    /// <param name="isOptional">Whether the step is optional</param>
    public void SetOptional(bool isOptional)
    {
        IsOptional = isOptional;
        UpdateVersion();
    }

    /// <summary>
    /// Sets the timeout for the step
    /// </summary>
    /// <param name="timeoutSeconds">The timeout in seconds</param>
    public void SetTimeout(int timeoutSeconds)
    {
        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

        TimeoutSeconds = timeoutSeconds;
        UpdateVersion();
    }

    /// <summary>
    /// Adds a dependency to another step
    /// </summary>
    /// <param name="stepId">The step ID to depend on</param>
    public void AddDependency(Guid stepId)
    {
        if (stepId == StepId)
            throw new ArgumentException("Step cannot depend on itself", nameof(stepId));

        if (!DependsOnSteps.Contains(stepId))
        {
            DependsOnSteps.Add(stepId);
            UpdateVersion();
        }
    }

    /// <summary>
    /// Removes a dependency from another step
    /// </summary>
    /// <param name="stepId">The step ID to remove dependency from</param>
    public void RemoveDependency(Guid stepId)
    {
        if (DependsOnSteps.Remove(stepId))
        {
            UpdateVersion();
        }
    }

    /// <summary>
    /// Clears all dependencies
    /// </summary>
    public void ClearDependencies()
    {
        if (DependsOnSteps.Count > 0)
        {
            DependsOnSteps.Clear();
            UpdateVersion();
        }
    }

    /// <summary>
    /// Checks if this step has dependencies
    /// </summary>
    /// <returns>True if the step has dependencies, false otherwise</returns>
    public bool HasDependencies()
    {
        return DependsOnSteps.Count > 0;
    }

    /// <summary>
    /// Checks if this step depends on the specified step
    /// </summary>
    /// <param name="stepId">The step ID to check</param>
    /// <returns>True if this step depends on the specified step, false otherwise</returns>
    public bool DependsOn(Guid stepId)
    {
        return DependsOnSteps.Contains(stepId);
    }

    /// <summary>
    /// Validates the step
    /// </summary>
    /// <returns>A validation result</returns>
    public ValidationResult ValidateStep()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(StepName))
            result.AddError(nameof(StepName), "Step name cannot be empty");

        if (string.IsNullOrWhiteSpace(StepType))
            result.AddError(nameof(StepType), "Step type cannot be empty");

        if (string.IsNullOrWhiteSpace(RequiredCapability))
            result.AddError(nameof(RequiredCapability), "Required capability cannot be empty");

        if (StepOrder < 0)
            result.AddError(nameof(StepOrder), "Step order cannot be negative");

        if (TimeoutSeconds <= 0)
            result.AddError(nameof(TimeoutSeconds), "Timeout must be positive");

        // Validate parameters
        var paramValidation = Parameters.ValidateParameters();
        if (!paramValidation.IsValid)
        {
            foreach (var error in paramValidation.Errors)
            {
                result.AddError($"Parameters.{error.PropertyName}", error.ErrorMessage);
            }
        }

        // Check for circular dependencies
        if (DependsOnSteps.Contains(StepId))
        {
            result.AddError(nameof(DependsOnSteps), "Step cannot depend on itself");
        }

        return result;
    }

    /// <summary>
    /// Clones the workflow template step
    /// </summary>
    /// <param name="newStepId">The new step identifier</param>
    /// <param name="newTemplateId">The new template identifier</param>
    /// <returns>A cloned workflow template step</returns>
    public WorkflowTemplateStep Clone(Guid newStepId, Guid newTemplateId)
    {
        var clonedStep = new WorkflowTemplateStep(
            newStepId,
            newTemplateId,
            StepOrder,
            StepType,
            StepName,
            StepDescription,
            RequiredCapability,
            Parameters,
            IsOptional,
            TimeoutSeconds
        );

        // Copy dependencies (note: these may need to be updated after all steps are cloned)
        clonedStep.DependsOnSteps.AddRange(DependsOnSteps);

        return clonedStep;
    }

    /// <summary>
    /// Gets the step type as an enumeration
    /// </summary>
    /// <returns>The step type</returns>
    public WorkflowStepType GetStepType()
    {
        return StepType.ToLower() switch
        {
            "agent" => WorkflowStepType.Agent,
            "condition" => WorkflowStepType.Condition,
            "loop" => WorkflowStepType.Loop,
            "parallel" => WorkflowStepType.Parallel,
            "delay" => WorkflowStepType.Delay,
            "script" => WorkflowStepType.Script,
            "manual" => WorkflowStepType.Manual,
            _ => WorkflowStepType.Agent
        };
    }

    /// <summary>
    /// Checks if the step can be executed in parallel with other steps
    /// </summary>
    /// <returns>True if the step can be parallelized, false otherwise</returns>
    public bool CanExecuteInParallel()
    {
        return GetStepType() != WorkflowStepType.Manual && !HasDependencies();
    }
}

/// <summary>
/// Workflow step type enumeration
/// </summary>
public enum WorkflowStepType
{
    /// <summary>
    /// Agent execution step
    /// </summary>
    Agent = 1,

    /// <summary>
    /// Conditional step
    /// </summary>
    Condition = 2,

    /// <summary>
    /// Loop step
    /// </summary>
    Loop = 3,

    /// <summary>
    /// Parallel execution step
    /// </summary>
    Parallel = 4,

    /// <summary>
    /// Delay step
    /// </summary>
    Delay = 5,

    /// <summary>
    /// Script execution step
    /// </summary>
    Script = 6,

    /// <summary>
    /// Manual intervention step
    /// </summary>
    Manual = 7
}