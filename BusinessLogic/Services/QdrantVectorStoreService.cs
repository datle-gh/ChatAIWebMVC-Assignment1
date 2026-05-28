using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BusinessLogic.DTOs.Responses;
using BusinessLogic.Infrastructure;
using BusinessObject.Entities;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class QdrantVectorStoreService : IVectorStoreService
{
    private readonly HttpClient _httpClient;
    private readonly IDocumentChunkRepository _chunkRepository;
    private readonly QdrantSettings _settings;
    private readonly ILogger<QdrantVectorStoreService> _logger;

    public QdrantVectorStoreService(
        HttpClient httpClient,
        IDocumentChunkRepository chunkRepository,
        IConfiguration configuration,
        ILogger<QdrantVectorStoreService> logger)
    {
        _httpClient = httpClient;
        _chunkRepository = chunkRepository;
        _settings = VectorStoreSettings.FromConfiguration(configuration).Qdrant;
        _logger = logger;

        _httpClient.BaseAddress ??= new Uri(_settings.BaseUrl);
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey)
            && !_httpClient.DefaultRequestHeaders.Contains("api-key"))
        {
            _httpClient.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);
        }
    }

    public string Name => "Qdrant";

    public async Task UpsertAsync(
        string embeddingModel,
        string provider,
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<float[]> embeddings,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0 || embeddings.Count == 0)
        {
            return;
        }

        var dimension = embeddings.First().Length;
        var collectionName = GetCollectionName(embeddingModel);
        await EnsureCollectionAsync(collectionName, dimension, cancellationToken);

        var points = chunks
            .Zip(embeddings)
            .Where(item => item.Second.Length == dimension)
            .Select(item => new QdrantPoint(
                item.First.Id,
                item.Second,
                new Dictionary<string, object?>
                {
                    ["subjectId"] = item.First.Document.SubjectId,
                    ["documentId"] = item.First.DocumentId,
                    ["chunkId"] = item.First.Id,
                    ["pageNumber"] = item.First.PageNumber,
                    ["slideNumber"] = item.First.SlideNumber,
                    ["embeddingModel"] = embeddingModel,
                    ["embeddingProvider"] = provider
                }))
            .ToList();

        if (points.Count == 0)
        {
            return;
        }

        var response = await _httpClient.PutAsJsonAsync(
            $"/collections/{collectionName}/points?wait=true",
            new QdrantUpsertRequest(points),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Qdrant upsert failed: {(int)response.StatusCode}. {detail}");
        }
    }

    public async Task<IReadOnlyList<RetrievedChunkDto>> SearchAsync(
        int subjectId,
        string embeddingModel,
        float[] questionEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var collectionName = GetCollectionName(embeddingModel);
        var response = await _httpClient.PostAsJsonAsync(
            $"/collections/{collectionName}/points/search",
            new QdrantSearchRequest(
                questionEmbedding,
                Math.Max(1, topK),
                true,
                new QdrantFilter([
                    new QdrantMustCondition("subjectId", new QdrantMatch(subjectId))
                ])),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Qdrant search failed: {(int)response.StatusCode}. {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(
            cancellationToken: cancellationToken);

        var scoredPoints = payload?.Result ?? [];
        var chunks = await _chunkRepository.GetIndexedChunksByIdsAsync(
            scoredPoints.Select(point => point.Id),
            cancellationToken);
        var chunksById = chunks.ToDictionary(chunk => chunk.Id);

        return scoredPoints
            .Where(point => chunksById.ContainsKey(point.Id))
            .Select(point =>
            {
                var chunk = chunksById[point.Id];
                return new RetrievedChunkDto(
                    chunk.Id,
                    chunk.DocumentId,
                    chunk.Document.Title,
                    chunk.Document.OriginalFileName,
                    chunk.ChunkIndex,
                    chunk.Content,
                    chunk.PageNumber,
                    chunk.SlideNumber,
                    Math.Round((decimal)point.Score, 6),
                    embeddingModel,
                    Name);
            })
            .ToList();
    }

    private async Task EnsureCollectionAsync(
        string collectionName,
        int dimension,
        CancellationToken cancellationToken)
    {
        var exists = await _httpClient.GetAsync($"/collections/{collectionName}", cancellationToken);
        if (exists.IsSuccessStatusCode)
        {
            return;
        }

        if (exists.StatusCode != HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Unable to check Qdrant collection {Collection}: {Status}",
                collectionName,
                exists.StatusCode);
        }

        var response = await _httpClient.PutAsJsonAsync(
            $"/collections/{collectionName}",
            new QdrantCreateCollectionRequest(new QdrantVectorParams(dimension, "Cosine")),
            cancellationToken);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Conflict)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Qdrant collection create failed: {(int)response.StatusCode}. {detail}");
        }
    }

    private string GetCollectionName(string embeddingModel)
    {
        var safeModel = Regex.Replace(embeddingModel.ToLowerInvariant(), @"[^a-z0-9_]+", "_").Trim('_');
        var safePrefix = Regex.Replace(_settings.CollectionPrefix.ToLowerInvariant(), @"[^a-z0-9_]+", "_").Trim('_');
        return $"{safePrefix}_{safeModel}";
    }

    private sealed record QdrantCreateCollectionRequest(
        [property: JsonPropertyName("vectors")] QdrantVectorParams Vectors);

    private sealed record QdrantVectorParams(
        [property: JsonPropertyName("size")] int Size,
        [property: JsonPropertyName("distance")] string Distance);

    private sealed record QdrantUpsertRequest(
        [property: JsonPropertyName("points")] IReadOnlyList<QdrantPoint> Points);

    private sealed record QdrantPoint(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("vector")] IReadOnlyList<float> Vector,
        [property: JsonPropertyName("payload")] IDictionary<string, object?> Payload);

    private sealed record QdrantSearchRequest(
        [property: JsonPropertyName("vector")] IReadOnlyList<float> Vector,
        [property: JsonPropertyName("limit")] int Limit,
        [property: JsonPropertyName("with_payload")] bool WithPayload,
        [property: JsonPropertyName("filter")] QdrantFilter Filter);

    private sealed record QdrantFilter(
        [property: JsonPropertyName("must")] IReadOnlyList<QdrantMustCondition> Must);

    private sealed record QdrantMustCondition(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("match")] QdrantMatch Match);

    private sealed record QdrantMatch(
        [property: JsonPropertyName("value")] int Value);

    private sealed record QdrantSearchResponse(
        [property: JsonPropertyName("result")] IReadOnlyList<QdrantScoredPoint>? Result);

    private sealed record QdrantScoredPoint(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("score")] double Score);
}
