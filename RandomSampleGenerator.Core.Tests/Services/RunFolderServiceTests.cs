using RandomSampleGenerator.Core.Services;

namespace RandomSampleGenerator.Core.Tests.Services;

public sealed class RunFolderServiceTests
{
    [Fact]
    public void CreateRunArtifacts_UsesPerDayOrdinalAndCreatesLogFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), $"rsg-runfolder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "20260130 Sample Run 00"));

        try
        {
            var sut = new RunFolderService();

            var result = sut.CreateRunArtifacts(root, new DateTimeOffset(2026, 1, 30, 9, 0, 0, TimeSpan.Zero));

            Assert.Equal("20260130 Sample Run 01", result.runName);
            Assert.True(Directory.Exists(result.runFolderPath));
            Assert.Equal(Path.Combine(root, "logs", "20260130 Sample Run 01.log"), result.logFilePath);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
