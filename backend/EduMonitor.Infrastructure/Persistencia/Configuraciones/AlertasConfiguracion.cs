using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMonitor.Infrastructure.Persistencia.Configuraciones;

// Reglas, alertas y tareas de refuerzo (Sprints 2 y 3).

internal sealed class ReglaAlertaConfiguracion : IEntityTypeConfiguration<ReglaAlerta>
{
    public void Configure(EntityTypeBuilder<ReglaAlerta> builder)
    {
        builder.ToTable("ReglaAlerta");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.CursoId, r.Codigo }).IsUnique();
        builder.HasOne(r => r.Curso).WithMany().HasForeignKey(r => r.CursoId);
        builder.Property(r => r.Codigo).HasMaxLength(10).IsRequired();
        builder.Property(r => r.Umbral).HasPrecision(9, 5);
    }
}

internal sealed class AlertaConfiguracion : IEntityTypeConfiguration<Alerta>
{
    public void Configure(EntityTypeBuilder<Alerta> builder)
    {
        builder.ToTable("Alerta");
        builder.HasKey(a => a.Id);
        builder.HasOne(a => a.Regla).WithMany().HasForeignKey(a => a.ReglaId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(a => a.Estudiante).WithMany().HasForeignKey(a => a.EstudianteId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(a => a.Evidencia).IsRequired();
        builder.Property(a => a.Estado).HasMaxLength(20).IsRequired();
    }
}

internal sealed class TareaRefuerzoConfiguracion : IEntityTypeConfiguration<TareaRefuerzo>
{
    public void Configure(EntityTypeBuilder<TareaRefuerzo> builder)
    {
        builder.ToTable("TareaRefuerzo");
        builder.HasKey(t => t.Id);
        builder.HasOne(t => t.Tema).WithMany().HasForeignKey(t => t.TemaId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.Estudiante).WithMany().HasForeignKey(t => t.EstudianteId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(t => t.Contenido).IsRequired();
        builder.Property(t => t.Estado).HasMaxLength(20).IsRequired();
    }
}
