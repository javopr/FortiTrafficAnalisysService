using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FortiTrafficAnalysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordHashToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "AppUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.InsertData(
                table: "AppUsers",
                columns: new[] { "AppAccessID", "AppGroupID", "AppUserEmail", "AppUserName", "PasswordHash", "UserUPN" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), new Guid("22222222-2222-2222-2222-222222222222"), "admin@fgtas.local", "System Administrator", "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=", "admin@fgtas.local" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AppUsers",
                keyColumn: "AppAccessID",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "AppUsers");
        }
    }
}
