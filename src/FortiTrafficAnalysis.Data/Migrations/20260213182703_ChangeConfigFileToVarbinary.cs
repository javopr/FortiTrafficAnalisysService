using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FortiTrafficAnalysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeConfigFileToVarbinary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfigFile",
                table: "FortiGates");

            migrationBuilder.AddColumn<byte[]>(
                name: "ConfigFileCompressed",
                table: "FortiGates",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfigFileCompressed",
                table: "FortiGates");

            migrationBuilder.AddColumn<string>(
                name: "ConfigFile",
                table: "FortiGates",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
