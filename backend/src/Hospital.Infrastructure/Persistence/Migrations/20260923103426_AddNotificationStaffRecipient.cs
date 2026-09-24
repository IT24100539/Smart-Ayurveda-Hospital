using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationStaffRecipient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StaffUserId",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_StaffUserId",
                table: "notifications",
                column: "StaffUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_staff_users_StaffUserId",
                table: "notifications",
                column: "StaffUserId",
                principalTable: "staff_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notifications_staff_users_StaffUserId",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_StaffUserId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "StaffUserId",
                table: "notifications");
        }
    }
}
