using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services;

public interface IChatbotService
{
    Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
}
