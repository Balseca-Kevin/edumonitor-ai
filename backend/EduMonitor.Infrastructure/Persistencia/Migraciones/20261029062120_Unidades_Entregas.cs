using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMonitor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Unidades_Entregas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Finalizada",
                table: "Calificacion",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnidadId",
                table: "Actividad",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Unidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CursoId = table.Column<int>(type: "int", nullable: false),
                    MoodleSeccionId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Parcial = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unidad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Unidad_Curso_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Curso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actividad_UnidadId",
                table: "Actividad",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Unidad_CursoId",
                table: "Unidad",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_Unidad_MoodleSeccionId",
                table: "Unidad",
                column: "MoodleSeccionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Actividad_Unidad_UnidadId",
                table: "Actividad",
                column: "UnidadId",
                principalTable: "Unidad",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actividad_Unidad_UnidadId",
                table: "Actividad");

            migrationBuilder.DropTable(
                name: "Unidad");

            migrationBuilder.DropIndex(
                name: "IX_Actividad_UnidadId",
                table: "Actividad");

            migrationBuilder.DropColumn(
                name: "Finalizada",
                table: "Calificacion");

            migrationBuilder.DropColumn(
                name: "UnidadId",
                table: "Actividad");
        }
    }
}
