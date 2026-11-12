using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMonitor.Infrastructure.Persistencia.Configuraciones;

// Mapeo temático (Sprint 2). Las llaves foráneas sin cascada evitan que SQL Server vea dos caminos de borrado desde Curso.

internal sealed class TemaConfiguracion : IEntityTypeConfiguration<Tema>
{
    public void Configure(EntityTypeBuilder<Tema> builder)
    {
        builder.ToTable("Tema");
        builder.HasKey(t => t.Id);
        builder.HasOne(t => t.Unidad).WithMany().HasForeignKey(t => t.UnidadId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(t => t.Nombre).HasMaxLength(250).IsRequired();
    }
}

internal sealed class ContenidoMinimoConfiguracion : IEntityTypeConfiguration<ContenidoMinimo>
{
    public void Configure(EntityTypeBuilder<ContenidoMinimo> builder)
    {
        builder.ToTable("ContenidoMinimo");
        builder.HasKey(c => c.Id);
        builder.HasOne(c => c.Tema).WithMany(t => t.ContenidosMinimos).HasForeignKey(c => c.TemaId);
        builder.Property(c => c.Descripcion).HasMaxLength(1000).IsRequired();
    }
}

internal sealed class ActividadTemaConfiguracion : IEntityTypeConfiguration<ActividadTema>
{
    public void Configure(EntityTypeBuilder<ActividadTema> builder)
    {
        builder.ToTable("ActividadTema");
        builder.HasKey(at => new { at.ActividadId, at.TemaId });
        builder.HasOne(at => at.Actividad).WithMany().HasForeignKey(at => at.ActividadId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(at => at.Tema).WithMany().HasForeignKey(at => at.TemaId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(at => at.PesoRelativo).HasPrecision(9, 5);
    }
}
