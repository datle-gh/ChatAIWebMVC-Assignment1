using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BusinessLogic.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Services;

public sealed class RagasEvaluatorClient : IRagasEvaluatorClient
{
    private readonly HttpClient _httpClient;

    public RagasEvaluatorClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var settings = RagasSettings.FromConfiguration(configuration);
        _httpClient.BaseAddress ??= new Uri(settings.ServiceBaseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
    }

    public async Task<IReadOnlyList<RagasEvaluationScore>> EvaluateAsync(
        IReadOnlyList<RagasEvaluationSample> samples,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/evaluate",
            new RagasEvaluateRequest(samples.Select(sample => new RagasEvaluateSample(
                sample.Question,
                sample.GroundTruthAnswer,
                sample.GeneratedAnswer,
                sample.RetrievedContexts)).ToList()),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RagasEvaluateResponse>(
            cancellationToken: cancellationToken);

        return payload?.Results?
            .Select(result => new RagasEvaluationScore(
                result.Faithfulness,
                result.AnswerRelevancy,
                result.ContextPrecision,
                result.ContextRecall))
            .ToList() ?? [];
    }

    private sealed record RagasEvaluateRequest(
        [property: JsonPropertyName("samples")] IReadOnlyList<RagasEvaluateSample> Samples);

    private sealed record RagasEvaluateSample(
        [property: JsonPropertyName("question")] string Question,
        [property: JsonPropertyName("groundTruthAnswer")] string GroundTruthAnswer,
        [property: JsonPropertyName("generatedAnswer")] string GeneratedAnswer,
        [property: JsonPropertyName("retrievedContexts")] IReadOnlyList<string> RetrievedContexts);

    private sealed record RagasEvaluateResponse(
        [property: JsonPropertyName("results")] IReadOnlyList<RagasEvaluateResult>? Results);

    private sealed record RagasEvaluateResult(
        [property: JsonPropertyName("faithfulness")] decimal Faithfulness,
        [property: JsonPropertyName("answerRelevancy")] decimal AnswerRelevancy,
        [property: JsonPropertyName("contextPrecision")] decimal ContextPrecision,
        [property: JsonPropertyName("contextRecall")] decimal ContextRecall);
}
