# Changelog

## [0.2.0] - 2026-04-18

### Added
- **Craft_Inspect** (read-only category, bypasses transaction framework):
  - `Craft_CaptureGameView` — Game view screenshot via `ScreenCapture`
  - `Craft_CaptureSceneView` — Scene view render via `SceneView.lastActiveSceneView` or named camera
  - `Craft_CaptureUIPanel` — UIDocument isolated render
  - `Craft_ReadConsoleLog` — filterable console entries with since-timestamp
  - `Craft_ProfileCapture` — `ProfilerRecorder` single-frame snapshot (draw calls, tris, memory, GC)
- **Craft_ImportSettings** (mutating, transaction-safe, rollback-cached):
  - `Craft_SetTextureImporter` + `SetTextureImporterOp` + `TextureImporterOverrides`
  - `Craft_SetModelImporter` + `SetModelImporterOp` + `ModelImporterOverrides`
- Operations wrap Apply/Undo in `AssetDatabase.StartAssetEditing`/`StopAssetEditing`
- Original importer state captured before Apply for clean rollback

### Notes
- Unity Editor compile test required after package reimport
- Inspect ops expose data previously only available through Unity internals (`LogEntries` via reflection, `ProfilerRecorder` API)

## [0.1.0] - 2026-04-08

### Added
- Core engine: CraftEngine, TransactionManager, CommandLog, TraceRecorder
- Models: CraftOperation, CraftResult, CraftTrace, ValidationResult
- Operations: CreateGameObject, ModifyComponent, DeleteGameObject
- Validation: StaticValidator (Tier 1)
- WorldQuery: name, component, tag, parent filters
- MCP Tools: Craft_Execute, Craft_Validate, Craft_Rollback, Craft_Query, Craft_Status
- Runtime: PersistentId MonoBehaviour
- Editor tests: TransactionManager, Operations, WorldQuery
