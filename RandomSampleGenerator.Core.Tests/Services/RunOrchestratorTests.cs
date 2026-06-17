using RandomSampleGenerator.Core.Models;
using RandomSampleGenerator.Core.Services;

namespace RandomSampleGenerator.Core.Tests.Services;

public sealed class RunOrchestratorTests
{
    [Fact]
    public void Run_ProcessesRowsSequentiallyAndTracksStatuses()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), $"rsg-source-{Guid.NewGuid():N}");
        var targetRoot = Path.Combine(Path.GetTempPath(), $"rsg-target-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceRoot);
        Directory.CreateDirectory(targetRoot);
        var songA = Path.Combine(sourceRoot, "Song A.wav");
        File.WriteAllBytes(songA, new byte[256]);

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
                        StemType = "vocals",
                        Model = "htdemucs",
                        Quantity = 1,
                        CandidateChunkLengthSeconds = 10,
                        FinalSampleLengthSeconds = 1
                    },
                    new StemRowConfiguration
                    {
                        StemType = "drums",
                        Model = "htdemucs",
                        Quantity = 1,
                        CandidateChunkLengthSeconds = 10,
                        FinalSampleLengthSeconds = 1
                    },
                    new StemRowConfiguration
                    {
                        StemType = "bass",
                        Model = "htdemucs",
                        Quantity = 0,
                        CandidateChunkLengthSeconds = 10,
                        FinalSampleLengthSeconds = 1
                    }
                ]
            };

            var progress = new List<RowProgressUpdate>();
            var sut = new RunOrchestrator(new RunFolderService(), new ValidationService());

            var result = sut.Run(runConfiguration, [songA], progressCallback: progress.Add);

            Assert.Equal(3, result.RowResults.Count);
            Assert.Equal("bass", result.RowResults[0].StemType);
            Assert.Equal(RowStatus.Skipped, result.RowResults[0].Status);
            Assert.Equal("drums", result.RowResults[1].StemType);
            Assert.Equal("vocals", result.RowResults[2].StemType);

            Assert.NotEqual(0, result.SongSelectionSeed);
            Assert.NotEqual(0, result.ProcessingSeed);
            Assert.Empty(result.ExportedSamples);

            Assert.Contains(progress, update => update.StemType == "bass" && update.FinalStatus == RowStatus.Skipped);
            Assert.Contains(progress, update => update.StemType == "drums" && update.FinalStatus.HasValue);
            Assert.Contains(progress, update => update.StemType == "vocals" && update.FinalStatus.HasValue);
        }
        finally
        {
            Directory.Delete(sourceRoot, true);
            Directory.Delete(targetRoot, true);
        }
    }

    [Fact]
    public void Run_WhenCancelled_MarksOverallAndRowAsCancelled()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), $"rsg-source-{Guid.NewGuid():N}");
        var targetRoot = Path.Combine(Path.GetTempPath(), $"rsg-target-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceRoot);
        Directory.CreateDirectory(targetRoot);
        var songA = Path.Combine(sourceRoot, "Song A.wav");
        File.WriteAllBytes(songA, new byte[256]);

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
                        Quantity = 5,
                        CandidateChunkLengthSeconds = 10,
                        FinalSampleLengthSeconds = 1
                    },
                    new StemRowConfiguration
                    {
                        StemType = "vocals",
                        Model = "htdemucs",
                        Quantity = 1,
                        CandidateChunkLengthSeconds = 10,
                        FinalSampleLengthSeconds = 1
                    }
                ]
            };

            var sut = new RunOrchestrator(new RunFolderService(), new ValidationService());
            using var cts = new CancellationTokenSource();

            var result = sut.Run(runConfiguration, [songA], cts.Token, update =>
            {
                if (update.StemType == "drums" && !update.FinalStatus.HasValue && update.ProducedCount >= 1)
                {
                    cts.Cancel();
                }
            });

            Assert.Equal(RunStatus.Cancelled, result.Status);
            Assert.Contains(result.RowResults, row => row.StemType == "drums" && row.Status == RowStatus.Cancelled);
        }
        finally
        {
            Directory.Delete(sourceRoot, true);
            Directory.Delete(targetRoot, true);
        }
    }
}
