using System.Text.Json.Serialization;
using CortexNetMCP.Data;

namespace CortexNetMCP.DTOs;

// ─── Base DTO ────────────────────────────────────────────────────────────────

public class ToolResponse
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ─── Core DTO ────────────────────────────────────────────────────────────────

public sealed class MemoryDto
{
    public long     Id        { get; set; }
    public string   Project   { get; set; } = string.Empty;
    public string   Category  { get; set; } = string.Empty;
    public string   Title     { get; set; } = string.Empty;
    public string   Content   { get; set; } = string.Empty;
    public string?  Tags      { get; set; }
    public string?  FilePaths { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string?  SessionId { get; set; }
}

// ─── Tool Result DTOs ────────────────────────────────────────────────────────

public sealed class SaveMemoryResult : ToolResponse
{
    public long   MemoryId { get; set; }
    public string Project  { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public sealed class SearchMemoriesResult : ToolResponse
{
    public string          Query        { get; set; } = string.Empty;
    public int             TotalResults { get; set; }
    public List<MemoryDto> Memories     { get; set; } = [];
}

public sealed class MemoryResult : ToolResponse
{
    public MemoryDto? Memory { get; set; }
}

public sealed class CategoryMemoriesResult : ToolResponse
{
    public string          Category     { get; set; } = string.Empty;
    public int             TotalResults { get; set; }
    public List<MemoryDto> Memories     { get; set; } = [];
}

public sealed class RelationResult : ToolResponse
{
    public long   RelationId     { get; set; }
    public long   SourceMemoryId { get; set; }
    public long   TargetMemoryId { get; set; }
    public string RelationType   { get; set; } = string.Empty;
}

public sealed class UpdateMemoryResult : ToolResponse
{
    public long    MemoryId  { get; set; }
    public string  Title     { get; set; } = string.Empty;
    public string? Tags      { get; set; }
    public string? FilePaths { get; set; }
}

public sealed class DeleteMemoryResult : ToolResponse
{
    public long MemoryId { get; set; }
}

public sealed class RelevantMemoryDto
{
    public long    Id             { get; set; }
    public string  Category       { get; set; } = string.Empty;
    public string  Title          { get; set; } = string.Empty;
    public string  Content        { get; set; } = string.Empty;
    public string? Tags           { get; set; }
    public double  RelevanceScore { get; set; }
}

public sealed class ProjectContextResult : ToolResponse
{
    public string                  Project          { get; set; } = string.Empty;
    public string                  TaskDescription  { get; set; } = string.Empty;
    public int                     TotalResults     { get; set; }
    public List<RelevantMemoryDto> RelevantMemories { get; set; } = [];
    public MemoryDto?              HandoffNote      { get; set; }
    public string                  ActiveSessionId  { get; set; } = string.Empty;
}

public sealed class RelatedMemoryDto
{
    public long    MemoryId     { get; set; }
    public string  Project      { get; set; } = string.Empty;
    public string  Category     { get; set; } = string.Empty;
    public string  Title        { get; set; } = string.Empty;
    public string? Tags         { get; set; }
    public string  RelationType { get; set; } = string.Empty;
    public string  Direction    { get; set; } = string.Empty;
}

public sealed class RelatedMemoriesResult : ToolResponse
{
    public long                   MemoryId        { get; set; }
    public int                    TotalResults    { get; set; }
    public List<RelatedMemoryDto> RelatedMemories { get; set; } = [];
}

// ─── AOT-Compatible JSON Source Generator ────────────────────────────────────

[JsonSerializable(typeof(ToolResponse))]
[JsonSerializable(typeof(MemoryDto))]
[JsonSerializable(typeof(SaveMemoryResult))]
[JsonSerializable(typeof(SearchMemoriesResult))]
[JsonSerializable(typeof(MemoryResult))]
[JsonSerializable(typeof(CategoryMemoriesResult))]
[JsonSerializable(typeof(RelationResult))]
[JsonSerializable(typeof(UpdateMemoryResult))]
[JsonSerializable(typeof(DeleteMemoryResult))]
[JsonSerializable(typeof(RelevantMemoryDto))]
[JsonSerializable(typeof(ProjectContextResult))]
[JsonSerializable(typeof(RelatedMemoryDto))]
[JsonSerializable(typeof(RelatedMemoriesResult))]
[JsonSerializable(typeof(List<MemoryDto>))]
[JsonSerializable(typeof(List<RelevantMemoryDto>))]
[JsonSerializable(typeof(List<RelatedMemoryDto>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy      = JsonKnownNamingPolicy.CamelCase,
    WriteIndented             = false,
    DefaultIgnoreCondition    = JsonIgnoreCondition.Never)]
public partial class CortexJsonContext : JsonSerializerContext { }

// ─── Internal mapping helpers ─────────────────────────────────────────────────

internal static class MemoryMapping
{
    internal static MemoryDto ToDto(this MemoryRecord r) => new()
    {
        Id        = r.Id,
        Project   = r.Project,
        Category  = r.Category,
        Title     = r.Title,
        Content   = r.Content,
        Tags      = r.Tags,
        FilePaths = r.FilePaths,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        SessionId = r.SessionId,
    };
}
