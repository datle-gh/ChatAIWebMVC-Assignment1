using BusinessObject.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ChatAIWebDbContext _context;

    public UserRepository(ChatAIWebDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLower();
        return _context.Users.FirstOrDefaultAsync(
            user => user.Email.ToLower() == normalizedEmail,
            cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Email)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> EmailExistsAsync(
        string email,
        int? excludedUserId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLower();
        return _context.Users.AnyAsync(
            user => user.Email.ToLower() == normalizedEmail
                && (!excludedUserId.HasValue || user.Id != excludedUserId.Value),
            cancellationToken);
    }

    public Task<bool> IsTeacherAssignedToSubjectAsync(
        int teacherId,
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        return _context.Subjects.AnyAsync(
            subject => subject.Id == subjectId
                && (subject.CreatedBy == teacherId
                    || subject.SubjectEnrollments.Any(enrollment =>
                        enrollment.UserId == teacherId
                        && enrollment.RoleInClass == "Teacher")),
            cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
