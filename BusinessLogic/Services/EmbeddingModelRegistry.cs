using BusinessLogic.DTOs.Responses;
using BusinessLogic.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class EmbeddingModelRegistry : IEmbeddingModelRegistry
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConfiguredEmbeddingService> _embeddingLogger;
    private readonly EmbeddingSettings _settings;

    public EmbeddingModelRegistry(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ConfiguredEmbeddingService> embeddingLogger)
    {
        _httpClientFactory = httpClientFactory;
        _embeddingLogger = embeddingLogger;
        _settings = EmbeddingSettings.FromConfiguration(configuration);
    }

    public IEmbeddingService GetDefault()
    {
        return GetRequired(_settings.DefaultModelKey);
    }

    public IEmbeddingService GetRequired(string modelKey)
    {
        var model = _settings.Models.FirstOrDefault(item =>
            item.Enabled && string.Equals(item.Key, modelKey, StringComparison.OrdinalIgnoreCase));

        if (model is null)
        {
            throw new InvalidOperationException($"Không tìm thấy embedding model '{modelKey}'.");
        }

        return new ConfiguredEmbeddingService(
            _httpClientFactory.CreateClient($"Embedding:{model.Key}"),
            model,
            _embeddingLogger);
    }

    public IReadOnlyList<EmbeddingModelDto> GetAvailableModels(bool benchmarkOnly = false)
    {
        return _settings.Models
            .Where(model => model.Enabled && (!benchmarkOnly || model.IncludeInBenchmark))
            .Select(model => new EmbeddingModelDto(
                model.Key,
                model.Provider,
                model.Model,
                model.Dimension,
                model.Enabled,
                model.IncludeInBenchmark))
            .ToList();
    }
}
