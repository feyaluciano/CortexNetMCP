namespace CortexNetMCP.Common;

public enum AgentTarget
{
    ClaudeCode,
    Cursor,
    VSCode,
    Windsurf,
}

public static class AgentTargetExtensions
{
    /// <summary>
    /// Resuelve la ruta del archivo de configuración del agente.
    /// Retorna null cuando la combinación (agente, global) no está soportada.
    /// </summary>
    public static string? ResolvePath(this AgentTarget agent, bool global)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string cwd  = Environment.CurrentDirectory;

        return (agent, global) switch
        {
            (AgentTarget.ClaudeCode, false) => Path.Combine(cwd,  "CLAUDE.md"),
            (AgentTarget.ClaudeCode, true)  => Path.Combine(home, ".claude", "CLAUDE.md"),

            (AgentTarget.Cursor,     false) => Path.Combine(cwd,  ".cursor", "rules", "cortexnet-memory.mdc"),
            (AgentTarget.Cursor,     true)  => Path.Combine(home, ".cursor", "rules", "cortexnet-memory.mdc"),

            (AgentTarget.VSCode,     false) => Path.Combine(cwd,  ".github", "copilot-instructions.md"),
            (AgentTarget.VSCode,     true)  => null,

            (AgentTarget.Windsurf,   false) => Path.Combine(cwd,  ".windsurfrules"),
            (AgentTarget.Windsurf,   true)  => Path.Combine(home, ".windsurfrules"),

            _ => null
        };
    }

    /// <summary>
    /// Resuelve la ruta del archivo de configuración MCP del agente.
    /// Retorna null cuando el agente no requiere configuración MCP propia.
    /// </summary>
    public static string? ResolveMcpConfigPath(this AgentTarget agent, bool global)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string cwd  = Environment.CurrentDirectory;

        return (agent, global) switch
        {
            (AgentTarget.ClaudeCode, true)  => Path.Combine(home, ".claude", "mcp.json"),
            (AgentTarget.ClaudeCode, false) => Path.Combine(cwd, ".mcp.json"),
            _                               => null,
        };
    }

    public static AgentTarget? TryParse(string name) =>
        name.ToLowerInvariant() switch
        {
            "claude-code" or "claudecode" or "claude" => AgentTarget.ClaudeCode,
            "cursor"                                   => AgentTarget.Cursor,
            "vscode" or "vs-code" or "copilot"        => AgentTarget.VSCode,
            "windsurf"                                 => AgentTarget.Windsurf,
            _                                          => null,
        };
}
