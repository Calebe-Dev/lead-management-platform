using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeadManager.Application.Leads;
using LeadManager.Domain.Leads;

namespace LeadManager.Tests.Integration;

public sealed class AuthAndLeadFlowTests : IClassFixture<ApiTestWebApplicationFactory>
{
    private readonly ApiTestWebApplicationFactory _factory;

    public AuthAndLeadFlowTests(ApiTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedEndpoints_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/leads");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MarketingToken_ShouldCreateAndListLead_ButCannotUpdateStatus()
    {
        using var client = _factory.CreateClient();
        var token = await RequestToken(client, "marketing", "marketing123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var seed = Random.Shared.Next(100000, 999999).ToString();

        var createResponse = await client.PostAsJsonAsync("/api/leads", new CreateLeadCommand(
            $"Jane {seed}",
            $"jane.{seed}@example.com",
            $"+55-11-99999-{seed}",
            "Acme",
            "CEO",
            "referral",
            "South",
            "Enterprise",
            "CRM",
            null));
        var createBody = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.StatusCode == HttpStatusCode.Created, $"Expected Created but got {createResponse.StatusCode}. Body: {createBody}");
        var createdLead = await createResponse.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(createdLead);

        var listResponse = await client.GetAsync("/api/leads");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listPayload = await listResponse.Content.ReadFromJsonAsync<ListLeadsResponse>();
        Assert.NotNull(listPayload);
        Assert.True(listPayload!.Items.Count > 0);

        var updateResponse = await client.PatchAsJsonAsync(
            $"/api/leads/{createdLead!.Id}/status",
            new UpdateLeadStatusCommand(LeadStatus.InService));

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task VendasToken_ShouldAllowStatusTransition()
    {
        using var client = _factory.CreateClient();
        var adminToken = await RequestToken(client, "admin", "admin123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var seed = Random.Shared.Next(100000, 999999).ToString();

        var createResponse = await client.PostAsJsonAsync("/api/leads", new CreateLeadCommand(
            $"John {seed}",
            $"john.{seed}@example.com",
            $"+55-11-98888-{seed}",
            "Contoso",
            "Manager",
            "organic",
            "South",
            "SMB",
            "CRM",
            null));
        var createBody = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.StatusCode == HttpStatusCode.Created, $"Expected Created but got {createResponse.StatusCode}. Body: {createBody}");

        var createdLead = await createResponse.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(createdLead);

        var vendasToken = await RequestToken(client, "vendas", "vendas123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vendasToken);

        var updateResponse = await client.PatchAsJsonAsync(
            $"/api/leads/{createdLead!.Id}/status",
            new UpdateLeadStatusCommand(LeadStatus.InService));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedLead = await updateResponse.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.Equal(nameof(LeadStatus.InService), updatedLead!.Status);
    }

    private static async Task<string> RequestToken(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/token", new
        {
            Username = username,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TokenPayload>();
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
        return payload.AccessToken;
    }

    private sealed record TokenPayload(string AccessToken, DateTime ExpiresAtUtc);
}
