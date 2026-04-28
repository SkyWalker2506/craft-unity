# SPRINT PLAN — craft-unity

**Date:** 2026-04-22
**Runs:** 3 forge runs × up to 5 tasks each
**Repo:** SkyWalker2506/craft-unity

---

## Sprint 1 — Core Completeness (Run 1)

| # | Task ID | Title | Priority | Effort | Wave |
|---|---------|-------|----------|--------|------|
| 1 | CU-6 | Implement SandboxValidator (dryRun target + param validation) | P0 | M | W1 |
| 2 | CU-7 | Implement Craft_ListOps MCP tool (list registered operation types + schemas) | P0 | S | W1 |
| 3 | CU-8 | Add WorldQuery integration test (scene object count, component query) | P1 | S | W1 |
| 4 | CU-9 | Add timeout support to CraftEngine.Execute via CancellationToken | P1 | M | W2 |
| 5 | CU-10 | Add operation plugin pattern: CraftEngine.RegisterAssemblyOperations(assembly) | P1 | M | W2 |

## Sprint 2 — API + Reliability (Run 2)

| # | Task ID | Title | Priority | Effort | Wave |
|---|---------|-------|----------|--------|------|
| 6 | CU-11 | Implement CraftCaptureGameView real screen capture (deferred CU-4) | P0 | M | W1 |
| 7 | CU-12 | Add error recovery hint in CraftResult (suggest correct op type on unknown) | P1 | S | W1 |
| 8 | CU-13 | Add CHANGELOG.md auto-generation script from git log | P2 | S | W1 |
| 9 | CU-14 | XML documentation pass on all Operations/*.cs | P2 | S | W2 |
| 10 | CU-15 | Add CommandLog serialization (persist transaction history to EditorPrefs JSON) | P1 | M | W2 |

## Sprint 3 — Polish + ImportPackage (Run 3)

| # | Task ID | Title | Priority | Effort | Wave |
|---|---------|-------|----------|--------|------|
| 11 | CU-16 | Partial ImportUnityPackageOp implementation (path resolve + AssetDatabase.ImportPackage call) | P0 | L | W1 |
| 12 | CU-17 | Add NUnit test for SandboxValidator | P1 | S | W1 |
| 13 | CU-18 | Add Craft_BatchExecute MCP tool (multiple transaction batches) | P1 | M | W2 |
| 14 | CU-19 | Verify CraftMcpBootstrap registration in Editor play mode (integration check) | P1 | S | W2 |

---

## Verify commands
- `dotnet build` (if available) or Unity compilation check
- NUnit test pass: Window > Test Runner > EditMode

## Skip rules
- Tasks marked effort L that exceed 2hr are skipped
- ImportUnityPackageOp full implementation (async URL fetch) is >2hr — do path-resolve + sync import only
