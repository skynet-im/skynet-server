using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SkynetServer.Migrations
{
    public partial class AddKeyHash : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ApplicationIdentifier",
                table: "Sessions",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "KeyHash",
                table: "Accounts",
                nullable: false,
                defaultValue: new byte[] {  });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeyHash",
                table: "Accounts");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationIdentifier",
                table: "Sessions",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}
