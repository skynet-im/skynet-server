using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SkynetServer.Migrations
{
    public partial class ModelImplementation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ContentPacket",
                table: "Messages",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "ContentPacketId",
                table: "Messages",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "ContentPacketVersion",
                table: "Messages",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "MessageFlags",
                table: "Messages",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<long>(
                name: "SenderId",
                table: "Messages",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<byte>(
                name: "ChannelType",
                table: "Channels",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<long>(
                name: "OtherId",
                table: "Channels",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OwnerId",
                table: "Channels",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "BlockedAccount",
                columns: table => new
                {
                    AccountId = table.Column<long>(nullable: false),
                    OwnerId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedAccount", x => new { x.OwnerId, x.AccountId });
                    table.ForeignKey(
                        name: "FK_BlockedAccount_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlockedAccount_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlockedConversation",
                columns: table => new
                {
                    ChannelId = table.Column<long>(nullable: false),
                    OwnerId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedConversation", x => new { x.OwnerId, x.ChannelId });
                    table.ForeignKey(
                        name: "FK_BlockedConversation_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "ChannelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlockedConversation_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMember",
                columns: table => new
                {
                    Flags = table.Column<byte>(nullable: false),
                    ChannelId = table.Column<long>(nullable: false),
                    AccountId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMember", x => new { x.ChannelId, x.AccountId });
                    table.ForeignKey(
                        name: "FK_GroupMember_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMember_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "ChannelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MailAddressConfirmation",
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
                    table.PrimaryKey("PK_MailAddressConfirmation", x => x.MailAddress);
                    table.UniqueConstraint("AK_MailAddressConfirmation_Token", x => x.Token);
                    table.ForeignKey(
                        name: "FK_MailAddressConfirmation_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageDependencies",
                columns: table => new
                {
                    AccountId = table.Column<long>(nullable: false),
                    ChannelId = table.Column<long>(nullable: false),
                    MessageId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageDependencies", x => new { x.AccountId, x.ChannelId, x.MessageId });
                    table.ForeignKey(
                        name: "FK_MessageDependencies_Messages_ChannelId_MessageId",
                        columns: x => new { x.ChannelId, x.MessageId },
                        principalTable: "Messages",
                        principalColumns: new[] { "ChannelId", "MessageId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<long>(nullable: false),
                    CreationTime = table.Column<DateTime>(nullable: false),
                    ApplicationIdentifier = table.Column<string>(nullable: true),
                    LastConnected = table.Column<DateTime>(nullable: false),
                    LastVersionCode = table.Column<int>(nullable: false),
                    AccountId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => new { x.AccountId, x.SessionId });
                    table.ForeignKey(
                        name: "FK_Sessions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_OtherId",
                table: "Channels",
                column: "OtherId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_OwnerId",
                table: "Channels",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedAccount_AccountId",
                table: "BlockedAccount",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedConversation_ChannelId",
                table: "BlockedConversation",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMember_AccountId",
                table: "GroupMember",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MailAddressConfirmation_AccountId",
                table: "MailAddressConfirmation",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageDependencies_ChannelId_MessageId",
                table: "MessageDependencies",
                columns: new[] { "ChannelId", "MessageId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Channels_Accounts_OtherId",
                table: "Channels",
                column: "OtherId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Channels_Accounts_OwnerId",
                table: "Channels",
                column: "OwnerId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Accounts_SenderId",
                table: "Messages",
                column: "SenderId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channels_Accounts_OtherId",
                table: "Channels");

            migrationBuilder.DropForeignKey(
                name: "FK_Channels_Accounts_OwnerId",
                table: "Channels");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Accounts_SenderId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "BlockedAccount");

            migrationBuilder.DropTable(
                name: "BlockedConversation");

            migrationBuilder.DropTable(
                name: "GroupMember");

            migrationBuilder.DropTable(
                name: "MailAddressConfirmation");

            migrationBuilder.DropTable(
                name: "MessageDependencies");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Messages_SenderId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Channels_OtherId",
                table: "Channels");

            migrationBuilder.DropIndex(
                name: "IX_Channels_OwnerId",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "ContentPacket",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ContentPacketId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ContentPacketVersion",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MessageFlags",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SenderId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ChannelType",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "OtherId",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Channels");
        }
    }
}
