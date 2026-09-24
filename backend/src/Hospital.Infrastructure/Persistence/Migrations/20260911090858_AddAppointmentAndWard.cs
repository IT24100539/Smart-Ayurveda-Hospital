using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentAndWard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_staff_users_DoctorId",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_DoctorId_ScheduledAt",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_PatientId",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "EndsAt",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "appointments");

            migrationBuilder.RenameColumn(
                name: "DoctorId",
                table: "appointments",
                newName: "TreatmentId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "appointments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DecidedAt",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DecidedBy",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RequestedDate",
                table: "appointments",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "RequestedTimeSlot",
                table: "appointments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduleId",
                table: "appointments",
                type: "uuid",
                nullable: true);

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

            migrationBuilder.CreateTable(
                name: "wards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NameSinhala = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Gender = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TotalCapacity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "beds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WardId = table.Column<Guid>(type: "uuid", nullable: false),
                    BedLabel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsOccupied = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_beds_wards_WardId",
                        column: x => x.WardId,
                        principalTable: "wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    WardId = table.Column<Guid>(type: "uuid", nullable: true),
                    BedId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PreferredDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedByAgent = table.Column<bool>(type: "boolean", nullable: false),
                    DecidedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admission_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_admission_requests_beds_BedId",
                        column: x => x.BedId,
                        principalTable: "beds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admission_requests_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admission_requests_users_DecidedBy",
                        column: x => x.DecidedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admission_requests_wards_WardId",
                        column: x => x.WardId,
                        principalTable: "wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_DecidedBy",
                table: "appointments",
                column: "DecidedBy");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_ScheduleId",
                table: "appointments",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_treatment_requested_date",
                table: "appointments",
                columns: new[] { "TreatmentId", "RequestedDate" });

            migrationBuilder.CreateIndex(
                name: "ux_appointments_patient_treatment_slot_active",
                table: "appointments",
                columns: new[] { "PatientId", "TreatmentId", "RequestedDate", "RequestedTimeSlot" },
                unique: true,
                filter: "\"Status\" <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_admission_requests_BedId",
                table: "admission_requests",
                column: "BedId");

            migrationBuilder.CreateIndex(
                name: "IX_admission_requests_DecidedBy",
                table: "admission_requests",
                column: "DecidedBy");

            migrationBuilder.CreateIndex(
                name: "IX_admission_requests_PatientId",
                table: "admission_requests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_admission_requests_WardId",
                table: "admission_requests",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_beds_WardId_BedLabel",
                table: "beds",
                columns: new[] { "WardId", "BedLabel" },
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_treatments_TreatmentId",
                table: "appointments",
                column: "TreatmentId",
                principalTable: "treatments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_users_DecidedBy",
                table: "appointments",
                column: "DecidedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_treatment_schedules_ScheduleId",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_treatments_TreatmentId",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_users_DecidedBy",
                table: "appointments");

            migrationBuilder.DropTable(
                name: "admission_requests");

            migrationBuilder.DropTable(
                name: "treatment_schedules");

            migrationBuilder.DropTable(
                name: "beds");

            migrationBuilder.DropTable(
                name: "wards");

            migrationBuilder.DropIndex(
                name: "IX_appointments_DecidedBy",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_ScheduleId",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "ix_appointments_treatment_requested_date",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "ux_appointments_patient_treatment_slot_active",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "DecidedAt",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "DecidedBy",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "RequestedDate",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "RequestedTimeSlot",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "appointments");

            migrationBuilder.RenameColumn(
                name: "TreatmentId",
                table: "appointments",
                newName: "DoctorId");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "appointments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "appointments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndsAt",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "appointments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledAt",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_appointments_DoctorId_ScheduledAt",
                table: "appointments",
                columns: new[] { "DoctorId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_PatientId",
                table: "appointments",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_staff_users_DoctorId",
                table: "appointments",
                column: "DoctorId",
                principalTable: "staff_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
