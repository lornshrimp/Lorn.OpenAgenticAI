using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.Enumerations;

/// <summary>
/// 服务状态枚举
/// </summary>
public class ServiceStatus : Enumeration
{
    public static ServiceStatus Available = new(1, nameof(Available));
    public static ServiceStatus Maintenance = new(2, nameof(Maintenance));
    public static ServiceStatus Deprecated = new(3, nameof(Deprecated));
    public static ServiceStatus Unavailable = new(4, nameof(Unavailable));
    public static ServiceStatus Unknown = new(5, nameof(Unknown));

    public ServiceStatus(int id, string name) : base(id, name)
    {
    }
}

/// <summary>
/// 认证方法枚举
/// </summary>
public class AuthenticationMethod : Enumeration
{
    public static AuthenticationMethod ApiKey = new(1, nameof(ApiKey));
    public static AuthenticationMethod OAuth2 = new(2, nameof(OAuth2));
    public static AuthenticationMethod BearerToken = new(3, nameof(BearerToken));
    public static AuthenticationMethod CustomAuth = new(4, nameof(CustomAuth));
    public static AuthenticationMethod None = new(5, nameof(None));

    public AuthenticationMethod(int id, string name) : base(id, name)
    {
    }
}

/// <summary>
/// 货币枚举
/// </summary>
public class Currency : Enumeration
{
    public static Currency USD = new(1, nameof(USD));
    public static Currency CNY = new(2, nameof(CNY));
    public static Currency EUR = new(3, nameof(EUR));
    public static Currency GBP = new(4, nameof(GBP));
    public static Currency JPY = new(5, nameof(JPY));

    public Currency(int id, string name) : base(id, name)
    {
    }
}

/// <summary>
/// 降级条件枚举
/// </summary>
public class FallbackCondition : Enumeration
{
    public static FallbackCondition HighLatency = new(1, nameof(HighLatency));
    public static FallbackCondition LowQuality = new(2, nameof(LowQuality));
    public static FallbackCondition ErrorRate = new(3, nameof(ErrorRate));
    public static FallbackCondition QuotaExceeded = new(4, nameof(QuotaExceeded));
    public static FallbackCondition ServiceUnavailable = new(5, nameof(ServiceUnavailable));

    public FallbackCondition(int id, string name) : base(id, name)
    {
    }
}

/// <summary>
/// 降级策略枚举
/// </summary>
public class FallbackStrategy : Enumeration
{
    public static FallbackStrategy BestPerformance = new(1, nameof(BestPerformance));
    public static FallbackStrategy LowestCost = new(2, nameof(LowestCost));
    public static FallbackStrategy HighestAvailability = new(3, nameof(HighestAvailability));
    public static FallbackStrategy CustomPriority = new(4, nameof(CustomPriority));

    public FallbackStrategy(int id, string name) : base(id, name)
    {
    }
}