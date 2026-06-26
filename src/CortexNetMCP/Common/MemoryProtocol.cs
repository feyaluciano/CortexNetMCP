namespace CortexNetMCP.Common;

public static class MemoryProtocol
{
    public const string SentinelStart = "<!-- cortexnetmcp-memory-protocol-start -->";
    public const string SentinelEnd   = "<!-- cortexnetmcp-memory-protocol-end -->";

    public static readonly string Block = """
        ## Memory Protocol — CortexNet MCP

        You have access to a persistent long-term technical memory server (CortexNet MCP) via Model Context Protocol tools. You MUST strictly adhere to these instructions during every development session.

        ### 1. When to SAVE a Memory

        Call the `GuardarRecuerdo` tool immediately after any of the following events occur:

        - **Bug Resolved** (`categoria=bug`): Document the root cause, specific symptoms, and the exact working solution applied.
        - **Architectural/Design Decision Made** (`categoria=architecture` or `decision`): Record the selected approach, its technical justification, and any discarded alternatives with their trade-offs.
        - **Reusable Pattern/Technique Discovered** (`categoria=pattern`): Document when, why, and how to apply this structural pattern or source generator in the future.
        - **Configuration/Environment Change** (`categoria=decision`): Document the previous values, the new configuration, and the rationale behind the change.
        - **Data Model/API Definition** (`categoria=entity` or `endpoint`): Record the strict contract, schema, or DTO behavior clarified during development.

        *CRITICAL:* Do not save trivial syntax observations, intermediate debugging steps, or ephemeral execution data.

        ### 2. When to SEARCH Memory

        **Reactive Search** — Activated when the user explicitly asks to recall past info:
        - Call `BuscarRecuerdos` using the specific keywords provided by the user.
        - Always provide the `proyecto` identifier if known to narrow down and optimize results.

        **Proactive Search** — *Mandatory first action* before starting any non-trivial development task:
        - Before writing any code, diagnosing a complex bug, or making a design choice, you MUST call `RecordarContextoProyecto`. Pass the current workspace identifier as `proyecto` and a short description of the user request as `tareaActual`.
        - Provide optional fields when available: `projectPath` (absolute local directory), `gitBranch` (current Git branch), `agentModel`, `agentClient`, `gitCommitHash`. These enrich the session record for future traceability.
        - The response includes two critical fields to read **before proceeding**:
          - `HandoffNote`: the `task` summary saved at the end of the previous session. If not null, read its `Content` field — it contains pending work, last status, and recommended next steps.
          - `ActiveSessionId`: the UUID of the session just opened server-side. You do NOT need to track or pass this — the server handles it automatically.
        - Thoroughly analyze both `HandoffNote` and `RelevantMemories` before proceeding.

        ### 3. Session Closure Protocol

        Before concluding a major interaction, finishing a task workflow, or when the user indicates they are checking out, you MUST execute a formal handoff. Call `GuardarRecuerdo` exactly once with the following payload structure:
        - `categoria` = `task`
        - `titulo` = Short session identifier tag (e.g., "Session 2026-06-25: refactor auth pipelines")
        - `contenido` = A highly structured Markdown summary including: Current project status, what was successfully completed, what tasks remain pending, and explicit recommended next steps for the next session.

        **The server automatically closes the active session when it receives a `task`-category save.** You do NOT need to manage session IDs or call any extra tool — saving the `task` memory IS the session close signal.

        ### 4. Context Recovery Post-Compaction

        If the conversation context window is reaching its token capacity or a fresh session begins on an ongoing project, you MUST call `RecordarContextoProyecto` as your **absolute first action** — before attempting to read local workspace files or generating new code. The call opens a fresh session server-side and returns `HandoffNote` with the previous session's summary to rehydrate your state.

        ### 5. Emergency Compaction Sequence

        If context is being compacted mid-session and you need to preserve state immediately:
        1. Save a `task` memory with your current progress summary — this **closes the active session**.
        2. Immediately call `RecordarContextoProyecto` again — this **opens a new session** and returns the `task` you just saved as `HandoffNote`.
        3. Read `HandoffNote.Content` and continue from where you left off.

        Never risk terminating a session without executing this sequence if there is in-progress work.

        ### 6. Tools Quick Reference Guide

        | Trigger Situation | MCP Tool to Call | Mandatory/Key Parameters |
        |---|---|---|
        | Saving an engineering fact, fix, or choice | `GuardarRecuerdo` | `proyecto`, `categoria`, `titulo`, `contenido` |
        | User explicitly asks to recall or look up info | `BuscarRecuerdos` | `textoBusqueda`, `proyecto` (optional but recommended) |
        | Proactive task initialization — always first | `RecordarContextoProyecto` | `proyecto`, `tareaActual`, optional: `projectPath`, `gitBranch`, `agentModel`, `agentClient`, `gitCommitHash` |
        | Standard session close / end of chat | `GuardarRecuerdo` | `categoria="task"`, structured `contenido` — also closes the session |
        | Emergency compaction or brand new chat | `RecordarContextoProyecto` → read `HandoffNote` | `proyecto`, `tareaActual` |

        *Allowed strict values for `categoria`:* `architecture`, `bug`, `decision`, `entity`, `endpoint`, `feature`, `task`, `pattern`, `lesson`.

        """;
}
