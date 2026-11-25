using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMonitor.Infrastructure.Persistencia.Configuraciones;

/// <summary>HU-15 · TA-15.A3 · Plan de refuerzo con sus temas y estudiantes; se borran con el plan.</summary>
internal sealed class PlanRefuerzoConfiguracion : IEntityTypeConfiguration<PlanRefuerzo>
{
    public void Configure(EntityTypeBuilder<PlanRefuerzo> builder)
    {
        builder.ToTable("PlanRefuerzo");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.CursoId);
        builder.HasOne(p => p.Curso).WithMany().HasForeignKey(p => p.CursoId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(p => p.Actividad).WithMany().HasForeignKey(p => p.ActividadId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Conversacion>().WithMany().HasForeignKey(p => p.ConversacionId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(p => p.Estado).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Tipo).HasMaxLength(20).IsRequired().HasDefaultValue(PlanRefuerzo.TipoRefuerzo);
        builder.Property(p => p.Texto).HasMaxLength(20000);
    }
}

internal sealed class PlanRefuerzoTemaConfiguracion : IEntityTypeConfiguration<PlanRefuerzoTema>
{
    public void Configure(EntityTypeBuilder<PlanRefuerzoTema> builder)
    {
        builder.ToTable("PlanRefuerzoTema");
        builder.HasKey(t => new { t.PlanRefuerzoId, t.TemaId });
        builder.HasOne(t => t.PlanRefuerzo).WithMany(p => p.Temas).HasForeignKey(t => t.PlanRefuerzoId);
        builder.HasOne(t => t.Tema).WithMany().HasForeignKey(t => t.TemaId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class PlanRefuerzoEstudianteConfiguracion : IEntityTypeConfiguration<PlanRefuerzoEstudiante>
{
    public void Configure(EntityTypeBuilder<PlanRefuerzoEstudiante> builder)
    {
        builder.ToTable("PlanRefuerzoEstudiante");
        builder.HasKey(e => new { e.PlanRefuerzoId, e.EstudianteId });
        builder.HasOne(e => e.PlanRefuerzo).WithMany(p => p.Estudiantes).HasForeignKey(e => e.PlanRefuerzoId);
        builder.HasOne(e => e.Estudiante).WithMany().HasForeignKey(e => e.EstudianteId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(e => e.FotoAlAprobar).HasMaxLength(4000);
    }
}
