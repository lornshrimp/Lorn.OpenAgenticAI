using System.Collections.Generic;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.Enumerations;

/// <summary>
/// Agent类型枚举
/// </summary>
public class AgentType : Enumeration
{
    public static AgentType ApplicationAutomation = new(1, nameof(ApplicationAutomation), "Application automation and control");
    public static AgentType DataProcessing = new(2, nameof(DataProcessing), "Data processing and transformation");
    public static AgentType WebService = new(3, nameof(WebService), "Web service and API interaction");
    public static AgentType FileSystem = new(4, nameof(FileSystem), "File system operations");
    public static AgentType Communication = new(5, nameof(Communication), "Communication and messaging");
    public static AgentType Custom = new(6, nameof(Custom), "Custom user-defined agent");

    public string Description { get; }

    public AgentType(int id, string name, string description) : base(id, name)
    {
        Description = description;
    }

    /// <summary>
    /// 获取支持的操作类型
    /// </summary>
    public List<string> GetSupportedActions()
    {
        return Id switch
        {
            1 => new List<string> { "OpenApplication", "CloseApplication", "SendInput", "GetWindowState" },
            2 => new List<string> { "ProcessData", "TransformData", "ValidateData", "ExportData" },
            3 => new List<string> { "HttpRequest", "WebHook", "ApiCall", "DataSync" },
            4 => new List<string> { "ReadFile", "WriteFile", "CopyFile", "DeleteFile", "ListDirectory" },
            5 => new List<string> { "SendEmail", "SendMessage", "CreateMeeting", "Notification" },
            6 => new List<string> { "CustomAction" },
            _ => new List<string>()
        };
    }
}