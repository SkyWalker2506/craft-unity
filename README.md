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

| Tool | Description |
|------|-------------|
| `Craft_Execute` | Execute operations as a single undoable transaction |
| `Craft_Validate` | Validate operations without executing |
| `Craft_Rollback` | Rollback by transaction ID or N steps |
| `Craft_Query` | Query scene objects by name, components, tags |
| `Craft_Status` | Engine status, recent transactions, last trace |

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

| Type | Description |
|------|-------------|
| `CreateGameObject` | Create empty, primitive, or with components |
| `ModifyComponent` | Set fields/properties via reflection |
| `DeleteGameObject` | Remove from scene with Undo |

## License

MIT
