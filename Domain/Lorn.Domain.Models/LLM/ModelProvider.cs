using Lorn.Domain.Models.Common;
using Lorn.Domain.Models.Enumerations;
using Lorn.Domain.Models.ValueObjects;
using Lorn.Domain.Models.Capabilities;

namespace Lorn.Domain.Models.LLM;

/// <summary>
/// Model provider aggregate root
/// </summary>
public class ModelProvider : AggregateRoot
{
    private readonly List<Model> _models = new();
    private readonly List<ProviderUserConfiguration> _userConfigurations = new();

    /// <summary>
    /// Gets the provider identifier
    /// </summary>
    public Guid ProviderId { get; private set; }

    /// <summary>
    /// Gets the provider name
    /// </summary>
    public string ProviderName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the provider type
    /// </summary>
    public ProviderType ProviderType { get; private set; } = null!;

    /// <summary>
    /// Gets the icon URL
    /// </summary>
    public string? IconUrl { get; private set; }

    /// <summary>
    /// Gets the website URL
    /// </summary>
    public string? WebsiteUrl { get; private set; }

    /// <summary>
    /// Gets the API key URL
    /// </summary>
    public string? ApiKeyUrl { get; private set; }

    /// <summary>
    /// Gets the documentation URL
    /// </summary>
    public string? DocsUrl { get; private set; }

    /// <summary>
    /// Gets the models URL
    /// </summary>
    public string? ModelsUrl { get; private set; }

    /// <summary>
    /// Gets the default API configuration
    /// </summary>
    public ApiConfiguration DefaultApiConfiguration { get; private set; } = null!;

    /// <summary>
    /// Gets whether this is a prebuilt provider
    /// </summary>
    public bool IsPrebuilt { get; private set; }

    /// <summary>
    /// Gets the service status
    /// </summary>
    public ServiceStatus Status { get; private set; } = ServiceStatus.Unknown;

    /// <summary>
    /// Gets the created by user identifier
    /// </summary>
    public Guid? CreatedBy { get; private set; }

    /// <summary>
    /// Gets the models
    /// </summary>
    public IReadOnlyList<Model> Models => _models.AsReadOnly();

    /// <summary>
    /// Gets the user configurations
    /// </summary>
    public IReadOnlyList<ProviderUserConfiguration> UserConfigurations => _userConfigurations.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the ModelProvider class
    /// </summary>
    /// <param name="providerId">The provider identifier</param>
    /// <param name="providerName">The provider name</param>
    /// <param name="providerType">The provider type</param>
    /// <param name="defaultApiConfiguration">The default API configuration</param>
    /// <param name="isPrebuilt">Whether this is a prebuilt provider</param>
    /// <param name="createdBy">The user who created this provider</param>
    public ModelProvider(
        Guid providerId,
        string providerName,
        ProviderType providerType,
        ApiConfiguration defaultApiConfiguration,
        bool isPrebuilt = false,
        Guid? createdBy = null)
    {
        ProviderId = providerId;
        ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
        ProviderType = providerType ?? throw new ArgumentNullException(nameof(providerType));
        DefaultApiConfiguration = defaultApiConfiguration ?? throw new ArgumentNullException(nameof(defaultApiConfiguration));
        IsPrebuilt = isPrebuilt;
        CreatedBy = createdBy;
        Status = ServiceStatus.Unknown;
        
        Id = providerId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ModelProviderCreatedEvent(providerId, providerName, providerType.TypeName));
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private ModelProvider() 
    {
        ProviderType = null!;
        DefaultApiConfiguration = null!;
    }

    /// <summary>
    /// Updates the provider information
    /// </summary>
    /// <param name="providerName">The new provider name</param>
    /// <param name="iconUrl">The icon URL</param>
    /// <param name="websiteUrl">The website URL</param>
    /// <param name="apiKeyUrl">The API key URL</param>
    /// <param name="docsUrl">The documentation URL</param>
    /// <param name="modelsUrl">The models URL</param>
    public void UpdateProviderInfo(
        string providerName,
        string? iconUrl = null,
        string? websiteUrl = null,
        string? apiKeyUrl = null,
        string? docsUrl = null,
        string? modelsUrl = null)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be empty", nameof(providerName));

        ProviderName = providerName;
        IconUrl = iconUrl;
        WebsiteUrl = websiteUrl;
        ApiKeyUrl = apiKeyUrl;
        DocsUrl = docsUrl;
        ModelsUrl = modelsUrl;
        
        UpdateVersion();
        AddDomainEvent(new ModelProviderUpdatedEvent(ProviderId, ProviderName));
    }

    /// <summary>
    /// Updates the API configuration
    /// </summary>
    /// <param name="apiConfiguration">The new API configuration</param>
    public void UpdateApiConfiguration(ApiConfiguration apiConfiguration)
    {
        DefaultApiConfiguration = apiConfiguration ?? throw new ArgumentNullException(nameof(apiConfiguration));
        UpdateVersion();
        
        AddDomainEvent(new ModelProviderConfigurationUpdatedEvent(ProviderId, ProviderName));
    }

    /// <summary>
    /// Updates the service status
    /// </summary>
    /// <param name="newStatus">The new service status</param>
    public void UpdateStatus(ServiceStatus newStatus)
    {
        if (newStatus == null)
            throw new ArgumentNullException(nameof(newStatus));

        var previousStatus = Status;
        Status = newStatus;
        UpdateVersion();
        
        AddDomainEvent(new ModelProviderStatusChangedEvent(ProviderId, ProviderName, previousStatus.Name, newStatus.Name));
    }

    /// <summary>
    /// Adds a model to this provider
    /// </summary>
    /// <param name="model">The model to add</param>
    public void AddModel(Model model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        if (model.ProviderId != ProviderId)
            throw new ArgumentException("Model does not belong to this provider", nameof(model));

        // Remove existing model with same ID
        var existingModel = _models.FirstOrDefault(m => m.ModelId == model.ModelId);
        if (existingModel != null)
            _models.Remove(existingModel);

        _models.Add(model);
        UpdateVersion();
        
        AddDomainEvent(new ModelAddedToProviderEvent(ProviderId, model.ModelId, model.ModelName));
    }

    /// <summary>
    /// Removes a model from this provider
    /// </summary>
    /// <param name="modelId">The model identifier</param>
    public void RemoveModel(Guid modelId)
    {
        var model = _models.FirstOrDefault(m => m.ModelId == modelId);
        if (model != null)
        {
            _models.Remove(model);
            UpdateVersion();
            
            AddDomainEvent(new ModelRemovedFromProviderEvent(ProviderId, modelId, model.ModelName));
        }
    }

    /// <summary>
    /// Validates the provider configuration
    /// </summary>
    /// <returns>A validation result</returns>
    public ValidationResult ValidateProvider()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(ProviderName))
            result.AddError("ProviderName", "Provider name is required");

        if (ProviderType == null)
            result.AddError("ProviderType", "Provider type is required");

        var configValidation = DefaultApiConfiguration?.ValidateConfiguration();
        if (configValidation != null && !configValidation.IsValid)
        {
            foreach (var error in configValidation.Errors)
            {
                result.AddError($"DefaultApiConfiguration.{error.PropertyName}", error.ErrorMessage);
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if this provider is compatible with the specified type
    /// </summary>
    /// <param name="type">The provider type to check</param>
    /// <returns>True if compatible, false otherwise</returns>
    public bool IsCompatibleWith(ProviderType type)
    {
        return ProviderType == type;
    }

    /// <summary>
    /// Gets models by capability
    /// </summary>
    /// <param name="capability">The capability to search for</param>
    /// <returns>List of models with the specified capability</returns>
    public List<Model> GetModelsByCapability(ModelCapability capability)
    {
        return _models.Where(m => m.SupportsCapability(capability)).ToList();
    }

    /// <summary>
    /// Gets the latest version models
    /// </summary>
    /// <returns>List of latest version models</returns>
    public List<Model> GetLatestVersionModels()
    {
        return _models.Where(m => m.IsLatestVersion).ToList();
    }

    /// <summary>
    /// Activates the provider
    /// </summary>
    public new void Activate()
    {
        base.Activate();
        UpdateStatus(ServiceStatus.Available);
    }

    /// <summary>
    /// Deactivates the provider
    /// </summary>
    public new void Deactivate()
    {
        base.Deactivate();
        UpdateStatus(ServiceStatus.Unavailable);
    }
}

/// <summary>
/// Provider type entity
/// </summary>
public class ProviderType : BaseEntity
{
    private readonly List<AuthenticationMethod> _supportedAuthMethods = new();

    /// <summary>
    /// Gets the type identifier
    /// </summary>
    public Guid TypeId { get; private set; }

    /// <summary>
    /// Gets the type name
    /// </summary>
    public string TypeName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the adapter class name
    /// </summary>
    public string AdapterClassName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the default settings
    /// </summary>
    public Dictionary<string, object> DefaultSettings { get; private set; } = new();

    /// <summary>
    /// Gets whether this is a built-in type
    /// </summary>
    public bool IsBuiltIn { get; private set; }

    /// <summary>
    /// Gets the supported authentication methods
    /// </summary>
    public IReadOnlyList<AuthenticationMethod> SupportedAuthMethods => _supportedAuthMethods.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the ProviderType class
    /// </summary>
    /// <param name="typeId">The type identifier</param>
    /// <param name="typeName">The type name</param>
    /// <param name="description">The description</param>
    /// <param name="adapterClassName">The adapter class name</param>
    /// <param name="isBuiltIn">Whether this is a built-in type</param>
    public ProviderType(
        Guid typeId,
        string typeName,
        string description,
        string adapterClassName,
        bool isBuiltIn = false)
    {
        TypeId = typeId;
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        AdapterClassName = adapterClassName ?? throw new ArgumentNullException(nameof(adapterClassName));
        IsBuiltIn = isBuiltIn;
        
        Id = typeId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private ProviderType() { }

    /// <summary>
    /// Adds a supported authentication method
    /// </summary>
    /// <param name="authMethod">The authentication method</param>
    public void AddSupportedAuthMethod(AuthenticationMethod authMethod)
    {
        if (authMethod == null)
            throw new ArgumentNullException(nameof(authMethod));

        if (!_supportedAuthMethods.Contains(authMethod))
        {
            _supportedAuthMethods.Add(authMethod);
            UpdateVersion();
        }
    }

    /// <summary>
    /// Checks if the specified authentication method is supported
    /// </summary>
    /// <param name="method">The authentication method</param>
    /// <returns>True if supported, false otherwise</returns>
    public bool SupportsAuthMethod(AuthenticationMethod method)
    {
        return _supportedAuthMethods.Contains(method);
    }

    /// <summary>
    /// Sets a default setting
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="value">The setting value</param>
    public void SetDefaultSetting(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        DefaultSettings[key] = value;
        UpdateVersion();
    }

    /// <summary>
    /// Gets a default setting
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <returns>The setting value or null if not found</returns>
    public object? GetDefaultSetting(string key)
    {
        return DefaultSettings.ContainsKey(key) ? DefaultSettings[key] : null;
    }
}

/// <summary>
/// Domain event raised when a model provider is created
/// </summary>
public class ModelProviderCreatedEvent : DomainEvent
{
    public Guid ProviderId { get; }
    public string ProviderName { get; }
    public string ProviderType { get; }

    public ModelProviderCreatedEvent(Guid providerId, string providerName, string providerType)
    {
        ProviderId = providerId;
        ProviderName = providerName;
        ProviderType = providerType;
    }
}

/// <summary>
/// Domain event raised when a model provider is updated
/// </summary>
public class ModelProviderUpdatedEvent : DomainEvent
{
    public Guid ProviderId { get; }
    public string ProviderName { get; }

    public ModelProviderUpdatedEvent(Guid providerId, string providerName)
    {
        ProviderId = providerId;
        ProviderName = providerName;
    }
}

/// <summary>
/// Domain event raised when a model provider configuration is updated
/// </summary>
public class ModelProviderConfigurationUpdatedEvent : DomainEvent
{
    public Guid ProviderId { get; }
    public string ProviderName { get; }

    public ModelProviderConfigurationUpdatedEvent(Guid providerId, string providerName)
    {
        ProviderId = providerId;
        ProviderName = providerName;
    }
}

/// <summary>
/// Domain event raised when a model provider status changes
/// </summary>
public class ModelProviderStatusChangedEvent : DomainEvent
{
    public Guid ProviderId { get; }
    public string ProviderName { get; }
    public string PreviousStatus { get; }
    public string NewStatus { get; }

    public ModelProviderStatusChangedEvent(Guid providerId, string providerName, string previousStatus, string newStatus)
    {
        ProviderId = providerId;
        ProviderName = providerName;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
    }
}

/// <summary>
/// Domain event raised when a model is added to a provider
/// </summary>
public class ModelAddedToProviderEvent : DomainEvent
{
    public Guid ProviderId { get; }
    public Guid ModelId { get; }
    public string ModelName { get; }

    public ModelAddedToProviderEvent(Guid providerId, Guid modelId, string modelName)
    {
        ProviderId = providerId;
        ModelId = modelId;
        ModelName = modelName;
    }
}

/// <summary>
/// Domain event raised when a model is removed from a provider
/// </summary>
public class ModelRemovedFromProviderEvent : DomainEvent
{
    public Guid ProviderId { get; }
    public Guid ModelId { get; }
    public string ModelName { get; }

    public ModelRemovedFromProviderEvent(Guid providerId, Guid modelId, string modelName)
    {
        ProviderId = providerId;
        ModelId = modelId;
        ModelName = modelName;
    }
}