namespace RandomSampleGenerator.Core.Services;

public sealed class ExportFileNameBuilder
{
    private readonly Dictionary<(string sourceIdentity, string stemType), int> _ordinals = new();

    private readonly Dictionary<string, HashSet<long>> _sizesBySanitizedName = new(StringComparer.OrdinalIgnoreCase);

    public string Build(string sourceFilePath, string stemType)
    {
        var sanitizedName = string.Concat(Path.GetFileNameWithoutExtension(sourceFilePath).Where(character => !char.IsWhiteSpace(character)));
        sanitizedName = string.IsNullOrWhiteSpace(sanitizedName) ? "Source" : sanitizedName;

        var fileSize = new FileInfo(sourceFilePath).Length;
        var sourceIdentity = BuildSourceIdentity(sanitizedName, fileSize);
        var key = (sourceIdentity, stemType.ToLowerInvariant());

        var ordinal = _ordinals.TryGetValue(key, out var existingOrdinal) ? existingOrdinal + 1 : 0;
        _ordinals[key] = ordinal;

        return $"{sourceIdentity}-{stemType.ToLowerInvariant()}-{ordinal:00}.wav";
    }

    private string BuildSourceIdentity(string sanitizedName, long size)
    {
        if (!_sizesBySanitizedName.TryGetValue(sanitizedName, out var sizes))
        {
            sizes = [];
            _sizesBySanitizedName[sanitizedName] = sizes;
        }

        var requiresSizeSuffix = sizes.Count > 0 && !sizes.Contains(size);
        sizes.Add(size);

        return requiresSizeSuffix ? $"{sanitizedName}-{size}" : sanitizedName;
    }
}
