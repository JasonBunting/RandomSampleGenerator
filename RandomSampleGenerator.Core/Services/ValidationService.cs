using RandomSampleGenerator.Core.Constants;
using RandomSampleGenerator.Core.Models;

namespace RandomSampleGenerator.Core.Services;

public sealed class ValidationService
{
    public IReadOnlyList<string> ValidateBeforeRun(RunConfiguration runConfiguration)
    {
        var errors = new List<string>();
        var config = runConfiguration.AppConfiguration;

        if (!Directory.Exists(config.SourceFolderPath))
        {
            errors.Add("Configured source folder does not exist.");
        }
        else if (!CanReadFolder(config.SourceFolderPath))
        {
            errors.Add("Configured source folder is inaccessible.");
        }

        if (!Directory.Exists(config.TargetFolderPath))
        {
            errors.Add("Configured target folder does not exist.");
        }

        if (!CanWriteToFolder(config.TargetFolderPath))
        {
            errors.Add("Configured target folder is not writable.");
        }

        var rows = runConfiguration.StemRows.ToList();
        if (rows.All(row => row.Quantity <= 0))
        {
            errors.Add("At least one row must have quantity greater than 0.");
        }

        foreach (var row in rows)
        {
            if (!StemTypes.IsSupported(row.StemType))
            {
                errors.Add($"Unsupported stem type: {row.StemType}.");
            }

            if (!StemTypes.ModelSupportsStem(row.Model, row.StemType))
            {
                errors.Add($"Model '{row.Model}' does not support stem '{row.StemType}'.");
            }

            if (row.Quantity is < 0 or > 99)
            {
                errors.Add($"Quantity for stem '{row.StemType}' must be between 0 and 99.");
            }

            if (row.CandidateChunkLengthSeconds is < 10 or > 30)
            {
                errors.Add($"Candidate chunk length for stem '{row.StemType}' must be between 10 and 30.");
            }

            if (row.FinalSampleLengthSeconds is < 1 || row.FinalSampleLengthSeconds > row.CandidateChunkLengthSeconds)
            {
                errors.Add($"Final sample length for stem '{row.StemType}' must be between 1 and candidate chunk length.");
            }
        }

        return errors;
    }

    public PreflightValidationResult ValidatePreflight(RunConfiguration runConfiguration, int discoveredSourceFileCount)
    {
        var errors = ValidateBeforeRun(runConfiguration).ToList();
        var warnings = new List<string>();

        if (discoveredSourceFileCount <= 0)
        {
            errors.Add("No supported source files found (.wav, .mp3).");
            return new PreflightValidationResult { Errors = errors, Warnings = warnings };
        }

        var rows = runConfiguration.StemRows.Where(row => row.Quantity > 0).ToList();
        var totalRequestedSamples = rows.Sum(row => row.Quantity);
        var maxCandidateChunkLength = rows.Count > 0 ? rows.Max(row => row.CandidateChunkLengthSeconds) : 0;

        var warningMultiplier = Math.Max(1, runConfiguration.AppConfiguration.WarningRequestedSamplesPerDiscoveredSourceMultiplier);
        var hardMultiplier = Math.Max(warningMultiplier, runConfiguration.AppConfiguration.HardRequestedSamplesPerDiscoveredSourceMultiplier);

        var warningCeiling = discoveredSourceFileCount * warningMultiplier;
        var hardCeiling = discoveredSourceFileCount * hardMultiplier;

        if (totalRequestedSamples > hardCeiling)
        {
            errors.Add($"Requested sample count ({totalRequestedSamples}) exceeds hard sanity ceiling ({hardCeiling}) for discovered source pool size ({discoveredSourceFileCount}).");
        }
        else if (totalRequestedSamples > warningCeiling)
        {
            warnings.Add($"Requested sample count ({totalRequestedSamples}) may be unrealistic for discovered source pool size ({discoveredSourceFileCount}).");
        }

        if (maxCandidateChunkLength >= 25 && discoveredSourceFileCount < 3)
        {
            warnings.Add("Long candidate chunk lengths with a very small source pool may reduce successful outputs.");
        }

        return new PreflightValidationResult
        {
            Errors = errors,
            Warnings = warnings
        };
    }

    private static bool CanWriteToFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return false;
        }

        var probePath = Path.Combine(folderPath, $".{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(probePath, "probe");
            File.Delete(probePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool CanReadFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return false;
        }

        try
        {
            _ = Directory.EnumerateFileSystemEntries(folderPath).Take(1).ToList();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
