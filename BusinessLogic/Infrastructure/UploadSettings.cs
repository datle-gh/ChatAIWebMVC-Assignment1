using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Infrastructure;

public sealed class UploadSettings
{
    public const string SectionName = "UploadSettings";

    public int MaxFileSizeMb { get; set; } = 100;

    public int MaxFilesPerBatch { get; set; } = 10;

    public int MaxBatchSizeMb { get; set; } = 500;

    public long MaxFileSizeBytes => ToBytes(MaxFileSizeMb);

    public long MaxBatchSizeBytes => ToBytes(MaxBatchSizeMb);

    public static UploadSettings FromConfiguration(IConfiguration configuration)
    {
        var settings = new UploadSettings();
        var section = configuration.GetSection(SectionName);

        settings.MaxFileSizeMb = ReadInt(section, nameof(MaxFileSizeMb), settings.MaxFileSizeMb);
        settings.MaxFilesPerBatch = ReadInt(section, nameof(MaxFilesPerBatch), settings.MaxFilesPerBatch);
        settings.MaxBatchSizeMb = ReadInt(section, nameof(MaxBatchSizeMb), settings.MaxBatchSizeMb);

        return settings;
    }

    private static long ToBytes(int megabytes)
    {
        return megabytes * 1024L * 1024L;
    }

    private static int ReadInt(IConfiguration section, string key, int fallback)
    {
        return int.TryParse(section[key], out var value) && value > 0 ? value : fallback;
    }
}
