using Microsoft.Data.Sqlite;
using System.Text;
using System.Text.RegularExpressions;

namespace CortexNetMCP.Data;

// ─────────────────────────────────────────────────────────────────────────────
// Modelos
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Representa un recuerdo técnico almacenado en la tabla Memories.</summary>
public sealed class MemoryRecord
{
    public long    Id        { get; set; }
    public string  Project   { get; set; } = "";
    public string  Category  { get; set; } = "";
    public string  Title     { get; set; } = "";
    public string  Content   { get; set; } = "";
    public string? Tags      { get; set; }
    public string? FilePaths { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? SessionId  { get; set; }
}

/// <summary>Representa una relación semántica entre dos recuerdos.</summary>
public sealed class MemoryRelationRecord
{
    public long   Id             { get; set; }
    public long   SourceMemoryId { get; set; }
    public long   TargetMemoryId { get; set; }
    public string RelationType   { get; set; } = "";
}

/// <summary>Representa una sesión de conversación en la tabla Sessions.</summary>
public sealed class SessionRecord
{
    public string    Id          { get; set; } = "";
    public string    Project     { get; set; } = "";
    public string?   ProjectPath { get; set; }
    public string?   GitBranch   { get; set; }
    public string?   Metadata    { get; set; }
    public DateTime  StartedAt   { get; set; }
    public DateTime? EndedAt     { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Repositorio
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Encapsula toda la lógica de acceso a datos para las tablas Memories y MemoryRelations.
/// Cada método abre y cierra su propia conexión para garantizar que el pool de
/// conexiones de SQLite maneje la reutilización.
/// </summary>
public class MemoryRepository
{
    private readonly string _connectionString;

    public MemoryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // ──────────────────────────────────────────────
    // Helpers privados
    // ──────────────────────────────────────────────

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    /// <summary>
    /// Convierte texto libre en una consulta FTS5 segura.
    /// Cada palabra se convierte en un término de prefijo (word*) unido con OR.
    /// Ejemplo: "AFIP token jwt" → "AFIP* OR token* OR jwt*"
    /// </summary>
    private static string BuildFtsQuery(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "\"\"";

        var words = input.Split([' ', ',', ';', '\t'], StringSplitOptions.RemoveEmptyEntries);
        var terms = words
            .Select(w => Regex.Replace(w, @"[""\*\^()\{\}\[\]~\\]", ""))
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => $"{w}*");

        return string.Join(" OR ", terms);
    }

    /// <summary>Mapea una fila del reader a un MemoryRecord (columnas 0–9).</summary>
    private static MemoryRecord MapMemory(SqliteDataReader reader) => new()
    {
        Id        = reader.GetInt64(0),
        Project   = reader.GetString(1),
        Category  = reader.GetString(2),
        Title     = reader.GetString(3),
        Content   = reader.GetString(4),
        Tags      = reader.IsDBNull(5) ? null : reader.GetString(5),
        FilePaths = reader.IsDBNull(6) ? null : reader.GetString(6),
        CreatedAt = DateTime.Parse(reader.GetString(7)),
        UpdatedAt = DateTime.Parse(reader.GetString(8)),
        SessionId = reader.IsDBNull(9) ? null : reader.GetString(9),
    };

    /// <summary>Ejecuta el comando y devuelve todos los registros como lista.</summary>
    private static List<MemoryRecord> ReadAll(SqliteCommand cmd)
    {
        var list = new List<MemoryRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(MapMemory(reader));
        return list;
    }

    // ──────────────────────────────────────────────
    // CRUD — Memories
    // ──────────────────────────────────────────────

    /// <summary>Inserta un nuevo recuerdo y retorna el ID generado.</summary>
    public long InsertMemory(
        string project, string category, string title,
        string content, string? tags, string? filePaths,
        string? sessionId = null)
    {
        using var conn = OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Memories (Project, Category, Title, Content, Tags, FilePaths, SessionId, CreatedAt, UpdatedAt)
            VALUES (@project, @category, @title, @content, @tags, @filePaths, @sessionId,
                    datetime('now'), datetime('now'));
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@project",   project);
        cmd.Parameters.AddWithValue("@category",  category);
        cmd.Parameters.AddWithValue("@title",     title);
        cmd.Parameters.AddWithValue("@content",   content);
        cmd.Parameters.AddWithValue("@tags",      (object?)tags      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@filePaths", (object?)filePaths ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sessionId", (object?)sessionId ?? DBNull.Value);
        return (long)(cmd.ExecuteScalar() ?? throw new InvalidOperationException("INSERT no retornó un ID."));
    }

    /// <summary>
    /// Búsqueda full-text usando FTS5 sobre Title, Content, Tags, Category y FilePaths.
    /// Filtra opcionalmente por proyecto. Resultados ordenados por relevancia (rank).
    /// </summary>
    public List<MemoryRecord> SearchMemories(string? project, string searchText)
    {
        var ftsQuery = BuildFtsQuery(searchText);
        using var conn = OpenConnection();
        using var cmd  = conn.CreateCommand();

        var sql = new StringBuilder("""
            SELECT m.Id, m.Project, m.Category, m.Title, m.Content,
                   m.Tags, m.FilePaths, m.CreatedAt, m.UpdatedAt, m.SessionId
            FROM   Memories_fts f
            JOIN   Memories m ON m.Id = f.rowid
            WHERE  f MATCH @query
            """);

        if (!string.IsNullOrWhiteSpace(project))
        {
            sql.Append(" AND m.Project = @project");
            cmd.Parameters.AddWithValue("@project", project);
        }

        sql.Append(" ORDER BY rank;");
        cmd.CommandText = sql.ToString();
        cmd.Parameters.AddWithValue("@query", ftsQuery);
        return ReadAll(cmd);
    }

    /// <summary>Devuelve todos los recuerdos de un proyecto filtrados por categoría.</summary>
    public List<MemoryRecord> GetByCategory(string project, string category)
    {
        using var conn = OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Project, Category, Title, Content, Tags, FilePaths, CreatedAt, UpdatedAt, SessionId
            FROM   Memories
            WHERE  Project = @project AND Category = @category
            ORDER  BY UpdatedAt DESC;
            """;
        cmd.Parameters.AddWithValue("@project",  project);
        cmd.Parameters.AddWithValue("@category", category);
        return ReadAll(cmd);
    }

    /// <summary>Devuelve un recuerdo por su ID, o null si no existe.</summary>
    public MemoryRecord? GetById(long id)
    {
        using var conn = OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Project, Category, Title, Content, Tags, FilePaths, CreatedAt, UpdatedAt, SessionId
            FROM   Memories
            WHERE  Id = @id;
            """;
        cmd.Parameters.AddWithValue("@id", id);
        return ReadAll(cmd).FirstOrDefault();
    }

    /// <summary>Actualiza título, contenido, tags y filePaths. Devuelve true si el registro existía.</summary>
    public bool UpdateMemory(long id, string title, string content, string? tags, string? filePaths)
    {
        using var conn = OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Memories
            SET    Title = @title, Content = @content, Tags = @tags,
                   FilePaths = @filePaths, UpdatedAt = datetime('now')
            WHERE  Id = @id;
            """;
        cmd.Parameters.AddWithValue("@id",        id);
        cmd.Parameters.AddWithValue("@title",     title);
        cmd.Parameters.AddWithValue("@content",   content);
        cmd.Parameters.AddWithValue("@tags",      (object?)tags      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@filePaths", (object?)filePaths ?? DBNull.Value);
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Búsqueda full-text con puntuación bm25 para ranking de relevancia.
    /// Filtra por proyecto. Resultados ordenados por relevancia descendente (más relevante primero).
    /// El Score es positivo; mayor valor indica mayor relevancia.
    /// </summary>
    public List<(MemoryRecord Memory, double Score)> SearchMemoriesWithScore(string project, string searchText)
    {
        var ftsQuery = BuildFtsQuery(searchText);
        using var conn = OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            SELECT m.Id, m.Project, m.Category, m.Title, m.Content,
                   m.Tags, m.FilePaths, m.CreatedAt, m.UpdatedAt, m.SessionId,
                   -bm25(Memories_fts) AS RelevanceScore
            FROM   Memories_fts f
            JOIN   Memories m ON m.Id = f.rowid
            WHERE  f MATCH @query
              AND  m.Project = @project
            ORDER  BY bm25(Memories_fts);
            """;
        cmd.Parameters.AddWithValue("@query",   ftsQuery);
        cmd.Parameters.AddWithValue("@project", project);
        var results = new List<(MemoryRecord, double)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var memory = MapMemory(reader);
            var score  = reader.GetDouble(10);
            results.Add((memory, score));
        }
        return results;
    }

    /// <summary>Elimina un recuerdo y sus relaciones asociadas. Devuelve true si existía.</summary>
    public bool DeleteMemory(long id)
    {
        using var conn = OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Memories WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    // ──────────────────────────────────────────────
    // MemoryRelations
    // ──────────────────────────────────────────────

    /// <summary>Crea una relación semántica entre dos recuerdos. Devuelve el ID de la relación.</summary>
    public long InsertRelation(long sourceId, long targetId, string relationType)
    {
        using var conn = OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO MemoryRelations (SourceMemoryId, TargetMemoryId, RelationType)
            VALUES (@sourceId, @targetId, @relationType);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@sourceId",     sourceId);
        cmd.Parameters.AddWithValue("@targetId",     targetId);
        cmd.Parameters.AddWithValue("@relationType", relationType);
        return (long)(cmd.ExecuteScalar() ?? throw new InvalidOperationException("INSERT de relación no retornó un ID."));
    }

    // ──────────────────────────────────────────────
    // Sessions
    // ──────────────────────────────────────────────

    /// <summary>Crea una nueva sesión y retorna el UUID generado.</summary>
    public string CreateSession(string project, string? projectPath, string? gitBranch, string? metadata)
    {
        var id = Guid.NewGuid().ToString("D");
        using var conn = OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Sessions (Id, Project, ProjectPath, GitBranch, Metadata, StartedAt)
            VALUES (@id, @project, @projectPath, @gitBranch, @metadata, datetime('now'));
            """;
        cmd.Parameters.AddWithValue("@id",          id);
        cmd.Parameters.AddWithValue("@project",     project);
        cmd.Parameters.AddWithValue("@projectPath", (object?)projectPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@gitBranch",   (object?)gitBranch   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@metadata",    (object?)metadata    ?? DBNull.Value);
        cmd.ExecuteNonQuery();
        return id;
    }

    /// <summary>Marca la sesión como cerrada con la fecha/hora actual.</summary>
    public void CloseSession(string sessionId)
    {
        using var conn = OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = "UPDATE Sessions SET EndedAt = datetime('now') WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", sessionId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Retorna la última sesión cerrada del proyecto, o null si no existe ninguna.</summary>
    public SessionRecord? GetLastClosedSession(string project)
    {
        using var conn = OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Project, ProjectPath, GitBranch, Metadata, StartedAt, EndedAt
            FROM   Sessions
            WHERE  Project = @project AND EndedAt IS NOT NULL
            ORDER  BY StartedAt DESC
            LIMIT  1;
            """;
        cmd.Parameters.AddWithValue("@project", project);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new SessionRecord
        {
            Id          = reader.GetString(0),
            Project     = reader.GetString(1),
            ProjectPath = reader.IsDBNull(2) ? null : reader.GetString(2),
            GitBranch   = reader.IsDBNull(3) ? null : reader.GetString(3),
            Metadata    = reader.IsDBNull(4) ? null : reader.GetString(4),
            StartedAt   = DateTime.Parse(reader.GetString(5)),
            EndedAt     = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)),
        };
    }

    /// <summary>Retorna el recuerdo task más reciente asociado a una sesión específica.</summary>
    public MemoryRecord? GetTaskMemoryForSession(string sessionId)
    {
        using var conn = OpenConnection();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Project, Category, Title, Content, Tags, FilePaths, CreatedAt, UpdatedAt, SessionId
            FROM   Memories
            WHERE  SessionId = @sessionId AND Category = 'task'
            ORDER  BY CreatedAt DESC
            LIMIT  1;
            """;
        cmd.Parameters.AddWithValue("@sessionId", sessionId);
        return ReadAll(cmd).FirstOrDefault();
    }

    /// <summary>
    /// Devuelve todos los recuerdos relacionados a uno dado, en ambas direcciones.
    /// Cada resultado incluye el recuerdo relacionado, el tipo de relación y la dirección
    /// ("outgoing" = el recuerdo origen apunta al relacionado; "incoming" = al revés).
    /// </summary>
    public List<(MemoryRecord Memory, string RelationType, string Direction)> GetRelatedMemories(long memoryId)
    {
        using var conn = OpenConnection();
        using var cmd  = conn.CreateCommand();

        // Las columnas 0–9 son los campos de MemoryRecord (incluyendo SessionId en col 9).
        // La columna 10 es RelationType y la 11 es Direction.
        cmd.CommandText = """
            SELECT m.Id, m.Project, m.Category, m.Title, m.Content,
                   m.Tags, m.FilePaths, m.CreatedAt, m.UpdatedAt, m.SessionId,
                   r.RelationType,
                   'outgoing' AS Direction
            FROM   MemoryRelations r
            JOIN   Memories m ON m.Id = r.TargetMemoryId
            WHERE  r.SourceMemoryId = @memoryId

            UNION ALL

            SELECT m.Id, m.Project, m.Category, m.Title, m.Content,
                   m.Tags, m.FilePaths, m.CreatedAt, m.UpdatedAt, m.SessionId,
                   r.RelationType,
                   'incoming' AS Direction
            FROM   MemoryRelations r
            JOIN   Memories m ON m.Id = r.SourceMemoryId
            WHERE  r.TargetMemoryId = @memoryId

            ORDER  BY m.UpdatedAt DESC;
            """;
        cmd.Parameters.AddWithValue("@memoryId", memoryId);

        var results = new List<(MemoryRecord, string, string)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var memory       = MapMemory(reader);
            var relationType = reader.GetString(10);
            var direction    = reader.GetString(11);
            results.Add((memory, relationType, direction));
        }
        return results;
    }
}
