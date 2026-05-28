using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BusinessLogic.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class OllamaEmbeddingService : IEmbeddingService
{
    private static readonly SemaphoreSlim OllamaRequestLock = new(1, 1);

    private readonly HttpClient _httpClient;
    private readonly OllamaEmbeddingSettings _settings;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public OllamaEmbeddingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OllamaEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var embeddingSettings = EmbeddingSettings.FromConfiguration(configuration);
        _settings = embeddingSettings.Ollama;

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(NormalizeBaseUrl(_settings.BaseUrl));
        }
    }

    public string ModelKey => _settings.Model;

    public string ModelName => _settings.Model;

    public string ProviderName => "Ollama";

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var cleanedText = NormalizeInput(text);
        if (string.IsNullOrWhiteSpace(cleanedText))
        {
            throw new InvalidOperationException("Nội dung cần embedding không được để trống.");
        }

        try
        {
            return await GenerateEmbeddingWithFallbackAsync(cleanedText, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Cannot connect to Ollama at {BaseUrl}", _settings.BaseUrl);
            throw new InvalidOperationException("Không thể kết nối Ollama để tạo embedding.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Ollama embedding request timed out");
            throw new InvalidOperationException("Quá thời gian chờ khi tạo embedding.", exception);
        }
    }

    private async Task<float[]> GenerateEmbeddingWithFallbackAsync(
        string text,
        CancellationToken cancellationToken)
    {
        var segments = SplitForEmbedding(text).ToList();
        if (segments.Count == 1)
        {
            try
            {
                return await GenerateSingleEmbeddingAsync(segments[0], cancellationToken);
            }
            catch (InvalidOperationException exception) when (ShouldRetryAsSubSegments(exception, segments[0]))
            {
                _logger.LogWarning(
                    exception,
                    "Retrying Ollama embedding by splitting problematic input. Length: {Length}",
                    segments[0].Length);

                segments = SplitTextByWords(segments[0], Math.Max(600, _settings.MaxInputCharacters / 2)).ToList();
            }
        }

        var weightedEmbeddings = new List<(float[] Embedding, int Weight)>();
        foreach (var segment in segments)
        {
            weightedEmbeddings.AddRange(
                await GenerateWeightedEmbeddingsForSegmentAsync(segment, depth: 0, cancellationToken));
        }

        return AverageEmbeddings(weightedEmbeddings);
    }

    private async Task<IReadOnlyList<(float[] Embedding, int Weight)>> GenerateWeightedEmbeddingsForSegmentAsync(
        string segment,
        int depth,
        CancellationToken cancellationToken)
    {
        try
        {
            var embedding = await GenerateSingleEmbeddingAsync(segment, cancellationToken);
            return [(embedding, CountWords(segment))];
        }
        catch (InvalidOperationException exception) when (depth < 3 && ShouldRetryAsSubSegments(exception, segment))
        {
            _logger.LogWarning(
                exception,
                "Retrying Ollama embedding segment by splitting it again. Length: {Length}, Depth: {Depth}",
                segment.Length,
                depth);

            var smallerSegments = SplitTextByWords(segment, Math.Max(300, segment.Length / 2)).ToList();
            var results = new List<(float[] Embedding, int Weight)>();

            foreach (var smallerSegment in smallerSegments)
            {
                results.AddRange(
                    await GenerateWeightedEmbeddingsForSegmentAsync(
                        smallerSegment,
                        depth + 1,
                        cancellationToken));
            }

            return results;
        }
    }

    private async Task<float[]> GenerateSingleEmbeddingAsync(
        string text,
        CancellationToken cancellationToken)
    {
        InvalidOperationException? lastFailure = null;

        for (var attempt = 0; attempt <= _settings.MaxRetryCount; attempt++)
        {
            if (attempt > 0)
            {
                await Task.Delay(_settings.RetryDelayMilliseconds * attempt, cancellationToken);
            }

            await OllamaRequestLock.WaitAsync(cancellationToken);
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "/api/embed",
                    new OllamaEmbedRequest(_settings.Model, text, _settings.Truncate),
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "Ollama embedding failed with status {StatusCode}: {Detail}",
                        response.StatusCode,
                        detail);

                    lastFailure = new InvalidOperationException(CreateFailureMessage(response.StatusCode, detail));
                    continue;
                }

                var payload = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(
                    cancellationToken: cancellationToken);

                var embedding = payload?.Embeddings?.FirstOrDefault();
                if (embedding is null || embedding.Length == 0 || embedding.Any(value => float.IsNaN(value) || float.IsInfinity(value)))
                {
                    lastFailure = new InvalidOperationException("Không nhận được embedding hợp lệ từ Ollama.");
                    continue;
                }

                return embedding;
            }
            finally
            {
                OllamaRequestLock.Release();
            }
        }

        throw lastFailure ?? new InvalidOperationException("Không thể tạo embedding cho tài liệu.");
    }

    private IEnumerable<string> SplitForEmbedding(string text)
    {
        if (text.Length <= _settings.MaxInputCharacters)
        {
            yield return text;
            yield break;
        }

        foreach (var segment in SplitTextByWords(text, _settings.MaxInputCharacters))
        {
            yield return segment;
        }
    }

    private static IEnumerable<string> SplitTextByWords(string text, int maxCharacters)
    {
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
                foreach (var slice in SplitLongWord(word, maxCharacters))
                {
                    yield return slice;
                }

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

    private static IEnumerable<string> SplitLongWord(string word, int maxCharacters)
    {
        for (var index = 0; index < word.Length; index += maxCharacters)
        {
            yield return word.Substring(index, Math.Min(maxCharacters, word.Length - index));
        }
    }

    private static float[] AverageEmbeddings(IReadOnlyList<(float[] Embedding, int Weight)> weightedEmbeddings)
    {
        if (weightedEmbeddings.Count == 0)
        {
            throw new InvalidOperationException("Không nhận được embedding hợp lệ từ Ollama.");
        }

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

        if (totalWeight == 0)
        {
            throw new InvalidOperationException("Không nhận được embedding hợp lệ từ Ollama.");
        }

        var averaged = result.Select(value => (float)(value / totalWeight)).ToArray();
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
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

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

    private static bool ShouldRetryAsSubSegments(Exception exception, string text)
    {
        return text.Length > 1200
            && exception.Message.Contains("Ollama", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateFailureMessage(HttpStatusCode statusCode, string detail)
    {
        return detail.Contains("NaN", StringComparison.OrdinalIgnoreCase)
            ? "Ollama không tạo được embedding ổn định cho một đoạn tài liệu."
            : $"Không thể tạo embedding cho tài liệu. Ollama trả về {(int)statusCode}.";
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        return string.IsNullOrWhiteSpace(baseUrl)
            ? "http://localhost:11434"
            : baseUrl.TrimEnd('/');
    }

    private sealed record OllamaEmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("truncate")] bool Truncate);

    private sealed record OllamaEmbedResponse(
        [property: JsonPropertyName("embeddings")] float[][]? Embeddings);
}
