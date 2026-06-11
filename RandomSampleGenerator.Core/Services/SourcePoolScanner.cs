namespace RandomSampleGenerator.Core.Services;

public sealed class SourcePoolScanner
{
    private static readonly HashSet<string> SupportedExtensions = [".wav", ".mp3", ".flac", ".m4a", ".aac", ".ogg", ".aiff", ".wma"];

    public IReadOnlyList<string> Scan(string sourceRootPath)
    {
        if (!Directory.Exists(sourceRootPath))
        {
            return [];
        }

        return Directory.EnumerateFiles(sourceRootPath, "*.*", SearchOption.AllDirectories)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
