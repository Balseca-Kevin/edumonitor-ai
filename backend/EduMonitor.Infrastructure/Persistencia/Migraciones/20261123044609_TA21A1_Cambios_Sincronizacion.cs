using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMonitor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class TA21A1_Cambios_Sincronizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CambiosRegistrados",
                table: "Sincronizacion",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CambioSincronizacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SincronizacionId = table.Column<int>(type: "int", nullable: false),
                    ActividadId = table.Column<int>(type: "int", nullable: false),
                    EstudianteId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NotaAntes = table.Column<decimal>(type: "decimal(9,5)", precision: 9, scale: 5, nullable: true),
                    EstadoAntes = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NotaAhora = table.Column<decimal>(type: "decimal(9,5)", precision: 9, scale: 5, nullable: true),
                    EstadoAhora = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CambioSincronizacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CambioSincronizacion_Actividad_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividad",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CambioSincronizacion_Estudiante_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Estudiante",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CambioSincronizacion_Sincronizacion_SincronizacionId",
                        column: x => x.SincronizacionId,
                        principalTable: "Sincronizacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CambioSincronizacion_ActividadId",
                table: "CambioSincronizacion",
                column: "ActividadId");

            migrationBuilder.CreateIndex(
                name: "IX_CambioSincronizacion_EstudianteId",
                table: "CambioSincronizacion",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_CambioSincronizacion_SincronizacionId",
                table: "CambioSincronizacion",
                column: "SincronizacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CambioSincronizacion");

            migrationBuilder.DropColumn(
                name: "CambiosRegistrados",
                table: "Sincronizacion");
        }
    }
}
