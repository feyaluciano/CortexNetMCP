namespace CortexNetMCP.Data;

public sealed class SessionManager
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, string> _activeSessions =
        new(StringComparer.OrdinalIgnoreCase);

    public string? GetActiveSession(string project)
    {
        lock (_lock)
            return _activeSessions.TryGetValue(project, out var id) ? id : null;
    }

    public void SetActiveSession(string project, string sessionId)
    {
        lock (_lock)
            _activeSessions[project] = sessionId;
    }

    public void ClearActiveSession(string project)
    {
        lock (_lock)
            _activeSessions.Remove(project);
    }
}
