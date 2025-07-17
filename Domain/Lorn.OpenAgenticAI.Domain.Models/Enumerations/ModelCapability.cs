using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.Enumerations;

/// <summary>
/// 模型能力枚举
/// </summary>
public class ModelCapability : Enumeration
{
    public static ModelCapability TextGeneration = new(1, nameof(TextGeneration));
    public static ModelCapability MultiModal = new(2, nameof(MultiModal));
    public static ModelCapability FunctionCalling = new(3, nameof(FunctionCalling));
    public static ModelCapability CodeGeneration = new(4, nameof(CodeGeneration));
    public static ModelCapability DataAnalysis = new(5, nameof(DataAnalysis));
    public static ModelCapability WebSearch = new(6, nameof(WebSearch));
    public static ModelCapability Embedding = new(7, nameof(Embedding));
    public static ModelCapability FineTuning = new(8, nameof(FineTuning));
    public static ModelCapability StreamingOutput = new(9, nameof(StreamingOutput));
    public static ModelCapability ImageGeneration = new(10, nameof(ImageGeneration));
    public static ModelCapability AudioProcessing = new(11, nameof(AudioProcessing));
    public static ModelCapability VideoProcessing = new(12, nameof(VideoProcessing));

    public ModelCapability(int id, string name) : base(id, name)
    {
    }
}