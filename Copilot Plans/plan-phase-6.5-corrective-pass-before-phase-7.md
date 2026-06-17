# 🎯 Phase 6.5 corrective pass before Phase 7

## Understanding
Apply a minimal corrective pass to address audited Phase 6.5 gaps: remove export-sample-rate leakage from Demucs candidate chunk extraction, enforce exact chunk-length extraction success criteria, isolate Demucs retry outputs to avoid stale-file false positives, make invalid model handling explicitly non-retryable, and add precision test coverage.
## Assumptions
- Existing orchestration flow and service boundaries should remain intact.
- Candidate chunk extraction can preserve source sample rate/channels while still emitting PCM WAV.
- Demucs retries should remain 2 retries after the initial attempt.
## Approach
I will update `CandidateChunkService` to preserve source rate/channels, emit PCM WAV, and require exact expected data length (with frame-alignment tolerance). I will update `RunOrchestrator` call sites to stop passing export sample rate. I will update `StemSeparationService` retry logic to isolate retry outputs and mark invalid model as explicit non-retryable. Then I will add focused tests for chunk byte-length precision and invalid-model/retry behavior and validate via build/tests.
## Key Files
- RandomSampleGenerator.Core/Services/CandidateChunkService.cs
- RandomSampleGenerator.Core/Services/RunOrchestrator.cs
- RandomSampleGenerator.Core/Services/StemSeparationService.cs
- RandomSampleGenerator.Core.Tests/Services/CandidateChunkServiceTests.cs
- RandomSampleGenerator.Core.Tests/Services/StemSeparationServiceTests.cs
- RandomSampleGenerator.Core.Tests/Services/RunOrchestratorTests.cs

**Progress**: 100% [██████████]

**Last Updated**: 2026-06-17 20:54:09

## 📝 Plan Steps
- ✅ **Update CandidateChunkService extraction format and exact-length success contract.**
- ✅ **Update RunOrchestrator to remove export-sample-rate dependency from candidate extraction.**
- ✅ **Update StemSeparationService retry handling for isolated outputs and explicit non-retryable invalid model behavior.**
- ✅ **Add/adjust tests for chunk length precision and retry/non-retryable model behavior.**
- ✅ **Build and run Core tests, then fix any regressions.**
- ✅ **Summarize changes and assumptions.**

