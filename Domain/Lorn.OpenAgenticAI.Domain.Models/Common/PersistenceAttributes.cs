using System;

namespace Lorn.OpenAgenticAI.Domain.Models.Common;

/// <summary>
/// 标记接口，用于标识需要在数据库中存储的实体
/// </summary>
public interface IEntity
{
    /// <summary>
    /// 实体唯一标识符
    /// </summary>
    Guid Id { get; }
}

/// <summary>
/// 标记接口，用于标识聚合根实体
/// </summary>
public interface IAggregateRoot : IEntity
{
}

/// <summary>
/// 标记特性，用于标识不需要在数据库中存储的类
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class NotPersistedAttribute : Attribute
{
    public string Reason { get; }

    public NotPersistedAttribute(string reason = "")
    {
        Reason = reason;
    }
}

/// <summary>
/// 标记特性，用于标识枚举类（不需要在数据库中创建表）
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EnumerationAttribute : NotPersistedAttribute
{
    public EnumerationAttribute() : base("Enumeration classes are value objects, not entities")
    {
    }
}

/// <summary>
/// 标记特性，用于标识值对象（不需要在数据库中创建表）
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ValueObjectAttribute : NotPersistedAttribute
{
    public ValueObjectAttribute() : base("Value objects are embedded in entities")
    {
    }
}

/// <summary>
/// 标记特性，用于标识 DTO 类（不需要在数据库中创建表）
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DataTransferObjectAttribute : NotPersistedAttribute
{
    public DataTransferObjectAttribute() : base("DTOs are for data transfer only")
    {
    }
}
