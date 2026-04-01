using LeadManager.Application.Abstractions;
using LeadManager.Application.Leads;
using LeadManager.Domain.Leads;

namespace LeadManager.Infrastructure.Persistence;

public sealed class InMemoryLeadRepository : ILeadRepository
{
    private static readonly List<Lead> Leads = [];
    private static readonly Lock Sync = new();

    public Task AddAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            Leads.Add(lead);
        }

        return Task.CompletedTask;
    }

    public Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            var lead = Leads.FirstOrDefault(existingLead => existingLead.Id == id);
            return Task.FromResult(lead);
        }
    }

    public Task<DuplicateLeadMatch?> FindDuplicateAsync(string email, string phone, string cnpj, CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            var normalizedPhone = NormalizeDigits(phone);
            var normalizedCnpj = NormalizeDigits(cnpj);

            var lead = Leads.FirstOrDefault(existing =>
                existing.Email == normalizedEmail
                || existing.Phone == normalizedPhone
                || (!string.IsNullOrWhiteSpace(normalizedCnpj) && existing.Cnpj == normalizedCnpj));

            if (lead is null)
            {
                return Task.FromResult<DuplicateLeadMatch?>(null);
            }

            var matched = new List<string>();
            if (lead.Email == normalizedEmail)
            {
                matched.Add("email");
            }

            if (lead.Phone == normalizedPhone)
            {
                matched.Add("phone");
            }

            if (!string.IsNullOrWhiteSpace(normalizedCnpj) && lead.Cnpj == normalizedCnpj)
            {
                matched.Add("cnpj");
            }

            return Task.FromResult<DuplicateLeadMatch?>(new DuplicateLeadMatch(lead, matched));
        }
    }

    public Task<PagedResult<Lead>> ListAsync(ListLeadsQuery query, CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            IEnumerable<Lead> filtered = Leads;

            if (query.Status.HasValue)
            {
                filtered = filtered.Where(lead => lead.Status == query.Status.Value);
            }

            if (query.Temperature.HasValue)
            {
                filtered = filtered.Where(lead => lead.Temperature == query.Temperature.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Region))
            {
                filtered = filtered.Where(lead => lead.Region == query.Region.Trim());
            }

            if (!string.IsNullOrWhiteSpace(query.LeadType))
            {
                filtered = filtered.Where(lead => lead.LeadType == query.LeadType.Trim());
            }

            if (!string.IsNullOrWhiteSpace(query.ProductInterest))
            {
                filtered = filtered.Where(lead => lead.ProductInterest == query.ProductInterest.Trim());
            }

            if (!string.IsNullOrWhiteSpace(query.AssignedTo))
            {
                filtered = filtered.Where(lead => lead.AssignedTo == query.AssignedTo.Trim());
            }

            if (query.MinScore.HasValue)
            {
                filtered = filtered.Where(lead => lead.Score >= query.MinScore.Value);
            }

            if (query.MaxScore.HasValue)
            {
                filtered = filtered.Where(lead => lead.Score <= query.MaxScore.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                filtered = filtered.Where(lead =>
                    lead.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || lead.Email.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || lead.Company.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (query.CampaignId.HasValue)
            {
                filtered = filtered.Where(lead => lead.CampaignId == query.CampaignId.Value);
            }

            var totalItems = filtered.Count();
            var items = filtered.OrderByDescending(x => x.CreatedAtUtc)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToArray();

            return Task.FromResult(new PagedResult<Lead>(items, query.Page, query.PageSize, totalItems));
        }
    }

    public Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            var index = Leads.FindIndex(existingLead => existingLead.Id == lead.Id);
            if (index >= 0)
            {
                Leads[index] = lead;
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            var removed = Leads.RemoveAll(lead => lead.Id == id) > 0;
            return Task.FromResult(removed);
        }
    }

    private static string NormalizeDigits(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsDigit).ToArray());
    }
}
