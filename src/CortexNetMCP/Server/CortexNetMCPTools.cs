using ModelContextProtocol.Server;
using System.ComponentModel;
using CortexNetMCP.Data;
using CortexNetMCP.DTOs;

namespace CortexNetMCP.Server;

/// <summary>
/// Herramientas MCP para el sistema de memoria técnica CortexNet.
///
/// Categorías válidas   : architecture | bug | decision | entity | endpoint |
///                        feature | task | pattern | lesson
/// Tipos de relación    : related_to | supersedes | depends_on | fixes | references
/// </summary>
[McpServerToolType]
public sealed class CortexNetMCPTools
{
    private readonly MemoryRepository _repo;

    private static readonly HashSet<string> ValidCategories =
    [
        "architecture", "bug", "decision", "entity",
        "endpoint", "feature", "task", "pattern", "lesson"
    ];

    private static readonly HashSet<string> ValidRelationTypes =
    [
        "related_to", "supersedes", "depends_on", "fixes", "references"
    ];

    public CortexNetMCPTools(MemoryRepository repository)
    {
        _repo = repository;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. GuardarRecuerdo
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Guarda un nuevo recuerdo técnico en la base de datos de memoria a largo plazo. " +
        "Úsalo para persistir conocimiento, decisiones de arquitectura, bugs resueltos, " +
        "patrones descubiertos o cualquier información técnica relevante. " +
        "Retorna SaveMemoryResult con el ID asignado, proyecto y categoría.")]
    public SaveMemoryResult GuardarRecuerdo(
        [Description("Nombre del proyecto (ej. 'NegocioNube', 'Fixtureando').")] string proyecto,
        [Description("Categoría del recuerdo. Valores válidos: architecture, bug, decision, entity, endpoint, feature, task, pattern, lesson.")] string categoria,
        [Description("Título breve y descriptivo del recuerdo (ej. 'Fix CORS en producción').")] string titulo,
        [Description("Contenido detallado: descripción completa, causa raíz, solución, contexto, etc.")] string contenido,
        [Description("Etiquetas separadas por comas para facilitar la búsqueda (ej. 'cors, nginx, produccion').")] string? tags = null,
        [Description("Rutas de archivos relacionados separadas por comas (ej. 'src/auth/jwt.service.ts, appsettings.json').")] string? filePaths = null)
    {
        var cat = categoria.Trim().ToLowerInvariant();
        if (!ValidCategories.Contains(cat))
            return new SaveMemoryResult
            {
                Success  = false,
                Message  = $"Categoría '{categoria}' no es válida. Opciones: {string.Join(", ", ValidCategories)}.",
                MemoryId = 0,
                Project  = proyecto,
                Category = cat,
            };

        try
        {
            var id = _repo.InsertMemory(proyecto.Trim(), cat, titulo.Trim(), contenido, tags, filePaths);
            return new SaveMemoryResult
            {
                Success  = true,
                Message  = $"Recuerdo #{id} guardado correctamente.",
                MemoryId = id,
                Project  = proyecto.Trim(),
                Category = cat,
            };
        }
        catch (Exception ex)
        {
            return new SaveMemoryResult
            {
                Success  = false,
                Message  = $"No se pudo guardar el recuerdo: {ex.Message}",
                MemoryId = 0,
                Project  = proyecto,
                Category = cat,
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. BuscarRecuerdos  (FTS5 — búsqueda inteligente)
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Busca recuerdos usando FTS5 (full-text search) sobre título, contenido, tags, " +
        "categoría y rutas de archivos. Soporta múltiples palabras separadas por espacios. " +
        "Filtra opcionalmente por proyecto. Resultados ordenados por relevancia. " +
        "Retorna SearchMemoriesResult con la lista de MemoryDto encontrados.")]
    public SearchMemoriesResult BuscarRecuerdos(
        [Description("Palabras clave a buscar (ej. 'AFIP autenticacion token', 'cors nginx'). Soporta múltiples términos.")] string textoBusqueda,
        [Description("Opcional: nombre del proyecto para limitar la búsqueda a un proyecto específico.")] string? proyecto = null)
    {
        if (string.IsNullOrWhiteSpace(textoBusqueda))
            return new SearchMemoriesResult
            {
                Success      = false,
                Message      = "El texto de búsqueda no puede estar vacío.",
                Query        = string.Empty,
                TotalResults = 0,
                Memories     = [],
            };

        try
        {
            var records = _repo.SearchMemories(proyecto?.Trim(), textoBusqueda.Trim());
            var dtos    = records.Select(r => r.ToDto()).ToList();
            return new SearchMemoriesResult
            {
                Success      = true,
                Message      = $"{dtos.Count} resultado(s) encontrado(s).",
                Query        = textoBusqueda.Trim(),
                TotalResults = dtos.Count,
                Memories     = dtos,
            };
        }
        catch (Exception ex)
        {
            return new SearchMemoriesResult
            {
                Success      = false,
                Message      = $"Fallo en la búsqueda: {ex.Message}",
                Query        = textoBusqueda,
                TotalResults = 0,
                Memories     = [],
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. ObtenerRecuerdosPorCategoria
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Obtiene todos los recuerdos de un proyecto filtrados por categoría. " +
        "Ideal para listar todas las decisiones de arquitectura o bugs resueltos de un proyecto. " +
        "Retorna CategoryMemoriesResult con la lista de MemoryDto.")]
    public CategoryMemoriesResult ObtenerRecuerdosPorCategoria(
        [Description("Nombre del proyecto.")] string proyecto,
        [Description("Categoría a filtrar. Valores válidos: architecture, bug, decision, entity, endpoint, feature, task, pattern, lesson.")] string categoria)
    {
        var cat = categoria.Trim().ToLowerInvariant();
        try
        {
            var records = _repo.GetByCategory(proyecto.Trim(), cat);
            var dtos    = records.Select(r => r.ToDto()).ToList();
            return new CategoryMemoriesResult
            {
                Success      = true,
                Message      = $"{dtos.Count} recuerdo(s) en categoría '{cat}'.",
                Category     = cat,
                TotalResults = dtos.Count,
                Memories     = dtos,
            };
        }
        catch (Exception ex)
        {
            return new CategoryMemoriesResult
            {
                Success      = false,
                Message      = $"Error al obtener recuerdos: {ex.Message}",
                Category     = cat,
                TotalResults = 0,
                Memories     = [],
            };
        }
    }
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Obtiene el contenido completo de un recuerdo a partir de su ID numérico. " +
        "Úsalo cuando ya conoces el ID y necesitas todos los detalles. " +
        "Retorna MemoryResult con el MemoryDto completo.")]
    public MemoryResult ObtenerRecuerdoPorId(
        [Description("El ID numérico del recuerdo (obtenido al guardar o al buscar).")] long id)
    {
        try
        {
            var record = _repo.GetById(id);
            if (record is null)
                return new MemoryResult
                {
                    Success = false,
                    Message = $"No existe un recuerdo con ID {id}.",
                    Memory  = null,
                };
            return new MemoryResult
            {
                Success = true,
                Message = $"Recuerdo #{id} recuperado correctamente.",
                Memory  = record.ToDto(),
            };
        }
        catch (Exception ex)
        {
            return new MemoryResult
            {
                Success = false,
                Message = $"Error al recuperar recuerdo #{id}: {ex.Message}",
                Memory  = null,
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. ActualizarRecuerdo
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Actualiza el título, contenido, tags y rutas de archivos de un recuerdo existente. " +
        "El campo UpdatedAt se actualiza automáticamente. " +
        "Retorna UpdateMemoryResult con los datos actualizados.")]
    public UpdateMemoryResult ActualizarRecuerdo(
        [Description("El ID numérico del recuerdo a actualizar.")] long id,
        [Description("Nuevo título del recuerdo.")] string titulo,
        [Description("Nuevo contenido completo del recuerdo (reemplaza el anterior).")] string contenido,
        [Description("Nuevas etiquetas separadas por comas (reemplaza las anteriores). Omitir para borrar.")] string? tags = null,
        [Description("Nuevas rutas de archivos separadas por comas (reemplaza las anteriores). Omitir para borrar.")] string? filePaths = null)
    {
        try
        {
            var updated = _repo.UpdateMemory(id, titulo.Trim(), contenido, tags, filePaths);
            if (!updated)
                return new UpdateMemoryResult
                {
                    Success   = false,
                    Message   = $"No existe un recuerdo con ID {id}.",
                    MemoryId  = id,
                    Title     = titulo,
                    Tags      = tags,
                    FilePaths = filePaths,
                };
            return new UpdateMemoryResult
            {
                Success   = true,
                Message   = $"Recuerdo #{id} actualizado correctamente.",
                MemoryId  = id,
                Title     = titulo.Trim(),
                Tags      = tags,
                FilePaths = filePaths,
            };
        }
        catch (Exception ex)
        {
            return new UpdateMemoryResult
            {
                Success   = false,
                Message   = $"Error al actualizar recuerdo #{id}: {ex.Message}",
                MemoryId  = id,
                Title     = titulo,
                Tags      = tags,
                FilePaths = filePaths,
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. EliminarRecuerdo
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Elimina permanentemente un recuerdo y todas sus relaciones usando su ID único. " +
        "Usar cuando la información está completamente obsoleta y ya no tiene valor. " +
        "Retorna DeleteMemoryResult con confirmación.")]
    public DeleteMemoryResult EliminarRecuerdo(
        [Description("El ID numérico del recuerdo a eliminar.")] long id)
    {
        try
        {
            var deleted = _repo.DeleteMemory(id);
            return new DeleteMemoryResult
            {
                Success  = deleted,
                Message  = deleted
                    ? $"Recuerdo #{id} y sus relaciones han sido eliminados permanentemente."
                    : $"No existe un recuerdo con ID {id}.",
                MemoryId = id,
            };
        }
        catch (Exception ex)
        {
            return new DeleteMemoryResult
            {
                Success  = false,
                Message  = $"Error al eliminar recuerdo #{id}: {ex.Message}",
                MemoryId = id,
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. RelacionarRecuerdos
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Crea una relación semántica entre dos recuerdos. " +
        "Tipos válidos: related_to (relacionado), supersedes (reemplaza), " +
        "depends_on (depende de), fixes (soluciona), references (referencia). " +
        "Retorna RelationResult con el ID de la relación creada.")]
    public RelationResult RelacionarRecuerdos(
        [Description("ID del recuerdo origen (el que apunta).")] long sourceMemoryId,
        [Description("ID del recuerdo destino (al que se apunta).")] long targetMemoryId,
        [Description("Tipo de relación. Valores válidos: related_to, supersedes, depends_on, fixes, references.")] string relationType)
    {
        if (sourceMemoryId == targetMemoryId)
            return new RelationResult
            {
                Success        = false,
                Message        = "Un recuerdo no puede relacionarse consigo mismo.",
                RelationId     = 0,
                SourceMemoryId = sourceMemoryId,
                TargetMemoryId = targetMemoryId,
                RelationType   = relationType,
            };

        var rel = relationType.Trim().ToLowerInvariant();
        if (!ValidRelationTypes.Contains(rel))
            return new RelationResult
            {
                Success        = false,
                Message        = $"Tipo '{relationType}' no válido. Opciones: {string.Join(", ", ValidRelationTypes)}.",
                RelationId     = 0,
                SourceMemoryId = sourceMemoryId,
                TargetMemoryId = targetMemoryId,
                RelationType   = rel,
            };

        try
        {
            var id = _repo.InsertRelation(sourceMemoryId, targetMemoryId, rel);
            return new RelationResult
            {
                Success        = true,
                Message        = $"Relación #{id} creada: #{sourceMemoryId} --[{rel}]--> #{targetMemoryId}.",
                RelationId     = id,
                SourceMemoryId = sourceMemoryId,
                TargetMemoryId = targetMemoryId,
                RelationType   = rel,
            };
        }
        catch (Exception ex)
        {
            return new RelationResult
            {
                Success        = false,
                Message        = $"Error al crear relación: {ex.Message}",
                RelationId     = 0,
                SourceMemoryId = sourceMemoryId,
                TargetMemoryId = targetMemoryId,
                RelationType   = rel,
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 8. ObtenerRecuerdosRelacionados
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Obtiene todos los recuerdos relacionados a uno dado, en ambas direcciones " +
        "(salientes y entrantes). Útil para navegar el grafo de conocimiento. " +
        "Retorna RelatedMemoriesResult con la lista de RelatedMemoryDto.")]
    public RelatedMemoriesResult ObtenerRecuerdosRelacionados(
        [Description("El ID del recuerdo del que se desean conocer sus relaciones.")] long memoryId)
    {
        try
        {
            var relations = _repo.GetRelatedMemories(memoryId);
            var dtos = relations.Select(t => new RelatedMemoryDto
            {
                MemoryId     = t.Memory.Id,
                Project      = t.Memory.Project,
                Category     = t.Memory.Category,
                Title        = t.Memory.Title,
                Tags         = t.Memory.Tags,
                RelationType = t.RelationType,
                Direction    = t.Direction,
            }).ToList();
            return new RelatedMemoriesResult
            {
                Success         = true,
                Message         = $"{dtos.Count} relación(es) encontrada(s) para recuerdo #{memoryId}.",
                MemoryId        = memoryId,
                TotalResults    = dtos.Count,
                RelatedMemories = dtos,
            };
        }
        catch (Exception ex)
        {
            return new RelatedMemoriesResult
            {
                Success         = false,
                Message         = $"Error al obtener relaciones: {ex.Message}",
                MemoryId        = memoryId,
                TotalResults    = 0,
                RelatedMemories = [],
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 9. RecordarContextoProyecto
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Recupera automáticamente el conocimiento relevante de un proyecto antes de realizar una tarea. " +
        "Analiza la descripción de la tarea y busca en Title, Content, Tags, Category y FilePaths " +
        "usando FTS5 con ranking bm25(). Devuelve los recuerdos más útiles ordenados por relevancia " +
        "descendente para que el agente tome decisiones informadas, reutilice soluciones previas " +
        "y evite repetir bugs ya resueltos. Retorna ProjectContextResult con RelevantMemoryDto[].")]
    public ProjectContextResult RecordarContextoProyecto(
        [Description("Nombre del proyecto sobre el que se ejecuta la tarea.")] string proyecto,
        [Description("Descripción de la tarea actual que el agente va a realizar.")] string tareaActual)
    {
        if (string.IsNullOrWhiteSpace(tareaActual))
            return new ProjectContextResult
            {
                Success          = false,
                Message          = "La descripción de la tarea no puede estar vacía.",
                Project          = proyecto,
                TaskDescription  = string.Empty,
                TotalResults     = 0,
                RelevantMemories = [],
            };

        try
        {
            var scored = _repo.SearchMemoriesWithScore(proyecto.Trim(), tareaActual.Trim());
            var dtos = scored.Select(t => new RelevantMemoryDto
            {
                Id             = t.Memory.Id,
                Category       = t.Memory.Category,
                Title          = t.Memory.Title,
                Content        = t.Memory.Content,
                Tags           = t.Memory.Tags,
                RelevanceScore = t.Score,
            }).ToList();
            return new ProjectContextResult
            {
                Success          = true,
                Message          = $"{dtos.Count} recuerdo(s) relevante(s) encontrado(s) para la tarea.",
                Project          = proyecto.Trim(),
                TaskDescription  = tareaActual.Trim(),
                TotalResults     = dtos.Count,
                RelevantMemories = dtos,
            };
        }
        catch (Exception ex)
        {
            return new ProjectContextResult
            {
                Success          = false,
                Message          = $"Error al recuperar contexto: {ex.Message}",
                Project          = proyecto,
                TaskDescription  = tareaActual,
                TotalResults     = 0,
                RelevantMemories = [],
            };
        }
    }
}
