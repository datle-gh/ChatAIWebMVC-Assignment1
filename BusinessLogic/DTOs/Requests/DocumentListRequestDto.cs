using BusinessObject.Enums;

namespace BusinessLogic.DTOs.Requests;

public sealed record DocumentListRequestDto(
    string? SearchTerm,
    int? SubjectId,
    DocumentStatus? Status,
    int? CurrentUserId = null,
    string? CurrentUserRole = null);
