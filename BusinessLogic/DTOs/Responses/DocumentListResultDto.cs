namespace BusinessLogic.DTOs.Responses;

public sealed record DocumentListResultDto(
    IReadOnlyList<DocumentListItemDto> Documents,
    IReadOnlyList<SubjectOptionDto> Subjects);
