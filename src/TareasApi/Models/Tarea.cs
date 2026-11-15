namespace TareasApi.Models;

public class Tarea
{
    public int Id { get; set; }
    public required string Titulo { get; set; }
    public string? Descripcion { get; set; }
    public EstadoTarea Estado { get; set; } = EstadoTarea.Pendiente;
    public required string Responsable { get; set; }
    public DateOnly FechaLimite { get; set; }
    public DateTime CreadaEn { get; set; }
    public DateTime ActualizadaEn { get; set; }
}
