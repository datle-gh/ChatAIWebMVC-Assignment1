using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Infrastructure;

public sealed class RagasSettings
{
    public string ServiceBaseUrl { get; init; } = "http://localhost:8002";

    public static RagasSettings FromConfiguration(IConfiguration configuration)
    {
        var value = configuration["Ragas:ServiceBaseUrl"];
        return new RagasSettings
        {
            ServiceBaseUrl = string.IsNullOrWhiteSpace(value)
                ? "http://localhost:8002"
                : value.TrimEnd('/')
        };
    }
}
