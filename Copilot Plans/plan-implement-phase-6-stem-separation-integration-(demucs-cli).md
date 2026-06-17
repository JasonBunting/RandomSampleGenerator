# 🎯 Implement Phase 6 Stem Separation Integration (Demucs CLI)

## Understanding
Implement only Phase 6 by integrating real stem separation via direct Demucs CLI invocation (`py -3.9 -m demucs`) with run-scoped temp chunk/input-output workflow, one-stem-per-attempt behavior, failure/cancellation handling, and orchestration wiring. Exclude export/manifest/logging and keep existing UI/orchestration shape.
## Assumptions
- Existing placeholder production logic in `RunOrchestrator` should be replaced with separation-attempt success from Demucs output existence.
- Current codebase has no audio chunk extraction service; a minimal deterministic WAV chunk placeholder file can be used as input for this phase.
- MainForm cancellation token flow from Phase 5 should drive process cancellation.
## Approach
I will add a focused separation service that: validates model selection (`htdemucs`, `htdemucs_6s`), creates run-scoped temp directories, prepares candidate chunk WAV, executes `py -3.9 -m demucs` with required args, tracks active process for cancellation, and verifies requested stem output file exists on successful exit. Then I will wire `RunOrchestrator` to call this service per attempt (one stem per attempt), updating produced counts based on real separation success. Finally I will add tests for model validation/process-contract behavior where feasible and run build/tests.
## Key Files
- RandomSampleGenerator.Core/Services/RunOrchestrator.cs - replace placeholder attempt success with separation integration.
- RandomSampleGenerator.Core/Services/StemSeparationService.cs - new Demucs CLI integration boundary.
- RandomSampleGenerator.Core/Services/CandidateChunkService.cs - minimal run-scoped candidate WAV preparation placeholder.
- RandomSampleGenerator.Core.Tests/Services/RunOrchestratorTests.cs - adjust for new injected service contract.
- RandomSampleGenerator.Core.Tests/Services/StemSeparationServiceTests.cs - validate command/model/output contract behavior.
## Risks & Open Questions
- Test environment may not have Demucs installed; tests should avoid requiring actual Demucs execution and focus on contract/validation via injected process runner abstraction.
- Real chunk extraction is not present; placeholder chunk WAV will be minimal and clearly phase-scoped.

**Progress**: 100% [██████████]

**Last Updated**: 2026-06-17 20:13:13

## 📝 Plan Steps
- ✅ **Add Demucs process execution abstractions and implement StemSeparationService with direct `py -3.9 -m demucs` invocation and cancellation-aware active-process tracking.**
- ✅ **Add minimal CandidateChunkService for run-scoped temporary candidate WAV creation and temp path management.**
- ✅ **Refactor RunOrchestrator to use StemSeparationService per attempt (one requested stem type per attempt) and base produced counts on separation success.**
- ✅ **Update MainForm and composition points for any new RunOrchestrator dependencies.**
- ✅ **Add/update unit tests for orchestration and separation service contracts.**
- ✅ **Build and run relevant tests; fix issues.**
- ✅ **Summarize files changed, implemented behavior, and assumptions.**

