# RandomSampleGenerator
A Windows GUI application in C# that creates short WAV samples from a user-selected source music library.

## Solution Layout

- `/RandomSampleGenerator.App` - Windows Forms application shell.
- `/RandomSampleGenerator.Core` - Core configuration, validation, orchestration, manifest, logging, and export services.
- `/RandomSampleGenerator.Core.Tests` - Focused unit tests for run folders, validation, naming, manifest writing, and orchestration flow.

## Build and Test

```bash
dotnet build RandomSampleGenerator.slnx
dotnet test RandomSampleGenerator.slnx
```

## Tester setup note (v1)

The app requires Demucs to be installed separately and invokes it via:

`py -3.9 -m demucs ...`

Before using the app, make sure Python 3.9 is available through the Windows `py` launcher and verify:

`py -3.9 -m demucs --help`

If that command fails, fix Python/Demucs setup first.
