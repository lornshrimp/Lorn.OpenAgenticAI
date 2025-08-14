using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lorn.OpenAgenticAI.Infrastructure.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModel_20250113 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_UserPreferences_UserId_PreferenceCategory_PreferenceKey",
                table: "UserPreferences",
                newName: "IX_SQLite_UserPreferences_User_Category_Key");

            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "UserProfiles",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "UserProfiles",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "UserProfiles",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "IsDefault",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActiveTime",
                table: "UserProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "MachineId",
                table: "UserProfiles",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "ValueType",
                table: "UserPreferences",
                type: "TEXT",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.CreateTable(
                name: "ExecutionStepRecords",
                columns: table => new
                {
                    StepRecordId = table.Column<string>(type: "TEXT", nullable: false),
                    ExecutionId = table.Column<string>(type: "TEXT", nullable: false),
                    StepId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StepOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    StepDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ActionName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Parameters = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StepStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExecutionTime = table.Column<long>(type: "INTEGER", nullable: false),
                    IsSuccessful = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputData = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ResourceUsage_CpuUsagePercent = table.Column<double>(type: "REAL", nullable: false),
                    ResourceUsage_MemoryUsageBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ResourceUsage_DiskIOBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ResourceUsage_NetworkIOBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ResourceUsage_CustomMetrics = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionStepRecords", x => x.StepRecordId);
                    table.CheckConstraint("CK_ExecutionStepRecords_ActionName_Length", "length(ActionName) <= 100");
                    table.CheckConstraint("CK_ExecutionStepRecords_AgentId_Length", "length(AgentId) <= 100");
                    table.CheckConstraint("CK_ExecutionStepRecords_ErrorMessage_Length", "ErrorMessage IS NULL OR length(ErrorMessage) <= 1000");
                    table.CheckConstraint("CK_ExecutionStepRecords_StepDescription_Length", "StepDescription IS NULL OR length(StepDescription) <= 500");
                    table.CheckConstraint("CK_ExecutionStepRecords_StepId_Length", "length(StepId) <= 100");
                    table.ForeignKey(
                        name: "FK_ExecutionStepRecords_TaskExecutionHistories_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "TaskExecutionHistories",
                        principalColumn: "ExecutionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSecurityLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EventDetails = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    DeviceInfo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MachineId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsSuccessful = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSecurityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSecurityLogs_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_ExecutionStepRecords_Agent_Action_Time",
                table: "ExecutionStepRecords",
                columns: new[] { "AgentId", "ActionName", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_ExecutionStepRecords_Execution_Order",
                table: "ExecutionStepRecords",
                columns: new[] { "ExecutionId", "StepOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SQLite_ExecutionStepRecords_Status_Time",
                table: "ExecutionStepRecords",
                columns: new[] { "StepStatus", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurityLogs_EventType_Timestamp",
                table: "UserSecurityLogs",
                columns: new[] { "EventType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurityLogs_Severity_Timestamp",
                table: "UserSecurityLogs",
                columns: new[] { "Severity", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurityLogs_UserId_Timestamp",
                table: "UserSecurityLogs",
                columns: new[] { "UserId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionStepRecords");

            migrationBuilder.DropTable(
                name: "UserSecurityLogs");

            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LastActiveTime",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MachineId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserProfiles");

            migrationBuilder.RenameIndex(
                name: "IX_SQLite_UserPreferences_User_Category_Key",
                table: "UserPreferences",
                newName: "IX_UserPreferences_UserId_PreferenceCategory_PreferenceKey");

            migrationBuilder.AlterColumn<string>(
                name: "ValueType",
                table: "UserPreferences",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
