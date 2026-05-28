using BusinessObject.Entities;

namespace DataAccess.Repositories;

public interface IEvaluationQuestionRepository
{
    Task<IReadOnlyList<EvaluationQuestion>> GetBySubjectAsync(int subjectId, CancellationToken cancellationToken = default);
    Task<EvaluationQuestion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EvaluationQuestion> AddAsync(EvaluationQuestion entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<EvaluationQuestion> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(EvaluationQuestion entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(EvaluationQuestion entity, CancellationToken cancellationToken = default);
    Task<int> CountBySubjectAsync(int subjectId, CancellationToken cancellationToken = default);
}
