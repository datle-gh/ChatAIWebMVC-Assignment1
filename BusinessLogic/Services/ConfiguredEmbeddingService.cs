using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BusinessLogic.Infrastructure;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class ConfiguredEmbeddingService : IEmbeddingService
{
    private static readonly SemaphoreSlim OllamaRequestLock = new(1, 1);

    private readonly HttpClient _httpClient;
    private readonly EmbeddingModelSettings _model;
    private readonly ILogger _logger;

    public ConfiguredEmbeddingService(
        HttpClient httpClient,
        EmbeddingModelSettings model,
        ILogger logger)
    {
        _httpClient = httpClient;
        _model = model;
        _logger = logger;

        if (_httpClient.BaseAddress is null && !string.IsNullOrWhiteSpace(model.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(model.BaseUrl.TrimEnd('/'));
        }
    }

    public string ModelKey => _model.Key;

    public string ModelName => _model.Model;

    public string ProviderName => _model.Provider;

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var cleanedText = NormalizeInput(text);
        if (string.IsNullOrWhiteSpace(cleanedText))
        {
            throw new InvalidOperationException("Nội dung cần embedding không được để trống.");
        }

        return _model.Provider.ToLowerInvariant() switch
        {
            "fake" => GenerateFakeEmbedding(cleanedText),
            "ollama" => await GenerateOllamaEmbeddingAsync(cleanedText, cancellationToken),
            "phobert" => await GeneratePhoBertEmbeddingAsync(cleanedText, cancellationToken),
            _ => throw new InvalidOperationException($"Embedding provider '{_model.Provider}' chưa được hỗ trợ.")
        };
    }

    private async Task<float[]> GenerateOllamaEmbeddingAsync(
        string text,
        CancellationToken cancellationToken)
    {
        var segments = SplitForEmbedding(text).ToList();
        if (segments.Count == 1)
        {
            return await GenerateSingleOllamaEmbeddingAsync(segments[0], cancellationToken);
        }

        var weighted = new List<(float[] Embedding, int Weight)>();
        foreach (var segment in segments)
        {
            weighted.Add((await GenerateSingleOllamaEmbeddingAsync(segment, cancellationToken), CountWords(segment)));
        }

        return AverageEmbeddings(weighted);
    }

    private async Task<float[]> GenerateSingleOllamaEmbeddingAsync(
        string text,
        CancellationToken cancellationToken)
    {
        await OllamaRequestLock.WaitAsync(cancellationToken);
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/embed",
                new OllamaEmbedRequest(_model.Model, text, true),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Ollama embedding failed with status {StatusCode}: {Detail}",
                    response.StatusCode,
                    detail);

                throw new InvalidOperationException(CreateEmbeddingFailureMessage("Ollama", response.StatusCode));
            }

            var payload = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(
                cancellationToken: cancellationToken);

            return ValidateEmbedding(payload?.Embeddings?.FirstOrDefault(), "Ollama");
        }
        finally
        {
            OllamaRequestLock.Release();
        }
    }

    private async Task<float[]> GeneratePhoBertEmbeddingAsync(
        string text,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/embed",
            new PhoBertEmbedRequest(_model.Model, new[] { text }),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "PhoBERT embedding failed with status {StatusCode}: {Detail}",
                response.StatusCode,
                detail);

            throw new InvalidOperationException(CreateEmbeddingFailureMessage("PhoBERT", response.StatusCode));
        }

        var payload = await response.Content.ReadFromJsonAsync<PhoBertEmbedResponse>(
            cancellationToken: cancellationToken);

        return ValidateEmbedding(payload?.Embeddings?.FirstOrDefault(), "PhoBERT");
    }

    private IEnumerable<string> SplitForEmbedding(string text)
    {
        var maxCharacters = _model.MaxInputCharacters > 0 ? _model.MaxInputCharacters : 3000;
        if (text.Length <= maxCharacters)
        {
            yield return text;
            yield break;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var builder = new StringBuilder();
        foreach (var word in words)
        {
            if (builder.Length > 0 && builder.Length + word.Length + 1 > maxCharacters)
            {
                yield return builder.ToString();
                builder.Clear();
            }

            if (word.Length > maxCharacters)
            {
                yield return word[..maxCharacters];
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(word);
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }

    private static float[] ValidateEmbedding(float[]? embedding, string provider)
    {
        if (embedding is null || embedding.Length == 0 || embedding.Any(value => float.IsNaN(value) || float.IsInfinity(value)))
        {
            throw new InvalidOperationException($"Không nhận được embedding hợp lệ từ {provider}.");
        }

        NormalizeVector(embedding);
        return embedding;
    }

    private static float[] GenerateFakeEmbedding(string text)
    {
        const int dimensions = 16;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text.Trim().ToLowerInvariant()));
        var embedding = new float[dimensions];
        for (var index = 0; index < dimensions; index++)
        {
            var value = BitConverter.ToUInt16(hash, index * 2);
            embedding[index] = (value / 32767.5f) - 1f;
        }

        NormalizeVector(embedding);
        return embedding;
    }

    private static float[] AverageEmbeddings(IReadOnlyList<(float[] Embedding, int Weight)> weightedEmbeddings)
    {
        var dimensions = weightedEmbeddings[0].Embedding.Length;
        var result = new double[dimensions];
        var totalWeight = 0;

        foreach (var (embedding, weight) in weightedEmbeddings.Where(item => item.Embedding.Length == dimensions))
        {
            var safeWeight = Math.Max(1, weight);
            totalWeight += safeWeight;
            for (var index = 0; index < dimensions; index++)
            {
                result[index] += embedding[index] * safeWeight;
            }
        }

        var averaged = result.Select(value => (float)(value / Math.Max(1, totalWeight))).ToArray();
        NormalizeVector(averaged);
        return averaged;
    }

    private static void NormalizeVector(float[] embedding)
    {
        double magnitude = 0;
        foreach (var value in embedding)
        {
            magnitude += value * value;
        }

        if (magnitude <= 0)
        {
            return;
        }

        var divisor = Math.Sqrt(magnitude);
        for (var index = 0; index < embedding.Length; index++)
        {
            embedding[index] = (float)(embedding[index] / divisor);
        }
    }

    private static string NormalizeInput(string text)
    {
        var withoutControlCharacters = new string(
            text
                .Normalize(NormalizationForm.FormC)
                .Where(character =>
                    !char.IsControl(character)
                    || character is '\r' or '\n' or '\t')
                .ToArray());

        return Regex.Replace(withoutControlCharacters, @"\s+", " ").Trim();
    }

    private static int CountWords(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
    }

    private static string CreateEmbeddingFailureMessage(string provider, HttpStatusCode statusCode)
    {
        return $"Không thể tạo embedding. {provider} trả về {(int)statusCode}.";
    }

    private sealed record OllamaEmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("truncate")] bool Truncate);

    private sealed record OllamaEmbedResponse(
        [property: JsonPropertyName("embeddings")] float[][]? Embeddings);

    private sealed record PhoBertEmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("texts")] IReadOnlyList<string> Texts);

    private sealed record PhoBertEmbedResponse(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("dimension")] int Dimension,
        [property: JsonPropertyName("embeddings")] float[][]? Embeddings);
}
