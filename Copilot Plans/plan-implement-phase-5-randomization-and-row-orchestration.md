# 🎯 Implement Phase 5 Randomization and Row Orchestration

## Understanding
Implement only Phase 5 orchestration behavior using the existing app and core structure: sequential fixed-order row processing, global source pool usage from Phase 4, deterministic two-seed randomness plumbing, distinct-stem cap tracking, cancellation handling, and per-row/overall status tracking. Exclude real audio processing, export, manifest, and logging.
## Assumptions
- Existing Phase 4 run-start scan in `MainForm` should supply the global source pool to orchestration.
- `RunOrchestrator` is the correct place for sequencing/randomization/state rules.
- UI can consume lightweight progress callbacks to drive row visual state/progress.
## Approach
I will refactor `RunOrchestrator` to a Phase-5 skeleton that keeps deterministic orchestration and status outcomes but removes manifest/export/logging behavior. It will process rows sequentially in fixed stem order, use the provided global source pool, enforce distinct-stem caps, support cancellation, and expose progress updates via callback. Then I will wire `MainForm` run/cancel flow to execute orchestrator asynchronously and map progress/final statuses into existing row visual states. Finally, I will update and add core tests for the new orchestration contract.
## Key Files
- RandomSampleGenerator.Core/Services/RunOrchestrator.cs - Phase 5 orchestration logic and placeholder attempt behavior.
- RandomSampleGenerator.Core/Services/RandomizationService.cs - deterministic processing-seed placeholder decision hook.
- RandomSampleGenerator.App/MainForm.cs - run execution/cancel wiring and row UI updates from orchestration progress.
- RandomSampleGenerator.Core.Tests/Services/RunOrchestratorTests.cs - update tests for orchestration-only outcomes.
## Risks & Open Questions
- Existing tests currently assert manifest/export side effects; these must be adjusted to Phase 5 scope.
- UI thread marshalling is required for progress updates from background orchestration.

**Progress**: 100% [██████████]

**Last Updated**: 2026-06-17 06:37:10

## 📝 Plan Steps
- ✅ **Refactor RunOrchestrator to remove export/log/manifest work and implement Phase 5 sequential deterministic placeholder orchestration using supplied global source pool.**
- ✅ **Add deterministic processing-seed placeholder hook in RandomizationService and integrate it into per-attempt production logic.**
- ✅ **Wire MainForm run/cancel flow to execute orchestrator with the Phase 4 source pool and update row visual/progress state from orchestration updates.**
- ✅ **Update/add unit tests for Phase 5 orchestration behavior (sequential processing, skipped rows, cancellation, statuses, two-seed output).**
- ✅ **Build and run relevant tests.**
- ✅ **Summarize files changed, implemented behavior, and assumptions.**

