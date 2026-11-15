using Microsoft.EntityFrameworkCore;
using TareasApi.Models;

namespace TareasApi.Data;

public class TareasDbContext(DbContextOptions<TareasDbContext> options) : DbContext(options)
{
    public DbSet<Tarea> Tareas => Set<Tarea>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tarea>(e =>
        {
            e.ToTable("tareas");
            e.HasKey(t => t.Id);
            e.Property(t => t.Titulo).HasMaxLength(200).IsRequired();
            e.Property(t => t.Descripcion).HasMaxLength(2000);
            e.Property(t => t.Responsable).HasMaxLength(100).IsRequired();
            // Se guarda como texto para que la BD sea legible y no dependa del orden del enum.
            e.Property(t => t.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(t => t.FechaLimite).IsRequired();

            // Índices para los filtros del listado.
            e.HasIndex(t => t.Estado);
            e.HasIndex(t => t.Responsable);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var ahora = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Tarea>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreadaEn = ahora;
                entry.Entity.ActualizadaEn = ahora;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ActualizadaEn = ahora;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
