namespace RandomSampleGenerator.Core.Models;

public sealed class RunResult
{
    public required RunStatus Status { get; init; }

    public required DateTimeOffset RunStart { get; init; }

    public required DateTimeOffset RunEnd { get; init; }

    public required string SourceRootPath { get; init; }

    public required string TargetRootPath { get; init; }

    public required string RunFolderPath { get; init; }

    public required string RunName { get; init; }

    public required AppConfiguration ConfigurationUsed { get; init; }

    public required IReadOnlyList<StemRowConfiguration> RowSettingsUsed { get; init; }

    public required int SongSelectionSeed { get; init; }

    public required int ProcessingSeed { get; init; }

    public required ReplaySupportData ReplaySupportData { get; init; }

    public required IReadOnlyList<RowResult> RowResults { get; init; }

    public required IReadOnlyList<ExportedSampleRecord> ExportedSamples { get; init; }
}
