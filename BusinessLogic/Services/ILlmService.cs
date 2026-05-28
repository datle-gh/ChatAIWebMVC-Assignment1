namespace BusinessLogic.Services;

public interface ILlmService
{
    string ModelName { get; }

    Task<string> GenerateAnswerAsync(string prompt, CancellationToken cancellationToken = default);
}
