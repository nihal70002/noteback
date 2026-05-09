using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Note.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPackFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Users' AND column_name='PasswordResetTokenExpiresAt') THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""PasswordResetTokenExpiresAt"" timestamp with time zone NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Users' AND column_name='PasswordResetTokenHash') THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""PasswordResetTokenHash"" text NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='IsPack') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""IsPack"" boolean NOT NULL DEFAULT false;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='PackSize') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""PackSize"" integer NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='RazorpayOrderId') THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""RazorpayOrderId"" text NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='RazorpayPaymentId') THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""RazorpayPaymentId"" text NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='OrderItems' AND column_name='SelectedChoicesJson') THEN
                        ALTER TABLE ""OrderItems"" ADD COLUMN ""SelectedChoicesJson"" text NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CartItems' AND column_name='SelectedChoicesJson') THEN
                        ALTER TABLE ""CartItems"" ADD COLUMN ""SelectedChoicesJson"" text NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.CreateTable(
                name: "PackChoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PackProductId = table.Column<string>(type: "text", nullable: false),
                    ChoiceProductId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackChoices_Products_ChoiceProductId",
                        column: x => x.ChoiceProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PackChoices_Products_PackProductId",
                        column: x => x.PackProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "1",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "2",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "3",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "4",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "5",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "6",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "7",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: "8",
                columns: new[] { "IsPack", "PackSize" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "admin-user-id",
                columns: new[] { "PasswordResetTokenExpiresAt", "PasswordResetTokenHash" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_PackChoices_ChoiceProductId",
                table: "PackChoices",
                column: "ChoiceProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PackChoices_PackProductId",
                table: "PackChoices",
                column: "PackProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""PackChoices"";");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Users' AND column_name='PasswordResetTokenExpiresAt') THEN
                        ALTER TABLE ""Users"" DROP COLUMN ""PasswordResetTokenExpiresAt"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Users' AND column_name='PasswordResetTokenHash') THEN
                        ALTER TABLE ""Users"" DROP COLUMN ""PasswordResetTokenHash"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='IsPack') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""IsPack"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='PackSize') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""PackSize"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='RazorpayOrderId') THEN
                        ALTER TABLE ""Orders"" DROP COLUMN ""RazorpayOrderId"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='RazorpayPaymentId') THEN
                        ALTER TABLE ""Orders"" DROP COLUMN ""RazorpayPaymentId"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='OrderItems' AND column_name='SelectedChoicesJson') THEN
                        ALTER TABLE ""OrderItems"" DROP COLUMN ""SelectedChoicesJson"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CartItems' AND column_name='SelectedChoicesJson') THEN
                        ALTER TABLE ""CartItems"" DROP COLUMN ""SelectedChoicesJson"";
                    END IF;
                END $$;
            ");
        }
    }
}
