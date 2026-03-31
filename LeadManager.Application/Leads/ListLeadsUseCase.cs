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

    public async Task<IReadOnlyCollection<LeadResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cachedLeads = await _leadListCache.GetAsync(cancellationToken);
        if (cachedLeads is not null)
        {
            return cachedLeads;
        }

        var leads = await _leadRepository.ListAsync(cancellationToken);
        var response = leads.Select(lead => lead.ToResponse()).ToArray();
        await _leadListCache.SetAsync(response, CacheTtl, cancellationToken);
        return response;
    }
}
