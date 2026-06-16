namespace RandomSampleGenerator.App;

internal static class FolderValidation
{
    /// <summary>
    /// Verifies that the specified folder is writable by creating and deleting a temp file.
    /// </summary>
    public static bool IsFolderWritable(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return false;

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
}
