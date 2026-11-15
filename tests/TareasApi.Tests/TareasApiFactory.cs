using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TareasApi.Data;

namespace TareasApi.Tests;

/// <summary>
/// Levanta la API en memoria sustituyendo PostgreSQL por SQLite en memoria.
/// Cada instancia tiene su propia base de datos, así los tests no se pisan
/// y se pueden ejecutar sin tener Postgres instalado.
/// </summary>
public class TareasApiFactory : WebApplicationFactory<Program>
{
    // La BD en memoria de SQLite vive mientras la conexión esté abierta.
    private readonly SqliteConnection _conexion = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TareasDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TareasDbContext>>();

            _conexion.Open();
            services.AddDbContext<TareasDbContext>(o => o.UseSqlite(_conexion));

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<TareasDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conexion.Dispose();
    }
}
