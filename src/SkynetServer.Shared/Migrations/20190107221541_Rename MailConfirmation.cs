using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SkynetServer.Migrations
{
    public partial class RenameMailConfirmation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailAddressConfirmation");

            migrationBuilder.CreateTable(
                name: "MailConfirmations",
                columns: table => new
                {
                    MailAddress = table.Column<string>(nullable: false),
                    Token = table.Column<string>(nullable: false),
                    CreationTime = table.Column<DateTime>(nullable: false),
                    ConfirmationTime = table.Column<DateTime>(nullable: false),
                    AccountId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailConfirmations", x => x.MailAddress);
                    table.UniqueConstraint("AK_MailConfirmations_Token", x => x.Token);
                    table.ForeignKey(
                        name: "FK_MailConfirmations_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailConfirmations_AccountId",
                table: "MailConfirmations",
                column: "AccountId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailConfirmations");

            migrationBuilder.CreateTable(
                name: "MailAddressConfirmation",
                columns: table => new
                {
                    MailAddress = table.Column<string>(nullable: false),
                    AccountId = table.Column<long>(nullable: false),
                    ConfirmationTime = table.Column<DateTime>(nullable: false),
                    CreationTime = table.Column<DateTime>(nullable: false),
                    Token = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailAddressConfirmation", x => x.MailAddress);
                    table.UniqueConstraint("AK_MailAddressConfirmation_Token", x => x.Token);
                    table.ForeignKey(
                        name: "FK_MailAddressConfirmation_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailAddressConfirmation_AccountId",
                table: "MailAddressConfirmation",
                column: "AccountId");
        }
    }
}
