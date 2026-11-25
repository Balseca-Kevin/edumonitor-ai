using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMonitor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class HU23_Asistente_Memoria_Respaldo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cifras",
                table: "Conversacion",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HiloId",
                table: "Conversacion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Proveedor",
                table: "Conversacion",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Respaldo",
                table: "Conversacion",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ConfiguracionAsistente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoloModeloLocal = table.Column<bool>(type: "bit", nullable: false),
                    Actualizado = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionAsistente", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conversacion_HiloId",
                table: "Conversacion",
                column: "HiloId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionAsistente");

            migrationBuilder.DropIndex(
                name: "IX_Conversacion_HiloId",
                table: "Conversacion");

            migrationBuilder.DropColumn(
                name: "Cifras",
                table: "Conversacion");

            migrationBuilder.DropColumn(
                name: "HiloId",
                table: "Conversacion");

            migrationBuilder.DropColumn(
                name: "Proveedor",
                table: "Conversacion");

            migrationBuilder.DropColumn(
                name: "Respaldo",
                table: "Conversacion");
        }
    }
}
