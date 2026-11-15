using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TareasApi.Errores;

/// <summary>
/// Construye la respuesta 400 (ProblemDetails) cuando falla la validación del modelo.
/// Sustituye los mensajes internos de System.Text.Json (que exponen tipos de .NET)
/// por mensajes legibles y en español.
/// </summary>
public static class RespuestaValidacion
{
    private static readonly Dictionary<string, string> MensajesDeFormato = new(StringComparer.OrdinalIgnoreCase)
    {
        ["estado"] = "Estado no válido. Valores permitidos: pendiente, en_curso, hecha.",
        ["fechaLimite"] = "Fecha no válida. Usa el formato yyyy-MM-dd.",
    };

    public static IActionResult Crear(ActionContext context)
    {
        var errores = new Dictionary<string, string[]>();
        var hayErroresDeJson = context.ModelState.Keys.Any(k => k.StartsWith('$'));

        foreach (var (clave, entrada) in context.ModelState)
        {
            if (entrada.ValidationState != ModelValidationState.Invalid) continue;

            if (clave.StartsWith('$'))
            {
                // Error de deserialización: "$.estado" -> "estado"; "$" -> cuerpo entero.
                var campo = clave.TrimStart('$', '.');
                if (campo.Length == 0)
                    errores["body"] = ["El cuerpo de la petición no es un JSON válido."];
                else
                    errores[campo] = [MensajesDeFormato.GetValueOrDefault(campo, "Valor con formato no válido o JSON mal formado.")];
            }
            else if (clave.Length == 0 || EsParametroDelCuerpo(context, clave))
            {
                // Si el JSON no se pudo leer, ASP.NET añade además "el parámetro es obligatorio":
                // es ruido cuando ya informamos del error real.
                if (!hayErroresDeJson)
                    errores["body"] = ["El cuerpo de la petición es obligatorio."];
            }
            else
            {
                // Parámetros de query/ruta llegan con el nombre de C# ("Page"): se normalizan a camelCase.
                errores[JsonNamingPolicy.CamelCase.ConvertName(clave)] =
                    entrada.Errors.Select(e => e.ErrorMessage).ToArray();
            }
        }

        var problema = new ValidationProblemDetails(errores)
        {
            Title = "La petición contiene datos no válidos.",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Instance = context.HttpContext.Request.Path
        };
        return new BadRequestObjectResult(problema) { ContentTypes = { "application/problem+json" } };
    }

    private static bool EsParametroDelCuerpo(ActionContext context, string clave) =>
        context.ActionDescriptor.Parameters.Any(p =>
            p.BindingInfo?.BindingSource == BindingSource.Body && p.Name == clave);
}
