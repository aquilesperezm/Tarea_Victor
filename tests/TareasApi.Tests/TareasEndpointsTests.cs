using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TareasApi.Dtos;
using TareasApi.Models;

namespace TareasApi.Tests;

public class TareasEndpointsTests : IDisposable
{
    private const string Url = "/api/tareas";
    private readonly TareasApiFactory _factory = new();
    private readonly HttpClient _client;

    public TareasEndpointsTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    private static object NuevaTarea(string titulo = "Tarea", string estado = "pendiente",
        string responsable = "ana", string fecha = "2026-12-01") =>
        new { titulo, descripcion = "desc", estado, responsable, fechaLimite = fecha };

    private async Task<TareaResponse> CrearAsync(object tarea)
    {
        var resp = await _client.PostAsJsonAsync(Url, tarea);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<TareaResponse>())!;
    }

    // ---------- Crear ----------

    [Fact]
    public async Task Crear_TareaValida_Devuelve201ConLocationYLaTarea()
    {
        var resp = await _client.PostAsJsonAsync(Url, NuevaTarea(titulo: "Preparar demo", estado: "en_curso"));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var tarea = await resp.Content.ReadFromJsonAsync<TareaResponse>();
        Assert.NotNull(tarea);
        Assert.True(tarea.Id > 0);
        Assert.Equal("Preparar demo", tarea.Titulo);
        Assert.Equal(EstadoTarea.EnCurso, tarea.Estado);
        Assert.Equal(new DateOnly(2026, 12, 1), tarea.FechaLimite);
        Assert.EndsWith($"{Url}/{tarea.Id}", resp.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Crear_SinEstado_QuedaPendiente()
    {
        var tarea = await CrearAsync(new { titulo = "x", responsable = "ana", fechaLimite = "2026-12-01" });

        Assert.Equal(EstadoTarea.Pendiente, tarea.Estado);
    }

    [Fact]
    public async Task Crear_DatosInvalidos_Devuelve400ConErrorPorCampo()
    {
        var resp = await _client.PostAsJsonAsync(Url,
            new { titulo = "   ", responsable = "", fechaLimite = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("application/problem+json", resp.Content.Headers.ContentType?.MediaType);
        var errores = await LeerErroresAsync(resp);
        Assert.Contains("titulo", errores);
        Assert.Contains("responsable", errores);
        Assert.Contains("fechaLimite", errores);
    }

    [Theory]
    [InlineData("""{"titulo":"x","estado":"terminada","responsable":"ana","fechaLimite":"2026-12-01"}""", "estado")]
    [InlineData("""{"titulo":"x","responsable":"ana","fechaLimite":"2026-13-45"}""", "fechaLimite")]
    public async Task Crear_ValorConFormatoInvalido_Devuelve400SinDetallesInternos(string json, string campo)
    {
        var resp = await _client.PostAsync(Url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var errores = await LeerErroresAsync(resp);
        Assert.Equal([campo], errores.Keys);
        Assert.DoesNotContain("System.", errores[campo]);
    }

    // ---------- Ver / editar / borrar ----------

    [Fact]
    public async Task Obtener_TareaInexistente_Devuelve404()
    {
        var resp = await _client.GetAsync($"{Url}/9999");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Actualizar_TareaExistente_GuardaLosCambios()
    {
        var creada = await CrearAsync(NuevaTarea());

        var resp = await _client.PutAsJsonAsync($"{Url}/{creada.Id}",
            NuevaTarea(titulo: "Editada", estado: "hecha", responsable: "luis", fecha: "2027-01-15"));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var leida = await _client.GetFromJsonAsync<TareaResponse>($"{Url}/{creada.Id}");
        Assert.Equal("Editada", leida!.Titulo);
        Assert.Equal(EstadoTarea.Hecha, leida.Estado);
        Assert.Equal("luis", leida.Responsable);
        Assert.Equal(new DateOnly(2027, 1, 15), leida.FechaLimite);
        Assert.True(leida.ActualizadaEn >= creada.ActualizadaEn);
    }

    [Fact]
    public async Task Actualizar_TareaInexistente_Devuelve404()
    {
        var resp = await _client.PutAsJsonAsync($"{Url}/9999", NuevaTarea());

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Borrar_TareaExistente_Devuelve204YLuegoNoExiste()
    {
        var creada = await CrearAsync(NuevaTarea());

        var borrar = await _client.DeleteAsync($"{Url}/{creada.Id}");
        Assert.Equal(HttpStatusCode.NoContent, borrar.StatusCode);

        var otraVez = await _client.DeleteAsync($"{Url}/{creada.Id}");
        Assert.Equal(HttpStatusCode.NotFound, otraVez.StatusCode);
    }

    // ---------- Listado: filtros y paginación ----------

    [Fact]
    public async Task Listar_FiltraPorEstadoYResponsable()
    {
        await CrearAsync(NuevaTarea("A", "pendiente", "ana"));
        await CrearAsync(NuevaTarea("B", "hecha", "ana"));
        await CrearAsync(NuevaTarea("C", "pendiente", "Luis"));
        await CrearAsync(NuevaTarea("D", "en_curso", "luis"));

        var porEstado = await ListarAsync("?estado=pendiente");
        Assert.Equal(["A", "C"], porEstado.Items.Select(t => t.Titulo).Order());

        // Responsable sin distinguir mayúsculas.
        var porResponsable = await ListarAsync("?responsable=LUIS");
        Assert.Equal(["C", "D"], porResponsable.Items.Select(t => t.Titulo).Order());

        var combinados = await ListarAsync("?estado=en_curso&responsable=luis");
        Assert.Equal("D", Assert.Single(combinados.Items).Titulo);
        Assert.Equal(1, combinados.Total);
    }

    [Fact]
    public async Task Listar_PaginaLosResultados()
    {
        for (var i = 1; i <= 5; i++)
            await CrearAsync(NuevaTarea(titulo: $"T{i}", fecha: $"2026-12-0{i}"));

        var pagina2 = await ListarAsync("?page=2&pageSize=2");

        Assert.Equal(5, pagina2.Total);
        Assert.Equal(3, pagina2.TotalPages);
        Assert.Equal(2, pagina2.Page);
        Assert.Equal(["T3", "T4"], pagina2.Items.Select(t => t.Titulo));

        var ultima = await ListarAsync("?page=3&pageSize=2");
        Assert.Equal("T5", Assert.Single(ultima.Items).Titulo);
    }

    [Theory]
    [InlineData("?estado=terminada", "estado")]
    [InlineData("?page=0", "page")]
    [InlineData("?pageSize=101", "pageSize")]
    [InlineData("?page=abc", "page")]
    public async Task Listar_ParametrosInvalidos_Devuelve400(string query, string campo)
    {
        var resp = await _client.GetAsync(Url + query);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains(campo, await LeerErroresAsync(resp));
    }

    // ---------- Helpers ----------

    private async Task<PaginaResponse<TareaResponse>> ListarAsync(string query) =>
        (await _client.GetFromJsonAsync<PaginaResponse<TareaResponse>>(Url + query))!;

    private static async Task<Dictionary<string, string>> LeerErroresAsync(HttpResponseMessage resp)
    {
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("errors").EnumerateObject()
            .ToDictionary(p => p.Name, p => string.Join(" ", p.Value.EnumerateArray().Select(e => e.GetString())));
    }
}
