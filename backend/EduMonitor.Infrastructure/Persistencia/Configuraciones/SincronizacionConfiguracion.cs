using System.Text.Json;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMonitor.Infrastructure.Persistencia.Configuraciones;

internal sealed class SincronizacionConfiguracion : IEntityTypeConfiguration<Sincronizacion>
{
    public void Configure(EntityTypeBuilder<Sincronizacion> builder)
    {
        builder.ToTable("Sincronizacion");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.CursoId);
        builder.HasOne(s => s.Curso).WithMany().HasForeignKey(s => s.CursoId);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(s => s.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Origen).HasMaxLength(250).IsRequired();

        // Errores como JSON en una columna de texto: [{ "Funcion": "...", "Mensaje": "..." }].
        builder.Property(s => s.Errores)
            .HasConversion(
                errores => JsonSerializer.Serialize(errores, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<ErrorSincronizacion>>(json, (JsonSerializerOptions?)null) ?? new List<ErrorSincronizacion>(),
                new ValueComparer<List<ErrorSincronizacion>>(
                    (a, b) => a!.SequenceEqual(b!),
                    errores => errores.Aggregate(0, (hash, e) => HashCode.Combine(hash, e.GetHashCode())),
                    errores => errores.ToList()))
            .IsRequired();
    }
}

/// <summary>TA-21.A1 · Celdas que cambiaron en cada sincronización. Se borran con su sincronización.</summary>
internal sealed class CambioSincronizacionConfiguracion : IEntityTypeConfiguration<CambioSincronizacion>
{
    public void Configure(EntityTypeBuilder<CambioSincronizacion> builder)
    {
        builder.ToTable("CambioSincronizacion");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.SincronizacionId);
        builder.HasOne(c => c.Sincronizacion).WithMany().HasForeignKey(c => c.SincronizacionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.Actividad).WithMany().HasForeignKey(c => c.ActividadId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(c => c.Estudiante).WithMany().HasForeignKey(c => c.EstudianteId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(c => c.Tipo).HasMaxLength(20).IsRequired();
        builder.Property(c => c.NotaAntes).HasPrecision(9, 5);
        builder.Property(c => c.NotaAhora).HasPrecision(9, 5);
        builder.Property(c => c.EstadoAntes).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.EstadoAhora).HasConversion<string>().HasMaxLength(20);
    }
}
