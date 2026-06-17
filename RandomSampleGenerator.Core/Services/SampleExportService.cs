using NAudio.Wave;

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

    public double? GetAudioDurationSeconds(string audioPath)
    {
        try
        {
            using var reader = new AudioFileReader(audioPath);
            return reader.TotalTime.TotalSeconds;
        }
        catch
        {
            return null;
        }
    }

    public string ExportFinalSampleWav(
        string separatedStemPath,
        string destinationPath,
        double finalSampleStartSeconds,
        int finalSampleLengthSeconds,
        int exportSampleRate,
        int exportBitDepth)
    {
        if (exportBitDepth is not 16 and not 24 and not 32)
        {
            throw new ArgumentOutOfRangeException(nameof(exportBitDepth), "v1 supports 16/24/32-bit WAV export.");
        }

        using var directReader = new WaveFileReader(separatedStemPath);
        if (directReader.WaveFormat.Encoding == WaveFormatEncoding.Pcm
            && directReader.WaveFormat.SampleRate == exportSampleRate
            && directReader.WaveFormat.BitsPerSample == exportBitDepth)
        {
            return ExportSegmentWithoutConversion(separatedStemPath, destinationPath, finalSampleStartSeconds, finalSampleLengthSeconds);
        }

        using var sourceReader = new AudioFileReader(separatedStemPath);
        var targetFormat = new WaveFormat(exportSampleRate, exportBitDepth, sourceReader.WaveFormat.Channels);

        using var resampler = new MediaFoundationResampler(sourceReader, targetFormat) { ResamplerQuality = 60 };
        return ExportSegmentFromProvider(sourceReader, resampler, destinationPath, finalSampleStartSeconds, finalSampleLengthSeconds, targetFormat);
    }

    private static string ExportSegmentWithoutConversion(
        string sourcePath,
        string destinationPath,
        double startSeconds,
        int lengthSeconds)
    {
        using var reader = new WaveFileReader(sourcePath);
        var format = reader.WaveFormat;
        var frameSize = format.BlockAlign;
        var bytesPerSecond = format.AverageBytesPerSecond;
        var bytesRequired = checked(bytesPerSecond * lengthSeconds);

        var maxStartSeconds = Math.Max(0, reader.TotalTime.TotalSeconds - lengthSeconds);
        var safeStart = Math.Min(startSeconds, maxStartSeconds);
        var startByte = (long)(safeStart * bytesPerSecond);
        startByte -= startByte % frameSize;
        reader.Position = startByte;

        using var writer = new WaveFileWriter(destinationPath, format);
        var buffer = new byte[8192];
        var totalBytesWritten = 0;

        while (totalBytesWritten < bytesRequired)
        {
            var bytesToRead = Math.Min(buffer.Length, bytesRequired - totalBytesWritten);
            var bytesRead = reader.Read(buffer, 0, bytesToRead);
            if (bytesRead <= 0)
            {
                break;
            }

            if (bytesRead % frameSize != 0)
            {
                bytesRead -= bytesRead % frameSize;
                if (bytesRead <= 0)
                {
                    break;
                }
            }

            writer.Write(buffer, 0, bytesRead);
            totalBytesWritten += bytesRead;
        }

        if (Math.Abs(totalBytesWritten - bytesRequired) > frameSize)
        {
            throw new InvalidOperationException($"Final sample export length mismatch. Expected {bytesRequired} bytes, wrote {totalBytesWritten} bytes.");
        }

        return destinationPath;
    }

    private static string ExportSegmentFromProvider(
        AudioFileReader sourceReader,
        IWaveProvider waveProvider,
        string destinationPath,
        double startSeconds,
        int lengthSeconds,
        WaveFormat targetFormat)
    {
        var startOffset = TimeSpan.FromSeconds(startSeconds);
        var maxStart = TimeSpan.FromSeconds(Math.Max(0, sourceReader.TotalTime.TotalSeconds - lengthSeconds));
        if (startOffset > maxStart)
        {
            startOffset = maxStart;
        }

        sourceReader.CurrentTime = startOffset;

        var bytesPerSecond = targetFormat.AverageBytesPerSecond;
        var bytesRequired = checked(bytesPerSecond * lengthSeconds);
        var frameSize = targetFormat.BlockAlign;

        using var writer = new WaveFileWriter(destinationPath, targetFormat);
        var buffer = new byte[8192];
        var totalBytesWritten = 0;

        while (totalBytesWritten < bytesRequired)
        {
            var bytesToRead = Math.Min(buffer.Length, bytesRequired - totalBytesWritten);
            var bytesRead = waveProvider.Read(buffer, 0, bytesToRead);
            if (bytesRead <= 0)
            {
                break;
            }

            if (bytesRead % frameSize != 0)
            {
                bytesRead -= bytesRead % frameSize;
                if (bytesRead <= 0)
                {
                    break;
                }
            }

            writer.Write(buffer, 0, bytesRead);
            totalBytesWritten += bytesRead;
        }

        if (Math.Abs(totalBytesWritten - bytesRequired) > frameSize)
        {
            throw new InvalidOperationException($"Final sample export length mismatch. Expected {bytesRequired} bytes, wrote {totalBytesWritten} bytes.");
        }

        return destinationPath;
    }
}
