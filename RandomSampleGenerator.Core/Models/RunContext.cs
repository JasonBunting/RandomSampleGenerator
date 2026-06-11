namespace RandomSampleGenerator.Core.Models;

public sealed class RunContext
{
    public required string RunName { get; init; }

    public required string RunFolderPath { get; init; }

    public required string LogFilePath { get; init; }

    public required DateTimeOffset RunStart { get; init; }

    public required int SongSelectionSeed { get; init; }

    public required int ProcessingSeed { get; init; }
}
