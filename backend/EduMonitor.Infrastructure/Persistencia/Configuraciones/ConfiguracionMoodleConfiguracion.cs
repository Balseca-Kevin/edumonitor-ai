using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMonitor.Infrastructure.Persistencia.Configuraciones;

// HU-20 · Una sola fila con la conexión con Moodle; el token va protegido con Data Protection.
internal sealed class ConfiguracionMoodleConfiguracion : IEntityTypeConfiguration<ConfiguracionMoodle>
{
    public void Configure(EntityTypeBuilder<ConfiguracionMoodle> builder)
    {
        builder.ToTable("ConfiguracionMoodle");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Url).HasMaxLength(500).IsRequired();
        builder.Property(c => c.TokenProtegido).IsRequired();
    }
}
