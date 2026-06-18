# Live Demucs Output Window Spec

## Goal

Expose live stdout/stderr from the active Demucs process in a simple window so the user can:

* see that work is actively progressing
* inspect raw Demucs output while a run is in progress
* scroll through prior output
* keep the output visible after the process finishes
* review output after success, cancel, or failure

This is a visibility/debugging feature, not a replacement for logs.

## Scope

Implement a lightweight output window for live external-process output, starting with Demucs.

## Core behavior

When a Demucs process starts for an attempt:

* open a simple output window, or bring the existing one to front if already open
* stream stdout and stderr into that window live
* clearly label which run / stem / model / source attempt the output belongs to
* preserve prior lines in the window; do not clear automatically between attempts unless explicitly chosen later
* keep the window open after the run finishes or fails
* do not auto-close the window

## Window behavior

The output window should:

* be modeless
* allow scrolling
* allow text selection
* show appended lines live
* remain usable while the main app continues running
* remain open after completion/failure/cancel until the user closes it manually

A simple design is enough:

* title bar
* read-only multiline text area
* optional status line at top
* optional clear button later, but not required now

## Content shown

Append raw process output as text lines.

Include at least:

* process start banner
* command context summary
* stdout lines
* stderr lines
* process exit code
* retry notices
* cancellation notice
* process finished banner

Example contextual lines:

* `=== Run 20260618 Sample Run 00 ===`
* `Starting Demucs attempt`
* `Stem: vocals`
* `Model: htdemucs`
* `Source: C:\music\song01.mp3`
* `Chunk: start 00:01:23, duration 10s`
* `[stdout] ...`
* `[stderr] ...`
* `Exit code: 0`

## Stream labeling

Each appended line should preserve source identity.

Recommended prefixes:

* `[stdout]`
* `[stderr]`
* `[system]`

This avoids ambiguity without fancy UI work.

## Lifetime and accumulation

The window should accumulate output for the whole run.

Rules:

* multiple attempts in the same run append to the same window
* retries append to the same window
* if a new run starts later, either:

  * append a clear run header to the same window, or
  * open a new output window for the new run

Preferred v1.1 behavior:

* one output window per app session
* append a clear header for each new run

## Failure and cancel behavior

If the process fails or is cancelled:

* keep all captured output visible
* append a final system line stating failure/cancel
* do not clear the window
* do not close the window automatically

## Relationship to logs

This window is not the primary traceability artifact.

Rules:

* logs remain the durable artifact
* this window is for live visibility and quick inspection
* later, the same output may also be mirrored to logs, but that is not required by this feature alone if already captured elsewhere

## Main app integration

The main app should provide a simple way to show the output window.

Minimum acceptable behavior:

* auto-show it when Demucs starts

Nice-to-have later:

* menu item or button to reopen it if hidden

## Threading / process integration

The output window must receive live data from asynchronous process output callbacks.

Requirements:

* capture stdout asynchronously
* capture stderr asynchronously
* marshal UI updates safely to the UI thread
* append lines in arrival order as reasonably as possible
* do not block the UI waiting on process reads

## Retry behavior

When a retry happens:

* append a visible retry banner
* include retry attempt number
* continue appending output in the same window

Example:

* `[system] Demucs attempt failed; retry 1 of 2 starting`

## Minimal UX rules

Do:

* keep it simple
* show raw output
* keep it open
* let the user scroll back

Do not:

* parse Demucs output into structured progress yet
* build a rich terminal emulator
* auto-close or auto-clear
* hide stderr

## Suggested implementation shape

Possible components:

* `ProcessOutputWindow`
* `ProcessOutputSink` or `LiveProcessOutputService`
* event/callback from `StemSeparationService` for output lines

Suggested flow:

1. `StemSeparationService` starts process
2. stdout/stderr handlers emit text lines
3. main app or output service forwards lines to the output window
4. window appends lines in a read-only text control

## Required persisted behavior

No persistence required yet.

If the app closes, the window contents can be lost.
The durable artifact is still the run log.

## Success criteria

This feature is complete when:

* starting Demucs shows live stdout/stderr in a separate window
* the user can scroll and inspect output while the run continues
* output remains visible after success/failure/cancel
* retries and multiple attempts are clearly delineated
* the main UI stays responsive
