# API de Tareas

API REST para gestionar tareas: crear, listar (con filtros y paginación), ver, editar y borrar.
Hecha con **.NET 10 (ASP.NET Core)**, **EF Core** y **PostgreSQL**.

Cada tarea tiene: `titulo`, `descripcion`, `estado` (`pendiente` | `en_curso` | `hecha`), `responsable` y `fechaLimite`, además de `id`, `creadaEn` y `actualizadaEn`, que gestiona el servidor.

---

## Cómo ejecutarlo

### Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- PostgreSQL (probado con la 16)
- La herramienta de migraciones de EF Core: `dotnet tool install --global dotnet-ef`

### 1. Configurar la conexión

La cadena de conexión con la contraseña **no está en el repositorio**: `appsettings.Development.json` está en `.gitignore`.
Crea el tuyo a partir de la plantilla y pon tu usuario y contraseña de Postgres:

```bash
cp src/TareasApi/appsettings.Development.example.json src/TareasApi/appsettings.Development.json
```

```json
"ConnectionStrings": {
  "Tareas": "Host=localhost;Port=5432;Database=tareas;Username=postgres;Password=TU_PASSWORD"
}
```

### 2. Crear la base de datos (migraciones)

```bash
dotnet ef database update --project src/TareasApi
```

Este comando crea la base de datos `tareas` si no existe y aplica las migraciones de `src/TareasApi/Data/Migrations`.

### 3. Arrancar la API

```bash
dotnet run --project src/TareasApi --launch-profile http
```

La API queda en `http://localhost:5296/api/tareas`, y el documento OpenAPI en `http://localhost:5296/openapi/v1.json`.
En [src/TareasApi/TareasApi.http](src/TareasApi/TareasApi.http) hay peticiones de ejemplo de todas las operaciones, que se pueden lanzar desde VS Code (extensión REST Client) o Visual Studio.

### 4. Tests

```bash
dotnet test
```

Los tests no necesitan PostgreSQL, porque usan SQLite en memoria (ver *Decisiones*).

---

## Endpoints

| Método | Ruta | Respuestas |
|---|---|---|
| `POST` | `/api/tareas` | `201` + cabecera `Location` · `400` |
| `GET` | `/api/tareas?estado=&responsable=&page=1&pageSize=10` | `200` · `400` |
| `GET` | `/api/tareas/{id}` | `200` · `404` |
| `PUT` | `/api/tareas/{id}` | `200` · `400` · `404` |
| `DELETE` | `/api/tareas/{id}` | `204` · `404` |

Ejemplo de cuerpo de `POST` o `PUT`:

```json
{
  "titulo": "Preparar demo",
  "descripcion": "Slides y entorno de pruebas",
  "estado": "en_curso",
  "responsable": "ana",
  "fechaLimite": "2026-12-01"
}
```

Respuesta del listado:

```json
{ "items": [ ... ], "total": 5, "page": 2, "pageSize": 2, "totalPages": 3 }
```

Ejemplo de error de validación (`400`, `application/problem+json`):

```json
{
  "title": "La petición contiene datos no válidos.",
  "status": 400,
  "errors": {
    "titulo": ["El título es obligatorio."],
    "estado": ["Estado no válido. Valores permitidos: pendiente, en_curso, hecha."]
  }
}
```

### Reglas de validación

- `titulo`: obligatorio, entre 1 y 200 caracteres. Un título hecho solo de espacios no es válido.
- `responsable`: obligatorio, entre 1 y 100 caracteres.
- `fechaLimite`: obligatoria, con formato `yyyy-MM-dd`.
- `descripcion`: opcional, hasta 2000 caracteres.
- `estado`: opcional al crear (por defecto `pendiente`). Solo admite `pendiente`, `en_curso` o `hecha`.
- Listado: `page` ≥ 1 y `pageSize` entre 1 y 100.

---

## Estructura

```
src/TareasApi/
  Controllers/TareasController.cs   endpoints
  Models/                           entidad Tarea y enum EstadoTarea
  Dtos/                             contratos de entrada/salida y parámetros del listado
  Data/                             DbContext y migraciones
  Errores/RespuestaValidacion.cs    formato de los errores 400
tests/TareasApi.Tests/              tests de integración
```

---

## Decisiones que tomé

- **Controllers + EF Core en lugar de capas extra (repositorio/servicio).** Para un CRUD de una sola entidad, el `DbContext` ya funciona como repositorio y unidad de trabajo. Añadir capas solo habría añadido código sin aportar nada. Si apareciera lógica de negocio real, la sacaría a un servicio.
- **DTOs separados de la entidad.** El cliente no puede enviar `id`, `creadaEn` ni `actualizadaEn`, y la API no queda atada al esquema de la base de datos.
- **Estado como enum.** En JSON viaja como `pendiente` / `en_curso` / `hecha`, y en la base de datos se guarda como **texto** en lugar de entero, para que la tabla se entienda al consultarla y no dependa del orden del enum.
- **Errores en formato `ProblemDetails` (RFC 9457).** Es el estándar de ASP.NET Core. Personalicé la respuesta 400 por tres motivos:
  - los mensajes por defecto de un JSON con un valor no válido exponen tipos internos de .NET (`System.Nullable`1[...]`);
  - ASP.NET añade además un error duplicado ("the req field is required");
  - las claves salían como `Titulo` en vez de `titulo`.
  Ahora los mensajes están en español y las claves usan el nombre del campo en JSON. Las excepciones no controladas devuelven `500` sin trazas.
- **`PUT` es un reemplazo completo.** Hay que enviar todos los campos obligatorios, y si no se envía `descripcion`, queda vacía. La única excepción es `estado`: si se omite, se conserva el actual. No implementé `PATCH`.
- **Filtros.** `responsable` busca la coincidencia exacta sin distinguir mayúsculas (`ana` = `Ana`). El filtro `estado` se interpreta a mano porque el *model binding* de la query string no reconoce los nombres JSON del enum (solo aceptaría `EnCurso`, no `en_curso`).
- **Paginación por `page`/`pageSize`** con `total` y `totalPages`, ordenada por fecha límite y luego por id, para que el orden sea estable y no se repitan ni se salten elementos entre páginas. `pageSize` tiene un máximo de 100.
- **Se permite una fecha límite en el pasado.** Así se pueden registrar o editar tareas que ya están atrasadas. Es una regla de negocio fácil de cambiar si el cliente la quiere al revés.
- **El responsable es texto libre.** El enunciado no pide gestionar usuarios.
- **Las migraciones se aplican con un comando explícito** (`dotnet ef database update`) y no al arrancar la API, para no modificar el esquema de la base de datos sin que nadie lo decida.
- **Tests de integración con `WebApplicationFactory` y SQLite en memoria.** Recorren la API completa (enrutado, validación, serialización y base de datos), se ejecutan sin instalar nada y cada test tiene su propia base de datos. La contrapartida es que SQLite no es PostgreSQL: con más tiempo usaría Testcontainers con un Postgres real (ver abajo).
- **La contraseña no se sube al repositorio.** `appsettings.Development.json` está ignorado por git y se incluye una plantilla `.example`.

---

## Qué haría con más tiempo

- **Tests contra PostgreSQL real** con Testcontainers, para cubrir diferencias de SQL entre motores, además de tests unitarios de la validación.
- **Autenticación y autorización** (por ejemplo, JWT), y que el responsable fuera un usuario real en lugar de un texto.
- **`PATCH`**, para cambiar solo el estado sin enviar la tarea completa.
- **Más opciones de listado:** ordenación configurable, búsqueda por texto en el título y filtro por rango de fechas.
- **Concurrencia optimista** (ETag / `xmin` de Postgres), para que dos ediciones simultáneas no se pisen.
- **Docker Compose** (API + Postgres) y **CI** (GitHub Actions) que ejecute build y tests en cada push.
- **Swagger UI** o Scalar sobre el OpenAPI que ya se genera, y logging estructurado.
- Traducir también los títulos genéricos de los errores 404 y 500.

---

## Cómo usé la IA

Trabajé con **Claude Code** (asistente de programación en el editor) durante todo el desarrollo.

**Qué le pedí**
- Primero, que me propusiera un plan **sin escribir código**: stack, diseño de endpoints, formato de errores, tests y cómo organizar los commits para que se viera el progreso.
- Elegí .NET entre las opciones que propuso y le pedí que implementara el plan paso a paso, haciendo un commit por cada paso.
- Le pedí que no subiera la contraseña de la base de datos al repositorio.

**Qué acepté**
- La estructura general (controllers + EF Core, sin capas extra), los DTOs, el formato `ProblemDetails` y el uso de SQLite en memoria para los tests.
- La secuencia de commits: estructura → modelo → migración → CRUD → validación → filtros y paginación → tests → README.

**Qué se corrigió por el camino**

Estos problemas no se veían leyendo el código generado. Salieron al compilar, al probar la API con peticiones reales y al ejecutar los tests:
- Por defecto se instalaba `Microsoft.EntityFrameworkCore.Design` **11**, incompatible con .NET 10. Se fijó la versión 10.0.4.
- La plantilla de .NET traía `Microsoft.OpenApi` 2.0.0, con una **vulnerabilidad conocida**. Se actualizó a 2.13.0.
- Al enviar un estado o una fecha no válidos, el 400 mostraba **tipos internos de .NET** y un error duplicado. Se escribió la fábrica de respuestas de `Errores/RespuestaValidacion.cs`.
- Las claves de error de la query string salían en PascalCase (`Page`) y en inglés. Se normalizaron.
- El filtro `?estado=en_curso` no funcionaba con el *binding* por defecto. Se cambió a un parseo propio.
- `page` muy grande podía desbordar el cálculo de `Skip`. Se puso un tope.
- **Un test detectó** que el atributo `[Produces("application/json")]` hacía que los errores salieran como `application/json` en lugar de `application/problem+json`. Se eliminó el atributo.

**Mi criterio:** la IA acelera mucho el código repetitivo y la configuración, pero cada paso se comprobó lanzando peticiones reales contra la API y con los tests. Varias de las correcciones anteriores solo aparecieron al probar, no al leer el código.
