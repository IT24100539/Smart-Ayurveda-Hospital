using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowExecutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ObjectiveText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PlanJson = table.Column<string>(type: "jsonb", nullable: false),
                    CompletedStepsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ToolResultsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ValidationResultsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ErrorsJson = table.Column<string>(type: "jsonb", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FinalOutcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RelatedEntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_executions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_executions_agent_name",
                table: "workflow_executions",
                column: "AgentName");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_executions_approval_status",
                table: "workflow_executions",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_executions_created_at",
                table: "workflow_executions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_executions_related_entity",
                table: "workflow_executions",
                columns: new[] { "RelatedEntityType", "RelatedEntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_executions");
        }
    }
}
