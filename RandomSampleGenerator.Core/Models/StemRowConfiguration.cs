namespace RandomSampleGenerator.Core.Models;

public sealed class StemRowConfiguration
{
    public string StemType { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int CandidateChunkLengthSeconds { get; set; } = 10;

    public int FinalSampleLengthSeconds { get; set; } = 1;
}
