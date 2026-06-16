# Sample Harvester v1 Technical Spec

## Purpose
Build a Windows GUI application in C# that creates short WAV samples from a user-selected source music library.

The app should:
- Scan a configured source folder recursively for audio files.
- Randomly pick source songs from the pool.
- For each requested stem type, randomly pick a candidate chunk from a chosen song.
- Run stem separation using the configured model for that stem type.
- Export exactly one final sample from that attempt.
- Write a per-run manifest JSON for traceability.
- Write a per-run log file for diagnostics.

The app is **not** a DAW or sample editor. It does not do creative shaping such as pitch, gain, fades, envelopes, trimming artistry, or post-processing beyond required export conversion.

## Product Philosophy
- Randomness is a feature.
- The user is assumed to be competent.
- The app should stay out of the way.
- The destination sampler or groovebox is where detailed sample shaping happens.
- Traceability matters.
- Replayability is nice-to-have, not the primary purpose.

## Primary User Workflow
1. User launches app.
2. On first run, user selects an existing source folder and an existing target folder.
3. App persists those paths.
4. Main screen shows a fixed alphabetical stem table.
5. User selects a model and quantity per stem row, and also per-row candidate chunk length and final sample length.
6. User clicks Run.
7. App creates the run folder and log file immediately.
8. App scans the source folder recursively and builds the source pool.
9. App processes rows sequentially, skipping rows with quantity 0.
10. App updates live row progress in the UI.
11. App writes exported WAV files directly into the run folder.
12. At the end, app writes `run-manifest.json`.
13. App shows completion message including output path, and may optionally auto-open the run folder.

## Out of Scope for v1
- In-app audio preview.
- Drag and drop.
- Persistent multi-run stem cache.
- Full replay UI.
- Fine-grained audio event detection such as kick or snare classification.
- Hash-based source identity.
- Crash recovery.
- Aggressive preflight analysis of the source library.
- Source file modification.

## Supported Stem Types
Fixed alphabetical row order:
- bass
- drums
- guitar
- other
- piano
- vocals

## Supported Separation Models
Initial known models:
- `htdemucs` -> vocals, other, drums, bass
- `htdemucs_6s` -> piano, guitar, vocals, other, drums, bass

Model choice is configured per stem row on the main screen and persists across runs.

## Main Screen Layout
Each fixed row contains:
- stem type label
- model dropdown
- quantity input
- candidate chunk length input
- final sample length input

Behavior:
- Quantity defaults to 0.
- Quantity accepts integers only, 0 through 99.
- Candidate chunk length accepts whole seconds only, 10 through 30.
- Final sample length accepts whole seconds only, 1 through candidate chunk length.
- Rows with quantity 0 are visually marked as `Skipped` from the start.
- Run button is disabled unless at least one row has quantity > 0.
- During a run, all input is disabled except Cancel.

## Settings / Config Dialog
Contains:
- source folder path
- target folder path
- global export sample rate
- global export bit depth
- global export file format (WAV only in v1)
- option to auto-open output folder after run completion
- option to disable app logging later
- hidden or advanced validation settings
- optional setting: max distinct stem types a single song may contribute in a run

Defaults:
- Logging enabled.
- Auto-open output folder configurable.
- Max distinct stem types per song per run defaults to 3.

## First-Run Setup
On first launch:
- Prompt user for existing source folder.
- Prompt user for existing target folder.
- Verify target folder is writable by creating and deleting a small temp file.
- Persist both paths.

## Run Folder and Log Naming
Each run gets its own subfolder under the target root.

Run folder naming pattern:
- `YYYYMMDD Sample Run 00`
- ordinal is scoped per day

Examples:
- `20260130 Sample Run 00`
- `20260130 Sample Run 01`

A shared logs folder exists under the main target root:
- `<TargetRoot>\logs\`

Each run gets one log file whose name matches the run folder name:
- `20260130 Sample Run 00.log`

## Export File Naming
Exported sample filenames follow this pattern:
- `sourceFileNameWithoutWhitespaceOrExtension-stemType-ordinal.wav`

Examples:
- `MySong-drums-00.wav`
- `MySong-bass-00.wav`
- `MySong-drums-01.wav`

Rules:
- Ordinal is scoped to the `(source file + stem type)` pair.
- If sanitized source names collide, append source file size as a modifier.
- If same sanitized name and same exact file size occur, treat them as the same source identity for naming purposes.
- Full original source path is always preserved in the manifest.

## Core Run Logic
### Source Pool
At run start:
- Recursively scan the configured source folder.
- Build the pool of audio files.
- All songs in the pool are eligible for any requested stem type.

### Processing Order
- Process rows sequentially in fixed UI order.
- Skip quantity 0 rows.
- For a row with quantity > 0, keep attempting until enough samples are produced, unless user cancels or row fails.

### Attempt Logic
For one requested stem type attempt:
1. Pick a random song from the global source pool.
2. Pick a random candidate chunk within that source song.
3. Run the configured separation model for the row’s stem type.
4. Select/export exactly one final sample from that attempt.
5. Write the sample directly into the final run folder.

Rules:
- One attempt targets exactly one stem type.
- One candidate chunk yields at most one exported sample.
- No minimum distance between chunk picks.
- Overlap is allowed.
- Same song can be reused later in the run.
- A song may contribute to up to 3 distinct stem types per run by default.

## Randomness and Replay Support
The system should be designed for future replay-from-manifest support, but without exposing that UI in v1.

Use two seeds:
- `songSelectionSeed`: used to select songs from the full discovered pool during the original run
- `processingSeed`: used for downstream random choices after songs are selected

Manifest should also include a compact replay support structure:
- source file map
- ordered chosen-song list referencing that map

This replay support data is secondary to traceability.

## Manifest
Each run writes exactly one file in the run folder:
- `run-manifest.json`

Purpose:
- Traceability first
- Replay support second

Manifest includes:
- overall run status
- run start and end timestamps
- source root path
- target root path
- run folder path
- run name
- all persisted relevant config used for the run
- all row settings used for the run
- global export settings used for the run
- `songSelectionSeed`
- `processingSeed`
- replay support source map and ordered pick list
- per-row results summary
- per-export result records

### Overall Run Status Values
- `Completed`
- `Cancelled`
- `Failed`

### Per-Row Status Values
- `Completed`
- `Partial`
- `Failed`
- `Cancelled`
- `Skipped`

Definitions:
- `Completed`: produced exactly requested count
- `Partial`: produced at least 1 but fewer than requested
- `Failed`: produced 0 or encountered actual error
- `Cancelled`: user cancelled during that row
- `Skipped`: requested quantity was 0

Overall run status rules:
- If user cancels at any point, overall run status is `Cancelled`.
- Otherwise, if any row is `Failed` or `Partial`, overall run status is `Failed`.
- Otherwise, overall run status is `Completed`.

### Per-Row Summary Fields
Each row summary should include at least:
- stem type
- selected model
- requested count
- produced count
- candidate chunk length
- final sample length
- row status

### Per-Export Record Fields
Each exported sample record should include at least:
- full source file path
- candidate chunk start time
- candidate chunk duration
- model used
- stem type used
- exported filename
- exported full path

## Logging
Logging is enabled by default.

Requirements:
- One log file per run
- Stored in shared `<TargetRoot>\logs\` folder
- Log file name matches run folder name
- Logging can later be disabled in config

Logs are for diagnostics and tracing, not for end-user workflow.

## Validation Rules
### Before Run Starts
Block run if:
- configured source folder does not exist
- configured target folder does not exist
- target folder is not writable
- source folder is inaccessible
- no row has quantity > 0
- any row value violates controlled UI rules

Do not block run for:
- model executable/dependency not found
- lack of aggressive source analysis

### Heuristics
The app may have simple plausibility checks.

Types:
- warning-only checks
- hard validation checks

Hard checks are configuration-driven and not a major UI feature.

v1 should keep plausibility checks simple and conservative.

## Cancellation Behavior
- Cancel button is always available during a run.
- Cancel requires confirmation.
- Cancel should abort immediately.
- Partial outputs are kept.
- Partial files are left alone.
- Run folder is left as-is.
- No cleanup heroics.

## Failure Behavior
- A failure in one row should skip that row and continue to the next row.
- If any row ends `Failed` or `Partial`, overall run status becomes `Failed` unless user cancelled.
- No crash-recovery logic in v1.
- If app is killed abruptly, whatever is on disk is on disk.

## Live UI State During Run
The UI should show each row visually as one of:
- `Skipped`
- `Idle`
- `Active`
- `Completed`
- `Partial`
- `Failed`
- `Cancelled`

Live display should emphasize:
- current row
- current progress count toward requested count
- simple visual progress by row

Do not clutter the UI with failure counters or low-level diagnostics.

## FAQ Notes to Preserve
These should be documented later in user-facing FAQ/help:
- Some source files may not be included if they are too short.
- Logging can be disabled in config.
- File naming collision handling may append file size.
- Same sanitized name plus same file size is treated as the same source identity for naming purposes.
- Replay from manifest is best-effort and secondary to traceability.
- Changes in the source library can affect replay behavior.
- The app is not responsible for final trimming, pitch, volume, or sampler-specific shaping.

## Suggested Technical Architecture
Keep the app modular so Copilot can help in isolated pieces.

Suggested components:
- `ConfigurationService`
- `RunOrchestrator`
- `SourcePoolScanner`
- `RowProcessor`
- `RandomizationService`
- `StemSeparationService`
- `SampleExportService`
- `ManifestBuilder`
- `LoggingService`
- `RunFolderService`
- `ValidationService`

Suggested data models:
- `AppConfiguration`
- `RunConfiguration`
- `StemRowConfiguration`
- `RunContext`
- `RunResult`
- `RowResult`
- `ExportedSampleRecord`
- `ReplaySupportData`

## Build Order for Copilot
Implement in these pieces, in order.

### Phase 1: Configuration and App Shell
Goal:
- Create desktop app shell and persistent config.

Tasks:
- First-run setup flow
- Persist source/target paths
- Persist model selections
- Settings dialog
- Validate existing paths and target writability
- Fixed main screen with alphabetical rows

### Phase 2: Run Identity and File System Layout
Goal:
- Make runs create predictable artifacts.

Tasks:
- Create run folder naming logic
- Create shared logs folder
- Create per-run log filename
- Create fixed manifest filename constant
- Create export filename builder

### Phase 3: Run Screen Behavior
Goal:
- Make UI rules solid before audio work.

Tasks:
- Quantity input constraints 0–99
- Candidate chunk length 10–30 whole seconds
- Final sample length 1..candidate length
- Disable Run when all rows are 0
- Disable all controls except Cancel during run
- Row visual states and progress display

### Phase 4: Source Pool Scanning
Goal:
- Build source pool at run start.

Tasks:
- Recursive source scan
- Audio file filtering by extension
- Basic runtime source access validation
- In-memory pool creation

### Phase 5: Randomization and Row Orchestration
Goal:
- Implement deterministic orchestration without audio details first.

Tasks:
- Sequential row processing
- Global song pool selection
- Two-seed design
- Song distinct-stem cap tracking
- Cancellation flow
- Per-row requested/produced tracking

### Phase 6: Stem Separation Integration
Goal:
- Integrate external separation execution.

Tasks:
- Per-row model selection wiring
- Candidate chunk extraction temp workflow
- Run one stem type per attempt
- Handle execution errors and row failure semantics
- Leave temp cache run-scoped only

### Phase 7: Export Pipeline
Goal:
- Produce final WAV files correctly.

Tasks:
- Export directly to run folder
- Apply only technical format conversion
- No fades or creative processing
- One exported sample per successful attempt

### Phase 8: Manifest and Logging
Goal:
- Persist traceability artifacts.

Tasks:
- Write per-run log file
- Build run manifest model
- Write single manifest at end of run lifecycle
- Capture statuses, config, seeds, results, replay data

### Phase 9: Guardrails and UX Polish
Goal:
- Add practical safety rails without overbuilding.

Tasks:
- Preflight blocking validation
- Simple plausibility warnings and hard checks
- Completion message with output path
- Optional auto-open output folder
- Confirmation on model changes
- Confirmation on Cancel

## Copilot Prompt Strategy
When feeding this to Copilot, do not ask for the entire app in one shot.

Prompt one phase at a time, for example:
- “Create the C# models for AppConfiguration, RunConfiguration, StemRowConfiguration, RunResult, RowResult, and ExportedSampleRecord based on this spec.”
- “Implement run folder naming and per-day ordinal logic.”
- “Build validation rules for the main screen controls.”
- “Implement a manifest writer matching this schema.”
- “Implement sequential row orchestration with cancellation support.”

## v1 Success Criteria
v1 is successful if:
- The app can be configured and run without manual file wrangling each time.
- It creates run folders and logs predictably.
- It processes requested stem rows sequentially.
- It exports WAV files into the correct run folder.
- It writes a manifest with enough traceability to understand what happened.
- It stays intentionally narrow and does not turn into a sample editor.

