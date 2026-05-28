using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;
using BusinessObject.Entities;
using BusinessObject.Enums;
using DataAccess.Repositories;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class SubjectService : ISubjectService
{
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
        return subjects.Select(MapToDto).ToList();
    }

    public async Task<SubjectDto?> GetSubjectByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var subject = await _subjectRepository.GetByIdAsync(id, cancellationToken);
        return subject is null ? null : MapToDto(subject);
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

    private static SubjectDto MapToDto(Subject s)
    {
        var totalDocs = s.Documents.Count;
        var indexedDocs = s.Documents.Count(d => d.Status == DocumentStatus.Indexed);
        var students = s.SubjectEnrollments.Count(e => e.RoleInClass == "Student");
        var teacherEnrollments = s.SubjectEnrollments.Where(e => e.RoleInClass == "Teacher").ToList();
        var teachers = teacherEnrollments.Count;
        var teacherNames = teacherEnrollments
            .Select(e => e.User?.FullName ?? "Giảng viên")
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
            teacherNames);
    }
}
