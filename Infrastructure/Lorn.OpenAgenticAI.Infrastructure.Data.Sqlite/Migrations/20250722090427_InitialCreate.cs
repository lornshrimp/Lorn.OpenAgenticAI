using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiHeaderEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HeaderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HeaderValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsEnabled = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ApiConfigurationId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiHeaderEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelParameterEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ConfigurationId = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ValueJson = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelParameterEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelProviders",
                columns: table => new
                {
                    ProviderId = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProviderTypeId = table.Column<string>(type: "TEXT", nullable: false),
                    IconUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ApiKeyUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DocsUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ModelsUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DefaultApiConfiguration = table.Column<string>(type: "TEXT", nullable: false),
                    IsPrebuilt = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelProviders", x => x.ProviderId);
                });

            migrationBuilder.CreateTable(
                name: "PricingSpecialEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PricingKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Price = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsEnabled = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PricingInfoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingSpecialEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QualityThresholdEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ConfigurationId = table.Column<string>(type: "TEXT", nullable: false),
                    ThresholdName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ThresholdValue = table.Column<double>(type: "REAL", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityThresholdEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceUtilizationEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ResourceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UtilizationRate = table.Column<double>(type: "decimal(5,4)", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsEnabled = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExecutionMetricsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceUtilizationEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StepExecutionTimeEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StepName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ExecutionTimeMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsEnabled = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExecutionMetricsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepExecutionTimeEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StepParameters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsageQuota",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DailyLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    MonthlyLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    CostLimit = table.Column<string>(type: "TEXT", nullable: true),
                    AlertThreshold = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageQuota", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<int>(type: "INTEGER", nullable: false),
                    ProfileVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    SecuritySettings_AuthenticationMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SecuritySettings_SessionTimeoutMinutes = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 30),
                    SecuritySettings_RequireTwoFactor = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    SecuritySettings_PasswordLastChanged = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SecuritySettings_AdditionalSettings = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowMetadataEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MetadataKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ValueJson = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsEnabled = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WorkflowDefinitionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowMetadataEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Models",
                columns: table => new
                {
                    ModelId = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderId = table.Column<string>(type: "TEXT", nullable: false),
                    ModelName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModelGroup = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ContextLength = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxOutputTokens = table.Column<int>(type: "INTEGER", nullable: true),
                    SupportedCapabilities = table.Column<string>(type: "TEXT", nullable: false),
                    PricingInfo = table.Column<string>(type: "TEXT", nullable: false),
                    PerformanceMetrics = table.Column<string>(type: "TEXT", nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsLatestVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPrebuilt = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Models", x => x.ModelId);
                    table.ForeignKey(
                        name: "FK_Models_ModelProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "ModelProviders",
                        principalColumn: "ProviderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StepParameterEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    StepParametersId = table.Column<string>(type: "TEXT", nullable: false),
                    ParameterType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValueJson = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepParameterEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StepParameterEntries_StepParameters_StepParametersId",
                        column: x => x.StepParametersId,
                        principalTable: "StepParameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsageQuotaCustomLimitEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UsageQuotaId = table.Column<string>(type: "TEXT", nullable: false),
                    LimitName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LimitValue = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageQuotaCustomLimitEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageQuotaCustomLimitEntries_UsageQuota_UsageQuotaId",
                        column: x => x.UsageQuotaId,
                        principalTable: "UsageQuota",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MCPConfigurations",
                columns: table => new
                {
                    ConfigurationId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Command = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Arguments = table.Column<string>(type: "TEXT", nullable: false),
                    EnvironmentVariables = table.Column<string>(type: "TEXT", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: true),
                    ProviderInfo = table.Column<string>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    LastUsedTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AdapterConfiguration = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MCPConfigurations", x => x.ConfigurationId);
                    table.ForeignKey(
                        name: "FK_MCPConfigurations_UserProfiles_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProviderUserConfigurations",
                columns: table => new
                {
                    ConfigurationId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderId = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    UsageQuotaId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderUserConfigurations", x => x.ConfigurationId);
                    table.ForeignKey(
                        name: "FK_ProviderUserConfigurations_ModelProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "ModelProviders",
                        principalColumn: "ProviderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderUserConfigurations_UsageQuota_UsageQuotaId",
                        column: x => x.UsageQuotaId,
                        principalTable: "UsageQuota",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderUserConfigurations_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskExecutionHistories",
                columns: table => new
                {
                    ExecutionId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RequestId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserInput = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RequestType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ExecutionStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalExecutionTime = table.Column<long>(type: "INTEGER", nullable: false),
                    IsSuccessful = table.Column<int>(type: "INTEGER", nullable: false),
                    ResultSummary = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LlmProvider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LlmModel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TokenUsage = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedCost = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskExecutionHistories", x => x.ExecutionId);
                    table.ForeignKey(
                        name: "FK_TaskExecutionHistories_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserMetadataEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ValueJson = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMetadataEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMetadataEntries_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    PreferenceId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    PreferenceCategory = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PreferenceKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PreferenceValue = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastUpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsSystemDefault = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.PreferenceId);
                    table.ForeignKey(
                        name: "FK_UserPreferences_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTemplates",
                columns: table => new
                {
                    TemplateId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    TemplateName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsPublic = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSystemTemplate = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateVersion = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<double>(type: "REAL", nullable: false),
                    TemplateDefinition = table.Column<string>(type: "TEXT", nullable: false),
                    RequiredCapabilities = table.Column<string>(type: "TEXT", nullable: false),
                    EstimatedExecutionTime = table.Column<long>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    IconUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ThumbnailData = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTemplates", x => x.TemplateId);
                    table.ForeignKey(
                        name: "FK_WorkflowTemplates_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelUserConfigurations",
                columns: table => new
                {
                    ConfigurationId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderId = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProviderUserConfigurationConfigurationId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelUserConfigurations", x => x.ConfigurationId);
                    table.ForeignKey(
                        name: "FK_ModelUserConfigurations_ModelProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "ModelProviders",
                        principalColumn: "ProviderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModelUserConfigurations_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModelUserConfigurations_ProviderUserConfigurations_ProviderUserConfigurationConfigurationId",
                        column: x => x.ProviderUserConfigurationConfigurationId,
                        principalTable: "ProviderUserConfigurations",
                        principalColumn: "ConfigurationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModelUserConfigurations_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProviderCustomSettingEntries",
                columns: table => new
                {
                    EntryId = table.Column<string>(type: "TEXT", nullable: false),
                    ConfigurationId = table.Column<string>(type: "TEXT", nullable: false),
                    SettingKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderCustomSettingEntries", x => x.EntryId);
                    table.ForeignKey(
                        name: "FK_ProviderCustomSettingEntries_ProviderUserConfigurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "ProviderUserConfigurations",
                        principalColumn: "ConfigurationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceMetrics",
                columns: table => new
                {
                    MetricId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ExecutionId = table.Column<string>(type: "TEXT", nullable: false),
                    MetricTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MetricType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MetricName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MetricValue = table.Column<double>(type: "REAL", nullable: false),
                    MetricUnit = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AggregationPeriod = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceMetrics", x => x.MetricId);
                    table.ForeignKey(
                        name: "FK_PerformanceMetrics_TaskExecutionHistories_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "TaskExecutionHistories",
                        principalColumn: "ExecutionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceMetrics_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTemplateSteps",
                columns: table => new
                {
                    StepId = table.Column<string>(type: "TEXT", nullable: false),
                    TemplateId = table.Column<string>(type: "TEXT", nullable: false),
                    StepOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    StepType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StepName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StepDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RequiredCapability = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ParametersId = table.Column<string>(type: "TEXT", nullable: false),
                    DependsOnSteps = table.Column<string>(type: "TEXT", nullable: false),
                    IsOptional = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTemplateSteps", x => x.StepId);
                    table.ForeignKey(
                        name: "FK_WorkflowTemplateSteps_StepParameters_ParametersId",
                        column: x => x.ParametersId,
                        principalTable: "StepParameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTemplateSteps_WorkflowTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "WorkflowTemplates",
                        principalColumn: "TemplateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MetricContextEntries",
                columns: table => new
                {
                    EntryId = table.Column<string>(type: "TEXT", nullable: false),
                    MetricId = table.Column<string>(type: "TEXT", nullable: false),
                    ContextKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ContextValue = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricContextEntries", x => x.EntryId);
                    table.ForeignKey(
                        name: "FK_MetricContextEntries_PerformanceMetrics_MetricId",
                        column: x => x.MetricId,
                        principalTable: "PerformanceMetrics",
                        principalColumn: "MetricId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MetricTagEntries",
                columns: table => new
                {
                    EntryId = table.Column<string>(type: "TEXT", nullable: false),
                    MetricId = table.Column<string>(type: "TEXT", nullable: false),
                    TagKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TagValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricTagEntries", x => x.EntryId);
                    table.ForeignKey(
                        name: "FK_MetricTagEntries_PerformanceMetrics_MetricId",
                        column: x => x.MetricId,
                        principalTable: "PerformanceMetrics",
                        principalColumn: "MetricId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MCPConfigurations_CreatedBy_IsEnabled_Type",
                table: "MCPConfigurations",
                columns: new[] { "CreatedBy", "IsEnabled", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_MCPConfigurations_CreatedBy_Name",
                table: "MCPConfigurations",
                columns: new[] { "CreatedBy", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_MCPConfiguration_IsEnabled",
                table: "MCPConfigurations",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_MCPConfiguration_Name",
                table: "MCPConfigurations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_MCPConfiguration_Type",
                table: "MCPConfigurations",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_MCPConfiguration_Type_Enabled",
                table: "MCPConfigurations",
                columns: new[] { "Type", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricContextEntries_MetricId_ContextKey",
                table: "MetricContextEntries",
                columns: new[] { "MetricId", "ContextKey" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricTagEntries_MetricId_TagKey",
                table: "MetricTagEntries",
                columns: new[] { "MetricId", "TagKey" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelParameterEntries_ConfigurationId_Key",
                table: "ModelParameterEntries",
                columns: new[] { "ConfigurationId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_ModelProvider_Name",
                table: "ModelProviders",
                column: "ProviderName");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_ModelProvider_Prebuilt",
                table: "ModelProviders",
                column: "IsPrebuilt");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_ModelProvider_Type",
                table: "ModelProviders",
                column: "ProviderTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_Model_Group",
                table: "Models",
                column: "ModelGroup");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_Model_LatestVersion",
                table: "Models",
                column: "IsLatestVersion");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_Model_Name",
                table: "Models",
                column: "ModelName");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_Model_Prebuilt",
                table: "Models",
                column: "IsPrebuilt");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_Model_Provider_Name",
                table: "Models",
                columns: new[] { "ProviderId", "ModelName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelUserConfigurations_ModelId",
                table: "ModelUserConfigurations",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelUserConfigurations_ProviderId",
                table: "ModelUserConfigurations",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelUserConfigurations_ProviderUserConfigurationConfigurationId",
                table: "ModelUserConfigurations",
                column: "ProviderUserConfigurationConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelUserConfigurations_UserId_ModelId_ProviderId",
                table: "ModelUserConfigurations",
                columns: new[] { "UserId", "ModelId", "ProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_ExecutionId",
                table: "PerformanceMetrics",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_MetricType_MetricName_MetricTimestamp",
                table: "PerformanceMetrics",
                columns: new[] { "MetricType", "MetricName", "MetricTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_MetricType_MetricTimestamp",
                table: "PerformanceMetrics",
                columns: new[] { "MetricType", "MetricTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_UserId",
                table: "PerformanceMetrics",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCustomSettingEntries_ConfigurationId_SettingKey",
                table: "ProviderCustomSettingEntries",
                columns: new[] { "ConfigurationId", "SettingKey" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderUserConfigurations_ProviderId",
                table: "ProviderUserConfigurations",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderUserConfigurations_UsageQuotaId",
                table: "ProviderUserConfigurations",
                column: "UsageQuotaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderUserConfigurations_UserId_ProviderId",
                table: "ProviderUserConfigurations",
                columns: new[] { "UserId", "ProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityThresholdEntries_ConfigurationId_ThresholdName",
                table: "QualityThresholdEntries",
                columns: new[] { "ConfigurationId", "ThresholdName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StepParameterEntries_StepParametersId",
                table: "StepParameterEntries",
                column: "StepParametersId");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_TaskExecutionHistory_IsSuccessful",
                table: "TaskExecutionHistories",
                column: "IsSuccessful");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_TaskExecutionHistory_RequestType",
                table: "TaskExecutionHistories",
                column: "RequestType");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_TaskExecutionHistory_StartTime",
                table: "TaskExecutionHistories",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_TaskExecutionHistory_Status",
                table: "TaskExecutionHistories",
                column: "ExecutionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_TaskExecutionHistory_User_StartTime",
                table: "TaskExecutionHistories",
                columns: new[] { "UserId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_TaskExecutionHistory_UserId",
                table: "TaskExecutionHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionHistories_UserId_StartTime_ExecutionStatus",
                table: "TaskExecutionHistories",
                columns: new[] { "UserId", "StartTime", "ExecutionStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageQuotaCustomLimitEntries_UsageQuotaId",
                table: "UsageQuotaCustomLimitEntries",
                column: "UsageQuotaId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMetadataEntries_UserId_Key",
                table: "UserMetadataEntries",
                columns: new[] { "UserId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId_PreferenceCategory_PreferenceKey",
                table: "UserPreferences",
                columns: new[] { "UserId", "PreferenceCategory", "PreferenceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_CreatedTime",
                table: "UserProfiles",
                column: "CreatedTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_IsActive",
                table: "UserProfiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Username",
                table: "UserProfiles",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_WorkflowTemplate_Category",
                table: "WorkflowTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_WorkflowTemplate_Category_Public_System",
                table: "WorkflowTemplates",
                columns: new[] { "Category", "IsPublic", "IsSystemTemplate" });

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_WorkflowTemplate_IsPublic",
                table: "WorkflowTemplates",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_WorkflowTemplate_IsSystemTemplate",
                table: "WorkflowTemplates",
                column: "IsSystemTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_WorkflowTemplate_Name",
                table: "WorkflowTemplates",
                column: "TemplateName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_WorkflowTemplate_UserId",
                table: "WorkflowTemplates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplateSteps_ParametersId",
                table: "WorkflowTemplateSteps",
                column: "ParametersId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplateSteps_TemplateId_StepOrder",
                table: "WorkflowTemplateSteps",
                columns: new[] { "TemplateId", "StepOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiHeaderEntries");

            migrationBuilder.DropTable(
                name: "MCPConfigurations");

            migrationBuilder.DropTable(
                name: "MetricContextEntries");

            migrationBuilder.DropTable(
                name: "MetricTagEntries");

            migrationBuilder.DropTable(
                name: "ModelParameterEntries");

            migrationBuilder.DropTable(
                name: "ModelUserConfigurations");

            migrationBuilder.DropTable(
                name: "PricingSpecialEntries");

            migrationBuilder.DropTable(
                name: "ProviderCustomSettingEntries");

            migrationBuilder.DropTable(
                name: "QualityThresholdEntries");

            migrationBuilder.DropTable(
                name: "ResourceUtilizationEntries");

            migrationBuilder.DropTable(
                name: "StepExecutionTimeEntries");

            migrationBuilder.DropTable(
                name: "StepParameterEntries");

            migrationBuilder.DropTable(
                name: "UsageQuotaCustomLimitEntries");

            migrationBuilder.DropTable(
                name: "UserMetadataEntries");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "WorkflowMetadataEntries");

            migrationBuilder.DropTable(
                name: "WorkflowTemplateSteps");

            migrationBuilder.DropTable(
                name: "PerformanceMetrics");

            migrationBuilder.DropTable(
                name: "Models");

            migrationBuilder.DropTable(
                name: "ProviderUserConfigurations");

            migrationBuilder.DropTable(
                name: "StepParameters");

            migrationBuilder.DropTable(
                name: "WorkflowTemplates");

            migrationBuilder.DropTable(
                name: "TaskExecutionHistories");

            migrationBuilder.DropTable(
                name: "ModelProviders");

            migrationBuilder.DropTable(
                name: "UsageQuota");

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
