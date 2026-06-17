using RandomSampleGenerator.Core.Services;

namespace RandomSampleGenerator.Core.Tests.Services;

public sealed class SourcePoolScannerTests
{
	 [Fact]
	 public void Scan_RecursivelyIncludesOnlyWavAndMp3()
	 {
		  var root = Path.Combine(Path.GetTempPath(), $"rsg-scan-{Guid.NewGuid():N}");
		  var nested = Path.Combine(root, "nested", "deep");
		  Directory.CreateDirectory(nested);

		  var wav = Path.Combine(root, "song-a.wav");
		  var mp3 = Path.Combine(nested, "song-b.mp3");
		  var flac = Path.Combine(root, "song-c.flac");
		  var txt = Path.Combine(nested, "notes.txt");

		  File.WriteAllBytes(wav, [1]);
		  File.WriteAllBytes(mp3, [2]);
		  File.WriteAllBytes(flac, [3]);
		  File.WriteAllText(txt, "ignore");

		  try
		  {
				var sut = new SourcePoolScanner();

				var pool = sut.Scan(root);

				Assert.Equal(2, pool.Count);
				Assert.Contains(wav, pool, StringComparer.OrdinalIgnoreCase);
				Assert.Contains(mp3, pool, StringComparer.OrdinalIgnoreCase);
				Assert.DoesNotContain(flac, pool, StringComparer.OrdinalIgnoreCase);
				Assert.DoesNotContain(txt, pool, StringComparer.OrdinalIgnoreCase);
		  }
		  finally
		  {
				Directory.Delete(root, true);
		  }
	 }

	 [Fact]
	 public void Scan_MissingFolder_ReturnsEmptyPool()
	 {
		  var root = Path.Combine(Path.GetTempPath(), $"rsg-missing-{Guid.NewGuid():N}");

		  var sut = new SourcePoolScanner();
		  var pool = sut.Scan(root);

		  Assert.Empty(pool);
	 }
}
