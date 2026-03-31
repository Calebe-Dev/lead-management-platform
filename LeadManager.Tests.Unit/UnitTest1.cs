using LeadManager.Application.Abstractions;
using LeadManager.Application.Leads;
using LeadManager.Domain.Leads;

namespace LeadManager.Tests.Unit;

public sealed class LeadDomainTests
{
    [Fact]
    public void Create_ShouldCalculateHotScore_WhenDecisionMakerFromReferralWithCompleteData()
    {
        var lead = Lead.Create(
            "Jane Doe",
            "jane.doe@example.com",
            "+55-11-99999-0000",
            "Acme",
            "CEO",
            "referral");

        Assert.Equal(85, lead.Score);
        Assert.Equal(LeadTemperature.Hot, lead.Temperature);
    }

    [Fact]
    public void Create_ShouldThrow_WhenEmailIsInvalid()
    {
        Assert.Throws<ArgumentException>(() => Lead.Create(
            "Jane Doe",
            "invalid-email",
            "+55-11-99999-0000",
            "Acme",
            "CEO",
            "referral"));
    }

    [Fact]
    public void RecalculateScore_ShouldApplyCompletenessPenalty_WhenOptionalDataIsMissing()
    {
        var lead = Lead.Create(
            "John Doe",
            "john.doe@example.com",
            "+55-11-99999-1111",
            string.Empty,
            string.Empty,
            "organic");

        Assert.Equal(5, lead.Score);
        Assert.Equal(LeadTemperature.Cold, lead.Temperature);
    }

    [Fact]
    public void ChangeStatus_ShouldThrow_WhenTransitionIsInvalid()
    {
        var lead = Lead.Create(
            "John Doe",
            "john.doe@example.com",
            "+55-11-99999-1111",
            "Acme",
            "Manager",
            "event");

        Assert.Throws<InvalidOperationException>(() => lead.ChangeStatus(LeadStatus.Converted));
    }
}

public sealed class LeadApplicationUseCaseTests
{
    [Fact]
    public async Task CreateLeadUseCase_ShouldPersistAndReturnMappedLead()
    {
        var repository = new FakeLeadRepository();
        var useCase = new CreateLeadUseCase(repository);

        var response = await useCase.ExecuteAsync(new CreateLeadCommand(
            "Jane Doe",
            "jane.doe@example.com",
            "+55-11-99999-0000",
            "Acme",
            "CEO",
            "referral"));

        Assert.Equal(response.Id, repository.StoredLead!.Id);
        Assert.Equal("Hot", response.Temperature);
    }

    [Fact]
    public async Task GetLeadByIdUseCase_ShouldReturnNull_WhenLeadDoesNotExist()
    {
        var repository = new FakeLeadRepository();
        var useCase = new GetLeadByIdUseCase(repository);

        var response = await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.Null(response);
    }

    [Fact]
    public async Task UpdateLeadStatusUseCase_ShouldUpdateStatus_WhenLeadExists()
    {
        var repository = new FakeLeadRepository();
        var lead = Lead.Create("Jane Doe", "jane.doe@example.com", "+55-11-99999-0000", "Acme", "CEO", "referral");
        await repository.AddAsync(lead);

        var useCase = new UpdateLeadStatusUseCase(repository);
        var response = await useCase.ExecuteAsync(lead.Id, new UpdateLeadStatusCommand(LeadStatus.InService));

        Assert.NotNull(response);
        Assert.Equal("InService", response!.Status);
    }

    [Fact]
    public async Task RecalculateLeadScoreUseCase_ShouldRecalculateAndPersist_WhenLeadExists()
    {
        var repository = new FakeLeadRepository();
        var lead = Lead.Create("Jane Doe", "jane.doe@example.com", "+55-11-99999-0000", "Acme", "CEO", "referral");
        lead.ApplyScore(0);
        await repository.AddAsync(lead);

        var useCase = new RecalculateLeadScoreUseCase(repository);
        var response = await useCase.ExecuteAsync(lead.Id);

        Assert.NotNull(response);
        Assert.Equal(85, response!.Score);
        Assert.Equal(1, repository.UpdatedCount);
    }

    [Fact]
    public async Task ListLeadsUseCase_ShouldReturnAllLeads()
    {
        var repository = new FakeLeadRepository();
        await repository.AddAsync(Lead.Create("Lead One", "one@example.com", "+55-11-99999-0001", "Acme", "Manager", "organic"));
        await repository.AddAsync(Lead.Create("Lead Two", "two@example.com", "+55-11-99999-0002", "Beta", "CEO", "referral"));

        var useCase = new ListLeadsUseCase(repository);
        var response = await useCase.ExecuteAsync();

        Assert.Equal(2, response.Count);
    }
}

internal sealed class FakeLeadRepository : ILeadRepository
{
    private readonly List<Lead> _leads = [];

    public Lead? StoredLead => _leads.LastOrDefault();
    public int UpdatedCount { get; private set; }

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

    public Task<IReadOnlyCollection<Lead>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Lead> snapshot = _leads.ToArray();
        return Task.FromResult(snapshot);
    }

    public Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        var index = _leads.FindIndex(existingLead => existingLead.Id == lead.Id);
        if (index >= 0)
        {
            _leads[index] = lead;
            UpdatedCount++;
        }

        return Task.CompletedTask;
    }
}
