namespace RandomSampleGenerator.Core.Services;

public sealed class CandidateChunkService
{
	 public string EnsureRunTempRoot(string runFolderPath)
	 {
		  var tempRoot = Path.Combine(runFolderPath, ".temp");
		  Directory.CreateDirectory(tempRoot);
		  return tempRoot;
	 }

	 public string PrepareCandidateChunkWav(
		  string runTempRoot,
		  string sourceSongPath,
		  string stemType,
		  int attemptNumber,
		  int candidateChunkLengthSeconds,
		  int sampleRate = 44100,
		  int bitDepth = 16)
	 {
		  var inputRoot = Path.Combine(runTempRoot, "inputs", stemType);
		  Directory.CreateDirectory(inputRoot);

		  var sourceName = Path.GetFileNameWithoutExtension(sourceSongPath);
		  var fileName = $"{Sanitize(sourceName)}-{stemType}-attempt-{attemptNumber:000}.wav";
		  var destinationPath = Path.Combine(inputRoot, fileName);

		  WriteSilenceWav(destinationPath, sampleRate, bitDepth, candidateChunkLengthSeconds);
		  return destinationPath;
	 }

	 public string GetAttemptOutputRoot(string runTempRoot, string stemType, int attemptNumber)
	 {
		  var outputRoot = Path.Combine(runTempRoot, "demucs-output", stemType, $"attempt-{attemptNumber:000}");
		  Directory.CreateDirectory(outputRoot);
		  return outputRoot;
	 }

	 private static string Sanitize(string value)
	 {
		  var chars = value.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_').ToArray();
		  if (chars.Length == 0)
		  {
				return "source";
		  }

		  return new string(chars);
	 }

	 private static void WriteSilenceWav(string destinationPath, int sampleRate, int bitDepth, int lengthSeconds)
	 {
		  if (bitDepth != 16)
		  {
				throw new ArgumentOutOfRangeException(nameof(bitDepth), "Phase 6 candidate chunk placeholder supports 16-bit WAV only.");
		  }

		  var channels = 1;
		  var bytesPerSample = bitDepth / 8;
		  var sampleCount = sampleRate * lengthSeconds;
		  var dataSize = sampleCount * channels * bytesPerSample;

		  using var stream = File.Create(destinationPath);
		  using var writer = new BinaryWriter(stream);

		  writer.Write("RIFF"u8.ToArray());
		  writer.Write(36 + dataSize);
		  writer.Write("WAVE"u8.ToArray());
		  writer.Write("fmt "u8.ToArray());
		  writer.Write(16);
		  writer.Write((short)1);
		  writer.Write((short)channels);
		  writer.Write(sampleRate);
		  writer.Write(sampleRate * channels * bytesPerSample);
		  writer.Write((short)(channels * bytesPerSample));
		  writer.Write((short)bitDepth);
		  writer.Write("data"u8.ToArray());
		  writer.Write(dataSize);
		  writer.Write(new byte[dataSize]);
	 }
}
