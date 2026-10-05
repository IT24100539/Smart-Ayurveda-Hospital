using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "doctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Specialty = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Qualifications = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSample = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PhotoStorageKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PhotoContentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PhotoSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_DoctorId",
                table: "appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_doctors_IsActive",
                table: "doctors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_doctors_Name",
                table: "doctors",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_doctors_DoctorId",
                table: "appointments",
                column: "DoctorId",
                principalTable: "doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_doctors_DoctorId",
                table: "appointments");

            migrationBuilder.DropTable(
                name: "doctors");

            migrationBuilder.DropIndex(
                name: "IX_appointments_DoctorId",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "appointments");
        }
    }
}
