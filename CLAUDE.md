
<!-- cortexnetmcp-memory-protocol-start -->

## Memory Protocol — CortexNet MCP

Tenés acceso a un servidor de memoria técnica a largo plazo (CortexNet MCP).
Seguí estas reglas en cada sesión de trabajo.

### 1. Cuándo GUARDAR un recuerdo

Llamá a `GuardarRecuerdo` inmediatamente después de cualquiera de estos eventos:

- Se resolvió un bug (`categoria=bug`): registrá causa raíz, síntomas y la solución aplicada.
- Se tomó una decisión de arquitectura (`categoria=architecture` o `decision`): registrá la decisión, su justificación y las alternativas descartadas.
- Se descubrió un patrón o técnica reutilizable (`categoria=pattern`): registrá cuándo y cómo aplicarlo.
- Se realizó un cambio de configuración o entorno (`categoria=decision`): registrá el valor anterior, el nuevo y el motivo.
- Se definió o aclaró una entidad, endpoint o modelo de datos (`categoria=entity` / `endpoint`): registrá su contrato y comportamiento.

No guardes observaciones triviales, pasos intermedios ni información efímera.

### 2. Cuándo BUSCAR en memoria

**Reactivo** — cuando el usuario pide explícitamente recordar algo:
- Llamá a `BuscarRecuerdos` con las palabras clave del usuario.
- Si conocés el nombre del proyecto, pasalo como `proyecto` para acotar los resultados.

**Proactivo** — al inicio de cualquier tarea no trivial:
- Antes de escribir código, diagnosticar un bug o tomar una decisión de diseño, llamá a `RecordarContextoProyecto` con `proyecto` = nombre del proyecto actual y `tareaActual` = descripción en lenguaje natural de la tarea.
- Leé los recuerdos devueltos antes de continuar. Esto evita repetir bugs ya resueltos y mantiene consistencia con decisiones previas.

### 3. Protocolo de cierre de sesión

Antes de terminar cualquier sesión donde se realizó trabajo, llamá a `GuardarRecuerdo` una vez con:
- `categoria` = `task`
- `titulo` = etiqueta corta de la sesión (ej. "Sesión 2025-06-25: refactor auth")
- `contenido` = resumen estructurado: qué se completó, qué quedó pendiente, próximos pasos recomendados y decisiones clave tomadas.

Este resumen es tu nota de traspaso para la próxima sesión.

### 4. Recuperación de contexto post-compactación

Si el contexto de la conversación fue compactado o una nueva sesión comienza sobre un proyecto en curso, llamá a `RecordarContextoProyecto` como **primera acción** — antes de leer archivos o escribir código. Usá el nombre del proyecto y la descripción de la tarea actual para rehidratar el contexto desde la memoria a largo plazo.

### Referencia rápida de herramientas

| Situación | Herramienta | Parámetros clave |
|---|---|---|
| Guardar un hecho / decisión / fix | `GuardarRecuerdo` | `proyecto`, `categoria`, `titulo`, `contenido` |
| El usuario pide recordar algo | `BuscarRecuerdos` | `textoBusqueda`, opcionalmente `proyecto` |
| Inicio de tarea (proactivo) | `RecordarContextoProyecto` | `proyecto`, `tareaActual` |
| Resumen de sesión | `GuardarRecuerdo` | `categoria=task`, `contenido` estructurado |
| Después de compactación / sesión nueva | `RecordarContextoProyecto` | `proyecto`, `tareaActual` |

Valores válidos para `categoria`: `architecture`, `bug`, `decision`, `entity`, `endpoint`, `feature`, `task`, `pattern`, `lesson`.

<!-- cortexnetmcp-memory-protocol-end -->
