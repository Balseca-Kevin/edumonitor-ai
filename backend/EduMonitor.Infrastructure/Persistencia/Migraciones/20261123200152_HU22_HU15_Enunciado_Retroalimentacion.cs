using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMonitor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class HU22_HU15_Enunciado_Retroalimentacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "PlanRefuerzo",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "REFUERZO");

            migrationBuilder.AddColumn<string>(
                name: "Archivos",
                table: "Actividad",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Enunciado",
                table: "Actividad",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "PlanRefuerzo");

            migrationBuilder.DropColumn(
                name: "Archivos",
                table: "Actividad");

            migrationBuilder.DropColumn(
                name: "Enunciado",
                table: "Actividad");
        }
    }
}
