using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMonitor.Infrastructure.Persistencia.Configuraciones;

// Conversaciones con el agente, trazas de herramientas y auditoría (Sprints 2 y 3).

internal sealed class ConversacionConfiguracion : IEntityTypeConfiguration<Conversacion>
{
    public void Configure(EntityTypeBuilder<Conversacion> builder)
    {
        builder.ToTable("Conversacion");
        builder.HasKey(c => c.Id);
        builder.HasOne(c => c.Usuario).WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(c => c.Curso).WithMany().HasForeignKey(c => c.CursoId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(c => c.Pregunta).HasMaxLength(500).IsRequired();
        builder.Property(c => c.Respuesta).IsRequired();
        builder.Property(c => c.Modelo).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Proveedor).HasMaxLength(20).IsRequired();
        builder.HasIndex(c => c.HiloId);
    }
}

internal sealed class ConfiguracionAsistenteConfiguracion : IEntityTypeConfiguration<ConfiguracionAsistente>
{
    public void Configure(EntityTypeBuilder<ConfiguracionAsistente> builder)
    {
        builder.ToTable("ConfiguracionAsistente");
        builder.HasKey(c => c.Id);
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
        builder.Property(t => t.Estado).HasMaxLength(20).IsRequired();
    }
}

internal sealed class AuditoriaConfiguracion : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        builder.ToTable("Auditoria");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.Fecha);
        builder.HasIndex(a => a.Accion);
        builder.HasOne(a => a.Usuario).WithMany().HasForeignKey(a => a.UsuarioId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        builder.Property(a => a.Accion).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Entidad).HasMaxLength(250).IsRequired();
        builder.Property(a => a.Detalle).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Resultado).HasMaxLength(10).IsRequired();
    }
}
