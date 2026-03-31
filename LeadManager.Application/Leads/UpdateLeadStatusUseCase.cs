using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Leads;

public sealed class UpdateLeadStatusUseCase
{
    private readonly ILeadRepository _leadRepository;

    public UpdateLeadStatusUseCase(ILeadRepository leadRepository)
    {
        _leadRepository = leadRepository;
    }

    public async Task<LeadResponse?> ExecuteAsync(Guid id, UpdateLeadStatusCommand command, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(command);

        var lead = await _leadRepository.GetByIdAsync(id, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        lead.ChangeStatus(command.Status);
        await _leadRepository.UpdateAsync(lead, cancellationToken);

        return lead.ToResponse();
    }
}
