using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsletterApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gubuns",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gubuns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    login_id = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    is_admin = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_suspended = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "newsletter_configs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    send_time = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    gubun_id = table.Column<long>(type: "bigint", nullable: false),
                    is_use = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newsletter_configs", x => x.id);
                    table.ForeignKey(
                        name: "FK_newsletter_configs_gubuns_gubun_id",
                        column: x => x.gubun_id,
                        principalTable: "gubuns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "newsletters",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    gubun_id = table.Column<long>(type: "bigint", nullable: false),
                    send_day = table.Column<DateOnly>(type: "date", nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newsletters", x => x.id);
                    table.ForeignKey(
                        name: "FK_newsletters_gubuns_gubun_id",
                        column: x => x.gubun_id,
                        principalTable: "gubuns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    gubun_id = table.Column<long>(type: "bigint", nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscriptions_gubuns_gubun_id",
                        column: x => x.gubun_id,
                        principalTable: "gubuns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "historys",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    newsletter_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historys", x => x.id);
                    table.ForeignKey(
                        name: "FK_historys_newsletters_newsletter_id",
                        column: x => x.newsletter_id,
                        principalTable: "newsletters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_historys_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gubuns_name",
                table: "gubuns",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_historys_user_id",
                table: "historys",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_historys_newsletter_id_user_id",
                table: "historys",
                columns: new[] { "newsletter_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_newsletter_configs_gubun_id",
                table: "newsletter_configs",
                column: "gubun_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_newsletters_create_date",
                table: "newsletters",
                column: "create_date");

            migrationBuilder.CreateIndex(
                name: "IX_newsletters_gubun_id_send_day",
                table: "newsletters",
                columns: new[] { "gubun_id", "send_day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_subscriptions_gubun_id",
                table: "subscriptions",
                column: "gubun_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_user_id_gubun_id",
                table: "subscriptions",
                columns: new[] { "user_id", "gubun_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_login_id",
                table: "users",
                column: "login_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historys");

            migrationBuilder.DropTable(
                name: "newsletter_configs");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "newsletters");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "gubuns");
        }
    }
}
