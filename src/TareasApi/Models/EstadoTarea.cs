using System.Text.Json.Serialization;

namespace TareasApi.Models;

// En la API viaja como texto: "pendiente", "en_curso", "hecha".
[JsonConverter(typeof(JsonStringEnumConverter<EstadoTarea>))]
public enum EstadoTarea
{
    [JsonStringEnumMemberName("pendiente")] Pendiente,
    [JsonStringEnumMemberName("en_curso")] EnCurso,
    [JsonStringEnumMemberName("hecha")] Hecha
}

public static class EstadoTareaTexto
{
    public const string ValoresPermitidos = "pendiente, en_curso, hecha";

    /// <summary>
    /// Convierte el texto de la API a enum. Se usa en la query string, donde el
    /// model binding no conoce los nombres JSON del enum.
    /// </summary>
    public static bool TryParse(string? texto, out EstadoTarea estado)
    {
        switch (texto?.Trim().ToLowerInvariant())
        {
            case "pendiente": estado = EstadoTarea.Pendiente; return true;
            case "en_curso": estado = EstadoTarea.EnCurso; return true;
            case "hecha": estado = EstadoTarea.Hecha; return true;
            default: estado = default; return false;
        }
    }
}
