# 🎯 Implement Phase 2 Run Identity and File Layout

## Understanding
Phase 2 requires predictable run artifacts: deterministic run folder naming, shared logs folder + per-run log file naming, a fixed manifest filename constant, and export filename building rules. The workspace already includes partial implementations, so the goal is to align behavior and tests with the Phase 2 spec and close any gaps.
## Assumptions
- Phase 1 UI/configuration work is already complete and should remain unchanged.
- Phase 2 changes should primarily live in Core services and tests.
- Existing orchestration flow should continue to create run artifacts at run start.
## Approach
I will verify current implementations of run artifact naming and export naming against the spec, then make focused updates in services where logic is incomplete or fragile. I will then add/adjust unit tests to lock in the Phase 2 contract (run folder/log naming, manifest constant usage, and export filename collision/ordinal behavior). Finally, I will run the test project and fix any regressions.

Core files likely impacted are [RunFolderService.cs](RandomSampleGenerator.Core/Services/RunFolderService.cs), [ExportFileNameBuilder.cs](RandomSampleGenerator.Core/Services/ExportFileNameBuilder.cs), and possibly [RunOrchestrator.cs](RandomSampleGenerator.Core/Services/RunOrchestrator.cs) for usage alignment.
## Key Files
- RandomSampleGenerator.Core/Services/RunFolderService.cs - creates run folder, logs folder, and per-run log path.
- RandomSampleGenerator.Core/Services/ExportFileNameBuilder.cs - builds export filenames and collision handling.
- RandomSampleGenerator.Core/Services/ManifestBuilder.cs - consumes fixed manifest filename constant.
- RandomSampleGenerator.Core.Tests/Services/RunFolderServiceTests.cs - validates run identity/file layout behavior.
- RandomSampleGenerator.Core.Tests/Services/ExportFileNameBuilderTests.cs - validates export filename spec behavior.
- RandomSampleGenerator.Core.Tests/Services/ManifestBuilderTests.cs - validates manifest naming contract.
## Risks & Open Questions
- Existing run ordinal parsing currently assumes 2-digit ordinals and may break at 100+ runs in a day.
- Export filename sanitization currently removes only whitespace; spec says “without whitespace,” so punctuation retention appears acceptable, but this should remain consistent unless future phases refine it.

**Progress**: 100% [██████████]

**Last Updated**: 2026-06-16 23:50:28

## 📝 Plan Steps
- ✅ **Validate current Phase 2 behavior coverage against spec requirements.**
- ✅ **Update run artifact naming logic for robust per-day ordinal handling.**
- ✅ **Update export filename builder behavior where spec gaps exist.**
- ✅ **Adjust/add unit tests for run folder/log/manifest/export naming contracts.**
- ✅ **Run test suite for Core and fix any issues caused by changes.**
- ✅ **Summarize implemented Phase 2 scope and remaining items for next phase.**

