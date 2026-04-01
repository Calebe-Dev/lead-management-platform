using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Leads;

public sealed class ListLeadsUseCase
{
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadListCache _leadListCache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public ListLeadsUseCase(ILeadRepository leadRepository, ILeadListCache leadListCache)
    {
        _leadRepository = leadRepository;
        _leadListCache = leadListCache;
    }

    public async Task<ListLeadsResponse> ExecuteAsync(ListLeadsQuery? query, CancellationToken cancellationToken = default)
    {
        query ??= new ListLeadsQuery();

        if (query.Page <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Page), "Page must be greater than zero.");
        }

        if (query.PageSize <= 0 || query.PageSize > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(query.PageSize), "Page size must be between 1 and 200.");
        }

        if (query.MinScore is < 0 || query.MaxScore is < 0)
        {
            throw new ArgumentOutOfRangeException("Score filters cannot be negative.");
        }

        if (query.MinScore.HasValue && query.MaxScore.HasValue && query.MinScore > query.MaxScore)
        {
            throw new ArgumentException("MinScore cannot be greater than MaxScore.");
        }

        var cacheKey = BuildCacheKey(query);
        var cachedResponse = await _leadListCache.GetAsync(cacheKey, cancellationToken);
        if (cachedResponse is not null)
        {
            return cachedResponse;
        }

        var leads = await _leadRepository.ListAsync(query, cancellationToken);
        var response = new ListLeadsResponse(
            leads.Items.Select(lead => lead.ToResponse()).ToArray(),
            leads.Page,
            leads.PageSize,
            leads.TotalItems,
            leads.TotalPages);
        await _leadListCache.SetAsync(cacheKey, response, CacheTtl, cancellationToken);
        return response;
    }

    private static string BuildCacheKey(ListLeadsQuery query)
    {
        var status = query.Status?.ToString() ?? "*";
        var temperature = query.Temperature?.ToString() ?? "*";
        var region = NormalizeToken(query.Region);
        var leadType = NormalizeToken(query.LeadType);
        var productInterest = NormalizeToken(query.ProductInterest);
        var assignedTo = NormalizeToken(query.AssignedTo);
        var search = NormalizeToken(query.Search);
        var campaignId = query.CampaignId?.ToString() ?? "*";
        var minScore = query.MinScore?.ToString() ?? "*";
        var maxScore = query.MaxScore?.ToString() ?? "*";

        return $"leads:list:{status}:{temperature}:{region}:{leadType}:{productInterest}:{assignedTo}:{search}:{campaignId}:{minScore}:{maxScore}:{query.Page}:{query.PageSize}";
    }

    private static string NormalizeToken(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "*" : value.Trim().ToLowerInvariant();
}
