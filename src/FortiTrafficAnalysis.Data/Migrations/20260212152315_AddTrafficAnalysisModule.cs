using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FortiTrafficAnalysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrafficAnalysisModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrafficLogs_Customers_CustomerID",
                table: "TrafficLogs");

            migrationBuilder.DropIndex(
                name: "IX_TrafficLogs_DestinationIP",
                table: "TrafficLogs");

            migrationBuilder.DropIndex(
                name: "IX_TrafficLogs_LogTimestamp",
                table: "TrafficLogs");

            migrationBuilder.DropIndex(
                name: "IX_TrafficLogs_PolicyAction",
                table: "TrafficLogs");

            migrationBuilder.DropIndex(
                name: "IX_TrafficLogs_SourceIP",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "DestinationIP",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "DestinationPort",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "PolicyAction",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "Protocol",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "SourceIP",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "SourcePort",
                table: "TrafficLogs");

            migrationBuilder.RenameColumn(
                name: "LogTimestamp",
                table: "TrafficLogs",
                newName: "ImportedDate");

            migrationBuilder.RenameColumn(
                name: "CustomerID",
                table: "TrafficLogs",
                newName: "TrafficAnalysisID");

            migrationBuilder.RenameColumn(
                name: "LogTempID",
                table: "TrafficLogs",
                newName: "TrafficLogID");

            migrationBuilder.RenameIndex(
                name: "IX_TrafficLogs_CustomerID",
                table: "TrafficLogs",
                newName: "IX_TrafficLogs_TrafficAnalysisID");

            migrationBuilder.AlterColumn<string>(
                name: "RawLogLine",
                table: "TrafficLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "TrafficLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DstIP",
                table: "TrafficLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DstInt",
                table: "TrafficLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DstPort",
                table: "TrafficLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "TrafficLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LogDate",
                table: "TrafficLogs",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogId",
                table: "TrafficLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogTime",
                table: "TrafficLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PolicyId",
                table: "TrafficLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PolicyName",
                table: "TrafficLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Proto",
                table: "TrafficLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RcvdByte",
                table: "TrafficLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SentByte",
                table: "TrafficLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Service",
                table: "TrafficLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "TrafficLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SrcIP",
                table: "TrafficLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SrcInt",
                table: "TrafficLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SrcPort",
                table: "TrafficLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrafficAnalysis",
                columns: table => new
                {
                    TrafficAnalysisID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FGID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FTAID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUPN = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficAnalysis", x => x.TrafficAnalysisID);
                    table.ForeignKey(
                        name: "FK_TrafficAnalysis_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrafficAnalysis_FTAServices_FTAID",
                        column: x => x.FTAID,
                        principalTable: "FTAServices",
                        principalColumn: "FTAID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrafficAnalysis_FortiGates_FGID",
                        column: x => x.FGID,
                        principalTable: "FortiGates",
                        principalColumn: "FGID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrafficAnalysisRecommendations",
                columns: table => new
                {
                    TrafficAnalysisRecommendationID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrafficAnalysisID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecommendationText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalysisDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUPN = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficAnalysisRecommendations", x => x.TrafficAnalysisRecommendationID);
                    table.ForeignKey(
                        name: "FK_TrafficAnalysisRecommendations_TrafficAnalysis_TrafficAnalysisID",
                        column: x => x.TrafficAnalysisID,
                        principalTable: "TrafficAnalysis",
                        principalColumn: "TrafficAnalysisID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrafficLogs_Action",
                table: "TrafficLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficLogs_DstIP",
                table: "TrafficLogs",
                column: "DstIP");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficLogs_LogDate",
                table: "TrafficLogs",
                column: "LogDate");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficLogs_SrcIP",
                table: "TrafficLogs",
                column: "SrcIP");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficAnalysis_CreatedByUPN",
                table: "TrafficAnalysis",
                column: "CreatedByUPN");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficAnalysis_CreatedDate",
                table: "TrafficAnalysis",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficAnalysis_CustomerID",
                table: "TrafficAnalysis",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficAnalysis_FGID",
                table: "TrafficAnalysis",
                column: "FGID");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficAnalysis_FTAID",
                table: "TrafficAnalysis",
                column: "FTAID");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficAnalysis_Status",
                table: "TrafficAnalysis",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficAnalysisRecommendations_CreatedDate",
                table: "TrafficAnalysisRecommendations",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficAnalysisRecommendations_TrafficAnalysisID",
                table: "TrafficAnalysisRecommendations",
                column: "TrafficAnalysisID");

            migrationBuilder.AddForeignKey(
                name: "FK_TrafficLogs_TrafficAnalysis_TrafficAnalysisID",
                table: "TrafficLogs",
                column: "TrafficAnalysisID",
                principalTable: "TrafficAnalysis",
                principalColumn: "TrafficAnalysisID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrafficLogs_TrafficAnalysis_TrafficAnalysisID",
                table: "TrafficLogs");

            migrationBuilder.DropTable(
                name: "TrafficAnalysisRecommendations");

            migrationBuilder.DropTable(
                name: "TrafficAnalysis");

            migrationBuilder.DropIndex(
                name: "IX_TrafficLogs_Action",
                table: "TrafficLogs");

            migrationBuilder.DropIndex(
                name: "IX_TrafficLogs_DstIP",
                table: "TrafficLogs");

            migrationBuilder.DropIndex(
                name: "IX_TrafficLogs_LogDate",
                table: "TrafficLogs");

            migrationBuilder.DropIndex(
                name: "IX_TrafficLogs_SrcIP",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "DstIP",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "DstInt",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "DstPort",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "LogDate",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "LogId",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "LogTime",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "PolicyId",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "PolicyName",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "Proto",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "RcvdByte",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "SentByte",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "Service",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "SrcIP",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "SrcInt",
                table: "TrafficLogs");

            migrationBuilder.DropColumn(
                name: "SrcPort",
                table: "TrafficLogs");

            migrationBuilder.RenameColumn(
                name: "TrafficAnalysisID",
                table: "TrafficLogs",
                newName: "CustomerID");

            migrationBuilder.RenameColumn(
                name: "ImportedDate",
                table: "TrafficLogs",
                newName: "LogTimestamp");

            migrationBuilder.RenameColumn(
                name: "TrafficLogID",
                table: "TrafficLogs",
                newName: "LogTempID");

            migrationBuilder.RenameIndex(
                name: "IX_TrafficLogs_TrafficAnalysisID",
                table: "TrafficLogs",
                newName: "IX_TrafficLogs_CustomerID");

            migrationBuilder.AlterColumn<string>(
                name: "RawLogLine",
                table: "TrafficLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationIP",
                table: "TrafficLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DestinationPort",
                table: "TrafficLogs",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PolicyAction",
                table: "TrafficLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Protocol",
                table: "TrafficLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceIP",
                table: "TrafficLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourcePort",
                table: "TrafficLogs",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficLogs_DestinationIP",
                table: "TrafficLogs",
                column: "DestinationIP");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficLogs_LogTimestamp",
                table: "TrafficLogs",
                column: "LogTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficLogs_PolicyAction",
                table: "TrafficLogs",
                column: "PolicyAction");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficLogs_SourceIP",
                table: "TrafficLogs",
                column: "SourceIP");

            migrationBuilder.AddForeignKey(
                name: "FK_TrafficLogs_Customers_CustomerID",
                table: "TrafficLogs",
                column: "CustomerID",
                principalTable: "Customers",
                principalColumn: "CustomerID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
