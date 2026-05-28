using BusinessObject.Entities;

namespace DataAccess.Repositories;

public interface ISubjectRepository
{
    Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subject>> GetUploadableByTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<Subject?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Subject subject, CancellationToken cancellationToken = default);

    Task UpdateAsync(Subject subject, CancellationToken cancellationToken = default);

    Task DeleteAsync(Subject subject, CancellationToken cancellationToken = default);
}
