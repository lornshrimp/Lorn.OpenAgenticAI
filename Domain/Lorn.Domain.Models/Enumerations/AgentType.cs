namespace Lorn.Domain.Models.Enumerations;

/// <summary>
/// Agent type enumeration
/// </summary>
public sealed class AgentType : Enumeration
{
    /// <summary>
    /// Application automation agent
    /// </summary>
    public static readonly AgentType ApplicationAutomation = new(1, nameof(ApplicationAutomation), "Automates desktop and web applications");

    /// <summary>
    /// Data processing agent
    /// </summary>
    public static readonly AgentType DataProcessing = new(2, nameof(DataProcessing), "Processes and transforms data");

    /// <summary>
    /// Web service agent
    /// </summary>
    public static readonly AgentType WebService = new(3, nameof(WebService), "Interfaces with web services and APIs");

    /// <summary>
    /// File system agent
    /// </summary>
    public static readonly AgentType FileSystem = new(4, nameof(FileSystem), "Manages files and directories");

    /// <summary>
    /// Communication agent
    /// </summary>
    public static readonly AgentType Communication = new(5, nameof(Communication), "Handles communication tasks");

    /// <summary>
    /// Custom agent
    /// </summary>
    public static readonly AgentType Custom = new(6, nameof(Custom), "User-defined custom agent");

    /// <summary>
    /// Gets the description of the agent type
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the AgentType class
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <param name="name">The name</param>
    /// <param name="description">The description</param>
    private AgentType(int id, string name, string description) : base(id, name)
    {
        Description = description;
    }

    /// <summary>
    /// Gets the supported actions for this agent type
    /// </summary>
    /// <returns>List of supported actions</returns>
    public List<string> GetSupportedActions()
    {
        return this switch
        {
            var t when t == ApplicationAutomation => new List<string> { "Click", "Type", "Navigate", "Screenshot", "Extract" },
            var t when t == DataProcessing => new List<string> { "Transform", "Filter", "Aggregate", "Validate", "Convert" },
            var t when t == WebService => new List<string> { "Get", "Post", "Put", "Delete", "Upload", "Download" },
            var t when t == FileSystem => new List<string> { "Read", "Write", "Create", "Delete", "Move", "Copy", "List" },
            var t when t == Communication => new List<string> { "Send", "Receive", "Forward", "Schedule", "Notify" },
            var t when t == Custom => new List<string> { "Execute", "Process", "Handle" },
            _ => new List<string>()
        };
    }
}