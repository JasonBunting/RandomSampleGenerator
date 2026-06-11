using System.Text.Json;
using RandomSampleGenerator.Core.Models;

namespace RandomSampleGenerator.Core.Services;

public sealed class ManifestBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string WriteManifest(string runFolderPath, RunResult result)
    {
        var outputPath = Path.Combine(runFolderPath, RunFolderService.ManifestFileName);
        File.WriteAllText(outputPath, JsonSerializer.Serialize(result, JsonOptions));
        return outputPath;
    }
}
