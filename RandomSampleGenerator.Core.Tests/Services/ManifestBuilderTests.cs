using System.Text.Json;
using RandomSampleGenerator.Core.Models;
using RandomSampleGenerator.Core.Services;

namespace RandomSampleGenerator.Core.Tests.Services;

public sealed class ManifestBuilderTests
{
    [Fact]
    public void WriteManifest_WritesExpectedFileNameAndContent()
    {
        var runFolder = Path.Combine(Path.GetTempPath(), $"rsg-manifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(runFolder);

        try
        {
            var result = new RunResult
            {
                Status = RunStatus.Completed,
                RunStart = DateTimeOffset.UtcNow,
                RunEnd = DateTimeOffset.UtcNow,
                SourceRootPath = "C:/Source",
                TargetRootPath = "C:/Target",
                RunFolderPath = runFolder,
                RunName = "20260130 Sample Run 00",
                ConfigurationUsed = new AppConfiguration(),
                RowSettingsUsed =
                [
                    new StemRowConfiguration
                    {
                        StemType = "drums",
                        Model = "htdemucs",
                        Quantity = 1,
                        CandidateChunkLengthSeconds = 10,
                        FinalSampleLengthSeconds = 1
                    }
                ],
                SongSelectionSeed = 1,
                ProcessingSeed = 2,
                ReplaySupportData = new ReplaySupportData(),
                RowResults =
                [
                    new RowResult
                    {
                        StemType = "drums",
                        Model = "htdemucs",
                        RequestedCount = 1,
                        ProducedCount = 1,
                        CandidateChunkLengthSeconds = 10,
                        FinalSampleLengthSeconds = 1,
                        Status = RowStatus.Completed
                    }
                ],
                ExportedSamples = []
            };

            var sut = new ManifestBuilder();
            var path = sut.WriteManifest(runFolder, result);

            Assert.Equal(Path.Combine(runFolder, "run-manifest.json"), path);
            Assert.True(File.Exists(path));

            var json = File.ReadAllText(path);
            var manifest = JsonSerializer.Deserialize<RunResult>(json);
            Assert.NotNull(manifest);
            Assert.Equal(RunStatus.Completed, manifest.Status);
            Assert.Equal("20260130 Sample Run 00", manifest.RunName);
        }
        finally
        {
            Directory.Delete(runFolder, true);
        }
    }
}
