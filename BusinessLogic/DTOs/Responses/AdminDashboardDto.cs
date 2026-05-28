namespace BusinessLogic.DTOs.Responses;

public sealed record AdminDashboardDto(
    int StudentCount,
    int TeacherCount,
    int AdminCount,
    int SubjectCount,
    int TotalDocumentCount,
    int IndexedDocumentCount,
    int ProcessingDocumentCount,
    int FailedDocumentCount,
    int PdfCount,
    int DocxCount,
    int PptxCount,
    int EvaluationQuestionCount,
    int BenchmarkRunCount,
    int ChatSessionCount,
    int ChatMessageCount,
    IReadOnlyList<RecentDocumentDto> RecentDocuments,
    IReadOnlyList<RecentBenchmarkDto> RecentBenchmarks);

public sealed record RecentDocumentDto(
    int Id,
    string Title,
    string FileType,
    string SubjectName,
    string Status,
    DateTime UploadedAt);

public sealed record RecentBenchmarkDto(
    int Id,
    string SubjectName,
    string EmbeddingModel,
    string? LlmModel,
    decimal? OverallScore,
    DateTime CreatedAt);
