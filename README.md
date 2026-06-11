# RandomSampleGenerator
A Windows GUI application in C# that creates short WAV samples from a user-selected source music library.

## Solution Layout

- `/RandomSampleGenerator.App` - Windows Forms application shell.
- `/RandomSampleGenerator.Core` - Core configuration, validation, orchestration, manifest, logging, and export services.
- `/RandomSampleGenerator.Core.Tests` - Focused unit tests for run folders, validation, naming, manifest writing, and orchestration flow.

## Build and Test

```bash
dotnet build /home/runner/work/RandomSampleGenerator/RandomSampleGenerator/JasonBunting/RandomSampleGenerator/RandomSampleGenerator.slnx
dotnet test /home/runner/work/RandomSampleGenerator/RandomSampleGenerator/JasonBunting/RandomSampleGenerator/RandomSampleGenerator.slnx
```
