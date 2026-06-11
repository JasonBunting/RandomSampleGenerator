namespace RandomSampleGenerator.Core.Constants;

public static class StemTypes
{
    public static readonly string[] Ordered = ["bass", "drums", "guitar", "other", "piano", "vocals"];

    public static readonly IReadOnlyDictionary<string, string[]> SupportedModels = new Dictionary<string, string[]>
    {
        ["htdemucs"] = ["vocals", "other", "drums", "bass"],
        ["htdemucs_6s"] = ["piano", "guitar", "vocals", "other", "drums", "bass"]
    };

    public static bool IsSupported(string stemType) => Ordered.Contains(stemType, StringComparer.OrdinalIgnoreCase);

    public static bool ModelSupportsStem(string model, string stemType) =>
        SupportedModels.TryGetValue(model, out var stems) && stems.Contains(stemType, StringComparer.OrdinalIgnoreCase);
}
