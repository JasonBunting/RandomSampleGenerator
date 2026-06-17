using NAudio.Wave;

namespace RandomSampleGenerator.Core.Services;

public sealed class CandidateChunkService
{
	 public sealed class CandidateChunkExtractionResult
	 {
		  public required bool IsSuccess { get; init; }
		  public string? CandidateChunkPath { get; init; }
		  public string? FailureReason { get; init; }
		  public double? SourceDurationSeconds { get; init; }

		  public static CandidateChunkExtractionResult Success(string chunkPath, double sourceDurationSeconds) => new()
		  {
				 IsSuccess = true,
				 CandidateChunkPath = chunkPath,
				 SourceDurationSeconds = sourceDurationSeconds
		  };

		  public static CandidateChunkExtractionResult Failure(string reason, double? sourceDurationSeconds = null) => new()
		  {
				 IsSuccess = false,
				 FailureReason = reason,
				 SourceDurationSeconds = sourceDurationSeconds
		  };
	 }

	 public double? GetSourceDurationSeconds(string sourceSongPath)
	 {
		  try
		  {
				 using var reader = CreateReader(sourceSongPath);
				 return reader.TotalTime.TotalSeconds;
		  }
		  catch
		  {
				 return null;
		  }
	 }

	 public string EnsureRunTempRoot(string runFolderPath)
	 {
		  var tempRoot = Path.Combine(runFolderPath, ".temp");
		  Directory.CreateDirectory(tempRoot);
		  return tempRoot;
	 }

	 public CandidateChunkExtractionResult PrepareCandidateChunkWav(
		  string runTempRoot,
		  string sourceSongPath,
		  string stemType,
		  int attemptNumber,
		  double candidateChunkStartSeconds,
		  int candidateChunkLengthSeconds)
	 {
		  var sourceExtension = Path.GetExtension(sourceSongPath);
		  if (!sourceExtension.Equals(".wav", StringComparison.OrdinalIgnoreCase)
				  && !sourceExtension.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
		  {
				 return CandidateChunkExtractionResult.Failure($"Unsupported source format '{sourceExtension}'.");
		  }

		  var inputRoot = Path.Combine(runTempRoot, "inputs", stemType);
		  Directory.CreateDirectory(inputRoot);

		  var sourceName = Path.GetFileNameWithoutExtension(sourceSongPath);
		  var fileName = $"{Sanitize(sourceName)}-{stemType}-attempt-{attemptNumber:000}.wav";
		  var destinationPath = Path.Combine(inputRoot, fileName);

		  using var reader = CreateReader(sourceSongPath);
		  var sourceDurationSeconds = reader.TotalTime.TotalSeconds;
		  if (sourceDurationSeconds < candidateChunkLengthSeconds)
		  {
				 return CandidateChunkExtractionResult.Failure(
					  $"Source duration {sourceDurationSeconds:F2}s is shorter than candidate chunk length {candidateChunkLengthSeconds}s.",
					  sourceDurationSeconds);
		  }

		  var targetWaveFormat = new WaveFormat(reader.WaveFormat.SampleRate, 16, reader.WaveFormat.Channels);

		  var startOffset = TimeSpan.FromSeconds(candidateChunkStartSeconds);
		  var maxStart = TimeSpan.FromSeconds(Math.Max(0, sourceDurationSeconds - candidateChunkLengthSeconds));
		  if (startOffset > maxStart)
		  {
				 startOffset = maxStart;
		  }

		  reader.CurrentTime = startOffset;
		  var bytesPerSecond = targetWaveFormat.AverageBytesPerSecond;
		  var bytesRequired = checked(bytesPerSecond * candidateChunkLengthSeconds);
		  var frameSize = targetWaveFormat.BlockAlign;
		  var toleranceBytes = frameSize;

		  var buffer = new byte[8192];
		  using var writer = new WaveFileWriter(destinationPath, targetWaveFormat);
		  var totalBytesWritten = 0;

		  while (totalBytesWritten < bytesRequired)
		  {
				 var bytesToRead = Math.Min(buffer.Length, bytesRequired - totalBytesWritten);
				 var bytesRead = reader.Read(buffer, 0, bytesToRead);
				 if (bytesRead <= 0)
				 {
					  break;
				 }

				 if (bytesRead % frameSize != 0)
				 {
					  bytesRead -= bytesRead % frameSize;
					  if (bytesRead <= 0)
					  {
							break;
					  }
				 }

				 writer.Write(buffer, 0, bytesRead);
				 totalBytesWritten += bytesRead;
		  }

		  if (totalBytesWritten <= 0)
		  {
				 return CandidateChunkExtractionResult.Failure("Failed to extract candidate chunk audio data.", sourceDurationSeconds);
		  }

		  if (Math.Abs(totalBytesWritten - bytesRequired) > toleranceBytes)
		  {
				return CandidateChunkExtractionResult.Failure(
					 $"Extracted chunk length mismatch. Expected {bytesRequired} bytes, wrote {totalBytesWritten} bytes.",
					 sourceDurationSeconds);
		  }

		  return CandidateChunkExtractionResult.Success(destinationPath, sourceDurationSeconds);
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

	 private static AudioFileReader CreateReader(string sourceSongPath)
	 {
		  return new AudioFileReader(sourceSongPath);
	 }
}
