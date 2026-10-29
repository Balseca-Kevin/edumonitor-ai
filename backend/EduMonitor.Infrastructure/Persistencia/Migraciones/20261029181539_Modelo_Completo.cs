using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMonitor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Modelo_Completo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Auditoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Entidad = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Auditoria_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Conversacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CursoId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversacion_Curso_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Curso",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Conversacion_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReglaAlerta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CursoId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Umbral = table.Column<decimal>(type: "decimal(9,5)", precision: 9, scale: 5, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglaAlerta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReglaAlerta_Curso_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Curso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnidadId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tema", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tema_Unidad_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "Unidad",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrazaHerramienta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversacionId = table.Column<int>(type: "int", nullable: false),
                    Herramienta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Parametros = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrazaHerramienta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrazaHerramienta_Conversacion_ConversacionId",
                        column: x => x.ConversacionId,
                        principalTable: "Conversacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alerta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReglaId = table.Column<int>(type: "int", nullable: false),
                    EstudianteId = table.Column<int>(type: "int", nullable: false),
                    Evidencia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerta_Estudiante_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Estudiante",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Alerta_ReglaAlerta_ReglaId",
                        column: x => x.ReglaId,
                        principalTable: "ReglaAlerta",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ActividadTema",
                columns: table => new
                {
                    ActividadId = table.Column<int>(type: "int", nullable: false),
                    TemaId = table.Column<int>(type: "int", nullable: false),
                    PesoRelativo = table.Column<decimal>(type: "decimal(9,5)", precision: 9, scale: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActividadTema", x => new { x.ActividadId, x.TemaId });
                    table.ForeignKey(
                        name: "FK_ActividadTema_Actividad_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividad",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActividadTema_Tema_TemaId",
                        column: x => x.TemaId,
                        principalTable: "Tema",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContenidoMinimo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemaId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContenidoMinimo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContenidoMinimo_Tema_TemaId",
                        column: x => x.TemaId,
                        principalTable: "Tema",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TareaRefuerzo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemaId = table.Column<int>(type: "int", nullable: false),
                    EstudianteId = table.Column<int>(type: "int", nullable: true),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TareaRefuerzo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TareaRefuerzo_Estudiante_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Estudiante",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TareaRefuerzo_Tema_TemaId",
                        column: x => x.TemaId,
                        principalTable: "Tema",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActividadTema_TemaId",
                table: "ActividadTema",
                column: "TemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerta_EstudianteId",
                table: "Alerta",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerta_ReglaId",
                table: "Alerta",
                column: "ReglaId");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Fecha",
                table: "Auditoria",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_UsuarioId",
                table: "Auditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ContenidoMinimo_TemaId",
                table: "ContenidoMinimo",
                column: "TemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversacion_CursoId",
                table: "Conversacion",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversacion_UsuarioId",
                table: "Conversacion",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ReglaAlerta_CursoId_Codigo",
                table: "ReglaAlerta",
                columns: new[] { "CursoId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TareaRefuerzo_EstudianteId",
                table: "TareaRefuerzo",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_TareaRefuerzo_TemaId",
                table: "TareaRefuerzo",
                column: "TemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tema_UnidadId",
                table: "Tema",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "IX_TrazaHerramienta_ConversacionId",
                table: "TrazaHerramienta",
                column: "ConversacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActividadTema");

            migrationBuilder.DropTable(
                name: "Alerta");

            migrationBuilder.DropTable(
                name: "Auditoria");

            migrationBuilder.DropTable(
                name: "ContenidoMinimo");

            migrationBuilder.DropTable(
                name: "TareaRefuerzo");

            migrationBuilder.DropTable(
                name: "TrazaHerramienta");

            migrationBuilder.DropTable(
                name: "ReglaAlerta");

            migrationBuilder.DropTable(
                name: "Tema");

            migrationBuilder.DropTable(
                name: "Conversacion");
        }
    }
}
