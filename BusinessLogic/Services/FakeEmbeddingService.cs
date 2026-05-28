using System.Security.Cryptography;
using System.Text;

namespace BusinessLogic.Services;

public sealed class FakeEmbeddingService : IEmbeddingService
{
    private const int Dimensions = 16;

    public string ModelKey => "fake";

    public string ModelName => "fake-deterministic-v1";

    public string ProviderName => "Fake";

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var normalizedText = text.Trim().ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedText));
        var embedding = new float[Dimensions];

        for (var index = 0; index < Dimensions; index++)
        {
            var value = BitConverter.ToUInt16(hash, index * 2);
            embedding[index] = (value / 32767.5f) - 1f;
        }

        return Task.FromResult(embedding);
    }
}
