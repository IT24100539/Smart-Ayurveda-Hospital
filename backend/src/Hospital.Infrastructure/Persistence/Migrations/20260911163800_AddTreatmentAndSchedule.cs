using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentAndSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionSinhala",
                table: "treatments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameSinhala",
                table: "treatments",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                ALTER TABLE treatments
                ALTER COLUMN "Category" TYPE character varying(32)
                USING (
                    CASE "Category"
                        WHEN 2 THEN 'Panchakarma'
                        WHEN 4 THEN 'Shirodhara'
                        WHEN 5 THEN 'Nasya'
                        ELSE 'General'
                    END);
                """);

            migrationBuilder.CreateTable(
                name: "therapists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Specialization = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_therapists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_therapists_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_treatment_schedules_ScheduleId",
                table: "appointments");

            migrationBuilder.DropTable(
                name: "treatment_schedules");

            migrationBuilder.CreateTable(
                name: "treatment_schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TherapistId = table.Column<Guid>(type: "uuid", nullable: true),
                    DayOfWeek = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    MaxSlotsPerDay = table.Column<int>(type: "integer", nullable: false),
                    TimeSlot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: ""),
                    MaxPatients = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_treatment_schedules_therapists_TherapistId",
                        column: x => x.TherapistId,
                        principalTable: "therapists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_treatment_schedules_treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_therapists_UserId",
                table: "therapists",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_schedules_treatment_day",
                table: "treatment_schedules",
                columns: new[] { "TreatmentId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "ix_schedules_unique_slot",
                table: "treatment_schedules",
                columns: new[] { "TreatmentId", "TherapistId", "DayOfWeek", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_treatment_schedules_TherapistId",
                table: "treatment_schedules",
                column: "TherapistId");

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_treatment_schedules_ScheduleId",
                table: "appointments",
                column: "ScheduleId",
                principalTable: "treatment_schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_treatment_schedules_ScheduleId",
                table: "appointments");

            migrationBuilder.DropTable(
                name: "treatment_schedules");

            migrationBuilder.CreateTable(
                name: "treatment_schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    TimeSlot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MaxPatients = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_treatment_schedules_treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_treatment_schedules_TreatmentId_DayOfWeek_TimeSlot",
                table: "treatment_schedules",
                columns: new[] { "TreatmentId", "DayOfWeek", "TimeSlot" });

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_treatment_schedules_ScheduleId",
                table: "appointments",
                column: "ScheduleId",
                principalTable: "treatment_schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropTable(
                name: "therapists");

            migrationBuilder.DropColumn(
                name: "DescriptionSinhala",
                table: "treatments");

            migrationBuilder.DropColumn(
                name: "NameSinhala",
                table: "treatments");

            migrationBuilder.Sql(
                """
                ALTER TABLE treatments
                ALTER COLUMN "Category" TYPE integer
                USING (
                    CASE "Category"
                        WHEN 'Panchakarma' THEN 2
                        WHEN 'Shirodhara' THEN 4
                        WHEN 'Nasya' THEN 5
                        ELSE 1
                    END);
                """);
        }
    }
}
