using System.Text;
using System.Text.Json.Nodes;

namespace CortexNetMCP.Common;

public static class SetupCommand
{
    /// <summary>
    /// Punto de entrada para "cortexnetmcp setup [agente] [--global] [--print]".
    /// Retorna 0 en éxito, 1 en error.
    /// </summary>
    public static int Run(string[] args)
    {
        if (args.Contains("--print", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine(MemoryProtocol.Block);
            return 0;
        }

        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        string agentName = args[0];
        bool   isGlobal  = args.Contains("--global", StringComparer.OrdinalIgnoreCase);

        AgentTarget? agent = AgentTargetExtensions.TryParse(agentName);
        if (agent is null)
        {
            Console.Error.WriteLine($"Agente desconocido '{agentName}'. Soportados: claude-code, cursor, vscode, windsurf.");
            Console.Error.WriteLine();
            PrintUsage();
            return 1;
        }

        string? targetPath = agent.Value.ResolvePath(isGlobal);
        if (targetPath is null)
        {
            Console.Error.WriteLine(
                $"El agente '{agentName}' no soporta el scope --global. " +
                $"Ejecutá sin --global para inyectar en la configuración del proyecto actual.");
            return 1;
        }

        string? dir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            Console.WriteLine($"Directorio creado: {dir}");
        }

        string existingContent = File.Exists(targetPath)
            ? File.ReadAllText(targetPath, Encoding.UTF8)
            : string.Empty;

        if (existingContent.Contains(MemoryProtocol.SentinelStart, StringComparison.Ordinal))
        {
            Console.WriteLine($"El Memory Protocol ya está presente en: {targetPath}");
            Console.WriteLine("No se realizaron cambios. Para actualizar, eliminá el bloque existente y volvé a ejecutar el comando.");
            return 0;
        }

        string separator = existingContent.Length > 0 && !existingContent.EndsWith('\n')
            ? Environment.NewLine + Environment.NewLine
            : Environment.NewLine;

        File.AppendAllText(targetPath, separator + MemoryProtocol.Block + Environment.NewLine, Encoding.UTF8);

        string? mcpConfigPath = agent.Value.ResolveMcpConfigPath(isGlobal);
        if (mcpConfigPath is not null)
            RegisterMcpServer(mcpConfigPath, isGlobal);

        string scope = isGlobal ? "global" : "proyecto";
        Console.WriteLine();
        Console.WriteLine("Memory Protocol de CortexNet inyectado correctamente.");
        Console.WriteLine($"  Agente : {agentName}");
        Console.WriteLine($"  Scope  : {scope}");
        Console.WriteLine($"  Archivo: {targetPath}");
        if (mcpConfigPath is not null)
            Console.WriteLine($"  MCP    : {mcpConfigPath}");
        Console.WriteLine();
        Console.WriteLine("El agente de IA leerá y seguirá estas reglas desde la próxima sesión.");
        Console.WriteLine();

        return 0;
    }

    private static void RegisterMcpServer(string mcpConfigPath, bool isGlobal)
    {
        string? dir = Path.GetDirectoryName(mcpConfigPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        JsonObject root;
        if (File.Exists(mcpConfigPath))
        {
            try { root = JsonNode.Parse(File.ReadAllText(mcpConfigPath, Encoding.UTF8))?.AsObject() ?? []; }
            catch { root = []; }
        }
        else
        {
            root = [];
        }

        // Global scope usa { "mcpServers": { ... } }; proyecto usa formato plano { "nombre": { ... } }
        if (isGlobal)
        {
            if (root["mcpServers"] is not JsonObject servers)
            {
                servers = [];
                root["mcpServers"] = servers;
            }
            servers["CortexNetMCP"] = new JsonObject { ["command"] = "cortexnetmcp" };
        }
        else
        {
            root["CortexNetMCP"] = new JsonObject { ["command"] = "cortexnetmcp" };
        }

        File.WriteAllText(mcpConfigPath,
            root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
            Encoding.UTF8);
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("""
            Uso:
              cortexnetmcp setup <agente>           Inyectar en la configuración del proyecto actual
              cortexnetmcp setup <agente> --global  Inyectar en la configuración global del usuario (~/....)
              cortexnetmcp setup --print            Imprimir el Memory Protocol en stdout sin tocar archivos

            Agentes soportados:
              claude-code   CLAUDE.md (proyecto) o ~/.claude/CLAUDE.md (global)
              cursor        .cursor/rules/cortexnet-memory.mdc
              vscode        .github/copilot-instructions.md (solo proyecto)
              windsurf      .windsurfrules (proyecto) o ~/.windsurfrules (global)
            """);
    }
}
