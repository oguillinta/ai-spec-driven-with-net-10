using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BancaDigitalPeru.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDebitCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "debit_cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    expiration_month = table.Column<int>(type: "integer", nullable: false),
                    expiration_year = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_debit_cards", x => x.id);
                    table.ForeignKey(
                        name: "fk_debit_cards_accounts",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_debit_cards_customers",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_debit_cards_account_id",
                table: "debit_cards",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_debit_cards_customer_id",
                table: "debit_cards",
                column: "customer_id");

            // Datos ficticios de referencia (spec.md §6).
            migrationBuilder.InsertData(
                table: "debit_cards",
                columns: new[] { "id", "account_id", "customer_id", "number", "expiration_month", "expiration_year", "status" },
                values: new object[,]
                {
                    {
                        new Guid("cccccccc-1111-1111-1111-111111111111"),
                        new Guid("aaaaaaaa-1111-1111-1111-111111111111"),
                        new Guid("a1111111-1111-1111-1111-111111111111"),
                        "4111111111114582", 11, 2027, "Active"
                    },
                    {
                        new Guid("cccccccc-2222-2222-2222-222222222222"),
                        new Guid("aaaaaaaa-2222-2222-2222-222222222222"),
                        new Guid("a1111111-1111-1111-1111-111111111111"),
                        "4111111111119911", 3, 2026, "Blocked"
                    },
                    {
                        new Guid("dddddddd-1111-1111-1111-111111111111"),
                        new Guid("bbbbbbbb-1111-1111-1111-111111111111"),
                        new Guid("b2222222-2222-2222-2222-222222222222"),
                        "4111111111110000", 6, 2028, "Active"
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "debit_cards");
        }
    }
}
