using LeadManager.Application.Campaigns;

namespace LeadManager.Application.Abstractions;

public interface ICampaignRepository
{
    Task<CampaignResponse> CreateAsync(UpsertCampaignCommand command, CancellationToken cancellationToken = default);
    Task<CampaignResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CampaignResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<CampaignResponse?> UpdateAsync(Guid id, UpsertCampaignCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
