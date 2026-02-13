using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FortiTrafficAnalysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigFileToFortiGate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConfigFile",
                table: "FortiGates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfigUploadedDate",
                table: "FortiGates",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfigFile",
                table: "FortiGates");

            migrationBuilder.DropColumn(
                name: "ConfigUploadedDate",
                table: "FortiGates");
        }
    }
}
