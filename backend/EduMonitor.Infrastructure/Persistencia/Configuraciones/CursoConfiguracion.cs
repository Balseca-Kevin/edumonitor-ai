using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMonitor.Infrastructure.Persistencia.Configuraciones;

internal sealed class CursoConfiguracion : IEntityTypeConfiguration<Curso>
{
    public void Configure(EntityTypeBuilder<Curso> builder)
    {
        builder.ToTable("Curso");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.MoodleCursoId).IsUnique();
        builder.Property(c => c.NombreCorto).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Nombre).HasMaxLength(250).IsRequired();
        builder.Property(c => c.Periodo).HasMaxLength(20).IsRequired();
    }
}

internal sealed class CursoDocenteConfiguracion : IEntityTypeConfiguration<CursoDocente>
{
    public void Configure(EntityTypeBuilder<CursoDocente> builder)
    {
        builder.ToTable("DocenteCurso");
        builder.HasKey(cd => new { cd.UsuarioId, cd.CursoId });
        builder.HasOne(cd => cd.Usuario).WithMany(u => u.Cursos).HasForeignKey(cd => cd.UsuarioId);
        builder.HasOne(cd => cd.Curso).WithMany(c => c.Docentes).HasForeignKey(cd => cd.CursoId);
    }
}
