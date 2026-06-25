using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.Reflection;
using System.Text.Json;

// Argumentos CLI: no requieren base de datos ni host MCP.
if (args.Length >= 1)
{
    if (args[0].Equals("setup", StringComparison.OrdinalIgnoreCase))
    {
        Environment.Exit(SetupCommand.Run(args[1..]));
        return;
    }

    if (args[0] is "--version" or "-v")
    {
        var version = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "desconocida";
        Console.WriteLine($"cortexnetmcp {version}");
        return;
    }

    if (args[0] is "--help" or "-h")
    {
        Console.WriteLine("""
            cortexnetmcp — Servidor MCP de memoria técnica a largo plazo

            Uso:
              cortexnetmcp                   Iniciar el servidor MCP (transporte stdio)
              cortexnetmcp setup <agente>    Inyectar el Memory Protocol en el agente de IA
              cortexnetmcp setup --print     Imprimir el Memory Protocol sin modificar archivos
              cortexnetmcp --version         Mostrar la versión instalada
              cortexnetmcp --help            Mostrar esta ayuda

            Agentes soportados en setup:
              claude-code, cursor, vscode, windsurf
            """);
        return;
    }
}

// Ruta del archivo SQLite. La base de datos se crea automáticamente si no existe.
const string connectionString = "Data Source=mi_memoria.db";

var builder = Host.CreateApplicationBuilder(args);

// 1. ¡CRÍTICO! Limpiar todos los proveedores de log por defecto de .NET
builder.Logging.ClearProviders();


// Redirige todos los logs a stderr para no contaminar el canal stdout de MCP.
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Opciones de serialización JSON AOT-compatible basadas en los source generators de CortexJsonContext.
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
jsonOptions.TypeInfoResolverChain.Insert(0, CortexJsonContext.Default);

builder.Services.AddSingleton(jsonOptions);

// Registrar servicios de infraestructura como Singleton para reutilizar conexiones.
builder.Services.AddSingleton(new DatabaseInitializer(connectionString));
builder.Services.AddSingleton(new MemoryRepository(connectionString));

// Registrar el servidor MCP con las herramientas definidas en CortexNetMCPTools.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Inicializar la base de datos (tablas, FTS5, índices, triggers) antes de arrancar.
app.Services.GetRequiredService<DatabaseInitializer>().Initialize();

await app.RunAsync();
