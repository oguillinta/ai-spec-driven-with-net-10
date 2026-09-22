using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BancaDigitalPeru.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                });

            // Datos ficticios de referencia (spec.md §6): Cliente A y Cliente B.
            migrationBuilder.InsertData(
                table: "customers",
                columns: new[] { "id", "display_name" },
                values: new object[,]
                {
                    { new Guid("a1111111-1111-1111-1111-111111111111"), "Cliente A (ficticio)" },
                    { new Guid("b2222222-2222-2222-2222-222222222222"), "Cliente B (ficticio)" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customers");
        }
    }
}
