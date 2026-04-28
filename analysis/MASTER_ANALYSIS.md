# MASTER ANALYSIS — craft-unity (CRAFT Framework)

**Date:** 2026-04-22
**Analyst:** Jarvis | Sonnet 4.6
**Scope:** Full codebase analysis — Unity Editor C# package

---

## Executive Summary

**Overall Score: 6.2 / 10**

CRAFT is a well-architected Unity Editor package providing transaction-safe AI operation execution on top of Unity MCP. The core engine (CraftEngine + TransactionManager + ICraftOperation) is clean, MCP-agnostic, and follows solid SOLID principles. The separation of McpTools as the single MCP-touching layer is a mature architectural decision.

Key strengths: rollback via Unity Undo, transaction lifecycle, workin test coverage for core. Key weaknesses: ImportUnityPackageOp is a stub (W5 unclosed), SandboxValidator appears empty, CaptureGameView tool deferred from run-1, no async/timeout handling in Execute, no Jira tracking set up, and CraftContextWindow uses clipboard workaround instead of a real context injection API.

---

## Scorecard

| # | Category | Score | Notes |
|---|----------|-------|-------|
| 1 | Architecture | **8.0**/10 | Clean separation Core/McpTools/Operations; MCP-agnostic core |
| 2 | Test Coverage | **5.5**/10 | TransactionManager + Operation tests exist; SandboxValidator untested; no WorldQuery integration test |
| 3 | API Completeness | **5.0**/10 | ImportUnityPackageOp is stub; CaptureGameView deferred |
| 4 | Error Handling | **5.0**/10 | Exception caught in Execute but no timeout/async safety |
| 5 | Documentation | **6.0**/10 | XML doc on tools, missing operation-level schema docs |
| 6 | Validation | **4.5**/10 | StaticValidator exists; SandboxValidator is empty shell |
| 7 | Developer Experience | **7.0**/10 | ContextPanel helpful; pre-commit hook good; PipelineVariantAuditor useful |
| 8 | Release Readiness | **5.5**/10 | No CHANGELOG automation, no npm/UPM publish CI |

**Average: 6.2/10**

---

## Top Priority Actions

| # | Task | Category | Priority | Effort | Est. |
|---|------|----------|----------|--------|------|
| 1 | Implement ImportUnityPackageOp (close stub) | API | P0 | L | 90min |
| 2 | Implement SandboxValidator (Tier 2 — dryRun schema check) | Validation | P0 | M | 45min |
| 3 | Add CraftCaptureGameView real implementation (was deferred CU-4) | API | P0 | M | 45min |
| 4 | Add WorldQuery integration test | Tests | P1 | S | 30min |
| 5 | Add timeout/cancellation to CraftEngine.Execute | Reliability | P1 | M | 45min |
| 6 | Add CraftEngine.RegisterOperation from external assemblies (plugin pattern) | Architecture | P1 | M | 45min |
| 7 | Add XML doc to all ICraftOperation implementations | Docs | P2 | S | 20min |
| 8 | Automate CHANGELOG from git commits on version bump | DX | P2 | S | 20min |
| 9 | Add async variant of Execute (Task-based) for long ops | Architecture | P2 | L | >2hr — skip |
| 10 | Add MCP tool for listing registered operations (Craft_ListOps) | API | P2 | S | 30min |

---

## Decisions

### D001: ImportUnityPackageOp stub priority
The stub already has a thorough contract spec (parameters, return type, transaction safety notes). Implementing the actual logic is highest value — it closes W5 from run-1 notes.

### D002: SandboxValidator scope for run-2
SandboxValidator should validate dryRun path: check that operation targets (scene objects) exist and parameters are within allowed ranges, without mutating Unity state.
