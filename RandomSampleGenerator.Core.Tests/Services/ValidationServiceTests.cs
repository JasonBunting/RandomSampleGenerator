using RandomSampleGenerator.Core.Models;
using RandomSampleGenerator.Core.Services;

namespace RandomSampleGenerator.Core.Tests.Services;

public sealed class ValidationServiceTests
{
    [Fact]
    public void ValidateBeforeRun_RejectsInvalidRowValues()
    {
        var source = Path.Combine(Path.GetTempPath(), $"rsg-source-{Guid.NewGuid():N}");
        var target = Path.Combine(Path.GetTempPath(), $"rsg-target-{Guid.NewGuid():N}");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(target);

        try
        {
            var config = new AppConfiguration
            {
                SourceFolderPath = source,
                TargetFolderPath = target
            };

            var runConfiguration = new RunConfiguration
            {
                AppConfiguration = config,
                StemRows =
                [
                    new StemRowConfiguration
                    {
                        StemType = "drums",
                        Model = "htdemucs",
                        Quantity = 1,
                        CandidateChunkLengthSeconds = 9,
                        FinalSampleLengthSeconds = 10
                    }
                ]
            };

            var sut = new ValidationService();
            var errors = sut.ValidateBeforeRun(runConfiguration);

            Assert.Contains(errors, error => error.Contains("Candidate chunk length", StringComparison.Ordinal));
            Assert.Contains(errors, error => error.Contains("Final sample length", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(source, true);
            Directory.Delete(target, true);
        }
    }
}
