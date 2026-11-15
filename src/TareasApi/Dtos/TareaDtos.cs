using System.ComponentModel.DataAnnotations;
using TareasApi.Models;

namespace TareasApi.Dtos;

/// <summary>Cuerpo para crear o reemplazar una tarea (POST y PUT).</summary>
public class TareaRequest
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "El título debe tener entre 1 y 200 caracteres.")]
    public string? Titulo { get; set; }

    [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    public string? Descripcion { get; set; }

    // Opcional al crear (por defecto "pendiente"); nullable para detectar si viene.
    public EstadoTarea? Estado { get; set; }

    [Required(ErrorMessage = "El responsable es obligatorio.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "El responsable debe tener entre 1 y 100 caracteres.")]
    public string? Responsable { get; set; }

    [Required(ErrorMessage = "La fecha límite es obligatoria.")]
    public DateOnly? FechaLimite { get; set; }
}

public record TareaResponse(
    int Id,
    string Titulo,
    string? Descripcion,
    EstadoTarea Estado,
    string Responsable,
    DateOnly FechaLimite,
    DateTime CreadaEn,
    DateTime ActualizadaEn)
{
    public static TareaResponse From(Tarea t) => new(
        t.Id, t.Titulo, t.Descripcion, t.Estado, t.Responsable, t.FechaLimite, t.CreadaEn, t.ActualizadaEn);
}

public record PaginaResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize, int TotalPages);
