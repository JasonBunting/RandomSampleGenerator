namespace RandomSampleGenerator.Core.Services;

public sealed class SampleExportService
{
    public string ExportSilenceWav(string destinationPath, int sampleRate, int bitDepth, int lengthSeconds)
    {
        if (bitDepth != 16)
        {
            throw new ArgumentOutOfRangeException(nameof(bitDepth), "v1 supports 16-bit WAV export.");
        }

        var channels = 1;
        var bytesPerSample = bitDepth / 8;
        var sampleCount = sampleRate * lengthSeconds;
        var dataSize = sampleCount * channels * bytesPerSample;

        using var stream = File.Create(destinationPath);
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bytesPerSample);
        writer.Write((short)(channels * bytesPerSample));
        writer.Write((short)bitDepth);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);
        writer.Write(new byte[dataSize]);

        return destinationPath;
    }
}
