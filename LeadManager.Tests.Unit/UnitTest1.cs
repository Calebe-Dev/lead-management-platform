using LeadManager.Domain.Leads;

namespace LeadManager.Tests.Unit;

public sealed class LeadTests
{
    [Fact]
    public void ApplyScore_ShouldClassifyAsHot_WhenScoreIsAbove60()
    {
        var lead = Lead.Create(
            "Jane Doe",
            "jane.doe@example.com",
            "+55-11-99999-0000",
            "Acme",
            "CEO",
            "organic");

        lead.ApplyScore(75);

        Assert.Equal(LeadTemperature.Hot, lead.Temperature);
        Assert.Equal(75, lead.Score);
    }
}
