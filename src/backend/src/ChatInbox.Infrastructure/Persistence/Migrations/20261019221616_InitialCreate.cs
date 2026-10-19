using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ChatInbox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contacts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LastName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: false),
                    UnreadCount = table.Column<int>(type: "integer", nullable: false),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversations_contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramUpdateId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramMessageId = table.Column<long>(type: "bigint", nullable: false),
                    Text = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    Direction = table.Column<short>(type: "smallint", nullable: false),
                    TelegramSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_messages_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_contacts_public_id",
                table: "contacts",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_contacts_telegram_user_id",
                table: "contacts",
                column: "TelegramUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conversations_last_message_at",
                table: "conversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "ux_conversations_contact_id",
                table: "conversations",
                column: "ContactId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_conversations_public_id",
                table: "conversations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_conversations_telegram_chat_id",
                table: "conversations",
                column: "TelegramChatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_telegram_sent_at",
                table: "messages",
                columns: new[] { "ConversationId", "TelegramSentAt" });

            migrationBuilder.CreateIndex(
                name: "ux_messages_conversation_telegram_message_id",
                table: "messages",
                columns: new[] { "ConversationId", "TelegramMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_messages_public_id",
                table: "messages",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_messages_telegram_update_id",
                table: "messages",
                column: "TelegramUpdateId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "conversations");

            migrationBuilder.DropTable(
                name: "contacts");
        }
    }
}
