using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Linq;
using System.Reflection;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core 模型配置扩展，用于自动处理标记的类型
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// 自动配置标记的类型（枚举、值对象、DTO等）
    /// </summary>
    public static void IgnoreMarkedTypes(this ModelBuilder modelBuilder)
    {
        var assembly = Assembly.GetAssembly(typeof(Lorn.OpenAgenticAI.Domain.Models.Common.ValueObject));
        if (assembly == null) return;

        var typesToIgnore = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<NotPersistedAttribute>() != null)
            .ToList();

        foreach (var type in typesToIgnore)
        {
            modelBuilder.Ignore(type);
        }
    }

    /// <summary>
    /// 配置实体接口
    /// </summary>
    public static void ConfigureEntityInterfaces(this ModelBuilder modelBuilder)
    {
        // 获取所有实体类型的副本，避免在枚举过程中修改集合
        var entityTypes = modelBuilder.Model.GetEntityTypes().ToList();

        // 配置所有实现 IEntity 的类型的基础属性
        foreach (var entityType in entityTypes)
        {
            var clrType = entityType.ClrType;

            if (typeof(IEntity).IsAssignableFrom(clrType))
            {
                // 为实体添加通用配置
                var idProperty = entityType.FindProperty("Id");
                if (idProperty != null)
                {
                    // 在 EF Core 中，列名通常由约定自动设置
                    // 如果需要自定义列名，应该在具体的实体配置中处理
                }

                // 如果是聚合根，可以添加特定配置
                if (typeof(IAggregateRoot).IsAssignableFrom(clrType))
                {
                    // 聚合根特定配置 - 可以添加审计字段等
                    // 例如：添加创建时间、更新时间等
                }
            }
        }
    }

    /// <summary>
    /// 自动配置值对象为拥有类型（Owned Types）
    /// </summary>
    public static void ConfigureValueObjects(this ModelBuilder modelBuilder)
    {
        var assembly = Assembly.GetAssembly(typeof(Lorn.OpenAgenticAI.Domain.Models.Common.ValueObject));
        if (assembly == null) return;

        var valueObjectTypes = assembly.GetTypes()
            .Where(type => typeof(ValueObject).IsAssignableFrom(type) && !type.IsAbstract)
            .Where(type => type.GetCustomAttribute<ValueObjectAttribute>() != null)
            .ToList();

        // 值对象将作为复杂类型嵌入到实体中，而不是独立的表
        foreach (var valueObjectType in valueObjectTypes)
        {
            // 获取所有实体类型的副本，避免在枚举过程中修改集合
            var entityTypes = modelBuilder.Model.GetEntityTypes().ToList();

            // 查找使用此值对象的实体属性
            foreach (var entityType in entityTypes)
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(prop => prop.PropertyType == valueObjectType);

                foreach (var property in properties)
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .OwnsOne(valueObjectType, property.Name);
                }
            }
        }
    }
}
