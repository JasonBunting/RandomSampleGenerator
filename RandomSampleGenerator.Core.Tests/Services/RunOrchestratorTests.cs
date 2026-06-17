using System.Diagnostics;
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
        CreateSineWave(songA, durationSeconds: 12);

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
            var sut = new RunOrchestrator(
                new RunFolderService(),
                new ValidationService(),
                new CandidateChunkService(),
                new StemSeparationService(new FakeProcessRunner(simulateSuccess: true)),
                new SampleExportService(),
                new ExportFileNameBuilder(),
                new ManifestBuilder());

            var result = sut.Run(runConfiguration, [songA], progressCallback: progress.Add);

            Assert.Equal(3, result.RowResults.Count);
            Assert.Equal("bass", result.RowResults[0].StemType);
            Assert.Equal(RowStatus.Skipped, result.RowResults[0].Status);
            Assert.Equal("drums", result.RowResults[1].StemType);
            Assert.Equal("vocals", result.RowResults[2].StemType);

            Assert.NotEqual(0, result.SongSelectionSeed);
            Assert.NotEqual(0, result.ProcessingSeed);
            Assert.NotEmpty(result.ExportedSamples);
            Assert.All(result.ExportedSamples, sample => Assert.True(File.Exists(sample.ExportedFullPath)));

            var runFolder = result.RunFolderPath;
            var logPath = Path.Combine(targetRoot, "logs", $"{result.RunName}.log");
            var manifestPath = Path.Combine(runFolder, RunFolderService.ManifestFileName);
            Assert.True(File.Exists(logPath));
            Assert.True(File.Exists(manifestPath));

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
        CreateSineWave(songA, durationSeconds: 12);

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

            var sut = new RunOrchestrator(
                new RunFolderService(),
                new ValidationService(),
                new CandidateChunkService(),
                new StemSeparationService(new FakeProcessRunner(simulateSuccess: true)),
                new SampleExportService(),
                new ExportFileNameBuilder(),
                new ManifestBuilder());
            using var cts = new CancellationTokenSource();

            var result = sut.Run(runConfiguration, [songA], cts.Token, update =>
            {
                if (update.StemType == "drums" && !update.FinalStatus.HasValue)
                {
                    cts.Cancel();
                }
            });

            Assert.Equal(RunStatus.Cancelled, result.Status);
            Assert.Contains(result.RowResults, row => row.StemType == "drums" && row.Status == RowStatus.Cancelled);
            Assert.True(File.Exists(Path.Combine(result.RunFolderPath, RunFolderService.ManifestFileName)));
        }
        finally
        {
            Directory.Delete(sourceRoot, true);
            Directory.Delete(targetRoot, true);
        }
    }

    private static void CreateSineWave(string outputPath, int durationSeconds)
    {
        const int sampleRate = 44100;
        const short bitsPerSample = 16;
        const short channels = 1;
        var sampleCount = sampleRate * durationSeconds;
        var bytesPerSample = bitsPerSample / 8;
        var dataSize = sampleCount * channels * bytesPerSample;

        using var stream = File.Create(outputPath);
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bytesPerSample);
        writer.Write((short)(channels * bytesPerSample));
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        var frequency = 440.0;
        var amplitude = short.MaxValue * 0.2;
        for (var i = 0; i < sampleCount; i++)
        {
            var sample = (short)(Math.Sin((2 * Math.PI * frequency * i) / sampleRate) * amplitude);
            writer.Write(sample);
        }
    }

    private sealed class FakeProcessRunner(bool simulateSuccess) : IProcessRunner
    {
        public Process Start(ProcessStartInfo startInfo)
        {
            var outputRoot = GetArgumentValue(startInfo.Arguments, "--out");
            var model = GetArgumentValue(startInfo.Arguments, "-n");
            var inputPath = GetLastQuotedPath(startInfo.Arguments);

            if (simulateSuccess)
            {
                var inputName = Path.GetFileNameWithoutExtension(inputPath);
                var outputDir = Path.Combine(outputRoot, model, inputName);
                Directory.CreateDirectory(outputDir);
                WriteTestWav(Path.Combine(outputDir, "drums.wav"), 2);
                WriteTestWav(Path.Combine(outputDir, "vocals.wav"), 2);
                WriteTestWav(Path.Combine(outputDir, "bass.wav"), 2);
                WriteTestWav(Path.Combine(outputDir, "other.wav"), 2);
                WriteTestWav(Path.Combine(outputDir, "piano.wav"), 2);
                WriteTestWav(Path.Combine(outputDir, "guitar.wav"), 2);
            }

            var psi = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = "/c exit 0",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            if (process is null)
            {
                throw new InvalidOperationException("Unable to start fake process.");
            }

            return process;
        }

        private static string GetArgumentValue(string arguments, string name)
        {
            if (name == "-n")
            {
                var marker = "-n ";
                var start = arguments.IndexOf(marker, StringComparison.Ordinal);
                if (start < 0)
                {
                    return string.Empty;
                }

                start += marker.Length;
                var end = arguments.IndexOf(' ', start);
                return end > start ? arguments[start..end] : arguments[start..];
            }

            var quotedMarker = $"{name} \"";
            var quotedStart = arguments.IndexOf(quotedMarker, StringComparison.Ordinal);
            if (quotedStart < 0)
            {
                return string.Empty;
            }

            quotedStart += quotedMarker.Length;
            var quotedEnd = arguments.IndexOf('"', quotedStart);
            return quotedEnd > quotedStart ? arguments[quotedStart..quotedEnd] : string.Empty;
        }

        private static string GetLastQuotedPath(string arguments)
        {
            var start = arguments.LastIndexOf('"');
            if (start <= 0)
            {
                return string.Empty;
            }

            var previous = arguments.LastIndexOf('"', start - 1);
            if (previous < 0)
            {
                return string.Empty;
            }

            return arguments[(previous + 1)..start];
        }

        private static void WriteTestWav(string outputPath, int durationSeconds)
        {
            const int sampleRate = 44100;
            const short bitsPerSample = 16;
            const short channels = 1;
            var sampleCount = sampleRate * durationSeconds;
            var bytesPerSample = bitsPerSample / 8;
            var dataSize = sampleCount * channels * bytesPerSample;

            using var stream = File.Create(outputPath);
            using var writer = new BinaryWriter(stream);

            writer.Write("RIFF"u8.ToArray());
            writer.Write(36 + dataSize);
            writer.Write("WAVE"u8.ToArray());
            writer.Write("fmt "u8.ToArray());
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bytesPerSample);
            writer.Write((short)(channels * bytesPerSample));
            writer.Write(bitsPerSample);
            writer.Write("data"u8.ToArray());
            writer.Write(dataSize);

            var frequency = 440.0;
            var amplitude = short.MaxValue * 0.2;
            for (var i = 0; i < sampleCount; i++)
            {
                var sample = (short)(Math.Sin((2 * Math.PI * frequency * i) / sampleRate) * amplitude);
                writer.Write(sample);
            }
        }
    }
}
