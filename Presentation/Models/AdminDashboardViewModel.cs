namespace Presentation.Models;

public sealed class AdminDashboardViewModel
{
    public int StudentCount { get; set; }
    public int TeacherCount { get; set; }
    public int AdminCount { get; set; }
    public int SubjectCount { get; set; }
    public int TotalDocumentCount { get; set; }
    public int IndexedDocumentCount { get; set; }
    public int ProcessingDocumentCount { get; set; }
    public int FailedDocumentCount { get; set; }
    public int PdfCount { get; set; }
    public int DocxCount { get; set; }
    public int PptxCount { get; set; }
    public int EvaluationQuestionCount { get; set; }
    public int BenchmarkRunCount { get; set; }
    public int ChatSessionCount { get; set; }
    public int ChatMessageCount { get; set; }
    public List<RecentDocumentItem> RecentDocuments { get; set; } = new();
    public List<RecentBenchmarkItem> RecentBenchmarks { get; set; } = new();
}

public sealed class RecentDocumentItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public sealed class RecentBenchmarkItem
{
    public int Id { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public string? LlmModel { get; set; }
    public decimal? OverallScore { get; set; }
    public DateTime CreatedAt { get; set; }
}
