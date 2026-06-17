using RandomSampleGenerator.Core.Models;

namespace RandomSampleGenerator.Core.Services;

public sealed class RunOrchestrator
{
	 private readonly RunFolderService _runFolderService;
	 private readonly ValidationService _validationService;
	 private readonly CandidateChunkService _candidateChunkService;
	 private readonly StemSeparationService _stemSeparationService;
	 private readonly SampleExportService _sampleExportService;
	 private readonly ExportFileNameBuilder _exportFileNameBuilder;
	 private readonly ManifestBuilder _manifestBuilder;

	 public RunOrchestrator(
		  RunFolderService runFolderService,
		  ValidationService validationService,
		  CandidateChunkService candidateChunkService,
		  StemSeparationService stemSeparationService,
		  SampleExportService sampleExportService,
		  ExportFileNameBuilder exportFileNameBuilder,
		  ManifestBuilder manifestBuilder)
	 {
		  _runFolderService = runFolderService;
		  _validationService = validationService;
		  _candidateChunkService = candidateChunkService;
		  _stemSeparationService = stemSeparationService;
		  _sampleExportService = sampleExportService;
		  _exportFileNameBuilder = exportFileNameBuilder;
		  _manifestBuilder = manifestBuilder;
	 }

	 public RunResult Run(
		  RunConfiguration runConfiguration,
		  IReadOnlyList<string> sourcePool,
		  CancellationToken cancellationToken = default,
		  Action<RowProgressUpdate>? progressCallback = null)
	 {
		  var validationErrors = _validationService.ValidateBeforeRun(runConfiguration);
		  if (validationErrors.Count > 0)
		  {
				throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
		  }

		  var now = DateTimeOffset.UtcNow;
		  var artifacts = _runFolderService.CreateRunArtifacts(runConfiguration.AppConfiguration.TargetFolderPath, now);
		  var runContext = new RunContext
		  {
				RunName = artifacts.runName,
				RunFolderPath = artifacts.runFolderPath,
				LogFilePath = artifacts.logFilePath,
				RunStart = now,
				SongSelectionSeed = Random.Shared.Next(),
				ProcessingSeed = Random.Shared.Next()
		  };

		  var artifactWriteErrors = new List<Exception>();
		  LoggingService? logger = null;

		  try
		  {
				logger = new LoggingService(runContext.LogFilePath, runConfiguration.AppConfiguration.LoggingEnabled);
		  }
		  catch (Exception ex)
		  {
				artifactWriteErrors.Add(new InvalidOperationException("Failed to initialize run logger.", ex));
		  }

		  void SafeLog(string message)
		  {
				if (logger is null)
				{
					 return;
				}

				try
				{
					logger.Info(message);
				}
				catch (Exception ex)
				{
					 artifactWriteErrors.Add(new InvalidOperationException($"Failed to write run log entry: {message}", ex));
				}
		  }

		  var randomizationService = new RandomizationService(runContext.SongSelectionSeed, runContext.ProcessingSeed);
		  var runTempRoot = _candidateChunkService.EnsureRunTempRoot(runContext.RunFolderPath);
		  var replayMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		  var replayData = new ReplaySupportData();
		  var rowResults = new List<RowResult>();
		  var exportRecords = new List<ExportedSampleRecord>();
		  var usedStemTypesBySong = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
		  Exception? runException = null;
		  StemRowConfiguration? activeRow = null;
		  int activeRowProduced = 0;
		  SafeLog($"Run started. Name='{runContext.RunName}', RunFolder='{runContext.RunFolderPath}', SourceCount={sourcePool.Count}, SongSelectionSeed={runContext.SongSelectionSeed}, ProcessingSeed={runContext.ProcessingSeed}");

		  try
		  {
				foreach (var row in runConfiguration.StemRows.OrderBy(row => Array.IndexOf(Constants.StemTypes.Ordered, row.StemType)))
				{
				activeRow = row;
				activeRowProduced = 0;
				if (row.Quantity == 0)
				{
					 var skipped = new RowResult
					 {
						  StemType = row.StemType,
						  Model = row.Model,
						  RequestedCount = row.Quantity,
						  ProducedCount = 0,
						  CandidateChunkLengthSeconds = row.CandidateChunkLengthSeconds,
						  FinalSampleLengthSeconds = row.FinalSampleLengthSeconds,
						  Status = RowStatus.Skipped
					 };

					 rowResults.Add(skipped);
					 progressCallback?.Invoke(new RowProgressUpdate(skipped.StemType, skipped.RequestedCount, skipped.ProducedCount, skipped.Status));
					 SafeLog($"Row '{row.StemType}' skipped because requested quantity is 0.");
					 continue;
				}

				if (cancellationToken.IsCancellationRequested)
				{
					 var cancelled = new RowResult
					 {
						  StemType = row.StemType,
						  Model = row.Model,
						  RequestedCount = row.Quantity,
						  ProducedCount = 0,
						  CandidateChunkLengthSeconds = row.CandidateChunkLengthSeconds,
						  FinalSampleLengthSeconds = row.FinalSampleLengthSeconds,
						  Status = RowStatus.Cancelled
					 };

					 rowResults.Add(cancelled);
					 progressCallback?.Invoke(new RowProgressUpdate(cancelled.StemType, cancelled.RequestedCount, cancelled.ProducedCount, cancelled.Status));
					 SafeLog($"Row '{row.StemType}' cancelled before processing started.");
					 break;
				}

				SafeLog($"Row '{row.StemType}' started. Requested={row.Quantity}, Model='{row.Model}', CandidateChunkLengthSeconds={row.CandidateChunkLengthSeconds}, FinalSampleLengthSeconds={row.FinalSampleLengthSeconds}");
				progressCallback?.Invoke(new RowProgressUpdate(row.StemType, row.Quantity, 0, null));

				var produced = 0;
				var maxAttempts = Math.Max(25, sourcePool.Count * 5);
				var attempts = 0;

				while (produced < row.Quantity && attempts < maxAttempts && !cancellationToken.IsCancellationRequested)
				{
					 attempts++;
					 if (sourcePool.Count == 0)
					 {
						  break;
					 }

					 var song = sourcePool[randomizationService.PickSongIndex(sourcePool.Count)];
					 if (!CanUseSongForStem(song, row.StemType, runConfiguration.AppConfiguration.MaxDistinctStemTypesPerSongPerRun, usedStemTypesBySong))
					 {
						  continue;
					 }

					 var sourceId = GetReplaySongId(song, replayMap, replayData.SourceFileMap);
					 replayData.OrderedChosenSongIds.Add(sourceId);

					 var sourceDurationSeconds = _candidateChunkService.GetSourceDurationSeconds(song);
					 if (!sourceDurationSeconds.HasValue)
					 {
						  continue;
					 }

					 var maxStartSeconds = sourceDurationSeconds.Value - row.CandidateChunkLengthSeconds;
					 if (maxStartSeconds < 0)
					 {
						  continue;
					 }

					 var candidateChunkStartSeconds = randomizationService.PickChunkStartSeconds(maxStartSeconds);

					 var candidateChunkPath = _candidateChunkService.PrepareCandidateChunkWav(
						  runTempRoot,
						  song,
						  row.StemType,
						  attempts,
						  candidateChunkStartSeconds,
						  row.CandidateChunkLengthSeconds);

					 if (!candidateChunkPath.IsSuccess || string.IsNullOrWhiteSpace(candidateChunkPath.CandidateChunkPath))
					 {
						  continue;
					 }

					 var attemptOutputRoot = _candidateChunkService.GetAttemptOutputRoot(runTempRoot, row.StemType, attempts);
					 var separationResult = _stemSeparationService.Separate(
						  row.Model,
						  row.StemType,
						  candidateChunkPath.CandidateChunkPath,
						  attemptOutputRoot,
						  cancellationToken);

					 if (separationResult.IsCancelled)
					 {
						 SafeLog($"Row '{row.StemType}' cancelled during stem separation on attempt {attempts}.");
						  break;
					 }

					 if (separationResult.IsSuccess)
					 {
						  var stemDurationSeconds = _sampleExportService.GetAudioDurationSeconds(separationResult.SeparatedStemPath!);
						  if (!stemDurationSeconds.HasValue)
						  {
								continue;
						  }

						  var finalMaxStart = stemDurationSeconds.Value - row.FinalSampleLengthSeconds;
						  if (finalMaxStart < 0)
						  {
								continue;
						  }

						  var finalSampleStartSeconds = randomizationService.PickChunkStartSeconds(finalMaxStart);
						  var exportFileName = _exportFileNameBuilder.Build(song, row.StemType);
						  var exportFullPath = Path.Combine(runContext.RunFolderPath, exportFileName);

						  try
						  {
								_sampleExportService.ExportFinalSampleWav(
									 separationResult.SeparatedStemPath!,
									 exportFullPath,
									 finalSampleStartSeconds,
									 row.FinalSampleLengthSeconds,
									 runConfiguration.AppConfiguration.ExportSampleRate,
									 runConfiguration.AppConfiguration.ExportBitDepth);
						  }
						  catch
						  {
								continue;
						  }

						  exportRecords.Add(new ExportedSampleRecord
						  {
								SourceFilePath = song,
								CandidateChunkStartSeconds = candidateChunkStartSeconds,
								CandidateChunkDurationSeconds = row.CandidateChunkLengthSeconds,
								ModelUsed = row.Model,
								StemTypeUsed = row.StemType,
								FinalSampleStartSeconds = finalSampleStartSeconds,
								FinalSampleDurationSeconds = row.FinalSampleLengthSeconds,
								ExportedFileName = exportFileName,
								ExportedFullPath = exportFullPath
						  });

						  produced++;
						 activeRowProduced = produced;
						  progressCallback?.Invoke(new RowProgressUpdate(row.StemType, row.Quantity, produced, null));
						 SafeLog($"Row '{row.StemType}' produced sample {produced}/{row.Quantity}: '{exportFileName}' from source '{song}'.");
					 }
				}

				var result = new RowResult
				{
					 StemType = row.StemType,
					 Model = row.Model,
					 RequestedCount = row.Quantity,
					 ProducedCount = produced,
					 CandidateChunkLengthSeconds = row.CandidateChunkLengthSeconds,
					 FinalSampleLengthSeconds = row.FinalSampleLengthSeconds,
					 Status = ResolveRowStatus(produced, row.Quantity, cancellationToken.IsCancellationRequested)
				};

				rowResults.Add(result);
				progressCallback?.Invoke(new RowProgressUpdate(result.StemType, result.RequestedCount, result.ProducedCount, result.Status));
				SafeLog($"Row '{result.StemType}' ended with status {result.Status}. Produced={result.ProducedCount}/{result.RequestedCount}.");
				activeRow = null;
				activeRowProduced = 0;
				}
		  }
		  catch (Exception ex)
		  {
				runException = ex;
				if (activeRow is not null
					 && !rowResults.Any(row => row.StemType.Equals(activeRow.StemType, StringComparison.OrdinalIgnoreCase)))
				{
					 rowResults.Add(new RowResult
					 {
						  StemType = activeRow.StemType,
						  Model = activeRow.Model,
						  RequestedCount = activeRow.Quantity,
						  ProducedCount = activeRowProduced,
						  CandidateChunkLengthSeconds = activeRow.CandidateChunkLengthSeconds,
						  FinalSampleLengthSeconds = activeRow.FinalSampleLengthSeconds,
						  Status = ResolveRowStatus(activeRowProduced, activeRow.Quantity, cancellationToken.IsCancellationRequested)
					 });
				}
				SafeLog($"Run failed with unhandled exception: {ex}");
		  }

		  var resolvedStatus = ResolveRunStatus(rowResults, cancellationToken.IsCancellationRequested);
		  if (runException is not null && resolvedStatus != RunStatus.Cancelled)
		  {
				resolvedStatus = RunStatus.Failed;
		  }

		  var runResult = new RunResult
		  {
				Status = resolvedStatus,
				RunStart = runContext.RunStart,
				RunEnd = DateTimeOffset.UtcNow,
				SourceRootPath = runConfiguration.AppConfiguration.SourceFolderPath,
				TargetRootPath = runConfiguration.AppConfiguration.TargetFolderPath,
				RunFolderPath = runContext.RunFolderPath,
				RunName = runContext.RunName,
				ConfigurationUsed = runConfiguration.AppConfiguration,
				RowSettingsUsed = runConfiguration.StemRows,
				SongSelectionSeed = runContext.SongSelectionSeed,
				ProcessingSeed = runContext.ProcessingSeed,
				ReplaySupportData = replayData,
				RowResults = rowResults,
			ExportedSamples = exportRecords
		  };

		  SafeLog($"Run ended with status {runResult.Status}. ExportedSamples={runResult.ExportedSamples.Count}.");

		  try
		  {
				var manifestPath = _manifestBuilder.WriteManifest(runContext.RunFolderPath, runResult);
				SafeLog($"Manifest written: '{manifestPath}'.");
		  }
		  catch (Exception ex)
		  {
				artifactWriteErrors.Add(new InvalidOperationException("Failed to write run manifest.", ex));
		  }

		  if (runException is not null)
		  {
				if (artifactWriteErrors.Count > 0)
				{
					runException.Data["ArtifactWriteFailures"] = new AggregateException("Artifact writing also encountered errors.", artifactWriteErrors);
				}

				throw new InvalidOperationException("Run failed during processing. Manifest was still attempted.", runException);
		  }

		  if (artifactWriteErrors.Count > 0)
		  {
				throw new AggregateException("Run completed but artifact writing encountered errors.", artifactWriteErrors);
		  }

		  return runResult;
	 }

	 private static int GetReplaySongId(string sourcePath, IDictionary<string, int> reverseMap, IDictionary<int, string> sourceMap)
	 {
		  if (reverseMap.TryGetValue(sourcePath, out var id))
		  {
				return id;
		  }

		  id = sourceMap.Count;
		  reverseMap[sourcePath] = id;
		  sourceMap[id] = sourcePath;
		  return id;
	 }

	 private static bool CanUseSongForStem(string sourceSong, string stemType, int maxDistinct, IDictionary<string, HashSet<string>> usedStemTypesBySong)
	 {
		  if (!usedStemTypesBySong.TryGetValue(sourceSong, out var stems))
		  {
				stems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				usedStemTypesBySong[sourceSong] = stems;
		  }

		  if (stems.Contains(stemType))
		  {
				return true;
		  }

		  if (stems.Count >= maxDistinct)
		  {
				return false;
		  }

		  stems.Add(stemType);
		  return true;
	 }

	 private static RowStatus ResolveRowStatus(int produced, int requested, bool cancelled) => cancelled
		  ? RowStatus.Cancelled
		  : produced == requested
				? RowStatus.Completed
				: produced > 0
					 ? RowStatus.Partial
					 : RowStatus.Failed;

	 private static RunStatus ResolveRunStatus(IEnumerable<RowResult> rowResults, bool cancelled)
	 {
		  if (cancelled || rowResults.Any(row => row.Status == RowStatus.Cancelled))
		  {
				return RunStatus.Cancelled;
		  }

		  if (rowResults.Any(row => row.Status is RowStatus.Failed or RowStatus.Partial))
		  {
				return RunStatus.Failed;
		  }

		  return RunStatus.Completed;
	 }
}

public sealed record RowProgressUpdate(
	 string StemType,
	 int RequestedCount,
	 int ProducedCount,
	 RowStatus? FinalStatus);
