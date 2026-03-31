using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Leads;

public sealed class ListLeadsUseCase
{
    private readonly ILeadRepository _leadRepository;

    public ListLeadsUseCase(ILeadRepository leadRepository)
    {
        _leadRepository = leadRepository;
    }

    public async Task<IReadOnlyCollection<LeadResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var leads = await _leadRepository.ListAsync(cancellationToken);
        return leads.Select(lead => lead.ToResponse()).ToArray();
    }
}
