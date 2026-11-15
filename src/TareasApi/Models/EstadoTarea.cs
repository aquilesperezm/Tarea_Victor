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
