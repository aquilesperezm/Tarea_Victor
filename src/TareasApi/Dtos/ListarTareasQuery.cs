using System.ComponentModel.DataAnnotations;
using TareasApi.Models;

namespace TareasApi.Dtos;

/// <summary>Parámetros de GET /api/tareas: filtros opcionales y paginación.</summary>
public class ListarTareasQuery : IValidatableObject
{
    public const int PageSizeMaximo = 100;

    /// <summary>pendiente | en_curso | hecha</summary>
    public string? Estado { get; set; }

    /// <summary>Coincidencia exacta, sin distinguir mayúsculas.</summary>
    public string? Responsable { get; set; }

    // Tope para que (page - 1) * pageSize no desborde un int.
    [Range(1, 1_000_000, ErrorMessage = "page debe estar entre 1 y 1000000.")]
    public int Page { get; set; } = 1;

    [Range(1, PageSizeMaximo, ErrorMessage = "pageSize debe estar entre 1 y 100.")]
    public int PageSize { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Estado) && !EstadoTareaTexto.TryParse(Estado, out _))
            yield return new ValidationResult(
                $"Estado no válido. Valores permitidos: {EstadoTareaTexto.ValoresPermitidos}.", [nameof(Estado)]);
    }
}
