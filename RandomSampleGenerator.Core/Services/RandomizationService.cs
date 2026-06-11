namespace RandomSampleGenerator.Core.Services;

public sealed class RandomizationService
{
    private readonly Random _songRandom;
    private readonly Random _processingRandom;

    public RandomizationService(int songSelectionSeed, int processingSeed)
    {
        _songRandom = new Random(songSelectionSeed);
        _processingRandom = new Random(processingSeed);
    }

    public int PickSongIndex(int songCount) => _songRandom.Next(songCount);

    public double PickChunkStartSeconds(double maxStartSeconds) => maxStartSeconds <= 0
        ? 0
        : _processingRandom.NextDouble() * maxStartSeconds;
}
