using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;
using BusinessObject.Entities;
using BusinessObject.Enums;
using DataAccess.Repositories;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class SubjectService : ISubjectService
{
    private const string FilterCreated = "created";
    private const string FilterEnrolled = "enrolled";
    private const string FilterAll = "all";

    private readonly ISubjectRepository _subjectRepository;
    private readonly IChatRepository _chatRepository;
    private readonly ILogger<SubjectService> _logger;

    public SubjectService(
        ISubjectRepository subjectRepository,
        IChatRepository chatRepository,
        ILogger<SubjectService> logger)
    {
        _subjectRepository = subjectRepository;
        _chatRepository = chatRepository;
        _logger = logger;
    }

    public async Task<StudentDashboardDto> GetStudentDashboardAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);
        var userSessions = await _chatRepository.GetSessionsByUserAsync(userId, cancellationToken);

        var totalIndexed = subjects.Sum(s => s.Documents.Count(d => d.Status == DocumentStatus.Indexed));
        var sessionCount = userSessions.Count;

        var recentCourses = subjects
            .Take(6)
            .Select(s =>
            {
                var totalDocs = s.Documents.Count;
                var indexedDocs = s.Documents.Count(d => d.Status == DocumentStatus.Indexed);
                var chatCount = s.ChatSessions.Count(c => c.UserId == userId);
                var progress = totalDocs > 0 ? (int)Math.Round((double)indexedDocs / totalDocs * 100) : 0;
                return new RecentCourseDto(
                    s.Id,
                    s.SubjectCode,
                    s.SubjectName,
                    s.Description,
                    totalDocs,
                    indexedDocs,
                    chatCount,
                    progress);
            })
            .ToList();

        return new StudentDashboardDto(
            subjects.Count,
            sessionCount,
            totalIndexed,
            recentCourses);
    }

    public async Task<IReadOnlyList<SubjectDto>> GetAllSubjectsAsync(
        CancellationToken cancellationToken = default)
    {
        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);
        return subjects.Select(subject => MapToDto(subject)).ToList();
    }

    public async Task<IReadOnlyList<SubjectDto>> GetManagementSubjectsAsync(
        int currentUserId,
        string? currentUserRole,
        string? filter,
        CancellationToken cancellationToken = default)
    {
        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);
        var normalizedFilter = NormalizeManagementFilter(filter, currentUserRole);

        var filteredSubjects = subjects.Where(subject => normalizedFilter switch
        {
            FilterCreated => subject.CreatedBy == currentUserId,
            FilterEnrolled => IsTeacherParticipant(subject, currentUserId),
            _ => true
        });

        return filteredSubjects
            .Select(subject => MapToDto(subject, currentUserId, currentUserRole))
            .ToList();
    }

    public async Task<IReadOnlyList<SubjectDto>> GetSelectableSubjectsAsync(
        int currentUserId,
        string? currentUserRole,
        string? filter,
        CancellationToken cancellationToken = default)
    {
        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);

        if (string.Equals(currentUserRole, UserRoleNames.Student, StringComparison.OrdinalIgnoreCase))
        {
            return subjects
                .Where(subject => subject.Documents.Any(document => document.Status == DocumentStatus.Indexed))
                .Select(subject => MapToDto(subject, currentUserId, currentUserRole))
                .ToList();
        }

        var normalizedFilter = NormalizeManagementFilter(filter, currentUserRole);
        var filteredSubjects = subjects.Where(subject => normalizedFilter switch
        {
            FilterCreated => subject.CreatedBy == currentUserId,
            FilterEnrolled => IsTeacherParticipant(subject, currentUserId),
            _ => true
        });

        return filteredSubjects
            .Select(subject => MapToDto(subject, currentUserId, currentUserRole))
            .ToList();
    }

    public async Task<SubjectDto?> GetSubjectByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var subject = await _subjectRepository.GetByIdAsync(id, cancellationToken);
        return subject is null ? null : MapToDto(subject);
    }

    public async Task<bool> CanManageSubjectAsync(
        int subjectId,
        int currentUserId,
        string? currentUserRole,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(currentUserRole, UserRoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.Equals(currentUserRole, UserRoleNames.Teacher, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var subject = await _subjectRepository.GetByIdAsync(subjectId, cancellationToken);
        return subject?.CreatedBy == currentUserId;
    }

    public async Task<OperationResult> EnrollStudentAsync(
        int subjectId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var subject = await _subjectRepository.GetByIdAsync(subjectId, cancellationToken);
        if (subject is null)
        {
            return new OperationResult(false, "Không tìm thấy môn học.");
        }

        if (!subject.Documents.Any(document => document.Status == DocumentStatus.Indexed))
        {
            return new OperationResult(false, "Môn học này chưa có tài liệu đã index.");
        }

        var alreadyEnrolled = subject.SubjectEnrollments.Any(enrollment =>
            enrollment.UserId == studentId
            && string.Equals(enrollment.RoleInClass, UserRoleNames.Student, StringComparison.OrdinalIgnoreCase));

        if (alreadyEnrolled)
        {
            return new OperationResult(true, "Bạn đã tham gia môn học này.");
        }

        await _subjectRepository.AddEnrollmentAsync(
            new SubjectEnrollment
            {
                SubjectId = subjectId,
                UserId = studentId,
                RoleInClass = UserRoleNames.Student,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);

        return new OperationResult(true, "Tham gia môn học thành công.");
    }

    public async Task<OperationResult> CreateSubjectAsync(
        CreateSubjectRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = new Subject
            {
                SubjectCode = request.SubjectCode.Trim(),
                SubjectName = request.SubjectName.Trim(),
                Description = request.Description?.Trim(),
                CreatedBy = request.CreatedBy,
                CreatedAt = DateTime.UtcNow
            };

            if (string.Equals(request.CreatorRole, UserRoleNames.Teacher, StringComparison.OrdinalIgnoreCase))
            {
                subject.SubjectEnrollments.Add(new SubjectEnrollment
                {
                    UserId = request.CreatedBy,
                    RoleInClass = UserRoleNames.Teacher,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _subjectRepository.AddAsync(subject, cancellationToken);
            _logger.LogInformation("Created subject {Code} by user {UserId}", subject.SubjectCode, request.CreatedBy);
            return new OperationResult(true, "Tạo môn học thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create subject {Code}", request.SubjectCode);
            return new OperationResult(false, "Có lỗi khi tạo môn học. Mã môn học có thể đã tồn tại.");
        }
    }

    public async Task<OperationResult> UpdateSubjectAsync(
        UpdateSubjectRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var subject = await _subjectRepository.GetByIdAsync(request.Id, cancellationToken);
        if (subject is null)
        {
            return new OperationResult(false, "Không tìm thấy môn học.");
        }

        try
        {
            subject.SubjectCode = request.SubjectCode.Trim();
            subject.SubjectName = request.SubjectName.Trim();
            subject.Description = request.Description?.Trim();
            subject.UpdatedAt = DateTime.UtcNow;

            await _subjectRepository.UpdateAsync(subject, cancellationToken);
            _logger.LogInformation("Updated subject {Id}", subject.Id);
            return new OperationResult(true, "Cập nhật môn học thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update subject {Id}", request.Id);
            return new OperationResult(false, "Có lỗi khi cập nhật môn học. Mã môn học có thể đã tồn tại.");
        }
    }

    public async Task<OperationResult> DeleteSubjectAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var subject = await _subjectRepository.GetByIdAsync(id, cancellationToken);
        if (subject is null)
        {
            return new OperationResult(false, "Không tìm thấy môn học.");
        }

        try
        {
            await _subjectRepository.DeleteAsync(subject, cancellationToken);
            _logger.LogInformation("Deleted subject {Id}", id);
            return new OperationResult(true, "Xóa môn học thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete subject {Id}", id);
            return new OperationResult(false, "Không thể xóa môn học. Môn học này có thể đang được sử dụng.");
        }
    }

    private static string NormalizeManagementFilter(string? filter, string? currentUserRole)
    {
        if (string.Equals(filter, FilterCreated, StringComparison.OrdinalIgnoreCase))
        {
            return FilterCreated;
        }

        if (string.Equals(filter, FilterEnrolled, StringComparison.OrdinalIgnoreCase))
        {
            return FilterEnrolled;
        }

        if (string.Equals(filter, FilterAll, StringComparison.OrdinalIgnoreCase))
        {
            return FilterAll;
        }

        return string.Equals(currentUserRole, UserRoleNames.Teacher, StringComparison.OrdinalIgnoreCase)
            ? FilterEnrolled
            : FilterAll;
    }

    private static bool IsTeacherParticipant(Subject subject, int userId)
    {
        return subject.CreatedBy == userId
            || subject.SubjectEnrollments.Any(enrollment =>
                enrollment.UserId == userId
                && string.Equals(enrollment.RoleInClass, UserRoleNames.Teacher, StringComparison.OrdinalIgnoreCase));
    }

    private static SubjectDto MapToDto(
        Subject s,
        int? currentUserId = null,
        string? currentUserRole = null)
    {
        var totalDocs = s.Documents.Count;
        var indexedDocs = s.Documents.Count(d => d.Status == DocumentStatus.Indexed);
        var students = s.SubjectEnrollments.Count(e => e.RoleInClass == "Student");
        var teacherEnrollments = s.SubjectEnrollments.Where(e => e.RoleInClass == "Teacher").ToList();
        var teachers = teacherEnrollments.Count;
        var isTeacherEnrolled = currentUserId.HasValue
            && IsTeacherParticipant(s, currentUserId.Value);
        var isStudentEnrolled = currentUserId.HasValue
            && s.SubjectEnrollments.Any(e =>
                e.UserId == currentUserId.Value
                && string.Equals(e.RoleInClass, UserRoleNames.Student, StringComparison.OrdinalIgnoreCase));
        var isCreatedByCurrentUser = currentUserId.HasValue
            && s.CreatedBy == currentUserId.Value;
        var canManage = string.Equals(currentUserRole, UserRoleNames.Admin, StringComparison.OrdinalIgnoreCase)
            || (string.Equals(currentUserRole, UserRoleNames.Teacher, StringComparison.OrdinalIgnoreCase)
                && isCreatedByCurrentUser);
        var teacherNames = teacherEnrollments
            .Select(e => e.User?.FullName ?? "Giảng viên")
            .ToList();
        var memberNames = s.SubjectEnrollments
            .OrderBy(e => e.RoleInClass == "Student" ? 0 : 1)
            .ThenBy(e => e.User?.FullName)
            .Select(e => e.User?.FullName ?? (e.RoleInClass == "Student" ? "Sinh viên" : "Giảng viên"))
            .ToList();
        return new SubjectDto(
            s.Id,
            s.SubjectCode,
            s.SubjectName,
            s.Description,
            totalDocs,
            indexedDocs,
            students,
            teachers,
            s.CreatedAt,
            s.CreatedBy,
            s.CreatedByNavigation?.FullName,
            isTeacherEnrolled,
            isStudentEnrolled,
            canManage,
            teacherNames,
            memberNames);
    }
}
