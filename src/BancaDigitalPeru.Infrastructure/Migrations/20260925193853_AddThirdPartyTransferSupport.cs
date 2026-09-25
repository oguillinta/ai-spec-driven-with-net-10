using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BancaDigitalPeru.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThirdPartyTransferSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_accounts_number",
                table: "accounts",
                column: "number",
                unique: true);

            // Ajuste de seed para esta feature (research.md §9): los nombres ficticios de
            // Cliente A/B ("Cliente A (ficticio)"/"Cliente B (ficticio)", sembrados en
            // InitialCreate de 001, ya aplicada, nunca editada) no tienen la forma "nombre +
            // apellido" que exige el algoritmo de enmascaramiento de FR-011. Se actualizan aquí,
            // en la migración nueva, no en la histórica.
            migrationBuilder.UpdateData(
                table: "customers",
                keyColumn: "id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111111"),
                column: "display_name",
                value: "María López Torres");

            migrationBuilder.UpdateData(
                table: "customers",
                keyColumn: "id",
                keyValue: new Guid("b2222222-2222-2222-2222-222222222222"),
                column: "display_name",
                value: "Juan Pérez García");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "customers",
                keyColumn: "id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111111"),
                column: "display_name",
                value: "Cliente A (ficticio)");

            migrationBuilder.UpdateData(
                table: "customers",
                keyColumn: "id",
                keyValue: new Guid("b2222222-2222-2222-2222-222222222222"),
                column: "display_name",
                value: "Cliente B (ficticio)");

            migrationBuilder.DropIndex(
                name: "ux_accounts_number",
                table: "accounts");
        }
    }
}
