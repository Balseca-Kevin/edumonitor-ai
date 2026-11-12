using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMonitor.Infrastructure.Persistencia.Configuraciones;

internal sealed class EstudianteConfiguracion : IEntityTypeConfiguration<Estudiante>
{
    public void Configure(EntityTypeBuilder<Estudiante> builder)
    {
        builder.ToTable("Estudiante");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.MoodleUsuarioId).IsUnique();
        builder.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Apellido).HasMaxLength(100).IsRequired();
    }
}

internal sealed class MatriculaConfiguracion : IEntityTypeConfiguration<Matricula>
{
    public void Configure(EntityTypeBuilder<Matricula> builder)
    {
        builder.ToTable("Matricula");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.CursoId, m.EstudianteId }).IsUnique();
        builder.HasOne(m => m.Curso).WithMany().HasForeignKey(m => m.CursoId);
        builder.HasOne(m => m.Estudiante).WithMany(e => e.Matriculas).HasForeignKey(m => m.EstudianteId);
    }
}
