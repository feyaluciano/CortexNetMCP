# CortexNetMCP

A .NET MCP (Model Context Protocol) server built with the [ModelContextProtocol](https://github.com/modelcontextprotocol/csharp-sdk) SDK.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Getting Started

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project CortexNetMCP/CortexNetMCP.csproj
```

## Project Structure

```
CortexNetMCP/
├── CortexNetMCP.slnx        # Solution file
└── CortexNetMCP/
    ├── CortexNetMCP.csproj  # Project file
    └── Program.cs           # Entry point and MCP tool definitions
```

## MCP Tools

| Tool | Description |
|------|-------------|
| `Echo` | Echoes the message back to the client |

## Dependencies

| Package | Version |
|---------|---------|
| Microsoft.Extensions.Hosting | 10.0.9 |
| ModelContextProtocol | 1.4.0 |
| System.Text.Json | 10.0.9 |

## Transport

Uses **stdio** transport — connect via any MCP-compatible client that supports stdio servers.
