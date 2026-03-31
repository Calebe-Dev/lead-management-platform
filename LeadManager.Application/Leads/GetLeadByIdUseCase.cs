using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Leads;

public sealed class GetLeadByIdUseCase
{
    private readonly ILeadRepository _leadRepository;

    public GetLeadByIdUseCase(ILeadRepository leadRepository)
    {
        _leadRepository = leadRepository;
    }

    public async Task<LeadResponse?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(id));
        }

        var lead = await _leadRepository.GetByIdAsync(id, cancellationToken);
        return lead?.ToResponse();
    }
}
