using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.Enumerations;

/// <summary>
/// MCP协议类型枚举
/// </summary>
public class MCPProtocolType : Enumeration
{
    public static MCPProtocolType StandardIO = new(1, nameof(StandardIO));
    public static MCPProtocolType ServerSentEvents = new(2, nameof(ServerSentEvents));
    public static MCPProtocolType StreamableHTTP = new(3, nameof(StreamableHTTP));
    public static MCPProtocolType WebSocket = new(4, nameof(WebSocket));
    public static MCPProtocolType NamedPipes = new(5, nameof(NamedPipes));
    public static MCPProtocolType UnixSocket = new(6, nameof(UnixSocket));

    public MCPProtocolType(int id, string name) : base(id, name)
    {
    }
}