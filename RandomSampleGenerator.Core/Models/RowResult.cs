namespace RandomSampleGenerator.Core.Models;

public sealed class RowResult
{
    public required string StemType { get; init; }

    public required string Model { get; init; }

    public required int RequestedCount { get; init; }

    public required int ProducedCount { get; init; }

    public required int CandidateChunkLengthSeconds { get; init; }

    public required int FinalSampleLengthSeconds { get; init; }

    public required RowStatus Status { get; init; }
}
