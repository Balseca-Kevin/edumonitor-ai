using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMonitor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Sincronizaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sincronizacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CursoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Inicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Fin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Origen = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    RegistrosUnidades = table.Column<int>(type: "int", nullable: false),
                    RegistrosActividades = table.Column<int>(type: "int", nullable: false),
                    RegistrosEstudiantes = table.Column<int>(type: "int", nullable: false),
                    RegistrosCalificaciones = table.Column<int>(type: "int", nullable: false),
                    Errores = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sincronizacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sincronizacion_Curso_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Curso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sincronizacion_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sincronizacion_CursoId",
                table: "Sincronizacion",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_Sincronizacion_UsuarioId",
                table: "Sincronizacion",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sincronizacion");
        }
    }
}
