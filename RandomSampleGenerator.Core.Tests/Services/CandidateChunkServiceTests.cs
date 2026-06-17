using RandomSampleGenerator.Core.Services;

namespace RandomSampleGenerator.Core.Tests.Services;

public sealed class CandidateChunkServiceTests
{
	 [Fact]
	 public void PrepareCandidateChunkWav_WhenSourceTooShort_ReturnsFailure()
	 {
		  var root = Path.Combine(Path.GetTempPath(), $"rsg-chunk-{Guid.NewGuid():N}");
		  Directory.CreateDirectory(root);
		  var source = Path.Combine(root, "short.wav");
		  CreateSineWave(source, durationSeconds: 2);

		  try
		  {
				var sut = new CandidateChunkService();
				var runTemp = sut.EnsureRunTempRoot(root);

				var result = sut.PrepareCandidateChunkWav(
					 runTemp,
					 source,
					 "drums",
					 1,
					 candidateChunkStartSeconds: 0,
					 candidateChunkLengthSeconds: 10,
					 sampleRate: 44100);

				Assert.False(result.IsSuccess);
				Assert.Contains("shorter than candidate chunk length", result.FailureReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
		  }
		  finally
		  {
				Directory.Delete(root, true);
		  }
	 }

	 [Fact]
	 public void PrepareCandidateChunkWav_ExtractsPcmWavChunk()
	 {
		  var root = Path.Combine(Path.GetTempPath(), $"rsg-chunk-{Guid.NewGuid():N}");
		  Directory.CreateDirectory(root);
		  var source = Path.Combine(root, "long.wav");
		  CreateSineWave(source, durationSeconds: 12);

		  try
		  {
				var sut = new CandidateChunkService();
				var runTemp = sut.EnsureRunTempRoot(root);

				var result = sut.PrepareCandidateChunkWav(
					 runTemp,
					 source,
					 "drums",
					 1,
					 candidateChunkStartSeconds: 1.5,
					 candidateChunkLengthSeconds: 5,
					 sampleRate: 44100);

				Assert.True(result.IsSuccess);
				Assert.NotNull(result.CandidateChunkPath);
				Assert.True(File.Exists(result.CandidateChunkPath));

				using var reader = new BinaryReader(File.OpenRead(result.CandidateChunkPath));
				var riff = new string(reader.ReadChars(4));
				reader.BaseStream.Position = 20;
				var audioFormat = reader.ReadInt16();

				Assert.Equal("RIFF", riff);
				Assert.Equal(1, audioFormat);
		  }
		  finally
		  {
				Directory.Delete(root, true);
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
}
