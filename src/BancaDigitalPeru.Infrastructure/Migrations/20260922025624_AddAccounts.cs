using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BancaDigitalPeru.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    balance_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    balance_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounts_customers",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accounts_customer_id",
                table: "accounts",
                column: "customer_id");

            // Datos ficticios de referencia (spec.md §6).
            migrationBuilder.InsertData(
                table: "accounts",
                columns: new[] { "id", "customer_id", "type", "number", "balance_amount", "balance_currency", "status" },
                values: new object[,]
                {
                    {
                        new Guid("aaaaaaaa-1111-1111-1111-111111111111"),
                        new Guid("a1111111-1111-1111-1111-111111111111"),
                        "Savings", "00123456780001", 2500.00m, "PEN", "Active"
                    },
                    {
                        new Guid("aaaaaaaa-2222-2222-2222-222222222222"),
                        new Guid("a1111111-1111-1111-1111-111111111111"),
                        "Savings", "00123456780002", 800.00m, "PEN", "Blocked"
                    },
                    {
                        new Guid("bbbbbbbb-1111-1111-1111-111111111111"),
                        new Guid("b2222222-2222-2222-2222-222222222222"),
                        "Savings", "00123456780003", 1500.00m, "PEN", "Active"
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounts");
        }
    }
}
