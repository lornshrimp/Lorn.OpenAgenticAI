using System.Text.Json;
using Lorn.OpenAgenticAI.Shared.Contracts.LLM;

namespace Lorn.OpenAgenticAI.Domain.LLM.Infrastructure;

/// <summary>
/// 基于System.Text.Json的缓存序列化器实现
/// </summary>
public class JsonCacheSerializer : ICacheSerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonCacheSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public byte[] Serialize<T>(T value) where T : class
    {
        if (value == null)
            return Array.Empty<byte>();

        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(value, _options);
        }
        catch (Exception)
        {
            return Array.Empty<byte>();
        }
    }

    /// <inheritdoc />
    public T? Deserialize<T>(byte[] data) where T : class
    {
        if (data == null || data.Length == 0)
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(data, _options);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
