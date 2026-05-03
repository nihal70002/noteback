using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Note.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddStorefrontConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StorefrontConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HeroImageUrl = table.Column<string>(type: "text", nullable: true),
                    HeroTitle = table.Column<string>(type: "text", nullable: true),
                    HeroSubtitle = table.Column<string>(type: "text", nullable: true),
                    HeroLink = table.Column<string>(type: "text", nullable: true),
                    Category1ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Category1Title = table.Column<string>(type: "text", nullable: true),
                    Category1Link = table.Column<string>(type: "text", nullable: true),
                    Category2ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Category2Title = table.Column<string>(type: "text", nullable: true),
                    Category2Link = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorefrontConfigs");
        }
    }
}
