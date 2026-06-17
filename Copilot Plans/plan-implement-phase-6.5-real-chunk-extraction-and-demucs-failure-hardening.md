# 🎯 Implement Phase 6.5 Real Chunk Extraction and Demucs Failure Hardening

## Understanding
Implement only Phase 6.5 by replacing placeholder candidate chunk generation with real NAudio-based extraction for `.wav`/`.mp3`, adding runtime too-short handling, wiring true random chunk start selection, and hardening Demucs execution with automatic retries for technical failures.
## Assumptions
- Existing Phase 6 services (`CandidateChunkService`, `StemSeparationService`, `RunOrchestrator`) are the intended integration points.
- Adding `NAudio` as a Core dependency is acceptable for this phase requirement.
- Existing row/run status semantics from Phase 5 remain unchanged.
## Approach
I will update `CandidateChunkService` to decode source files with NAudio and write PCM WAV chunk files from valid random start positions. I will update orchestration to use file durations for random start selection and treat too-short sources as failed attempts that continue looping. Then I will harden `StemSeparationService` with technical retry logic (initial try + 2 retries) and make failure details available in result text. I will add/adjust unit tests and run build/tests.
## Key Files
- RandomSampleGenerator.Core/RandomSampleGenerator.Core.csproj - add NAudio dependency.
- RandomSampleGenerator.Core/Services/CandidateChunkService.cs - real extraction, duration handling, too-short signaling.
- RandomSampleGenerator.Core/Services/RunOrchestrator.cs - use real chunk-start randomization and too-short attempt handling.
- RandomSampleGenerator.Core/Services/StemSeparationService.cs - technical retry hardening.
- RandomSampleGenerator.Core.Tests/Services/RunOrchestratorTests.cs - adjust for new chunk extraction requirements.
- RandomSampleGenerator.Core.Tests/Services/StemSeparationServiceTests.cs - add retry/technical failure behavior tests.
## Risks & Open Questions
- Unit tests should avoid requiring actual NAudio decoding of MP3 assets in repo; use generated WAV for deterministic tests and fake process runners.
- Runtime Demucs availability still external; failures should remain runtime row failures.

**Progress**: 100% [██████████]

**Last Updated**: 2026-06-17 20:39:28

## 📝 Plan Steps
- ✅ **Add NAudio dependency and refactor CandidateChunkService for real WAV/MP3 decoding and PCM WAV chunk extraction.**
- ✅ **Update RunOrchestrator attempt flow to compute valid random chunk start from actual source duration and handle too-short files as failed attempts.**
- ✅ **Add Demucs technical retry logic (2 retries after initial attempt) to StemSeparationService.**
- ✅ **Update and add tests for retry behavior, too-short handling, and orchestrator flow compatibility.**
- ✅ **Build solution and run relevant tests; fix regressions.**
- ✅ **Summarize changed files, implementation details, and assumptions.**

