using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services;

public interface ISubjectService
{
    /// <summary>
    /// Lấy dữ liệu tổng quan cho Dashboard Sinh viên.
    /// </summary>
    Task<StudentDashboardDto> GetStudentDashboardAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách toàn bộ môn học kèm thống kê phục vụ trang Chọn môn học và Quản lý môn học.
    /// </summary>
    Task<IReadOnlyList<SubjectDto>> GetAllSubjectsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubjectDto>> GetManagementSubjectsAsync(
        int currentUserId,
        string? currentUserRole,
        string? filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubjectDto>> GetSelectableSubjectsAsync(
        int currentUserId,
        string? currentUserRole,
        string? filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chi tiết một môn học theo Id.
    /// </summary>
    Task<SubjectDto?> GetSubjectByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> CanManageSubjectAsync(
        int subjectId,
        int currentUserId,
        string? currentUserRole,
        CancellationToken cancellationToken = default);

    Task<OperationResult> EnrollStudentAsync(
        int subjectId,
        int studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới môn học.
    /// </summary>
    Task<OperationResult> CreateSubjectAsync(
        CreateSubjectRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật thông tin môn học.
    /// </summary>
    Task<OperationResult> UpdateSubjectAsync(
        UpdateSubjectRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa môn học theo Id.
    /// </summary>
    Task<OperationResult> DeleteSubjectAsync(
        int id,
        CancellationToken cancellationToken = default);
}
