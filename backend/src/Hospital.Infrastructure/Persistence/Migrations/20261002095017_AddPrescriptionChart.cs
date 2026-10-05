using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionChart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                table: "PrescriptionItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Prescriptions",
                table: "Prescriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PrescriptionItems",
                table: "PrescriptionItems");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "Diagnosis",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "Directives",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "IsRefillable",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "PrescriptionDate",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "HerbFormulationName",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "SpecialInstructions",
                table: "PrescriptionItems");

            migrationBuilder.RenameTable(
                name: "Prescriptions",
                newName: "prescriptions");

            migrationBuilder.RenameTable(
                name: "PrescriptionItems",
                newName: "prescription_items");

            migrationBuilder.RenameColumn(
                name: "RemainingRefills",
                table: "prescriptions",
                newName: "RevisionNumber");

            migrationBuilder.RenameColumn(
                name: "DurationDays",
                table: "prescription_items",
                newName: "SortOrder");

            migrationBuilder.AlterColumn<string>(
                name: "DoctorName",
                table: "prescriptions",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                table: "prescriptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "prescriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IssuedAt",
                table: "prescriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RevisesPrescriptionId",
                table: "prescriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RootPrescriptionId",
                table: "prescriptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "prescriptions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SupersededAt",
                table: "prescriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersededByPrescriptionId",
                table: "prescriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Frequency",
                table: "prescription_items",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Dosage",
                table: "prescription_items",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "prescription_items",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "prescription_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "prescription_items",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            // The previous prescription tables were an unused stub with no visit.
            // Those rows cannot satisfy the appointment foreign key, so they are removed
            // before the new constraints are applied. Column defaults exist only for that step.
            migrationBuilder.Sql(
                """
                DELETE FROM prescription_items;
                DELETE FROM prescriptions;
                ALTER TABLE prescriptions ALTER COLUMN "AppointmentId" DROP DEFAULT;
                ALTER TABLE prescriptions ALTER COLUMN "RootPrescriptionId" DROP DEFAULT;
                ALTER TABLE prescriptions ALTER COLUMN "Status" DROP DEFAULT;
                ALTER TABLE prescription_items ALTER COLUMN "Duration" DROP DEFAULT;
                ALTER TABLE prescription_items ALTER COLUMN "Instructions" DROP DEFAULT;
                ALTER TABLE prescription_items ALTER COLUMN "Name" DROP DEFAULT;
                """);

            migrationBuilder.AddPrimaryKey(
                name: "PK_prescriptions",
                table: "prescriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_prescription_items",
                table: "prescription_items",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "prescription_revisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousPrescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisedPrescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    RevisedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescription_revisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prescription_revisions_prescriptions_PreviousPrescriptionId",
                        column: x => x.PreviousPrescriptionId,
                        principalTable: "prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prescription_revisions_prescriptions_RevisedPrescriptionId",
                        column: x => x.RevisedPrescriptionId,
                        principalTable: "prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prescription_revisions_users_RevisedByUserId",
                        column: x => x.RevisedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_DoctorUserId",
                table: "prescriptions",
                column: "DoctorUserId");

            migrationBuilder.CreateIndex(
                name: "ix_prescriptions_patient_id",
                table: "prescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_RevisesPrescriptionId",
                table: "prescriptions",
                column: "RevisesPrescriptionId");

            migrationBuilder.CreateIndex(
                name: "ix_prescriptions_root",
                table: "prescriptions",
                column: "RootPrescriptionId");

            migrationBuilder.CreateIndex(
                name: "ix_prescriptions_status",
                table: "prescriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_SupersededByPrescriptionId",
                table: "prescriptions",
                column: "SupersededByPrescriptionId");

            migrationBuilder.CreateIndex(
                name: "ux_prescriptions_appointment_open",
                table: "prescriptions",
                column: "AppointmentId",
                unique: true,
                filter: "\"Status\" IN ('Draft', 'Issued')");

            migrationBuilder.CreateIndex(
                name: "ix_prescription_items_order",
                table: "prescription_items",
                columns: new[] { "PrescriptionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "ix_prescription_revisions_previous",
                table: "prescription_revisions",
                column: "PreviousPrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_prescription_revisions_RevisedByUserId",
                table: "prescription_revisions",
                column: "RevisedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_prescription_revisions_revised",
                table: "prescription_revisions",
                column: "RevisedPrescriptionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_prescription_items_prescriptions_PrescriptionId",
                table: "prescription_items",
                column: "PrescriptionId",
                principalTable: "prescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_prescriptions_appointments_AppointmentId",
                table: "prescriptions",
                column: "AppointmentId",
                principalTable: "appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_prescriptions_patients_PatientId",
                table: "prescriptions",
                column: "PatientId",
                principalTable: "patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_prescriptions_prescriptions_RevisesPrescriptionId",
                table: "prescriptions",
                column: "RevisesPrescriptionId",
                principalTable: "prescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_prescriptions_prescriptions_SupersededByPrescriptionId",
                table: "prescriptions",
                column: "SupersededByPrescriptionId",
                principalTable: "prescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_prescriptions_users_DoctorUserId",
                table: "prescriptions",
                column: "DoctorUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prescription_items_prescriptions_PrescriptionId",
                table: "prescription_items");

            migrationBuilder.DropForeignKey(
                name: "FK_prescriptions_appointments_AppointmentId",
                table: "prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_prescriptions_patients_PatientId",
                table: "prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_prescriptions_prescriptions_RevisesPrescriptionId",
                table: "prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_prescriptions_prescriptions_SupersededByPrescriptionId",
                table: "prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_prescriptions_users_DoctorUserId",
                table: "prescriptions");

            migrationBuilder.DropTable(
                name: "prescription_revisions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_prescriptions",
                table: "prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_prescriptions_DoctorUserId",
                table: "prescriptions");

            migrationBuilder.DropIndex(
                name: "ix_prescriptions_patient_id",
                table: "prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_prescriptions_RevisesPrescriptionId",
                table: "prescriptions");

            migrationBuilder.DropIndex(
                name: "ix_prescriptions_root",
                table: "prescriptions");

            migrationBuilder.DropIndex(
                name: "ix_prescriptions_status",
                table: "prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_prescriptions_SupersededByPrescriptionId",
                table: "prescriptions");

            migrationBuilder.DropIndex(
                name: "ux_prescriptions_appointment_open",
                table: "prescriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_prescription_items",
                table: "prescription_items");

            migrationBuilder.DropIndex(
                name: "ix_prescription_items_order",
                table: "prescription_items");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "RevisesPrescriptionId",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "RootPrescriptionId",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "SupersededAt",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "SupersededByPrescriptionId",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "prescription_items");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "prescription_items");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "prescription_items");

            migrationBuilder.RenameTable(
                name: "prescriptions",
                newName: "Prescriptions");

            migrationBuilder.RenameTable(
                name: "prescription_items",
                newName: "PrescriptionItems");

            migrationBuilder.RenameColumn(
                name: "RevisionNumber",
                table: "Prescriptions",
                newName: "RemainingRefills");

            migrationBuilder.RenameColumn(
                name: "SortOrder",
                table: "PrescriptionItems",
                newName: "DurationDays");

            migrationBuilder.AlterColumn<string>(
                name: "DoctorName",
                table: "Prescriptions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.AddColumn<string>(
                name: "Diagnosis",
                table: "Prescriptions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Directives",
                table: "Prescriptions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsRefillable",
                table: "Prescriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PrescriptionDate",
                table: "Prescriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "Frequency",
                table: "PrescriptionItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Dosage",
                table: "PrescriptionItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AddColumn<string>(
                name: "HerbFormulationName",
                table: "PrescriptionItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpecialInstructions",
                table: "PrescriptionItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Prescriptions",
                table: "Prescriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PrescriptionItems",
                table: "PrescriptionItems",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                table: "PrescriptionItems",
                column: "PrescriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                table: "PrescriptionItems",
                column: "PrescriptionId",
                principalTable: "Prescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
