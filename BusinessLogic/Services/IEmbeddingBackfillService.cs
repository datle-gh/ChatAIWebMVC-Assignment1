namespace BusinessLogic.Services;

public interface IEmbeddingBackfillService
{
    Task<int> BackfillSubjectAsync(
        int subjectId,
        string embeddingModel,
        CancellationToken cancellationToken = default);
}
