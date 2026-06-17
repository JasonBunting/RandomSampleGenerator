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

    // Phase 5 placeholder hook for deterministic orchestration-only attempt outcomes.
    public bool ShouldProducePlaceholderAttempt() => _processingRandom.NextDouble() >= 0.35;

    public double PickChunkStartSeconds(double maxStartSeconds) => maxStartSeconds <= 0
        ? 0
        : _processingRandom.NextDouble() * maxStartSeconds;
}
