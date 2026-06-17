using RandomSampleGenerator.Core.Models;

namespace RandomSampleGenerator.Core.Services;

public sealed class RunOrchestrator
{
	 private readonly RunFolderService _runFolderService;
	 private readonly ValidationService _validationService;
	 private readonly CandidateChunkService _candidateChunkService;
	 private readonly StemSeparationService _stemSeparationService;

	 public RunOrchestrator(
		  RunFolderService runFolderService,
		  ValidationService validationService,
		  CandidateChunkService candidateChunkService,
		  StemSeparationService stemSeparationService)
	 {
		  _runFolderService = runFolderService;
		  _validationService = validationService;
		  _candidateChunkService = candidateChunkService;
		  _stemSeparationService = stemSeparationService;
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

		  var randomizationService = new RandomizationService(runContext.SongSelectionSeed, runContext.ProcessingSeed);
		  var runTempRoot = _candidateChunkService.EnsureRunTempRoot(runContext.RunFolderPath);
		  var replayMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		  var replayData = new ReplaySupportData();
		  var rowResults = new List<RowResult>();
		  var usedStemTypesBySong = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

		  foreach (var row in runConfiguration.StemRows.OrderBy(row => Array.IndexOf(Constants.StemTypes.Ordered, row.StemType)))
		  {
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
					 break;
				}

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
						  break;
					 }

					 if (separationResult.IsSuccess)
					 {
						  produced++;
						  progressCallback?.Invoke(new RowProgressUpdate(row.StemType, row.Quantity, produced, null));
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
		  }

		  var runResult = new RunResult
		  {
				Status = ResolveRunStatus(rowResults, cancellationToken.IsCancellationRequested),
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
				ExportedSamples = []
		  };

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
