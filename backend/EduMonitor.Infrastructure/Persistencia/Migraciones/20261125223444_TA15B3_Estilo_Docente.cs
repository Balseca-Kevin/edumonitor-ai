using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMonitor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class TA15B3_Estilo_Docente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlanRefuerzo_UsuarioId",
                table: "PlanRefuerzo");

            migrationBuilder.AddColumn<bool>(
                name: "ConEstilo",
                table: "PlanRefuerzo",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TextoAprobado",
                table: "PlanRefuerzo",
                type: "nvarchar(max)",
                maxLength: 20000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextoGenerado",
                table: "PlanRefuerzo",
                type: "nvarchar(max)",
                maxLength: 20000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanRefuerzo_UsuarioId_Estado",
                table: "PlanRefuerzo",
                columns: new[] { "UsuarioId", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlanRefuerzo_UsuarioId_Estado",
                table: "PlanRefuerzo");

            migrationBuilder.DropColumn(
                name: "ConEstilo",
                table: "PlanRefuerzo");

            migrationBuilder.DropColumn(
                name: "TextoAprobado",
                table: "PlanRefuerzo");

            migrationBuilder.DropColumn(
                name: "TextoGenerado",
                table: "PlanRefuerzo");

            migrationBuilder.CreateIndex(
                name: "IX_PlanRefuerzo_UsuarioId",
                table: "PlanRefuerzo",
                column: "UsuarioId");
        }
    }
}
