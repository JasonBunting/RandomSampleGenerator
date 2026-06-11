using RandomSampleGenerator.Core.Models;
using RandomSampleGenerator.Core.Services;

namespace RandomSampleGenerator.Core.Tests.Services;

public sealed class RunOrchestratorTests
{
    [Fact]
    public void Run_ProducesExportsAndManifest()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), $"rsg-source-{Guid.NewGuid():N}");
        var targetRoot = Path.Combine(Path.GetTempPath(), $"rsg-target-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceRoot);
        Directory.CreateDirectory(targetRoot);
        File.WriteAllBytes(Path.Combine(sourceRoot, "Song A.wav"), new byte[256]);

        try
        {
            var runConfiguration = new RunConfiguration
            {
                AppConfiguration = new AppConfiguration
                {
                    SourceFolderPath = sourceRoot,
                    TargetFolderPath = targetRoot,
                    ExportSampleRate = 44100,
                    ExportBitDepth = 16,
                    LoggingEnabled = true
                },
                StemRows =
                [
                    new StemRowConfiguration
                    {
                        StemType = "drums",
                        Model = "htdemucs",
                        Quantity = 1,
                        CandidateChunkLengthSeconds = 10,
                        FinalSampleLengthSeconds = 1
                    }
                ]
            };

            var sut = new RunOrchestrator(
                new SourcePoolScanner(),
                new RunFolderService(),
                new ValidationService(),
                new ManifestBuilder(),
                new SampleExportService(),
                new ExportFileNameBuilder());

            var result = sut.Run(runConfiguration);

            Assert.Equal(RunStatus.Completed, result.Status);
            Assert.Single(result.ExportedSamples);
            Assert.True(File.Exists(result.ExportedSamples[0].ExportedFullPath));
            Assert.True(File.Exists(Path.Combine(result.RunFolderPath, RunFolderService.ManifestFileName)));
            Assert.True(File.Exists(Path.Combine(targetRoot, "logs", $"{result.RunName}.log")));
        }
        finally
        {
            Directory.Delete(sourceRoot, true);
            Directory.Delete(targetRoot, true);
        }
    }
}
