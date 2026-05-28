namespace Presentation.Hubs;

public static class UploadProgressGroups
{
    public static string ForUserUpload(int userId, string uploadId)
    {
        return $"upload:{userId}:{uploadId}";
    }
}
