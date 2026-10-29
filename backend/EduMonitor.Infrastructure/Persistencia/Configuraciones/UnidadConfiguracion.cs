using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMonitor.Infrastructure.Persistencia.Configuraciones;

internal sealed class UnidadConfiguracion : IEntityTypeConfiguration<Unidad>
{
    public void Configure(EntityTypeBuilder<Unidad> builder)
    {
        builder.ToTable("Unidad");
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.MoodleSeccionId).IsUnique();
        builder.HasOne(u => u.Curso).WithMany().HasForeignKey(u => u.CursoId);
        builder.Property(u => u.Nombre).HasMaxLength(250).IsRequired();
    }
}
