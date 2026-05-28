using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Infrastructure;

public sealed class LocalFileStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx",
        ".pptx"
    };

    private readonly string _uploadRoot;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _uploadRoot = configuration["FileStorage:UploadRoot"]
            ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "Uploads");
    }

    public async Task<StoredFileInfo> SaveAsync(
        Stream fileStream,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("File không đúng định dạng được hỗ trợ.");
        }

        Directory.CreateDirectory(_uploadRoot);

        var safeOriginalName = Path.GetFileName(originalFileName);
        var storedFileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(_uploadRoot, storedFileName);

        await using var outputStream = File.Create(filePath);
        await fileStream.CopyToAsync(outputStream, cancellationToken);

        return new StoredFileInfo(
            safeOriginalName,
            storedFileName,
            filePath,
            extension.TrimStart('.').ToUpperInvariant());
    }
}
