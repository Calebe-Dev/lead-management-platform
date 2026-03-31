using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;

namespace LeadManager.Application.Leads;

public sealed class CreateLeadUseCase
{
    private readonly ILeadRepository _leadRepository;

    public CreateLeadUseCase(ILeadRepository leadRepository)
    {
        _leadRepository = leadRepository;
    }

    public async Task<LeadResponse> ExecuteAsync(CreateLeadCommand command, CancellationToken cancellationToken = default)
    {
        var lead = Lead.Create(
            command.Name,
            command.Email,
            command.Phone,
            command.Company,
            command.JobTitle,
            command.Source);

        await _leadRepository.AddAsync(lead, cancellationToken);
        return lead.ToResponse();
    }
}
