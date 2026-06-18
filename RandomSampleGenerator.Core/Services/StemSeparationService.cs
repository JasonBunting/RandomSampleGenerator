using System.Diagnostics;
using System.ComponentModel;

namespace RandomSampleGenerator.Core.Services;

public sealed class StemSeparationService
{
	private static readonly HashSet<string> SupportedModels = ["htdemucs", "htdemucs_6s"];
	private const int MaxRetries = 2;
	private readonly IProcessRunner _processRunner;
	private readonly IProcessOutputSink? _outputSink;
	private readonly Lock _processLock = new();
	private Process? _activeProcess;

	public StemSeparationService(IProcessRunner? processRunner = null, IProcessOutputSink? outputSink = null)
	{
		_processRunner = processRunner ?? new DefaultProcessRunner();
		_outputSink = outputSink;
	}

	public StemSeparationResult Separate(
		string model,
		string requestedStemType,
		string candidateChunkPath,
		string outputRootPath,
		CancellationToken cancellationToken)
	{
		if (!SupportedModels.Contains(model))
		{
			// Unsupported model values are non-retryable configuration/runtime errors.
			return StemSeparationResult.Failed(
				model,
				requestedStemType,
				candidateChunkPath,
				outputRootPath,
				$"Unsupported Demucs model '{model}' for v1.");
		}

		var attemptFailures = new List<string>();
		for (var attempt = 0; attempt <= MaxRetries; attempt++)
		{
			if (attempt > 0)
			{
				_outputSink?.AppendSystem($"Demucs attempt failed; retry {attempt} of {MaxRetries} starting.");
			}

			var retryOutputRoot = Path.Combine(outputRootPath, $"retry-{attempt:00}");
			var result = SeparateOnce(model, requestedStemType, candidateChunkPath, retryOutputRoot, cancellationToken);
			if (result.IsSuccess || result.IsCancelled)
			{
				return result;
			}

			attemptFailures.Add($"Attempt {attempt + 1}/{MaxRetries + 1}: {result.FailureReason}");
		}

		return StemSeparationResult.Failed(
			model,
			requestedStemType,
			candidateChunkPath,
			outputRootPath,
			string.Join(" | ", attemptFailures));
	}

	private StemSeparationResult SeparateOnce(
		string model,
		string requestedStemType,
		string candidateChunkPath,
		string outputRootPath,
		CancellationToken cancellationToken)
	{
		Directory.CreateDirectory(outputRootPath);

		var startInfo = new ProcessStartInfo
		{
			FileName = "py",
			Arguments = $"-3.9 -m demucs -n {model} --out \"{outputRootPath}\" \"{candidateChunkPath}\"",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var registration = cancellationToken.Register(CancelActiveProcess);

		Process process;
		lock (_processLock)
		{
			process = _processRunner.Start(startInfo);
			_activeProcess = process;
		}

		_outputSink?.AppendSystem("Starting Demucs attempt.");
		_outputSink?.AppendSystem($"Model: {model}");
		_outputSink?.AppendSystem($"Stem: {requestedStemType}");
		_outputSink?.AppendSystem($"Source chunk: {candidateChunkPath}");
		_outputSink?.AppendSystem($"Output root: {outputRootPath}");

		var stdoutLines = new List<string>();
		var stderrLines = new List<string>();
		process.OutputDataReceived += (_, e) =>
		{
			if (e.Data is null)
			{
				return;
			}

			lock (stdoutLines)
			{
				stdoutLines.Add(e.Data);
			}

			_outputSink?.AppendStdOut(e.Data);
		};

		process.ErrorDataReceived += (_, e) =>
		{
			if (e.Data is null)
			{
				return;
			}

			lock (stderrLines)
			{
				stderrLines.Add(e.Data);
			}

			_outputSink?.AppendStdErr(e.Data);
		};

		try
		{
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();
			process.WaitForExit();

			string stdout;
			lock (stdoutLines)
			{
				stdout = string.Join(Environment.NewLine, stdoutLines);
			}

			string stderr;
			lock (stderrLines)
			{
				stderr = string.Join(Environment.NewLine, stderrLines);
			}

			_outputSink?.AppendSystem($"Exit code: {process.ExitCode}");

			if (cancellationToken.IsCancellationRequested)
			{
				_outputSink?.AppendSystem("Demucs attempt cancelled.");
				return StemSeparationResult.Cancelled(model, requestedStemType, candidateChunkPath, outputRootPath);
			}

			if (process.ExitCode != 0)
			{
				_outputSink?.AppendSystem("Demucs attempt failed.");
				return StemSeparationResult.Failed(model, requestedStemType, candidateChunkPath, outputRootPath,
					$"Demucs exited with code {process.ExitCode}. {stderr}".Trim());
			}

			var separatedStemPath = FindSeparatedStemPath(outputRootPath, model, candidateChunkPath, requestedStemType);
			if (separatedStemPath is null)
			{
				_outputSink?.AppendSystem("Demucs completed but requested stem output was missing.");
				return StemSeparationResult.Failed(model, requestedStemType, candidateChunkPath, outputRootPath,
					"Demucs completed but expected requested stem output was not found.");
			}

			_outputSink?.AppendSystem("Demucs attempt succeeded.");

			return StemSeparationResult.Succeeded(model, requestedStemType, candidateChunkPath, outputRootPath, separatedStemPath, stdout, stderr);
		}
		catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
		{
			_outputSink?.AppendSystem($"Demucs invocation failed: {ex.Message}");
			return StemSeparationResult.Failed(model, requestedStemType, candidateChunkPath, outputRootPath,
				$"Failed to invoke Demucs via 'py -3.9 -m demucs'. {ex.Message}");
		}
		finally
		{
			lock (_processLock)
			{
				if (ReferenceEquals(_activeProcess, process))
				{
					_activeProcess = null;
				}
			}

			process.Dispose();
		}
	}

	public void CancelActiveProcess()
	{
		lock (_processLock)
		{
			if (_activeProcess is null)
			{
				return;
			}

			try
			{
				if (!_activeProcess.HasExited)
				{
					_activeProcess.Kill(true);
				}
			}
			catch
			{
				// Best-effort process termination for cancellation flow.
			}
		}
	}

	 private static string? FindSeparatedStemPath(string outputRootPath, string model, string candidateChunkPath, string requestedStemType)
	 {
		  var inputBaseName = Path.GetFileNameWithoutExtension(candidateChunkPath);
		  var stemPath = Path.Combine(outputRootPath, model, inputBaseName, $"{requestedStemType}.wav");
		  return File.Exists(stemPath) ? stemPath : null;
	 }
}

public sealed class StemSeparationResult
{
	 public required bool IsSuccess { get; init; }
	 public required bool IsCancelled { get; init; }
	 public required string Model { get; init; }
	 public required string RequestedStemType { get; init; }
	 public required string CandidateChunkPath { get; init; }
	 public required string OutputRootPath { get; init; }
	 public string? SeparatedStemPath { get; init; }
	 public string? FailureReason { get; init; }
	 public string? StandardOutput { get; init; }
	 public string? StandardError { get; init; }

	 public static StemSeparationResult Succeeded(
		  string model,
		  string requestedStemType,
		  string candidateChunkPath,
		  string outputRootPath,
		  string separatedStemPath,
		  string? stdout,
		  string? stderr) =>
		  new()
		  {
				IsSuccess = true,
				IsCancelled = false,
				Model = model,
				RequestedStemType = requestedStemType,
				CandidateChunkPath = candidateChunkPath,
				OutputRootPath = outputRootPath,
				SeparatedStemPath = separatedStemPath,
				StandardOutput = stdout,
				StandardError = stderr
		  };

	 public static StemSeparationResult Failed(
		  string model,
		  string requestedStemType,
		  string candidateChunkPath,
		  string outputRootPath,
		  string failureReason) =>
		  new()
		  {
				IsSuccess = false,
				IsCancelled = false,
				Model = model,
				RequestedStemType = requestedStemType,
				CandidateChunkPath = candidateChunkPath,
				OutputRootPath = outputRootPath,
				FailureReason = failureReason
		  };

	 public static StemSeparationResult Cancelled(
		  string model,
		  string requestedStemType,
		  string candidateChunkPath,
		  string outputRootPath) =>
		  new()
		  {
				IsSuccess = false,
				IsCancelled = true,
				Model = model,
				RequestedStemType = requestedStemType,
				CandidateChunkPath = candidateChunkPath,
				OutputRootPath = outputRootPath,
				FailureReason = "Separation cancelled."
		  };
}
