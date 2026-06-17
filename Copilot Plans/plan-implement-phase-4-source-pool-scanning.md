# 🎯 Implement Phase 4 Source Pool Scanning

## Understanding
Implement only Phase 4 behavior: recursive source scanning at run start, include only `.wav`/`.mp3`, block run with user-facing message if source folder is missing/inaccessible, and keep the discovered source pool in memory for later phases.
## Assumptions
- Current WinForms run-start entry point remains `MainForm.OnRunClick`.
- No processing/orchestration should be added in this phase.
- In-memory pool can be stored on `MainForm` for now as phase handoff state.
## Approach
I will tighten `SourcePoolScanner` extension filtering to exactly v1 types and keep recursive scan behavior. Then I will update `MainForm` run-start flow to validate source existence/accessibility before entering run mode, scan once at run start, and keep the resulting pool in a field. I will add focused unit tests for scanner recursive filtering behavior.
## Key Files
- RandomSampleGenerator.Core/Services/SourcePoolScanner.cs - recursive scan + extension filter.
- RandomSampleGenerator.App/MainForm.cs - run-start validation + in-memory pool creation.
- RandomSampleGenerator.Core.Tests/Services/SourcePoolScannerTests.cs - scanner behavior tests.
## Risks & Open Questions
- Recursive scan can fail on inaccessible nested directories; this phase keeps behavior simple and blocks run when scan throws.

**Progress**: 100% [██████████]

**Last Updated**: 2026-06-17 06:20:04

## 📝 Plan Steps
- ✅ **Restrict SourcePoolScanner supported extensions to `.wav` and `.mp3` while preserving recursive scan.**
- ✅ **Add run-start source folder existence/accessibility validation and in-memory pool creation in MainForm.**
- ✅ **Add unit tests for recursive scan and extension filtering behavior.**
- ✅ **Build and run relevant tests.**
- ✅ **Summarize files changed, implemented behavior, and assumptions.**

