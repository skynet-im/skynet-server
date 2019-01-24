using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SkynetServer.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountName = table.Column<string>(nullable: false),
                    KeyHash = table.Column<byte[]>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                    table.UniqueConstraint("AK_Accounts_AccountName", x => x.AccountName);
                });

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
                name: "Channels",
                columns: table => new
                {
                    ChannelId = table.Column<long>(nullable: false),
                    ChannelType = table.Column<byte>(nullable: false),
                    MessageIdCounter = table.Column<long>(nullable: false),
                    OwnerId = table.Column<long>(nullable: false),
                    OtherId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.ChannelId);
                    table.ForeignKey(
                        name: "FK_Channels_Accounts_OtherId",
                        column: x => x.OtherId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Channels_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<long>(nullable: false),
                    CreationTime = table.Column<DateTime>(nullable: false),
                    ApplicationIdentifier = table.Column<string>(nullable: false),
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
                name: "Messages",
                columns: table => new
                {
                    MessageId = table.Column<long>(nullable: false),
                    DispatchTime = table.Column<DateTime>(nullable: false),
                    MessageFlags = table.Column<byte>(nullable: false),
                    ContentPacketId = table.Column<byte>(nullable: false),
                    ContentPacketVersion = table.Column<byte>(nullable: false),
                    ContentPacket = table.Column<byte[]>(nullable: true),
                    ChannelId = table.Column<long>(nullable: false),
                    SenderId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => new { x.ChannelId, x.MessageId });
                    table.ForeignKey(
                        name: "FK_Messages_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "ChannelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Accounts_SenderId",
                        column: x => x.SenderId,
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

            migrationBuilder.CreateIndex(
                name: "IX_BlockedAccount_AccountId",
                table: "BlockedAccount",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedConversation_ChannelId",
                table: "BlockedConversation",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_OtherId",
                table: "Channels",
                column: "OtherId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_OwnerId",
                table: "Channels",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMember_AccountId",
                table: "GroupMember",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MailConfirmations_AccountId",
                table: "MailConfirmations",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageDependencies_ChannelId_MessageId",
                table: "MessageDependencies",
                columns: new[] { "ChannelId", "MessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockedAccount");

            migrationBuilder.DropTable(
                name: "BlockedConversation");

            migrationBuilder.DropTable(
                name: "GroupMember");

            migrationBuilder.DropTable(
                name: "MailConfirmations");

            migrationBuilder.DropTable(
                name: "MessageDependencies");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
