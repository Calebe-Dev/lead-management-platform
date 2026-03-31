using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Leads;

public sealed class RecalculateLeadScoreUseCase
{
    private readonly ILeadRepository _leadRepository;

    public RecalculateLeadScoreUseCase(ILeadRepository leadRepository)
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
        if (lead is null)
        {
            return null;
        }

        lead.RecalculateScore();
        await _leadRepository.UpdateAsync(lead, cancellationToken);

        return lead.ToResponse();
    }
}
