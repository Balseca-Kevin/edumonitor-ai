using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMonitor.Infrastructure.Persistencia.Configuraciones;

internal sealed class ActividadConfiguracion : IEntityTypeConfiguration<Actividad>
{
    public void Configure(EntityTypeBuilder<Actividad> builder)
    {
        builder.ToTable("Actividad");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.MoodleItemId).IsUnique();
        builder.HasOne(a => a.Curso).WithMany().HasForeignKey(a => a.CursoId);
        // Sin cascada: Curso ya borra en cascada sus actividades y SQL Server no admite dos caminos de cascada.
        builder.HasOne(a => a.Unidad).WithMany().HasForeignKey(a => a.UnidadId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(a => a.Nombre).HasMaxLength(250).IsRequired();
        builder.Property(a => a.Categoria).HasMaxLength(100);
        builder.Property(a => a.Peso).HasPrecision(9, 5);
        builder.Property(a => a.NotaMaxima).HasPrecision(9, 5);
        builder.Property(a => a.Modulo).HasMaxLength(50);
    }
}

internal sealed class CalificacionConfiguracion : IEntityTypeConfiguration<Calificacion>
{
    public void Configure(EntityTypeBuilder<Calificacion> builder)
    {
        builder.ToTable("Calificacion");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => new { c.ActividadId, c.EstudianteId }).IsUnique();
        builder.HasOne(c => c.Actividad).WithMany().HasForeignKey(c => c.ActividadId);
        builder.HasOne(c => c.Estudiante).WithMany().HasForeignKey(c => c.EstudianteId);
        builder.Property(c => c.Nota).HasPrecision(9, 5);
        builder.Property(c => c.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Comentario).HasMaxLength(4000);
    }
}

/// <summary>HU-08 · Criterios de rúbrica por actividad; el tema lo asocia el docente y se conserva al sincronizar.</summary>
internal sealed class CriterioRubricaConfiguracion : IEntityTypeConfiguration<CriterioRubrica>
{
    public void Configure(EntityTypeBuilder<CriterioRubrica> builder)
    {
        builder.ToTable("CriterioRubrica");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => new { c.ActividadId, c.MoodleCriterioId }).IsUnique();
        builder.HasOne(c => c.Actividad).WithMany().HasForeignKey(c => c.ActividadId);
        builder.HasOne(c => c.Tema).WithMany().HasForeignKey(c => c.TemaId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(c => c.Descripcion).HasMaxLength(500).IsRequired();
        builder.Property(c => c.PuntajeMaximo).HasPrecision(9, 5);
    }
}

/// <summary>HU-08 · Nivel de cada estudiante en cada criterio. Único por criterio y estudiante.</summary>
internal sealed class CalificacionCriterioConfiguracion : IEntityTypeConfiguration<CalificacionCriterio>
{
    public void Configure(EntityTypeBuilder<CalificacionCriterio> builder)
    {
        builder.ToTable("CalificacionCriterio");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => new { c.CriterioRubricaId, c.EstudianteId }).IsUnique();
        builder.HasOne(c => c.CriterioRubrica).WithMany().HasForeignKey(c => c.CriterioRubricaId);
        builder.HasOne(c => c.Estudiante).WithMany().HasForeignKey(c => c.EstudianteId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(c => c.Puntaje).HasPrecision(9, 5);
        builder.Property(c => c.Nivel).HasMaxLength(100).IsRequired();
    }
}
