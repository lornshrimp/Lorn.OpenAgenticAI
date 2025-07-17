using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.Enumerations;

/// <summary>
/// ½¡¿µ×´Ì¬Ã¶¾Ù
/// </summary>
public class HealthStatus : Enumeration
{
    public static HealthStatus Healthy = new(1, nameof(Healthy));
    public static HealthStatus Warning = new(2, nameof(Warning));
    public static HealthStatus Critical = new(3, nameof(Critical));
    public static HealthStatus Unknown = new(4, nameof(Unknown));
    public static HealthStatus Offline = new(5, nameof(Offline));

    public HealthStatus(int id, string name) : base(id, name)
    {
    }
}