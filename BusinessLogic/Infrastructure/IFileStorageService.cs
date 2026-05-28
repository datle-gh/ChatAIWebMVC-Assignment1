namespace BusinessLogic.Infrastructure;

public interface IFileStorageService
{
    Task<StoredFileInfo> SaveAsync(
        Stream fileStream,
        string originalFileName,
        CancellationToken cancellationToken = default);
}

public sealed record StoredFileInfo(
    string OriginalFileName,
    string StoredFileName,
    string FilePath,
    string FileType);
