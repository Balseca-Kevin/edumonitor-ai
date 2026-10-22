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
