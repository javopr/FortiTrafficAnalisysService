using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FortiTrafficAnalysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAIConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIConversations",
                columns: table => new
                {
                    ConversationID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrafficAnalysisID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserQuestion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AIResponse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUPN = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIConversations", x => x.ConversationID);
                    table.ForeignKey(
                        name: "FK_AIConversations_TrafficAnalysis_TrafficAnalysisID",
                        column: x => x.TrafficAnalysisID,
                        principalTable: "TrafficAnalysis",
                        principalColumn: "TrafficAnalysisID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_CreatedByUPN",
                table: "AIConversations",
                column: "CreatedByUPN");

            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_CreatedDate",
                table: "AIConversations",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_TrafficAnalysisID",
                table: "AIConversations",
                column: "TrafficAnalysisID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIConversations");
        }
    }
}
