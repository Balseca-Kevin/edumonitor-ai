using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMonitor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class HU08_Rubricas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comentario",
                table: "Calificacion",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CriterioRubrica",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActividadId = table.Column<int>(type: "int", nullable: false),
                    MoodleCriterioId = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PuntajeMaximo = table.Column<decimal>(type: "decimal(9,5)", precision: 9, scale: 5, nullable: false),
                    TemaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriterioRubrica", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CriterioRubrica_Actividad_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CriterioRubrica_Tema_TemaId",
                        column: x => x.TemaId,
                        principalTable: "Tema",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CalificacionCriterio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CriterioRubricaId = table.Column<int>(type: "int", nullable: false),
                    EstudianteId = table.Column<int>(type: "int", nullable: false),
                    Puntaje = table.Column<decimal>(type: "decimal(9,5)", precision: 9, scale: 5, nullable: false),
                    Nivel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalificacionCriterio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalificacionCriterio_CriterioRubrica_CriterioRubricaId",
                        column: x => x.CriterioRubricaId,
                        principalTable: "CriterioRubrica",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CalificacionCriterio_Estudiante_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Estudiante",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionCriterio_CriterioRubricaId_EstudianteId",
                table: "CalificacionCriterio",
                columns: new[] { "CriterioRubricaId", "EstudianteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionCriterio_EstudianteId",
                table: "CalificacionCriterio",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_CriterioRubrica_ActividadId_MoodleCriterioId",
                table: "CriterioRubrica",
                columns: new[] { "ActividadId", "MoodleCriterioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CriterioRubrica_TemaId",
                table: "CriterioRubrica",
                column: "TemaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalificacionCriterio");

            migrationBuilder.DropTable(
                name: "CriterioRubrica");

            migrationBuilder.DropColumn(
                name: "Comentario",
                table: "Calificacion");
        }
    }
}
