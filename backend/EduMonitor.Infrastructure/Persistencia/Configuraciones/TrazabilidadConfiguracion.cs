using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMonitor.Infrastructure.Persistencia.Configuraciones;

// Conversaciones, trazas de procesos y auditoría (Sprints 2 y 3).

internal sealed class ConversacionConfiguracion : IEntityTypeConfiguration<Conversacion>
{
    public void Configure(EntityTypeBuilder<Conversacion> builder)
    {
        builder.ToTable("Conversacion");
        builder.HasKey(c => c.Id);
        builder.HasOne(c => c.Usuario).WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(c => c.Curso).WithMany().HasForeignKey(c => c.CursoId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class TrazaHerramientaConfiguracion : IEntityTypeConfiguration<TrazaHerramienta>
{
    public void Configure(EntityTypeBuilder<TrazaHerramienta> builder)
    {
        builder.ToTable("TrazaHerramienta");
        builder.HasKey(t => t.Id);
        builder.HasOne(t => t.Conversacion).WithMany(c => c.Trazas).HasForeignKey(t => t.ConversacionId);
        builder.Property(t => t.Herramienta).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Parametros).IsRequired();
        builder.Property(t => t.Resultado).IsRequired();
    }
}

internal sealed class AuditoriaConfiguracion : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        builder.ToTable("Auditoria");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.Fecha);
        builder.HasOne(a => a.Usuario).WithMany().HasForeignKey(a => a.UsuarioId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(a => a.Accion).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Entidad).HasMaxLength(250).IsRequired();
    }
}
