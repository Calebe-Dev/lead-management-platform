using LeadManager.Application.Abstractions;
using LeadManager.Application.Leads;
using LeadManager.Domain.Leads;

namespace LeadManager.Tests.Unit;

public sealed class LeadDomainTests
{
    [Fact]
    public void Create_ShouldCalculateHotScore_AndNormalizeFields()
    {
        var lead = Lead.Create(
            "Jane Doe",
            "JANE.DOE@EXAMPLE.COM",
            "+55-11-99999-0000",
            "Acme",
            "CEO",
            "referral",
            "South",
            "Enterprise",
            "CRM",
            "12.345.678/0001-90");

        Assert.Equal("jane.doe@example.com", lead.Email);
        Assert.Equal("5511999990000", lead.Phone);
        Assert.Equal("12345678000190", lead.Cnpj);
        Assert.Equal(85, lead.Score);
        Assert.Equal(LeadTemperature.Hot, lead.Temperature);
    }

    [Fact]
    public void Create_ShouldThrow_WhenRegionIsMissing()
    {
        Assert.Throws<ArgumentException>(() => Lead.Create(
            "Jane Doe",
            "jane.doe@example.com",
            "+55-11-99999-0000",
            "Acme",
            "CEO",
            "referral",
            "",
            "",
            "",
            ""));
    }
}

public sealed class LeadApplicationUseCaseTests
{
    [Fact]
    public async Task CreateLeadUseCase_ShouldRejectDuplicateLead_ByEmail()
    {
        var repository = new FakeLeadRepository();
        var history = new FakeLeadHistoryRepository();
        var assignment = new FakeLeadAssignmentService();
        var cache = new FakeLeadListCache();

        await repository.AddAsync(Lead.Create(
            "Existing",
            "dup@example.com",
            "+55-11-99999-1111",
            "Acme",
            "CEO",
            "referral",
            "South",
            "Enterprise",
            "CRM",
            ""));

        var useCase = new CreateLeadUseCase(repository, history, assignment, cache, new FakeLeadScoringService(), new FakeOutboxRepository(), new FakeAuditTrailRepository(), new FakeAssignmentRepository());

        var ex = await Assert.ThrowsAsync<DuplicateLeadException>(() => useCase.ExecuteAsync(new CreateLeadCommand(
            "New Lead",
            "dup@example.com",
            "+55-11-99999-2222",
            "Beta",
            "Manager",
            "organic",
            "South",
            "SMB",
            "CRM",
            null,
            null)));

        Assert.Contains("email", ex.MatchedFields);
    }

    [Fact]
    public async Task CreateLeadUseCase_ShouldAssignAndWriteHistory()
    {
        var repository = new FakeLeadRepository();
        var history = new FakeLeadHistoryRepository();
        var assignment = new FakeLeadAssignmentService("ana.silva");
        var cache = new FakeLeadListCache();
        var useCase = new CreateLeadUseCase(repository, history, assignment, cache, new FakeLeadScoringService(), new FakeOutboxRepository(), new FakeAuditTrailRepository(), new FakeAssignmentRepository());

        var response = await useCase.ExecuteAsync(new CreateLeadCommand(
            "Jane Doe",
            "jane.doe@example.com",
            "+55-11-99999-0000",
            "Acme",
            "CEO",
            "referral",
            "South",
            "Enterprise",
            "CRM",
            "12.345.678/0001-90",
            null));

        Assert.Equal("ana.silva", response.AssignedTo);
        Assert.True(history.Entries.Count >= 4);
        Assert.Contains(history.Entries, x => x.EventType == "AssignmentChanged");
        Assert.Equal(1, cache.InvalidateCount);
    }

    [Fact]
    public async Task UpdateLeadStatusUseCase_ShouldPersistHistory()
    {
        var repository = new FakeLeadRepository();
        var history = new FakeLeadHistoryRepository();
        var cache = new FakeLeadListCache();
        var lead = Lead.Create(
            "Jane Doe",
            "jane2.doe@example.com",
            "+55-11-99999-0009",
            "Acme",
            "CEO",
            "referral",
            "South",
            "Enterprise",
            "CRM",
            "");

        await repository.AddAsync(lead);
        var useCase = new UpdateLeadStatusUseCase(repository, history, cache, new FakeOutboxRepository(), new FakeAuditTrailRepository());

        var response = await useCase.ExecuteAsync(lead.Id, new UpdateLeadStatusCommand(LeadStatus.InService));

        Assert.NotNull(response);
        Assert.Equal("InService", response!.Status);
        Assert.Contains(history.Entries, x => x.EventType == "StatusChanged" && x.OldValue == "New" && x.NewValue == "InService");
        Assert.Equal(1, cache.InvalidateCount);
    }

    [Fact]
    public async Task ListLeadsUseCase_ShouldReturnPagedFilteredData()
    {
        var repository = new FakeLeadRepository();
        var cache = new FakeLeadListCache();
        var useCase = new ListLeadsUseCase(repository, cache);

        await repository.AddAsync(Lead.Create("A", "a@example.com", "+55-11-99999-0001", "Acme", "Manager", "organic", "South", "SMB", "CRM", ""));
        await repository.AddAsync(Lead.Create("B", "b@example.com", "+55-11-99999-0002", "Beta", "CEO", "referral", "North", "Enterprise", "ERP", ""));

        var response = await useCase.ExecuteAsync(new ListLeadsQuery
        {
            Region = "South",
            MinScore = 0,
            MaxScore = 100,
            Page = 1,
            PageSize = 10
        });

        Assert.Single(response.Items);
        Assert.Equal("South", response.Items.Single().Region);
        Assert.Equal(1, response.TotalItems);
        Assert.Equal(1, cache.SetCount);

        var cachedResponse = await useCase.ExecuteAsync(new ListLeadsQuery
        {
            Region = "South",
            MinScore = 0,
            MaxScore = 100,
            Page = 1,
            PageSize = 10
        });

        Assert.Single(cachedResponse.Items);
        Assert.True(cache.GetCount > 0);
    }
}

internal sealed class FakeLeadRepository : ILeadRepository
{
    private readonly List<Lead> _leads = [];

    public Task AddAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        _leads.Add(lead);
        return Task.CompletedTask;
    }

    public Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lead = _leads.FirstOrDefault(existingLead => existingLead.Id == id);
        return Task.FromResult(lead);
    }

    public Task<DuplicateLeadMatch?> FindDuplicateAsync(string email, string phone, string cnpj, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedPhone = NormalizeDigits(phone);
        var normalizedCnpj = NormalizeDigits(cnpj);

        var lead = _leads.FirstOrDefault(existing =>
            existing.Email == normalizedEmail
            || existing.Phone == normalizedPhone
            || (!string.IsNullOrWhiteSpace(normalizedCnpj) && existing.Cnpj == normalizedCnpj));

        if (lead is null)
        {
            return Task.FromResult<DuplicateLeadMatch?>(null);
        }

        var fields = new List<string>();
        if (lead.Email == normalizedEmail) fields.Add("email");
        if (lead.Phone == normalizedPhone) fields.Add("phone");
        if (!string.IsNullOrWhiteSpace(normalizedCnpj) && lead.Cnpj == normalizedCnpj) fields.Add("cnpj");

        return Task.FromResult<DuplicateLeadMatch?>(new DuplicateLeadMatch(lead, fields));
    }

    public Task<PagedResult<Lead>> ListAsync(ListLeadsQuery query, CancellationToken cancellationToken = default)
    {
        IEnumerable<Lead> filtered = _leads;

        if (query.Status.HasValue) filtered = filtered.Where(x => x.Status == query.Status.Value);
        if (query.Temperature.HasValue) filtered = filtered.Where(x => x.Temperature == query.Temperature.Value);
        if (!string.IsNullOrWhiteSpace(query.Region)) filtered = filtered.Where(x => x.Region == query.Region);
        if (query.MinScore.HasValue) filtered = filtered.Where(x => x.Score >= query.MinScore.Value);
        if (query.MaxScore.HasValue) filtered = filtered.Where(x => x.Score <= query.MaxScore.Value);

        var total = filtered.Count();
        var items = filtered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToArray();

        return Task.FromResult(new PagedResult<Lead>(items, query.Page, query.PageSize, total));
    }

    public Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        var index = _leads.FindIndex(existingLead => existingLead.Id == lead.Id);
        if (index >= 0)
        {
            _leads[index] = lead;
        }

        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = _leads.RemoveAll(existingLead => existingLead.Id == id) > 0;
        return Task.FromResult(deleted);
    }

    private static string NormalizeDigits(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return new string(value.Where(char.IsDigit).ToArray());
    }
}

internal sealed class FakeLeadHistoryRepository : ILeadHistoryRepository
{
    public List<LeadHistoryEntry> Entries { get; } = [];

    public Task AddRangeAsync(IReadOnlyCollection<LeadHistoryEntry> historyEntries, CancellationToken cancellationToken = default)
    {
        Entries.AddRange(historyEntries);
        return Task.CompletedTask;
    }

    public Task<PagedResult<LeadHistoryEntry>> ListByLeadIdAsync(Guid leadId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = Entries.Where(x => x.LeadId == leadId).ToArray();
        return Task.FromResult(new PagedResult<LeadHistoryEntry>(items, page, pageSize, items.Length));
    }
}

internal sealed class FakeLeadAssignmentService : ILeadAssignmentService
{
    private readonly string? _assignee;

    public FakeLeadAssignmentService(string? assignee = null)
    {
        _assignee = assignee;
    }

    public Task<string?> SelectAssigneeAsync(Lead lead, CancellationToken cancellationToken = default) =>
        Task.FromResult(_assignee);
}

internal sealed class FakeLeadListCache : ILeadListCache
{
    public int InvalidateCount { get; private set; }
    public int GetCount { get; private set; }
    public int SetCount { get; private set; }
    private readonly Dictionary<string, ListLeadsResponse> _cache = [];

    public Task<ListLeadsResponse?> GetAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        GetCount++;
        _cache.TryGetValue(cacheKey, out var response);
        return Task.FromResult(response);
    }

    public Task SetAsync(string cacheKey, ListLeadsResponse response, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        SetCount++;
        _cache[cacheKey] = response;
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        InvalidateCount++;
        _cache.Clear();
        return Task.CompletedTask;
    }
}

internal sealed class FakeLeadScoringService : ILeadScoringService
{
    public Task<int?> ScoreAsync(Lead lead, CancellationToken cancellationToken = default) =>
        Task.FromResult<int?>(null);
}

internal sealed class FakeOutboxRepository : IOutboxRepository
{
    public Task EnqueueAsync(string eventType, string payloadJson, string idempotencyKey, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyCollection<OutboxMessage>> DequeuePendingAsync(int batchSize, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<OutboxMessage>>([]);

    public Task MarkProcessedAsync(Guid id, DateTime processedAtUtc, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task MarkFailedAsync(Guid id, string errorMessage, int nextRetryInSeconds, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class FakeAuditTrailRepository : IAuditTrailRepository
{
    public Task WriteInteractionAsync(InteractionAuditRecord record, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task WriteBehaviorEventAsync(BehaviorEventRecord record, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task WriteAiDecisionAsync(AiDecisionRecord record, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class FakeAssignmentRepository : IAssignmentRepository
{
    public Task AddAsync(Guid leadId, string assignee, string reason, DateTime assignedAtUtc, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
