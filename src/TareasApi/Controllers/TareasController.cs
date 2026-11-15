using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TareasApi.Data;
using TareasApi.Dtos;
using TareasApi.Models;

namespace TareasApi.Controllers;

[ApiController]
[Route("api/tareas")]
[Produces("application/json")]
public class TareasController(TareasDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TareaResponse>>> Listar(CancellationToken ct)
    {
        var tareas = await db.Tareas.AsNoTracking().OrderBy(t => t.Id).ToListAsync(ct);
        return Ok(tareas.Select(TareaResponse.From));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TareaResponse>> Obtener(int id, CancellationToken ct)
    {
        var tarea = await db.Tareas.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return tarea is null ? NotFound() : Ok(TareaResponse.From(tarea));
    }

    [HttpPost]
    public async Task<ActionResult<TareaResponse>> Crear(TareaRequest req, CancellationToken ct)
    {
        var tarea = new Tarea
        {
            Titulo = req.Titulo!.Trim(),
            Descripcion = req.Descripcion,
            Estado = req.Estado ?? EstadoTarea.Pendiente,
            Responsable = req.Responsable!.Trim(),
            FechaLimite = req.FechaLimite!.Value
        };
        db.Tareas.Add(tarea);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = tarea.Id }, TareaResponse.From(tarea));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TareaResponse>> Actualizar(int id, TareaRequest req, CancellationToken ct)
    {
        var tarea = await db.Tareas.FindAsync([id], ct);
        if (tarea is null) return NotFound();

        tarea.Titulo = req.Titulo!.Trim();
        tarea.Descripcion = req.Descripcion;
        tarea.Estado = req.Estado ?? tarea.Estado;
        tarea.Responsable = req.Responsable!.Trim();
        tarea.FechaLimite = req.FechaLimite!.Value;
        await db.SaveChangesAsync(ct);
        return Ok(TareaResponse.From(tarea));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Borrar(int id, CancellationToken ct)
    {
        var tarea = await db.Tareas.FindAsync([id], ct);
        if (tarea is null) return NotFound();

        db.Tareas.Remove(tarea);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
