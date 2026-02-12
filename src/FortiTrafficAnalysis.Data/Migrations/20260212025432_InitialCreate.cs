using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FortiTrafficAnalysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppGroups",
                columns: table => new
                {
                    AppGroupID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppGroupName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppGroups", x => x.AppGroupID);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerID);
                });

            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    AppAccessID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserUPN = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AppGroupID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppUserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AppUserEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.AppAccessID);
                    table.ForeignKey(
                        name: "FK_AppUsers_AppGroups_AppGroupID",
                        column: x => x.AppGroupID,
                        principalTable: "AppGroups",
                        principalColumn: "AppGroupID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FTAServices",
                columns: table => new
                {
                    FTAID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServiceEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServiceStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FTAServices", x => x.FTAID);
                    table.ForeignKey(
                        name: "FK_FTAServices_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FortiGates",
                columns: table => new
                {
                    FGID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FTAID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FGHostname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FGHost = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FGSerial = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FGvDOM = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FGapiKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FGStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FortiGates", x => x.FGID);
                    table.ForeignKey(
                        name: "FK_FortiGates_FTAServices_FTAID",
                        column: x => x.FTAID,
                        principalTable: "FTAServices",
                        principalColumn: "FTAID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrafficLogs",
                columns: table => new
                {
                    LogTempID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FGID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LogTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DestinationIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourcePort = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DestinationPort = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PolicyAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RawLogLine = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficLogs", x => x.LogTempID);
                    table.ForeignKey(
                        name: "FK_TrafficLogs_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrafficLogs_FortiGates_FGID",
                        column: x => x.FGID,
                        principalTable: "FortiGates",
                        principalColumn: "FGID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "AppGroups",
                columns: new[] { "AppGroupID", "AppGroupName" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Users" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Admins" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppGroups_AppGroupName",
                table: "AppGroups",
                column: "AppGroupName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_AppGroupID",
                table: "AppUsers",
                column: "AppGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_UserUPN",
                table: "AppUsers",
                column: "UserUPN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerName",
                table: "Customers",
                column: "CustomerName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FortiGates_FGSerial",
                table: "FortiGates",
                column: "FGSerial",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FortiGates_FTAID",
                table: "FortiGates",
                column: "FTAID");

            migrationBuilder.CreateIndex(
                name: "IX_FTAServices_CustomerID",
                table: "FTAServices",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_FTAServices_JobID",
                table: "FTAServices",
                column: "JobID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrafficLogs_CustomerID",
                table: "TrafficLogs",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficLogs_DestinationIP",
                table: "TrafficLogs",
                column: "DestinationIP");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficLogs_FGID",
                table: "TrafficLogs",
                column: "FGID");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "TrafficLogs");

            migrationBuilder.DropTable(
                name: "AppGroups");

            migrationBuilder.DropTable(
                name: "FortiGates");

            migrationBuilder.DropTable(
                name: "FTAServices");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
