using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMonitor.Infrastructure.Persistencia.Configuraciones;

internal sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuario");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.NombreUsuario).HasMaxLength(50).IsRequired();
        builder.HasIndex(u => u.NombreUsuario).IsUnique();
        builder.Property(u => u.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Correo).HasMaxLength(150);
        builder.Property(u => u.HashContrasena).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20).IsRequired();
    }
}
