using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMonitor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class HU18_Evaluacion_Agente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluacionAgente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CursoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Inicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Fin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluacionAgente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluacionAgente_Curso_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Curso",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EvaluacionAgente_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResultadoEvaluacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluacionAgenteId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Modo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Respondio = table.Column<bool>(type: "bit", nullable: false),
                    Aprobada = table.Column<bool>(type: "bit", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Respuesta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Herramientas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Proveedor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DuracionMs = table.Column<int>(type: "int", nullable: false),
                    CifrasVerificadas = table.Column<int>(type: "int", nullable: false),
                    CifrasTotal = table.Column<int>(type: "int", nullable: false),
                    ConversacionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultadoEvaluacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultadoEvaluacion_EvaluacionAgente_EvaluacionAgenteId",
                        column: x => x.EvaluacionAgenteId,
                        principalTable: "EvaluacionAgente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionAgente_CursoId",
                table: "EvaluacionAgente",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionAgente_UsuarioId",
                table: "EvaluacionAgente",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadoEvaluacion_EvaluacionAgenteId_Modo_Numero",
                table: "ResultadoEvaluacion",
                columns: new[] { "EvaluacionAgenteId", "Modo", "Numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResultadoEvaluacion");

            migrationBuilder.DropTable(
                name: "EvaluacionAgente");
        }
    }
}
