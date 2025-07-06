using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.UserManagement;
using Lorn.Domain.Models.ValueObjects;

namespace Lorn.Domain.Models.Workflow;

/// <summary>
/// Workflow template aggregate root
/// </summary>
public class WorkflowTemplate : AggregateRoot
{
    private readonly List<WorkflowTemplateStep> _templateSteps = new();

    /// <summary>
    /// Gets the template identifier
    /// </summary>
    public Guid TemplateId { get; private set; }

    /// <summary>
    /// Gets the user identifier
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the template name
    /// </summary>
    public string TemplateName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the category
    /// </summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// Gets whether the template is public
    /// </summary>
    public bool IsPublic { get; private set; }

    /// <summary>
    /// Gets whether this is a system template
    /// </summary>
    public bool IsSystemTemplate { get; private set; }

    /// <summary>
    /// Gets the template version
    /// </summary>
    public ValueObjects.Version TemplateVersion { get; private set; }

    /// <summary>
    /// Gets the last modified time
    /// </summary>
    public DateTime LastModifiedTime { get; private set; }

    /// <summary>
    /// Gets the usage count
    /// </summary>
    public int UsageCount { get; private set; }

    /// <summary>
    /// Gets the rating
    /// </summary>
    public double Rating { get; private set; }

    /// <summary>
    /// Gets the template definition
    /// </summary>
    public WorkflowDefinition TemplateDefinition { get; private set; }

    /// <summary>
    /// Gets the required capabilities
    /// </summary>
    public List<string> RequiredCapabilities { get; private set; } = new();

    /// <summary>
    /// Gets the estimated execution time in milliseconds
    /// </summary>
    public long EstimatedExecutionTime { get; private set; }

    /// <summary>
    /// Gets the tags
    /// </summary>
    public List<string> Tags { get; private set; } = new();

    /// <summary>
    /// Gets the icon URL
    /// </summary>
    public string? IconUrl { get; private set; }

    /// <summary>
    /// Gets the thumbnail data
    /// </summary>
    public byte[]? ThumbnailData { get; private set; }

    /// <summary>
    /// Gets the user profile
    /// </summary>
    public UserProfile User { get; private set; } = null!;

    /// <summary>
    /// Gets the template steps
    /// </summary>
    public IReadOnlyList<WorkflowTemplateStep> TemplateSteps => _templateSteps.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the WorkflowTemplate class
    /// </summary>
    /// <param name="templateId">The template identifier</param>
    /// <param name="userId">The user identifier</param>
    /// <param name="templateName">The template name</param>
    /// <param name="description">The description</param>
    /// <param name="category">The category</param>
    /// <param name="templateDefinition">The template definition</param>
    /// <param name="isPublic">Whether the template is public</param>
    /// <param name="isSystemTemplate">Whether this is a system template</param>
    public WorkflowTemplate(
        Guid templateId,
        Guid userId,
        string templateName,
        string description,
        string category,
        WorkflowDefinition templateDefinition,
        bool isPublic = false,
        bool isSystemTemplate = false)
    {
        TemplateId = templateId;
        UserId = userId;
        TemplateName = templateName ?? throw new ArgumentNullException(nameof(templateName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        TemplateDefinition = templateDefinition ?? throw new ArgumentNullException(nameof(templateDefinition));
        IsPublic = isPublic;
        IsSystemTemplate = isSystemTemplate;
        TemplateVersion = new ValueObjects.Version(1, 0, 0);
        LastModifiedTime = DateTime.UtcNow;
        UsageCount = 0;
        Rating = 0.0;
        
        Id = templateId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new WorkflowTemplateCreatedEvent(templateId, userId, templateName));
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private WorkflowTemplate() { 
        TemplateDefinition = new WorkflowDefinition("json", "{}");
        TemplateVersion = new ValueObjects.Version(1, 0, 0);
    }

    /// <summary>
    /// Updates the template information
    /// </summary>
    /// <param name="templateName">The new template name</param>
    /// <param name="description">The new description</param>
    /// <param name="category">The new category</param>
    public void UpdateTemplate(string templateName, string description, string category)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be empty", nameof(templateName));

        TemplateName = templateName;
        Description = description ?? string.Empty;
        Category = category ?? string.Empty;
        LastModifiedTime = DateTime.UtcNow;
        UpdateVersion();
        
        AddDomainEvent(new WorkflowTemplateUpdatedEvent(TemplateId, UserId, templateName));
    }

    /// <summary>
    /// Updates the template definition
    /// </summary>
    /// <param name="templateDefinition">The new template definition</param>
    public void UpdateDefinition(WorkflowDefinition templateDefinition)
    {
        TemplateDefinition = templateDefinition ?? throw new ArgumentNullException(nameof(templateDefinition));
        TemplateVersion = TemplateVersion.IncrementMinor();
        LastModifiedTime = DateTime.UtcNow;
        UpdateVersion();
        
        AddDomainEvent(new WorkflowTemplateDefinitionUpdatedEvent(TemplateId, TemplateVersion.ToString()));
    }

    /// <summary>
    /// Sets the template as public or private
    /// </summary>
    /// <param name="isPublic">Whether the template should be public</param>
    public void SetPublic(bool isPublic)
    {
        if (IsPublic != isPublic)
        {
            IsPublic = isPublic;
            LastModifiedTime = DateTime.UtcNow;
            UpdateVersion();
            
            AddDomainEvent(new WorkflowTemplateVisibilityChangedEvent(TemplateId, isPublic));
        }
    }

    /// <summary>
    /// Increments the usage count
    /// </summary>
    public void IncrementUsageCount()
    {
        UsageCount++;
        UpdateVersion();
        
        AddDomainEvent(new WorkflowTemplateUsedEvent(TemplateId, UsageCount));
    }

    /// <summary>
    /// Updates the rating
    /// </summary>
    /// <param name="newRating">The new rating (0-5)</param>
    public void UpdateRating(double newRating)
    {
        if (newRating < 0 || newRating > 5)
            throw new ArgumentOutOfRangeException(nameof(newRating), "Rating must be between 0 and 5");

        Rating = newRating;
        UpdateVersion();
        
        AddDomainEvent(new WorkflowTemplateRatedEvent(TemplateId, newRating));
    }

    /// <summary>
    /// Adds a required capability
    /// </summary>
    /// <param name="capability">The capability to add</param>
    public void AddRequiredCapability(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
            throw new ArgumentException("Capability cannot be empty", nameof(capability));

        if (!RequiredCapabilities.Contains(capability))
        {
            RequiredCapabilities.Add(capability);
            UpdateVersion();
        }
    }

    /// <summary>
    /// Removes a required capability
    /// </summary>
    /// <param name="capability">The capability to remove</param>
    public void RemoveRequiredCapability(string capability)
    {
        if (RequiredCapabilities.Remove(capability))
        {
            UpdateVersion();
        }
    }

    /// <summary>
    /// Adds a tag
    /// </summary>
    /// <param name="tag">The tag to add</param>
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
            UpdateVersion();
        }
    }

    /// <summary>
    /// Removes a tag
    /// </summary>
    /// <param name="tag">The tag to remove</param>
    public void RemoveTag(string tag)
    {
        if (Tags.Remove(tag))
        {
            UpdateVersion();
        }
    }

    /// <summary>
    /// Sets the estimated execution time
    /// </summary>
    /// <param name="estimatedExecutionTime">The estimated execution time in milliseconds</param>
    public void SetEstimatedExecutionTime(long estimatedExecutionTime)
    {
        if (estimatedExecutionTime < 0)
            throw new ArgumentException("Estimated execution time cannot be negative", nameof(estimatedExecutionTime));

        EstimatedExecutionTime = estimatedExecutionTime;
        UpdateVersion();
    }

    /// <summary>
    /// Sets the icon URL
    /// </summary>
    /// <param name="iconUrl">The icon URL</param>
    public void SetIconUrl(string? iconUrl)
    {
        IconUrl = iconUrl;
        UpdateVersion();
    }

    /// <summary>
    /// Sets the thumbnail data
    /// </summary>
    /// <param name="thumbnailData">The thumbnail data</param>
    public void SetThumbnailData(byte[]? thumbnailData)
    {
        ThumbnailData = thumbnailData;
        UpdateVersion();
    }

    /// <summary>
    /// Adds a template step
    /// </summary>
    /// <param name="step">The step to add</param>
    public void AddTemplateStep(WorkflowTemplateStep step)
    {
        if (step == null)
            throw new ArgumentNullException(nameof(step));

        _templateSteps.Add(step);
        LastModifiedTime = DateTime.UtcNow;
        UpdateVersion();
    }

    /// <summary>
    /// Removes a template step
    /// </summary>
    /// <param name="stepId">The step identifier</param>
    public void RemoveTemplateStep(Guid stepId)
    {
        var step = _templateSteps.FirstOrDefault(s => s.StepId == stepId);
        if (step != null)
        {
            _templateSteps.Remove(step);
            LastModifiedTime = DateTime.UtcNow;
            UpdateVersion();
        }
    }

    /// <summary>
    /// Clones the workflow template
    /// </summary>
    /// <param name="newTemplateId">The new template identifier</param>
    /// <param name="newUserId">The new user identifier</param>
    /// <param name="newTemplateName">The new template name</param>
    /// <returns>A cloned workflow template</returns>
    public WorkflowTemplate Clone(Guid newTemplateId, Guid newUserId, string newTemplateName)
    {
        var clonedTemplate = new WorkflowTemplate(
            newTemplateId,
            newUserId,
            newTemplateName,
            Description,
            Category,
            TemplateDefinition,
            false, // Cloned templates are private by default
            false  // Cloned templates are not system templates
        );

        // Copy properties
        clonedTemplate.RequiredCapabilities.AddRange(RequiredCapabilities);
        clonedTemplate.Tags.AddRange(Tags);
        clonedTemplate.EstimatedExecutionTime = EstimatedExecutionTime;
        clonedTemplate.IconUrl = IconUrl;
        clonedTemplate.ThumbnailData = ThumbnailData;

        // Clone steps
        foreach (var step in _templateSteps)
        {
            var clonedStep = step.Clone(Guid.NewGuid(), newTemplateId);
            clonedTemplate.AddTemplateStep(clonedStep);
        }

        return clonedTemplate;
    }

    /// <summary>
    /// Validates the workflow template
    /// </summary>
    /// <returns>A validation result</returns>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(TemplateName))
            result.AddError(nameof(TemplateName), "Template name cannot be empty");

        if (string.IsNullOrWhiteSpace(Category))
            result.AddError(nameof(Category), "Category cannot be empty");

        // Validate template definition
        var definitionValidation = TemplateDefinition.Validate();
        if (!definitionValidation.IsValid)
        {
            foreach (var error in definitionValidation.Errors)
            {
                result.AddError($"TemplateDefinition.{error.PropertyName}", error.ErrorMessage);
            }
        }

        // Validate steps
        foreach (var step in _templateSteps)
        {
            var stepValidation = step.ValidateStep();
            if (!stepValidation.IsValid)
            {
                foreach (var error in stepValidation.Errors)
                {
                    result.AddError($"Step[{step.StepId}].{error.PropertyName}", error.ErrorMessage);
                }
            }
        }

        return result;
    }
}

/// <summary>
/// Domain event raised when a workflow template is created
/// </summary>
public class WorkflowTemplateCreatedEvent : DomainEvent
{
    public Guid TemplateId { get; }
    public Guid UserId { get; }
    public string TemplateName { get; }

    public WorkflowTemplateCreatedEvent(Guid templateId, Guid userId, string templateName)
    {
        TemplateId = templateId;
        UserId = userId;
        TemplateName = templateName;
    }
}

/// <summary>
/// Domain event raised when a workflow template is updated
/// </summary>
public class WorkflowTemplateUpdatedEvent : DomainEvent
{
    public Guid TemplateId { get; }
    public Guid UserId { get; }
    public string TemplateName { get; }

    public WorkflowTemplateUpdatedEvent(Guid templateId, Guid userId, string templateName)
    {
        TemplateId = templateId;
        UserId = userId;
        TemplateName = templateName;
    }
}

/// <summary>
/// Domain event raised when a workflow template definition is updated
/// </summary>
public class WorkflowTemplateDefinitionUpdatedEvent : DomainEvent
{
    public Guid TemplateId { get; }
    public string Version { get; }

    public WorkflowTemplateDefinitionUpdatedEvent(Guid templateId, string version)
    {
        TemplateId = templateId;
        Version = version;
    }
}

/// <summary>
/// Domain event raised when a workflow template visibility is changed
/// </summary>
public class WorkflowTemplateVisibilityChangedEvent : DomainEvent
{
    public Guid TemplateId { get; }
    public bool IsPublic { get; }

    public WorkflowTemplateVisibilityChangedEvent(Guid templateId, bool isPublic)
    {
        TemplateId = templateId;
        IsPublic = isPublic;
    }
}

/// <summary>
/// Domain event raised when a workflow template is used
/// </summary>
public class WorkflowTemplateUsedEvent : DomainEvent
{
    public Guid TemplateId { get; }
    public int UsageCount { get; }

    public WorkflowTemplateUsedEvent(Guid templateId, int usageCount)
    {
        TemplateId = templateId;
        UsageCount = usageCount;
    }
}

/// <summary>
/// Domain event raised when a workflow template is rated
/// </summary>
public class WorkflowTemplateRatedEvent : DomainEvent
{
    public Guid TemplateId { get; }
    public double Rating { get; }

    public WorkflowTemplateRatedEvent(Guid templateId, double rating)
    {
        TemplateId = templateId;
        Rating = rating;
    }
}