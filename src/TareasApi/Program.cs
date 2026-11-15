using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using TareasApi.Data;
using TareasApi.Errores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TareasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Tareas")));

builder.Services
    .AddControllers(options =>
    {
        // Las claves de error usan el nombre JSON (camelCase: "titulo") y no el de C# ("Titulo").
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
        // Mensaje en español cuando un parámetro de query no tiene el tipo esperado (?page=abc).
        options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor(
            (valor, _) => $"El valor '{valor}' no es válido.");
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = RespuestaValidacion.Crear;
    });

// Todas las respuestas de error (404, 500...) en formato ProblemDetails (RFC 9457).
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

// Excepciones no controladas -> 500 ProblemDetails, sin trazas internas en la respuesta.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();

// Expuesto para los tests de integración (WebApplicationFactory<Program>).
public partial class Program;
