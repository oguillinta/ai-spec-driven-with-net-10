using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BancaDigitalPeru.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransfersAndAccountConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "accounts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfers", x => x.id);
                    table.ForeignKey(
                        name: "fk_transfers_customers",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfers_destination_account",
                        column: x => x.destination_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfers_source_account",
                        column: x => x.source_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_transfers_customer_id",
                table: "transfers",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_destination_account_id",
                table: "transfers",
                column: "destination_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_source_account_id",
                table: "transfers",
                column: "source_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_transfers_idempotency_key",
                table: "transfers",
                column: "idempotency_key",
                unique: true);

            // Ajuste de seed para esta feature (tasks.md T019): la Cuenta B del Cliente A quedó
            // sembrada como BLOQUEADA en la migración AddAccounts de 001 (ya aplicada, nunca se
            // edita). El ejemplo de referencia de 002 (spec §6) requiere que ambas cuentas de
            // demostración estén ACTIVAS; se actualiza aquí, en la migración nueva, no en la
            // histórica — editar una migración ya aplicada no tendría efecto en ninguna base de
            // datos donde ya corrió, incluida la del entorno local de desarrollo.
            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-2222-2222-2222-222222222222"),
                column: "status",
                value: "Active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-2222-2222-2222-222222222222"),
                column: "status",
                value: "Blocked");

            migrationBuilder.DropTable(
                name: "transfers");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "accounts");
        }
    }
}
