# 偏好设置服务使用指南

## 概述

偏好设置服务提供了类型安全的个性化配置管理功能，支持三大类偏好设置：界面设置、语言设置和操作设置。服务包含以下核心组件：

- **IPreferenceService**: 核心偏好设置CRUD操作
- **IPreferenceNotificationService**: 偏好设置变更通知
- **IPreferenceApplyService**: 偏好设置实时应用
- **PreferenceManagementService**: 综合管理服务

## 功能特性

### 1. 类型安全的配置管理

```csharp
// 获取强类型偏好设置
var theme = await preferenceService.GetPreferenceAsync<string>(userId, "UI", "Theme", "Auto");
var fontSize = await preferenceService.GetPreferenceAsync<int>(userId, "UI", "FontSize", 14);
var autoSave = await preferenceService.GetPreferenceAsync<bool>(userId, "Operation", "EnableAutoSave", true);

// 设置偏好设置
await preferenceService.SetPreferenceAsync(userId, "UI", "Theme", "Dark", "用户界面主题设置");
```

### 2. 三大类偏好设置

#### 界面设置 (UI)
- 主题 (Theme): Light, Dark, Auto
- 字体大小 (FontSize): 10, 12, 14, 16, 18, 20, 24
- 布局 (Layout): Compact, Standard, Spacious
- 侧边栏显示 (ShowSidebar)
- 工具栏显示 (ShowToolbar)
- 状态栏显示 (ShowStatusbar)
- 窗口透明度 (WindowOpacity)
- 动画效果 (EnableAnimations)
- 界面缩放 (ScaleFactor)
- 颜色方案 (ColorScheme)

#### 语言设置 (Language)
- 界面语言 (UILanguage): zh-CN, en-US, ja-JP, ko-KR
- 输入语言 (InputLanguage)
- 输出语言 (OutputLanguage)
- 日期时间格式 (DateTimeFormat)
- 数字格式 (NumberFormat)
- 货币格式 (CurrencyFormat)
- 时区设置 (Timezone)

#### 操作设置 (Operation)
- 默认LLM模型 (DefaultLLMModel)
- 任务超时时间 (TaskTimeout)
- 自动保存间隔 (AutoSaveInterval)
- 最大并发任务数 (MaxConcurrentTasks)
- 启用自动保存 (EnableAutoSave)
- 启用操作确认 (EnableConfirmation)
- 启用操作日志 (EnableOperationLog)
- 默认工作目录 (DefaultWorkDirectory)
- 临时文件清理间隔 (TempCleanupInterval)
- 智能提示启用 (EnableSmartSuggestions)
- 响应速度优先级 (ResponseSpeedPriority)

### 3. 强类型扩展方法

```csharp
// 使用扩展方法进行强类型访问
var theme = await preferenceService.GetThemeAsync(userId);
await preferenceService.SetThemeAsync(userId, "Dark");

var fontSize = await preferenceService.GetFontSizeAsync(userId);
await preferenceService.SetFontSizeAsync(userId, 16);

var language = await preferenceService.GetUILanguageAsync(userId);
await preferenceService.SetUILanguageAsync(userId, "en-US");

var defaultModel = await preferenceService.GetDefaultLLMModelAsync(userId);
await preferenceService.SetDefaultLLMModelAsync(userId, "gpt-4");
```

### 4. 配置的分类管理

```csharp
// 获取指定分类的所有偏好设置
var uiPreferences = await preferenceService.GetCategoryPreferencesAsync(userId, "UI");

// 获取所有偏好设置，按分类组织
var allPreferences = await preferenceService.GetAllPreferencesAsync(userId);

// 重置指定分类为默认值
var resetCount = await preferenceService.ResetCategoryPreferencesAsync(userId, "UI");

// 重置所有偏好设置为默认值
var totalResetCount = await preferenceService.ResetAllPreferencesAsync(userId);
```

### 5. 默认值处理

```csharp
// 初始化用户默认偏好设置
var initCount = await preferenceService.InitializeDefaultPreferencesAsync(userId);

// 强制重置所有偏好设置为默认值
var resetCount = await managementService.InitializeUserPreferencesAsync(userId, forceReset: true);
```

### 6. 配置变更的实时应用和通知机制

```csharp
// 订阅偏好设置变更事件
preferenceService.PreferenceChanged += (sender, e) =>
{
    Console.WriteLine($"偏好设置变更: {e.Category}.{e.Key} = {e.NewValue}");
};

// 注册分类特定的变更处理器
notificationService.Subscribe("UI", async (e) =>
{
    if (e.Key == "Theme")
    {
        // 立即应用主题变更
        await ApplyThemeChange(e.NewValue?.ToString());
    }
});

// 检查是否需要重启应用
if (applyService.RequiresRestart(eventArgs))
{
    // 提示用户重启应用
    ShowRestartPrompt();
}
```

### 7. 配置的导入导出功能

```csharp
// 导出用户偏好设置
var exportData = await preferenceService.ExportPreferencesAsync(userId, includeSystemDefaults: false);

// 将导出数据序列化为JSON
var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync("preferences.json", json);

// 从文件导入偏好设置
var importJson = await File.ReadAllTextAsync("preferences.json");
var importData = JsonSerializer.Deserialize<PreferenceExportData>(importJson);
var importedCount = await preferenceService.ImportPreferencesAsync(userId, importData, overwriteExisting: true);
```

### 8. 批量操作

```csharp
// 批量设置偏好设置
var preferences = new Dictionary<string, Dictionary<string, object>>
{
    ["UI"] = new Dictionary<string, object>
    {
        ["Theme"] = "Dark",
        ["FontSize"] = 16,
        ["Layout"] = "Compact"
    },
    ["Operation"] = new Dictionary<string, object>
    {
        ["DefaultLLMModel"] = "gpt-4",
        ["TaskTimeout"] = 300,
        ["EnableAutoSave"] = true
    }
};

var setBatchCount = await preferenceService.SetPreferencesBatchAsync(userId, preferences);

// 使用管理服务进行批量更新
var updateResult = await managementService.UpdatePreferencesAsync(userId, preferences);
if (updateResult.HasErrors)
{
    foreach (var error in updateResult.FailedUpdates)
    {
        Console.WriteLine($"更新失败: {error.Category}.{error.Key} - {error.Error}");
    }
}
```

### 9. 统计信息

```csharp
// 获取偏好设置统计信息
var statistics = await preferenceService.GetStatisticsAsync(userId);
Console.WriteLine($"分类数量: {statistics.CategoryCount}");
Console.WriteLine($"总偏好数量: {statistics.TotalPreferences}");
Console.WriteLine($"最后更新时间: {statistics.LastUpdated}");

foreach (var category in statistics.PreferencesByCategory)
{
    Console.WriteLine($"{category.Key}: {category.Value} 项");
}
```

### 10. 完整的用户偏好配置文件

```csharp
// 获取用户的完整偏好设置配置文件
var profile = await managementService.GetUserPreferenceProfileAsync(userId);

Console.WriteLine($"用户 {profile.UserId} 的偏好设置配置:");
Console.WriteLine($"最后更新: {profile.LastUpdated}");

foreach (var category in profile.Categories)
{
    Console.WriteLine($"\n{category.Key} ({category.Value.Count} 项):");
    foreach (var preference in category.Value.Preferences)
    {
        Console.WriteLine($"  {preference.Key}: {preference.Value}");
    }
}
```

## 依赖注入配置

```csharp
// 在 Program.cs 或 Startup.cs 中注册服务
services.AddApplicationServices(); // 注册所有应用服务

// 或者分别注册
services.AddUserAccountServices(); // 用户账户服务
services.AddPreferenceServices();  // 偏好设置服务
```

## 验证和错误处理

```csharp
// 验证偏好设置值
var validationResult = managementService.ValidatePreference("UI", "Theme", "InvalidTheme");
if (!validationResult.IsValid)
{
    Console.WriteLine($"验证失败: {validationResult.ErrorMessage}");
}

// 错误处理示例
try
{
    await preferenceService.SetThemeAsync(userId, "InvalidTheme");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"无效的主题设置: {ex.Message}");
}
```

## 性能注意事项

1. **缓存**: 服务内部使用适当的缓存机制，避免频繁数据库查询
2. **批量操作**: 对于多个偏好设置变更，使用批量操作以提高性能
3. **异步操作**: 所有操作都是异步的，避免阻塞UI线程
4. **事件处理**: 偏好设置变更事件在后台异步处理，不影响主流程性能

## 扩展指南

要添加新的偏好设置：

1. 在 `PreferenceConstants` 中定义常量
2. 在相应的扩展方法中添加强类型访问方法
3. 在验证方法中添加验证逻辑
4. 在应用服务中添加实时应用逻辑

## 最佳实践

1. 使用常量定义而不是硬编码字符串
2. 使用强类型扩展方法进行偏好设置访问
3. 为关键偏好设置变更添加验证逻辑
4. 合理使用批量操作提高性能
5. 适当处理偏好设置变更事件
6. 为需要重启的偏好设置提供用户提示
