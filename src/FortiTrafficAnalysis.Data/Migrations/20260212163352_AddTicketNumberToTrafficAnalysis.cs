using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FortiTrafficAnalysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketNumberToTrafficAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TicketNumber",
                table: "TrafficAnalysis",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficAnalysis_TicketNumber",
                table: "TrafficAnalysis",
                column: "TicketNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrafficAnalysis_TicketNumber",
                table: "TrafficAnalysis");

            migrationBuilder.DropColumn(
                name: "TicketNumber",
                table: "TrafficAnalysis");
        }
    }
}
