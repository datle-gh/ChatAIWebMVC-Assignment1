using BusinessObject.Entities;

namespace DataAccess.Repositories;

public interface ICitationRepository
{
    Task AddRangeAsync(IEnumerable<Citation> citations, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Citation>> GetByChatMessageIdAsync(
        int chatMessageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, IReadOnlyList<Citation>>> GetByChatMessageIdsAsync(
        IEnumerable<int> chatMessageIds,
        CancellationToken cancellationToken = default);
}
