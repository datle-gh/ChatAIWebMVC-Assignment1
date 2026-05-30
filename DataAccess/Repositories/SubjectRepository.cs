using BusinessObject.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed class SubjectRepository : ISubjectRepository
{
    private readonly ChatAIWebDbContext _context;

    public SubjectRepository(ChatAIWebDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Subjects
            .AsNoTracking()
            .Include(s => s.Documents)
            .Include(s => s.CreatedByNavigation)
            .Include(s => s.SubjectEnrollments)
                .ThenInclude(e => e.User)
            .Include(s => s.ChatSessions)
            .OrderBy(subject => subject.SubjectCode)
            .ThenBy(subject => subject.SubjectName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subject>> GetUploadableByTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Subjects
            .AsNoTracking()
            .Include(s => s.Documents)
            .Include(s => s.CreatedByNavigation)
            .Include(s => s.SubjectEnrollments)
            .Include(s => s.ChatSessions)
            .Where(subject =>
                subject.CreatedBy == teacherId
                || subject.SubjectEnrollments.Any(enrollment =>
                    enrollment.UserId == teacherId
                    && enrollment.RoleInClass == "Teacher"))
            .OrderBy(subject => subject.SubjectCode)
            .ThenBy(subject => subject.SubjectName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Subject?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Subjects
            .Include(s => s.Documents)
            .Include(s => s.CreatedByNavigation)
            .Include(s => s.SubjectEnrollments)
                .ThenInclude(e => e.User)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        await _context.Subjects.AddAsync(subject, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddEnrollmentAsync(
        SubjectEnrollment enrollment,
        CancellationToken cancellationToken = default)
    {
        await _context.SubjectEnrollments.AddAsync(enrollment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        _context.Subjects.Update(subject);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
