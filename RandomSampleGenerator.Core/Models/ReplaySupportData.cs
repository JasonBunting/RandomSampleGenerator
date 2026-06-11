namespace RandomSampleGenerator.Core.Models;

public sealed class ReplaySupportData
{
    public Dictionary<int, string> SourceFileMap { get; init; } = [];

    public List<int> OrderedChosenSongIds { get; init; } = [];
}
