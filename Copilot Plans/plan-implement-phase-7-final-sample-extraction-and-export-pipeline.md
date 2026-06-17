# 🎯 Implement Phase 7 Final Sample Extraction and Export Pipeline

## Understanding
Implement only Phase 7 by extending the current pipeline from successful Demucs stem output to final-sample region selection and WAV export into the run folder, while preserving existing orchestration behavior and avoiding manifest/logging/cleanup work.
## Assumptions
- Existing Phase 6.5 services and orchestration loop remain the integration points.
- NAudio is already available and can be reused for final sample extraction/export conversion.
- Export settings currently in configuration are authoritative for sample rate/bit depth.
## Approach
I will add real final-sample extraction/export logic in `SampleExportService`, then wire `RunOrchestrator` attempt success criteria to require separation success + valid final region + successful export. Export naming will use existing `ExportFileNameBuilder`; exported records will capture the additional in-memory trace fields needed for later manifest work. Finally I will update tests and validate build/tests.
## Key Files
- RandomSampleGenerator.Core/Services/SampleExportService.cs
- RandomSampleGenerator.Core/Services/RunOrchestrator.cs
- RandomSampleGenerator.Core/Models/ExportedSampleRecord.cs
- RandomSampleGenerator.Core.Tests/Services/RunOrchestratorTests.cs

**Progress**: 100% [██████████]

**Last Updated**: 2026-06-17 21:06:49

## 📝 Plan Steps
- ✅ **Add final-sample extraction/export method to SampleExportService using configured export settings and WAV output only.**
- ✅ **Extend ExportedSampleRecord with final-sample region fields needed for Phase 8 handoff.**
- ✅ **Wire RunOrchestrator attempt loop to select final region from separated stem, handle too-short stems as failed attempts, export final WAV to run folder, and record exported sample metadata.**
- ✅ **Update RunOrchestrator tests/fakes for real exported output expectations.**
- ✅ **Build and run Core tests; fix regressions.**
- ✅ **Summarize changed files, implemented behavior, and assumptions.**

