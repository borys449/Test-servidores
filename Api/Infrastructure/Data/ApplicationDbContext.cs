using Api.Domain.Entities;
using Api.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(builder =>
        {
            builder.ToTable("Usuarios");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Nombre).IsRequired().HasMaxLength(100);

            builder.Property(u => u.Apellido).IsRequired().HasMaxLength(100);

            // Mapear el Objeto de Valor de Vogen usando el EfCoreValueConverter autogenerado
            builder
                .Property(u => u.Email)
                .HasConversion(new Email.EfCoreValueConverter())
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.CreatedAt).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
