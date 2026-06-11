using RandomSampleGenerator.Core.Services;

namespace RandomSampleGenerator.Core.Tests.Services;

public sealed class ExportFileNameBuilderTests
{
    [Fact]
    public void Build_AppendsSizeOnSanitizedNameCollisionForDifferentFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"rsg-file-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        var first = Path.Combine(root, "My Song.wav");
        var second = Path.Combine(root, "MySong.mp3");
        File.WriteAllBytes(first, new byte[10]);
        File.WriteAllBytes(second, new byte[20]);

        try
        {
            var sut = new ExportFileNameBuilder();

            var firstName = sut.Build(first, "drums");
            var secondName = sut.Build(second, "drums");

            Assert.Equal("MySong-drums-00.wav", firstName);
            Assert.Equal("MySong-20-drums-00.wav", secondName);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
