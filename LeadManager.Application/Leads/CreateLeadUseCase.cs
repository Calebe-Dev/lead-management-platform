using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;

namespace LeadManager.Application.Leads;

public sealed class CreateLeadUseCase
{
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadListCache _leadListCache;

    public CreateLeadUseCase(ILeadRepository leadRepository, ILeadListCache leadListCache)
    {
        _leadRepository = leadRepository;
        _leadListCache = leadListCache;
    }

    public async Task<LeadResponse> ExecuteAsync(CreateLeadCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var lead = Lead.Create(
            command.Name,
            command.Email,
            command.Phone,
            command.Company,
            command.JobTitle,
            command.Source);

        await _leadRepository.AddAsync(lead, cancellationToken);
        await _leadListCache.InvalidateAsync(cancellationToken);
        return lead.ToResponse();
    }
}
