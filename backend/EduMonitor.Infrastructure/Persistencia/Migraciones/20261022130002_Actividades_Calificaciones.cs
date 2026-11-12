using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMonitor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Actividades_Calificaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actividad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CursoId = table.Column<int>(type: "int", nullable: false),
                    MoodleItemId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Peso = table.Column<decimal>(type: "decimal(9,5)", precision: 9, scale: 5, nullable: true),
                    NotaMaxima = table.Column<decimal>(type: "decimal(9,5)", precision: 9, scale: 5, nullable: true),
                    Modulo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Instancia = table.Column<int>(type: "int", nullable: true),
                    FechaLimite = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actividad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actividad_Curso_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Curso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Calificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActividadId = table.Column<int>(type: "int", nullable: false),
                    EstudianteId = table.Column<int>(type: "int", nullable: false),
                    Nota = table.Column<decimal>(type: "decimal(9,5)", precision: 9, scale: 5, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calificacion_Actividad_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Calificacion_Estudiante_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Estudiante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actividad_CursoId",
                table: "Actividad",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_Actividad_MoodleItemId",
                table: "Actividad",
                column: "MoodleItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Calificacion_ActividadId_EstudianteId",
                table: "Calificacion",
                columns: new[] { "ActividadId", "EstudianteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Calificacion_EstudianteId",
                table: "Calificacion",
                column: "EstudianteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Calificacion");

            migrationBuilder.DropTable(
                name: "Actividad");
        }
    }
}
