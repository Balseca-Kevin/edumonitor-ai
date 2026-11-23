using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMonitor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class TA15A3_Plan_Refuerzo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanRefuerzo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CursoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    ActividadId = table.Column<int>(type: "int", nullable: false),
                    Creado = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConversacionId = table.Column<int>(type: "int", nullable: true),
                    Texto = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: true),
                    AprobadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanRefuerzo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanRefuerzo_Actividad_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividad",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlanRefuerzo_Conversacion_ConversacionId",
                        column: x => x.ConversacionId,
                        principalTable: "Conversacion",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlanRefuerzo_Curso_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Curso",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlanRefuerzo_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlanRefuerzoEstudiante",
                columns: table => new
                {
                    PlanRefuerzoId = table.Column<int>(type: "int", nullable: false),
                    EstudianteId = table.Column<int>(type: "int", nullable: false),
                    FotoAlAprobar = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanRefuerzoEstudiante", x => new { x.PlanRefuerzoId, x.EstudianteId });
                    table.ForeignKey(
                        name: "FK_PlanRefuerzoEstudiante_Estudiante_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Estudiante",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlanRefuerzoEstudiante_PlanRefuerzo_PlanRefuerzoId",
                        column: x => x.PlanRefuerzoId,
                        principalTable: "PlanRefuerzo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanRefuerzoTema",
                columns: table => new
                {
                    PlanRefuerzoId = table.Column<int>(type: "int", nullable: false),
                    TemaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanRefuerzoTema", x => new { x.PlanRefuerzoId, x.TemaId });
                    table.ForeignKey(
                        name: "FK_PlanRefuerzoTema_PlanRefuerzo_PlanRefuerzoId",
                        column: x => x.PlanRefuerzoId,
                        principalTable: "PlanRefuerzo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanRefuerzoTema_Tema_TemaId",
                        column: x => x.TemaId,
                        principalTable: "Tema",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanRefuerzo_ActividadId",
                table: "PlanRefuerzo",
                column: "ActividadId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanRefuerzo_ConversacionId",
                table: "PlanRefuerzo",
                column: "ConversacionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanRefuerzo_CursoId",
                table: "PlanRefuerzo",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanRefuerzo_UsuarioId",
                table: "PlanRefuerzo",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanRefuerzoEstudiante_EstudianteId",
                table: "PlanRefuerzoEstudiante",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanRefuerzoTema_TemaId",
                table: "PlanRefuerzoTema",
                column: "TemaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanRefuerzoEstudiante");

            migrationBuilder.DropTable(
                name: "PlanRefuerzoTema");

            migrationBuilder.DropTable(
                name: "PlanRefuerzo");
        }
    }
}
