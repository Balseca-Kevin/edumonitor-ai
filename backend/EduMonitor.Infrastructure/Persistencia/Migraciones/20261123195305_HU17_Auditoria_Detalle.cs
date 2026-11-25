using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduMonitor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class HU17_Auditoria_Detalle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Auditoria",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CursoId",
                table: "Auditoria",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Detalle",
                table: "Auditoria",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Resultado",
                table: "Auditoria",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Accion",
                table: "Auditoria",
                column: "Accion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Auditoria_Accion",
                table: "Auditoria");

            migrationBuilder.DropColumn(
                name: "CursoId",
                table: "Auditoria");

            migrationBuilder.DropColumn(
                name: "Detalle",
                table: "Auditoria");

            migrationBuilder.DropColumn(
                name: "Resultado",
                table: "Auditoria");

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Auditoria",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
