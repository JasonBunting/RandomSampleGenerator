# 🎯 Implement Phase 3 Run Screen Behavior

## Understanding
Implement only Phase 3 main-screen behavior per spec: input constraints, run button enablement, skipped state defaults, runtime input lock with Cancel-only interaction, row visual state plumbing, and simple per-row live progress display. Avoid implementing or extending orchestration/processing/logging/cancellation backend mechanics.
## Assumptions
- Existing WinForms structure (`MainForm` + `StemRowControl`) is the intended UI surface.
- NumericUpDown controls are acceptable for controlled integer inputs.
- Row-state plumbing can be local UI state for this phase.
## Approach
I will extend `StemRowControl` with a row visual-state model and lightweight progress display, while preserving current input constraints and adding stricter controlled behavior for chunk/sample fields. Then I will update `MainForm` to support run-mode UI locking, add a Cancel button, and drive initial/transition visual states without invoking processing services. Finally, I will build to confirm compilation.
## Key Files
- RandomSampleGenerator.App/StemRowControl.cs - row state enum/plumbing, status rendering, progress display, input control behavior.
- RandomSampleGenerator.App/MainForm.cs - run/cancel UI flow, input lock, run button enablement behavior integration, and row state initialization.
## Risks & Open Questions
- Since processing is intentionally not wired in this phase, run-mode progress will be structural (0/requested with active-row indication) rather than real-time produced counts.

**Progress**: 100% [██████████]

**Last Updated**: 2026-06-17 06:07:58

## 📝 Plan Steps
- ✅ **Add row visual state model and progress display plumbing to StemRowControl.**
- ✅ **Update MainForm layout and behavior to include Cancel button and run-mode input locking.**
- ✅ **Replace Run click behavior with Phase 3 UI-mode transitions (no orchestration calls).**
- ✅ **Ensure row state initialization and quantity-driven skipped/idle behavior remain compliant.**
- ✅ **Build solution and fix any compile issues.**
- ✅ **Summarize files changed, implemented behavior, and assumptions.**

