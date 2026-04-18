# CRAFT — Claude's Reliable AI Framework for Transactions

Safe AI execution layer for Unity MCP. Adds transaction safety, validation, rollback, and execution tracing on top of Unity's MCP bridge.

## What it does

When AI agents (Claude Code, Cursor, etc.) manipulate Unity scenes via MCP, operations happen without safety nets. CRAFT wraps those operations in:

- **Transactions** — Group multiple operations into a single undoable unit
- **Validation** — Check operations before executing (type existence, parameter correctness)
- **Rollback** — Revert any transaction by ID or undo N steps
- **Tracing** — Step-by-step execution recording for debugging

## Requirements

- Unity 6 (6000.0+)
- `com.unity.ai.assistant` >= 2.0.0

## Installation

Add to your Unity project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.skywalker.craft": "https://github.com/SkyWalker2506/craft-unity.git"
  }
}
```

## MCP Tools

### Core (mutating, transaction-safe)

| Tool | Description |
|------|-------------|
| `Craft_Execute` | Execute operations as a single undoable transaction |
| `Craft_Validate` | Validate operations without executing |
| `Craft_Rollback` | Rollback by transaction ID or N steps |
| `Craft_Query` | Query scene objects by name, components, tags |
| `Craft_Status` | Engine status, recent transactions, last trace |

### Inspect (read-only, bypass transactions) — 0.2.0

| Tool | Description |
|------|-------------|
| `Craft_CaptureGameView` | Capture Game view at specified resolution (png/jpg) |
| `Craft_CaptureSceneView` | Capture Scene view (editor camera or named camera) |
| `Craft_CaptureUIPanel` | Capture UIDocument panel in isolation |
| `Craft_ReadConsoleLog` | Console entries since timestamp, filterable by level |
| `Craft_ProfileCapture` | Single-frame profiler snapshot (draw calls, tris, memory, GC) |

### ImportSettings (mutating, transaction-safe) — 0.2.0

| Tool | Description |
|------|-------------|
| `Craft_SetTextureImporter` | Batch texture import overrides (compression, size, mipmaps, crunch) with rollback |
| `Craft_SetModelImporter` | Batch model import overrides (mesh compression, animation, scale) with rollback |

Both record original importer state before applying so `Craft_Rollback` restores it cleanly.

## Architecture

```
McpTools/           ← MCP adapter (only layer that imports MCP namespaces)
Core/               ← MCP-agnostic engine (TransactionManager, CraftEngine)
Operations/         ← Unity API operations with Undo support
WorldQuery/         ← Scene query engine
Validation/         ← Static validation
```

Core and Operations have **zero MCP dependencies**. Only McpTools/ uses `[McpTool]` attributes. This means the engine can work with any MCP bridge (official or third-party) by swapping only the adapter layer.

## Supported Operations

### Core

| Type | Description |
|------|-------------|
| `CreateGameObject` | Create empty, primitive, or with components |
| `ModifyComponent` | Set fields/properties via reflection |
| `DeleteGameObject` | Remove from scene with Undo |

### ImportSettings (0.2.0)

| Type | Description |
|------|-------------|
| `SetTextureImporter` | Apply `TextureImporterOverrides` + rollback cache |
| `SetModelImporter` | Apply `ModelImporterOverrides` + rollback cache |

## Companion Plugin

[`ccplugin-unity-craft`](https://github.com/SkyWalker2506/ccplugin-unity-craft) is the Claude Code plugin that teaches AI agents to use this SDK — adds Claude Design bundle import, cinematic presets, autonomous screen-to-action, and performance audit workflows on top.

## License

MIT
