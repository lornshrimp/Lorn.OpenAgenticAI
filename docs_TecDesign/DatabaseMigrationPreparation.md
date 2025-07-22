# 数据库迁移准备工作总结

## 问题分析与解决方案

### 1. 枚举类处理 ✅ 已解决
**问题**: 继承自 `Enumeration` 的类（如 `ExecutionStatus`、`ModelCapability` 等）被EF Core视为需要创建数据库表的实体。

**解决方案**: 
- 创建了 `[Enumeration]` 标记特性
- 为相关枚举类添加了 `[Enumeration]` 标记
- 在 EF Core 配置中需要忽略这些类型

### 2. Dictionary<string, object> 复杂类型处理 ✅ 已解决
**问题**: EF Core 无法直接映射 `Dictionary<string, object>` 类型到数据库。

**解决方案**: 
1. **ModelParameters.AdditionalParameters** → 创建 `ModelParameterEntry` 实体
2. **QualitySettings.CustomThresholds** → 创建 `QualityThresholdEntry` 实体  
3. **UserProfile.Metadata** → 创建 `UserMetadataEntry` 实体

### 3. 数据库存储标记 ✅ 已完成
**创建的标记特性**:
- `IEntity` - 标识需要存储的实体
- `IAggregateRoot` - 标识聚合根实体
- `[NotPersisted]` - 标识不需要存储的类
- `[Enumeration]` - 标识枚举类
- `[ValueObject]` - 标识值对象
- `[DataTransferObject]` - 标识DTO类

## 已创建的新实体类

### 1. ModelParameterEntry
**用途**: 存储模型参数中的附加参数
**字段**: 
- ConfigurationId (关联ID)
- Key (参数键)
- ValueJson (JSON序列化的值)
- ValueType (类型信息)

### 2. QualityThresholdEntry  
**用途**: 存储质量设置中的自定义阈值
**字段**:
- ConfigurationId (关联ID)
- ThresholdName (阈值名称)
- ThresholdValue (阈值数值)
- Description (描述)

### 3. UserMetadataEntry
**用途**: 存储用户配置文件中的元数据
**字段**:
- UserId (用户ID)
- Key (元数据键)
- ValueJson (JSON序列化的值)
- ValueType (类型信息)
- Category (分类)

## 已修改的类

### 1. ModelParameters (值对象)
- ✅ 移除 `Dictionary<string, object> AdditionalParameters`
- ✅ 添加 `[ValueObject]` 标记
- ✅ 更新构造函数和相关方法

### 2. QualitySettings (值对象)
- ✅ 移除 `Dictionary<string, double> CustomThresholds`
- ✅ 添加 `[ValueObject]` 标记  
- ✅ 更新相关方法

### 3. UserProfile (聚合根实体)
- ✅ 移除 `Dictionary<string, object> Metadata`
- ✅ 实现 `IAggregateRoot` 接口
- ✅ 添加 `MetadataEntries` 导航属性

### 4. OpenAgenticAIDbContext
- ✅ 添加新的 DbSet
- ✅ 配置实体映射
- ✅ 配置关系映射

## 下一步需要处理的事项

### 1. EF Core 配置更新
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // 忽略枚举类型
    modelBuilder.Ignore<ExecutionStatus>();
    modelBuilder.Ignore<ModelCapability>();
    // ... 其他枚举类型
    
    // 忽略值对象
    modelBuilder.Ignore<ModelParameters>();
    modelBuilder.Ignore<QualitySettings>();
    // ... 其他值对象
}
```

### 2. 数据访问层适配
需要创建服务类来处理复杂类型的转换：
- `IModelParameterService` - 处理 ModelParameters 与 ModelParameterEntry 的转换
- `IQualitySettingsService` - 处理 QualitySettings 与 QualityThresholdEntry 的转换
- `IUserMetadataService` - 处理 UserProfile.Metadata 与 UserMetadataEntry 的转换

### 3. 应用层更新
更新使用这些类的应用层代码，使其通过新的实体类来访问复杂数据。

### 4. 其他需要检查的类
还需要检查以下类是否有类似问题：
- `WorkflowDefinition.Metadata`
- `StepParameters.InputParameters/OutputParameters`
- `Permission.Constraints`
- 其他包含 `Dictionary<string, object>` 的类

### 5. 迁移脚本生成
完成上述配置后，可以运行以下命令生成迁移：
```bash
dotnet ef migrations add InitialCreate --project Infrastructure/Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite
dotnet ef database update --project Infrastructure/Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite
```

## 设计原则总结

1. **枚举类**: 标记为 `[Enumeration]`，在 EF Core 中忽略
2. **值对象**: 标记为 `[ValueObject]`，作为复杂类型嵌入实体中
3. **复杂字典**: 创建独立实体类存储，支持 JSON 序列化
4. **实体类**: 实现 `IEntity` 或 `IAggregateRoot` 接口
5. **DTO 类**: 标记为 `[DataTransferObject]`，不持久化

这样的设计确保了：
- ✅ 清晰的领域模型边界
- ✅ EF Core 能够正确映射和迁移
- ✅ 支持复杂数据类型的存储和检索
- ✅ 保持了原有的业务逻辑完整性
