using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeForeignKeyIdSuffix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_admission_requests_users_DecidedBy",
                table: "admission_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_users_DecidedBy",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_complaints_staff_users_AssignedTo",
                table: "complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_feedbacks_staff_users_ModeratedBy",
                table: "feedbacks");

            migrationBuilder.RenameColumn(
                name: "ModeratedBy",
                table: "feedbacks",
                newName: "ModeratedById");

            migrationBuilder.RenameIndex(
                name: "IX_feedbacks_ModeratedBy",
                table: "feedbacks",
                newName: "IX_feedbacks_ModeratedById");

            migrationBuilder.RenameColumn(
                name: "AssignedTo",
                table: "complaints",
                newName: "AssignedToId");

            migrationBuilder.RenameIndex(
                name: "IX_complaints_AssignedTo",
                table: "complaints",
                newName: "IX_complaints_AssignedToId");

            migrationBuilder.RenameColumn(
                name: "DecidedBy",
                table: "appointments",
                newName: "DecidedById");

            migrationBuilder.RenameIndex(
                name: "IX_appointments_DecidedBy",
                table: "appointments",
                newName: "IX_appointments_DecidedById");

            migrationBuilder.RenameColumn(
                name: "DecidedBy",
                table: "admission_requests",
                newName: "DecidedById");

            migrationBuilder.RenameIndex(
                name: "IX_admission_requests_DecidedBy",
                table: "admission_requests",
                newName: "IX_admission_requests_DecidedById");

            migrationBuilder.AddForeignKey(
                name: "FK_admission_requests_users_DecidedById",
                table: "admission_requests",
                column: "DecidedById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_users_DecidedById",
                table: "appointments",
                column: "DecidedById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_complaints_staff_users_AssignedToId",
                table: "complaints",
                column: "AssignedToId",
                principalTable: "staff_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_feedbacks_staff_users_ModeratedById",
                table: "feedbacks",
                column: "ModeratedById",
                principalTable: "staff_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_admission_requests_users_DecidedById",
                table: "admission_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_users_DecidedById",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_complaints_staff_users_AssignedToId",
                table: "complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_feedbacks_staff_users_ModeratedById",
                table: "feedbacks");

            migrationBuilder.RenameColumn(
                name: "ModeratedById",
                table: "feedbacks",
                newName: "ModeratedBy");

            migrationBuilder.RenameIndex(
                name: "IX_feedbacks_ModeratedById",
                table: "feedbacks",
                newName: "IX_feedbacks_ModeratedBy");

            migrationBuilder.RenameColumn(
                name: "AssignedToId",
                table: "complaints",
                newName: "AssignedTo");

            migrationBuilder.RenameIndex(
                name: "IX_complaints_AssignedToId",
                table: "complaints",
                newName: "IX_complaints_AssignedTo");

            migrationBuilder.RenameColumn(
                name: "DecidedById",
                table: "appointments",
                newName: "DecidedBy");

            migrationBuilder.RenameIndex(
                name: "IX_appointments_DecidedById",
                table: "appointments",
                newName: "IX_appointments_DecidedBy");

            migrationBuilder.RenameColumn(
                name: "DecidedById",
                table: "admission_requests",
                newName: "DecidedBy");

            migrationBuilder.RenameIndex(
                name: "IX_admission_requests_DecidedById",
                table: "admission_requests",
                newName: "IX_admission_requests_DecidedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_admission_requests_users_DecidedBy",
                table: "admission_requests",
                column: "DecidedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_users_DecidedBy",
                table: "appointments",
                column: "DecidedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_complaints_staff_users_AssignedTo",
                table: "complaints",
                column: "AssignedTo",
                principalTable: "staff_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_feedbacks_staff_users_ModeratedBy",
                table: "feedbacks",
                column: "ModeratedBy",
                principalTable: "staff_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
