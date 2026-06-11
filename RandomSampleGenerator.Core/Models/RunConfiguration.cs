namespace RandomSampleGenerator.Core.Models;

public sealed class RunConfiguration
{
    public required AppConfiguration AppConfiguration { get; init; }

    public required IReadOnlyList<StemRowConfiguration> StemRows { get; init; }
}
