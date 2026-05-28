namespace Presentation.Models;

public sealed class SystemSettingsViewModel
{
    public string LlmProvider { get; set; } = "Fake";
    public string GeminiApiKey { get; set; } = string.Empty;
    public string GeminiModel { get; set; } = "gemini-2.0-flash";
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = "gpt-4o-mini";
    public string EmbeddingProvider { get; set; } = "Fake";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public int TopK { get; set; } = 5;
    public decimal SimilarityThreshold { get; set; } = 0.7m;
    public int MaxCitationSnippetLength { get; set; } = 250;
    public string ChatSystemPrompt { get; set; } = string.Empty;
    public string EvaluationSystemPrompt { get; set; } = string.Empty;
    public string? SuccessMessage { get; set; }
}
