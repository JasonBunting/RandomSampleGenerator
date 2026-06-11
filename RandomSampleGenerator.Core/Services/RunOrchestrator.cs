using RandomSampleGenerator.Core.Models;

namespace RandomSampleGenerator.Core.Services;

public sealed class RunOrchestrator
{
    private readonly SourcePoolScanner _sourcePoolScanner;
    private readonly RunFolderService _runFolderService;
    private readonly ValidationService _validationService;
    private readonly ManifestBuilder _manifestBuilder;
    private readonly SampleExportService _sampleExportService;
    private readonly ExportFileNameBuilder _exportFileNameBuilder;

    public RunOrchestrator(
        SourcePoolScanner sourcePoolScanner,
        RunFolderService runFolderService,
        ValidationService validationService,
        ManifestBuilder manifestBuilder,
        SampleExportService sampleExportService,
        ExportFileNameBuilder exportFileNameBuilder)
    {
        _sourcePoolScanner = sourcePoolScanner;
        _runFolderService = runFolderService;
        _validationService = validationService;
        _manifestBuilder = manifestBuilder;
        _sampleExportService = sampleExportService;
        _exportFileNameBuilder = exportFileNameBuilder;
    }

    public RunResult Run(RunConfiguration runConfiguration, CancellationToken cancellationToken = default)
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

        var logger = new LoggingService(runContext.LogFilePath, runConfiguration.AppConfiguration.LoggingEnabled);
        var randomizationService = new RandomizationService(runContext.SongSelectionSeed, runContext.ProcessingSeed);
        var sourcePool = _sourcePoolScanner.Scan(runConfiguration.AppConfiguration.SourceFolderPath);
        var replayMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var replayData = new ReplaySupportData();
        var rowResults = new List<RowResult>();
        var exportRecords = new List<ExportedSampleRecord>();
        var usedStemTypesBySong = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in runConfiguration.StemRows.OrderBy(row => Array.IndexOf(Constants.StemTypes.Ordered, row.StemType)))
        {
            if (row.Quantity == 0)
            {
                rowResults.Add(new RowResult
                {
                    StemType = row.StemType,
                    Model = row.Model,
                    RequestedCount = row.Quantity,
                    ProducedCount = 0,
                    CandidateChunkLengthSeconds = row.CandidateChunkLengthSeconds,
                    FinalSampleLengthSeconds = row.FinalSampleLengthSeconds,
                    Status = RowStatus.Skipped
                });
                continue;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                rowResults.Add(new RowResult
                {
                    StemType = row.StemType,
                    Model = row.Model,
                    RequestedCount = row.Quantity,
                    ProducedCount = 0,
                    CandidateChunkLengthSeconds = row.CandidateChunkLengthSeconds,
                    FinalSampleLengthSeconds = row.FinalSampleLengthSeconds,
                    Status = RowStatus.Cancelled
                });
                break;
            }

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

                var candidateChunkStart = randomizationService.PickChunkStartSeconds(300 - row.CandidateChunkLengthSeconds);
                var exportFileName = _exportFileNameBuilder.Build(song, row.StemType);
                var exportPath = Path.Combine(runContext.RunFolderPath, exportFileName);
                _sampleExportService.ExportSilenceWav(
                    exportPath,
                    runConfiguration.AppConfiguration.ExportSampleRate,
                    runConfiguration.AppConfiguration.ExportBitDepth,
                    row.FinalSampleLengthSeconds);

                exportRecords.Add(new ExportedSampleRecord
                {
                    SourceFilePath = song,
                    CandidateChunkStartSeconds = candidateChunkStart,
                    CandidateChunkDurationSeconds = row.CandidateChunkLengthSeconds,
                    ModelUsed = row.Model,
                    StemTypeUsed = row.StemType,
                    ExportedFileName = exportFileName,
                    ExportedFullPath = exportPath
                });

                produced++;
                logger.Info($"Exported {exportFileName} from {song} for stem {row.StemType} ({produced}/{row.Quantity}).");
            }

            rowResults.Add(new RowResult
            {
                StemType = row.StemType,
                Model = row.Model,
                RequestedCount = row.Quantity,
                ProducedCount = produced,
                CandidateChunkLengthSeconds = row.CandidateChunkLengthSeconds,
                FinalSampleLengthSeconds = row.FinalSampleLengthSeconds,
                Status = ResolveRowStatus(produced, row.Quantity, cancellationToken.IsCancellationRequested)
            });
        }

        var end = DateTimeOffset.UtcNow;
        var overallStatus = ResolveRunStatus(rowResults, cancellationToken.IsCancellationRequested);

        var runResult = new RunResult
        {
            Status = overallStatus,
            RunStart = runContext.RunStart,
            RunEnd = end,
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

        _manifestBuilder.WriteManifest(runContext.RunFolderPath, runResult);
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
