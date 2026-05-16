using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Note.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCartAnalyticsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AddedAt",
                table: "Carts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<bool>(
                name: "IsOrdered",
                table: "Carts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderedAt",
                table: "Carts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Carts",
                type: "text",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Carts",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Carts"
                SET "UserId" = substring("Id" from 6)
                WHERE "UserId" IS NULL AND "Id" LIKE 'cart_%';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Carts_AddedAt",
                table: "Carts",
                column: "AddedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Carts_AddedAt",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_Carts_UserId",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "AddedAt",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "IsOrdered",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "OrderedAt",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Carts");
        }
    }
}
