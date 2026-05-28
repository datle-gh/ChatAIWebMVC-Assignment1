using BusinessObject.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed class CitationRepository : ICitationRepository
{
    private readonly ChatAIWebDbContext _context;

    public CitationRepository(ChatAIWebDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(
        IEnumerable<Citation> citations,
        CancellationToken cancellationToken = default)
    {
        _context.Citations.AddRange(citations);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Citation>> GetByChatMessageIdAsync(
        int chatMessageId,
        CancellationToken cancellationToken = default)
    {
        return await CreateCitationQuery()
            .Where(citation => citation.ChatMessageId == chatMessageId)
            .OrderBy(citation => citation.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<Citation>>> GetByChatMessageIdsAsync(
        IEnumerable<int> chatMessageIds,
        CancellationToken cancellationToken = default)
    {
        var ids = chatMessageIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<Citation>>();
        }

        var citations = await CreateCitationQuery()
            .Where(citation => ids.Contains(citation.ChatMessageId))
            .OrderBy(citation => citation.Id)
            .ToListAsync(cancellationToken);

        return citations
            .GroupBy(citation => citation.ChatMessageId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Citation>)group.ToList());
    }

    private IQueryable<Citation> CreateCitationQuery()
    {
        return _context.Citations
            .AsNoTracking()
            .Include(citation => citation.Document)
            .Include(citation => citation.Chunk);
    }
}
