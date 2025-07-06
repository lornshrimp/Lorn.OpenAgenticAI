namespace Lorn.Domain.Models.Enumerations;

/// <summary>
/// Model capability enumeration
/// </summary>
public sealed class ModelCapability : Enumeration
{
    /// <summary>
    /// Text generation capability
    /// </summary>
    public static readonly ModelCapability TextGeneration = new(1, nameof(TextGeneration));

    /// <summary>
    /// Text embedding capability
    /// </summary>
    public static readonly ModelCapability Embedding = new(2, nameof(Embedding));

    /// <summary>
    /// Image generation capability
    /// </summary>
    public static readonly ModelCapability ImageGeneration = new(3, nameof(ImageGeneration));

    /// <summary>
    /// Image analysis capability
    /// </summary>
    public static readonly ModelCapability ImageAnalysis = new(4, nameof(ImageAnalysis));

    /// <summary>
    /// Code generation capability
    /// </summary>
    public static readonly ModelCapability CodeGeneration = new(5, nameof(CodeGeneration));

    /// <summary>
    /// Function calling capability
    /// </summary>
    public static readonly ModelCapability FunctionCalling = new(6, nameof(FunctionCalling));

    /// <summary>
    /// Streaming response capability
    /// </summary>
    public static readonly ModelCapability StreamingResponse = new(7, nameof(StreamingResponse));

    /// <summary>
    /// Fine-tuning capability
    /// </summary>
    public static readonly ModelCapability FineTuning = new(8, nameof(FineTuning));

    /// <summary>
    /// Multi-modal capability
    /// </summary>
    public static readonly ModelCapability MultiModal = new(9, nameof(MultiModal));

    /// <summary>
    /// Data analysis capability
    /// </summary>
    public static readonly ModelCapability DataAnalysis = new(10, nameof(DataAnalysis));

    /// <summary>
    /// Web search capability
    /// </summary>
    public static readonly ModelCapability WebSearch = new(11, nameof(WebSearch));

    /// <summary>
    /// Audio processing capability
    /// </summary>
    public static readonly ModelCapability AudioProcessing = new(12, nameof(AudioProcessing));

    /// <summary>
    /// Video processing capability
    /// </summary>
    public static readonly ModelCapability VideoProcessing = new(13, nameof(VideoProcessing));

    /// <summary>
    /// Initializes a new instance of the ModelCapability class
    /// </summary>
    /// <param name="id">The unique identifier</param>
    /// <param name="name">The name</param>
    private ModelCapability(int id, string name) : base(id, name) { }

    /// <summary>
    /// Checks if this capability is compatible with the specified capability
    /// </summary>
    /// <param name="other">The other capability</param>
    /// <returns>True if compatible, false otherwise</returns>
    public bool IsCompatibleWith(ModelCapability other)
    {
        // Define compatibility rules
        var compatiblePairs = new Dictionary<ModelCapability, List<ModelCapability>>
        {
            { TextGeneration, new List<ModelCapability> { FunctionCalling, StreamingResponse, CodeGeneration } },
            { MultiModal, new List<ModelCapability> { ImageAnalysis, AudioProcessing, VideoProcessing } },
            { DataAnalysis, new List<ModelCapability> { TextGeneration, CodeGeneration } },
            { WebSearch, new List<ModelCapability> { TextGeneration, DataAnalysis } }
        };

        return compatiblePairs.ContainsKey(this) && compatiblePairs[this].Contains(other);
    }
}