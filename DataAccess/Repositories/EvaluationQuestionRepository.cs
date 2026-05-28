using BusinessObject.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed class EvaluationQuestionRepository : IEvaluationQuestionRepository
{
    private readonly ChatAIWebDbContext _context;

    public EvaluationQuestionRepository(ChatAIWebDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EvaluationQuestion>> GetBySubjectAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        return await _context.EvaluationQuestions
            .AsNoTracking()
            .Include(eq => eq.Subject)
            .Include(eq => eq.CreatedByNavigation)
            .Where(eq => eq.SubjectId == subjectId)
            .OrderByDescending(eq => eq.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<EvaluationQuestion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.EvaluationQuestions
            .Include(eq => eq.Subject)
            .Include(eq => eq.CreatedByNavigation)
            .FirstOrDefaultAsync(eq => eq.Id == id, cancellationToken);
    }

    public async Task<EvaluationQuestion> AddAsync(EvaluationQuestion entity, CancellationToken cancellationToken = default)
    {
        _context.EvaluationQuestions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<EvaluationQuestion> entities, CancellationToken cancellationToken = default)
    {
        await _context.EvaluationQuestions.AddRangeAsync(entities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(EvaluationQuestion entity, CancellationToken cancellationToken = default)
    {
        _context.EvaluationQuestions.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(EvaluationQuestion entity, CancellationToken cancellationToken = default)
    {
        _context.EvaluationQuestions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountBySubjectAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        return await _context.EvaluationQuestions
            .CountAsync(eq => eq.SubjectId == subjectId, cancellationToken);
    }
}
