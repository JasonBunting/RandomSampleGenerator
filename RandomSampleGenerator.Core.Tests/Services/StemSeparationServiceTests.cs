using System.Diagnostics;
using RandomSampleGenerator.Core.Services;

namespace RandomSampleGenerator.Core.Tests.Services;

public sealed class StemSeparationServiceTests
{
	 [Fact]
	 public void Separate_UnsupportedModel_Throws()
	 {
		  var sut = new StemSeparationService(new NoopProcessRunner());

			 var result = sut.Separate("not-a-model", "drums", "in.wav", Path.GetTempPath(), CancellationToken.None);

		  Assert.False(result.IsSuccess);
		  Assert.Contains("Unsupported Demucs model", result.FailureReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("Attempt 1/3", result.FailureReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
	 }

	 [Fact]
	 public void Separate_WhenStemOutputMissing_ReturnsFailure()
	 {
		  var root = Path.Combine(Path.GetTempPath(), $"rsg-demucs-{Guid.NewGuid():N}");
		  Directory.CreateDirectory(root);
		  var input = Path.Combine(root, "candidate.wav");
		  File.WriteAllBytes(input, [0]);

		  try
		  {
				var sut = new StemSeparationService(new ExitSuccessProcessRunner());

				var result = sut.Separate("htdemucs", "drums", input, root, CancellationToken.None);

				Assert.False(result.IsSuccess);
				Assert.False(result.IsCancelled);
				Assert.Contains("expected requested stem output", result.FailureReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
				  Assert.Contains("Attempt 3/3", result.FailureReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
		  }
		  finally
		  {
				Directory.Delete(root, true);
		  }
	 }

	 private sealed class NoopProcessRunner : IProcessRunner
	 {
		  public Process Start(ProcessStartInfo startInfo) => throw new NotSupportedException();
	 }

	 private sealed class ExitSuccessProcessRunner : IProcessRunner
	 {
		  public Process Start(ProcessStartInfo startInfo)
		  {
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
					 throw new InvalidOperationException("Unable to start process.");
				}

				return process;
		  }
	 }
}
