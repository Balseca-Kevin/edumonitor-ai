using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMonitor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Conversacion_Chat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DuracionMs",
                table: "TrazaHerramienta",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "TrazaHerramienta",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Fecha",
                table: "TrazaHerramienta",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "DuracionMs",
                table: "Conversacion",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Modelo",
                table: "Conversacion",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Pregunta",
                table: "Conversacion",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Respuesta",
                table: "Conversacion",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuracionMs",
                table: "TrazaHerramienta");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "TrazaHerramienta");

            migrationBuilder.DropColumn(
                name: "Fecha",
                table: "TrazaHerramienta");

            migrationBuilder.DropColumn(
                name: "DuracionMs",
                table: "Conversacion");

            migrationBuilder.DropColumn(
                name: "Modelo",
                table: "Conversacion");

            migrationBuilder.DropColumn(
                name: "Pregunta",
                table: "Conversacion");

            migrationBuilder.DropColumn(
                name: "Respuesta",
                table: "Conversacion");
        }
    }
}
