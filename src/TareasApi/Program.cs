using Microsoft.EntityFrameworkCore;
using TareasApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TareasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Tareas")));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();

// Expuesto para los tests de integración (WebApplicationFactory<Program>).
public partial class Program;
