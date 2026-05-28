namespace BusinessLogic.Services;

public interface IRagasEvaluatorClient
{
    Task<IReadOnlyList<RagasEvaluationScore>> EvaluateAsync(
        IReadOnlyList<RagasEvaluationSample> samples,
        CancellationToken cancellationToken = default);
}

public sealed record RagasEvaluationSample(
    string Question,
    string GroundTruthAnswer,
    string GeneratedAnswer,
    IReadOnlyList<string> RetrievedContexts);

public sealed record RagasEvaluationScore(
    decimal Faithfulness,
    decimal AnswerRelevancy,
    decimal ContextPrecision,
    decimal ContextRecall)
{
    public decimal OverallScore => (Faithfulness + AnswerRelevancy + ContextPrecision + ContextRecall) / 4.0m;
}
