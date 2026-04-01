using LeadManager.Application.Abstractions;

namespace LeadManager.Application.Campaigns;

public sealed class CampaignManagementUseCase
{
    private readonly ICampaignRepository _campaignRepository;

    public CampaignManagementUseCase(ICampaignRepository campaignRepository)
    {
        _campaignRepository = campaignRepository;
    }

    public Task<IReadOnlyCollection<CampaignResponse>> ListAsync(CancellationToken cancellationToken = default) =>
        _campaignRepository.ListAsync(cancellationToken);

    public Task<CampaignResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Campaign id is required.", nameof(id));
        }

        return _campaignRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<CampaignResponse> CreateAsync(UpsertCampaignCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command);
        return _campaignRepository.CreateAsync(command, cancellationToken);
    }

    public Task<CampaignResponse?> UpdateAsync(Guid id, UpsertCampaignCommand command, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Campaign id is required.", nameof(id));
        }

        Validate(command);
        return _campaignRepository.UpdateAsync(id, command, cancellationToken);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Campaign id is required.", nameof(id));
        }

        return _campaignRepository.DeleteAsync(id, cancellationToken);
    }

    private static void Validate(UpsertCampaignCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Campaign name is required.", nameof(command.Name));
        }

        if (string.IsNullOrWhiteSpace(command.Channel))
        {
            throw new ArgumentException("Campaign channel is required.", nameof(command.Channel));
        }
    }
}
