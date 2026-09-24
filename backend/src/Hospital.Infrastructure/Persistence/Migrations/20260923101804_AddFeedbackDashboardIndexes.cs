using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackDashboardIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_notifications_CreatedAt",
                table: "notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_feedbacks_Category",
                table: "feedbacks",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_feedbacks_CreatedAt",
                table: "feedbacks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_feedbacks_Sentiment",
                table: "feedbacks",
                column: "Sentiment");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_CreatedAt",
                table: "complaints",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_CreatedAt",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_feedbacks_Category",
                table: "feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_feedbacks_CreatedAt",
                table: "feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_feedbacks_Sentiment",
                table: "feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_complaints_CreatedAt",
                table: "complaints");
        }
    }
}
