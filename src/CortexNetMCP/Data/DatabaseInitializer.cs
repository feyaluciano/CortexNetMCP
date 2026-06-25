using Microsoft.Data.Sqlite;

namespace CortexNetMCP.Data;

/// <summary>
/// Responsable de crear y mantener el esquema de la base de datos SQLite.
/// Crea la base de datos, tablas, tabla FTS5, triggers e índices si no existen.
/// Se ejecuta automáticamente al iniciar el servidor MCP.
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Punto de entrada principal. Inicializa todo el esquema de forma idempotente.
    /// </summary>
    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        EnableWalMode(connection);
        EnableForeignKeys(connection);
        CreateMemoriesTable(connection);
        CreateMemoryRelationsTable(connection);
        CreateFts5VirtualTable(connection);
        CreateFts5Triggers(connection);
        CreateIndexes(connection);
    }

    // Activa WAL para mejor rendimiento en escrituras concurrentes.
    private static void EnableWalMode(SqliteConnection conn) =>
        Execute(conn, "PRAGMA journal_mode = WAL;");

    private static void EnableForeignKeys(SqliteConnection conn) =>
        Execute(conn, "PRAGMA foreign_keys = ON;");

    private static void CreateMemoriesTable(SqliteConnection conn) =>
        Execute(conn, """
            CREATE TABLE IF NOT EXISTS Memories (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Project   TEXT NOT NULL,
                Category  TEXT NOT NULL,
                Title     TEXT NOT NULL,
                Content   TEXT NOT NULL,
                Tags      TEXT,
                FilePaths TEXT,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
            );
            """);

    private static void CreateMemoryRelationsTable(SqliteConnection conn) =>
        Execute(conn, """
            CREATE TABLE IF NOT EXISTS MemoryRelations (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                SourceMemoryId INTEGER NOT NULL REFERENCES Memories(Id) ON DELETE CASCADE,
                TargetMemoryId INTEGER NOT NULL REFERENCES Memories(Id) ON DELETE CASCADE,
                RelationType   TEXT NOT NULL
            );
            """);

    /// <summary>
    /// Tabla FTS5 para búsqueda de texto completo.
    /// Usa content table para no duplicar los datos; los triggers mantienen el índice sincronizado.
    /// Indexa: Title, Content, Tags, Category y FilePaths.
    /// </summary>
    private static void CreateFts5VirtualTable(SqliteConnection conn) =>
        Execute(conn, """
            CREATE VIRTUAL TABLE IF NOT EXISTS Memories_fts USING fts5(
                Title,
                Content,
                Tags,
                Category,
                FilePaths,
                content='Memories',
                content_rowid='Id'
            );
            """);

    private static void CreateFts5Triggers(SqliteConnection conn)
    {
        // Trigger AFTER INSERT: agrega al índice FTS5
        Execute(conn, """
            CREATE TRIGGER IF NOT EXISTS Memories_ai
            AFTER INSERT ON Memories BEGIN
                INSERT INTO Memories_fts(rowid, Title, Content, Tags, Category, FilePaths)
                VALUES (
                    new.Id,
                    new.Title,
                    new.Content,
                    COALESCE(new.Tags, ''),
                    new.Category,
                    COALESCE(new.FilePaths, '')
                );
            END;
            """);

        // Trigger AFTER DELETE: elimina del índice FTS5
        Execute(conn, """
            CREATE TRIGGER IF NOT EXISTS Memories_ad
            AFTER DELETE ON Memories BEGIN
                INSERT INTO Memories_fts(Memories_fts, rowid, Title, Content, Tags, Category, FilePaths)
                VALUES (
                    'delete',
                    old.Id,
                    old.Title,
                    old.Content,
                    COALESCE(old.Tags, ''),
                    old.Category,
                    COALESCE(old.FilePaths, '')
                );
            END;
            """);

        // Trigger AFTER UPDATE: actualiza el índice FTS5 (delete + re-insert)
        Execute(conn, """
            CREATE TRIGGER IF NOT EXISTS Memories_au
            AFTER UPDATE ON Memories BEGIN
                INSERT INTO Memories_fts(Memories_fts, rowid, Title, Content, Tags, Category, FilePaths)
                VALUES (
                    'delete',
                    old.Id,
                    old.Title,
                    old.Content,
                    COALESCE(old.Tags, ''),
                    old.Category,
                    COALESCE(old.FilePaths, '')
                );
                INSERT INTO Memories_fts(rowid, Title, Content, Tags, Category, FilePaths)
                VALUES (
                    new.Id,
                    new.Title,
                    new.Content,
                    COALESCE(new.Tags, ''),
                    new.Category,
                    COALESCE(new.FilePaths, '')
                );
            END;
            """);
    }

    private static void CreateIndexes(SqliteConnection conn)
    {
        Execute(conn, "CREATE INDEX IF NOT EXISTS IX_Memories_Project          ON Memories(Project);");
        Execute(conn, "CREATE INDEX IF NOT EXISTS IX_Memories_Category         ON Memories(Category);");
        Execute(conn, "CREATE INDEX IF NOT EXISTS IX_Memories_Project_Category ON Memories(Project, Category);");
        Execute(conn, "CREATE INDEX IF NOT EXISTS IX_MemRelations_Source       ON MemoryRelations(SourceMemoryId);");
        Execute(conn, "CREATE INDEX IF NOT EXISTS IX_MemRelations_Target       ON MemoryRelations(TargetMemoryId);");
    }

    private static void Execute(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
