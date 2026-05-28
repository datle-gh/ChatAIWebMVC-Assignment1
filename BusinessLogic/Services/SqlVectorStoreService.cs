using System.Text.Json;
using BusinessLogic.DTOs.Responses;
using BusinessObject.Entities;
using DataAccess.Repositories;

namespace BusinessLogic.Services;

public sealed class SqlVectorStoreService : IVectorStoreService
{
    private readonly IDocumentChunkEmbeddingRepository _embeddingRepository;

    public SqlVectorStoreService(IDocumentChunkEmbeddingRepository embeddingRepository)
    {
        _embeddingRepository = embeddingRepository;
    }

    public string Name => "Sql";

    public Task UpsertAsync(
        string embeddingModel,
        string provider,
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<float[]> embeddings,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<RetrievedChunkDto>> SearchAsync(
        int subjectId,
        string embeddingModel,
        float[] questionEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var embeddings = await _embeddingRepository.GetBySubjectAsync(
            subjectId,
            embeddingModel,
            cancellationToken);

        return embeddings
            .Select(item => new
            {
                Embedding = item,
                Vector = DeserializeEmbedding(item.EmbeddingJson)
            })
            .Where(item => item.Vector.Length > 0)
            .Select(item => new
            {
                item.Embedding,
                Similarity = CosineSimilarity(questionEmbedding, item.Vector)
            })
            .OrderByDescending(item => item.Similarity)
            .Take(Math.Max(1, topK))
            .Select(item =>
            {
                var chunk = item.Embedding.DocumentChunk;
                return new RetrievedChunkDto(
                    chunk.Id,
                    chunk.DocumentId,
                    chunk.Document.Title,
                    chunk.Document.OriginalFileName,
                    chunk.ChunkIndex,
                    chunk.Content,
                    chunk.PageNumber,
                    chunk.SlideNumber,
                    Math.Round((decimal)item.Similarity, 6),
                    embeddingModel,
                    Name);
            })
            .ToList();
    }

    private static float[] DeserializeEmbedding(string? embeddingJson)
    {
        if (string.IsNullOrWhiteSpace(embeddingJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<float[]>(embeddingJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        var length = Math.Min(left.Count, right.Count);
        if (length == 0)
        {
            return 0;
        }

        double dot = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;

        for (var index = 0; index < length; index++)
        {
            dot += left[index] * right[index];
            leftMagnitude += left[index] * left[index];
            rightMagnitude += right[index] * right[index];
        }

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}
