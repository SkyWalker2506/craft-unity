# CRAFT Unity Package

## Bu proje nedir

CRAFT (Claude's Reliable AI Framework for Transactions), Unity Editor icinde AI kaynakli islemleri guvenli, geri alinabilir sekilde calistiran bir execution engine. Unity MCP uzerine transaction safety, validation, rollback ve tracing ekler.

## Mimari

- **Core/** — MCP-agnostic. CraftEngine, TransactionManager, CommandLog, TraceRecorder. Hicbir MCP namespace import etmez.
- **Operations/** — Unity API kullanan op'lar. Undo entegreli.
- **WorldQuery/** — Scene sorgulama engine'i.
- **Validation/** — StaticValidator (Tier 1). SandboxValidator (Phase 2).
- **McpTools/** — MCP adapter katmani. Sadece bu klasor `[McpTool]` attribute kullanir.
- **Runtime/** — PersistentId MonoBehaviour.

## Kurallar

1. Core/ ve Operations/ icinde MCP namespace import etme
2. Her yeni operation ICraftOperation interface'ini implement etmeli
3. Tum scene mutation'lari Undo.RecordObject / RegisterCreatedObjectUndo ile yapilmali
4. Transaction disinda scene degisikligi yapma
5. Test'ler TearDown'da cleanup yapmali

## Test

Unity Test Runner ile Editor test'lerini calistir:
- Window > General > Test Runner > EditMode > Run All
