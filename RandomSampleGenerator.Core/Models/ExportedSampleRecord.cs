namespace RandomSampleGenerator.Core.Models;

public sealed class ExportedSampleRecord
{
    public required string SourceFilePath { get; init; }

    public required double CandidateChunkStartSeconds { get; init; }

    public required double CandidateChunkDurationSeconds { get; init; }

    public required string ModelUsed { get; init; }

    public required string StemTypeUsed { get; init; }

    public required string ExportedFileName { get; init; }

    public required string ExportedFullPath { get; init; }
}
