using System.Text.Json;
using RandomSampleGenerator.Core.Constants;
using RandomSampleGenerator.Core.Models;

namespace RandomSampleGenerator.Core.Services;

public sealed class ConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _configPath;

    public ConfigurationService(string configPath)
    {
        _configPath = configPath;
    }

    public AppConfiguration LoadOrDefault()
    {
        if (!File.Exists(_configPath))
        {
            return CreateDefault();
        }

        var json = File.ReadAllText(_configPath);
        var value = JsonSerializer.Deserialize<AppConfiguration>(json) ?? CreateDefault();

        foreach (var stem in StemTypes.Ordered)
        {
            if (!value.ModelByStemType.TryGetValue(stem, out var model))
            {
                value.ModelByStemType[stem] = "htdemucs_6s";
            }
            else if (!StemTypes.ModelSupportsStem(model, stem))
            {
                value.ModelByStemType[stem] = StemTypes.SupportedModels.First(pair => pair.Value.Contains(stem)).Key;
            }
        }

        return value;
    }

    public void Save(AppConfiguration configuration)
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_configPath, JsonSerializer.Serialize(configuration, JsonOptions));
    }

    public static AppConfiguration CreateDefault()
    {
        var config = new AppConfiguration();
        foreach (var stem in StemTypes.Ordered)
        {
            config.ModelByStemType[stem] = StemTypes.SupportedModels.First(pair => pair.Value.Contains(stem)).Key;
        }

        return config;
    }
}
