using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using LeadManager.Application.Abstractions;
using LeadManager.Domain.Leads;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LeadManager.Infrastructure.Scoring;

public sealed class LeadScoringService : ILeadScoringService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LeadScoringOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly IAuditTrailRepository _auditTrailRepository;

    public LeadScoringService(
        IHttpClientFactory httpClientFactory,
        IOptions<LeadScoringOptions> options,
        IHostEnvironment environment,
        IAuditTrailRepository auditTrailRepository)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _environment = environment;
        _auditTrailRepository = auditTrailRepository;
    }

    public async Task<int?> ScoreAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lead);
        if (!_options.Enabled)
        {
            return null;
        }

        var payload = CreatePayload(lead, _environment.IsProduction());

        var openAiScore = await TryScoreWithOpenAi(payload, cancellationToken);
        if (openAiScore.HasValue)
        {
            await _auditTrailRepository.WriteAiDecisionAsync(
                new AiDecisionRecord(
                    lead.Id,
                    "openai",
                    "v1",
                    $$"""{"score":{{openAiScore.Value}}}""",
                    DateTime.UtcNow),
                cancellationToken);
            return ClampScore(openAiScore.Value);
        }

        var azureScore = await TryScoreWithAzure(payload, cancellationToken);
        if (azureScore.HasValue)
        {
            await _auditTrailRepository.WriteAiDecisionAsync(
                new AiDecisionRecord(
                    lead.Id,
                    "azure-openai",
                    "v1",
                    $$"""{"score":{{azureScore.Value}}}""",
                    DateTime.UtcNow),
                cancellationToken);
            return ClampScore(azureScore.Value);
        }

        return null;
    }

    private async Task<int?> TryScoreWithOpenAi(object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.OpenAiEndpoint)
            || string.IsNullOrWhiteSpace(_options.OpenAiApiKey)
            || string.IsNullOrWhiteSpace(_options.OpenAiModel))
        {
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(LeadScoringService));
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.OpenAiEndpoint.TrimEnd('/') + "/v1/responses");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.OpenAiApiKey);
            request.Content = JsonContent.Create(new
            {
                model = _options.OpenAiModel,
                input = $$"""
                Score this lead from 0 to 100. Return only a number.
                {{JsonSerializer.Serialize(payload)}}
                """
            });

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            return ExtractFirstInteger(raw);
        }
        catch
        {
            return null;
        }
    }

    private async Task<int?> TryScoreWithAzure(object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AzureOpenAiEndpoint)
            || string.IsNullOrWhiteSpace(_options.AzureOpenAiApiKey)
            || string.IsNullOrWhiteSpace(_options.AzureOpenAiDeployment))
        {
            return null;
        }

        try
        {
            var baseEndpoint = _options.AzureOpenAiEndpoint.TrimEnd('/');
            var path = $"/openai/deployments/{_options.AzureOpenAiDeployment}/chat/completions?api-version={_options.AzureOpenAiApiVersion}";
            var client = _httpClientFactory.CreateClient(nameof(LeadScoringService));
            using var request = new HttpRequestMessage(HttpMethod.Post, baseEndpoint + path);
            request.Headers.Add("api-key", _options.AzureOpenAiApiKey);
            request.Content = JsonContent.Create(new
            {
                messages = new object[]
                {
                    new { role = "system", content = "Score this lead from 0 to 100. Return only numeric response." },
                    new { role = "user", content = JsonSerializer.Serialize(payload) }
                },
                temperature = 0
            });

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            return ExtractFirstInteger(raw);
        }
        catch
        {
            return null;
        }
    }

    private static object CreatePayload(Lead lead, bool includePii)
    {
        if (includePii)
        {
            return new
            {
                lead.Name,
                lead.Email,
                lead.Phone,
                lead.Company,
                lead.JobTitle,
                lead.Source,
                lead.Region,
                lead.LeadType,
                lead.ProductInterest,
                lead.Cnpj
            };
        }

        return new
        {
            Name = Mask(lead.Name),
            Email = MaskEmail(lead.Email),
            Phone = MaskPhone(lead.Phone),
            Company = Mask(lead.Company),
            JobTitle = lead.JobTitle,
            lead.Source,
            lead.Region,
            lead.LeadType,
            lead.ProductInterest,
            Cnpj = MaskPhone(lead.Cnpj)
        };
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= 2 ? "**" : value[..2] + "***";
    }

    private static string MaskEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
        {
            return "***";
        }

        var at = value.IndexOf('@');
        var prefix = value[..Math.Min(2, at)];
        return $"{prefix}***{value[at..]}";
    }

    private static string MaskPhone(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= 4 ? "****" : new string('*', value.Length - 4) + value[^4..];
    }

    private static int? ExtractFirstInteger(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var match = Regex.Match(raw, @"\b\d{1,3}\b");
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Value, out var score))
        {
            return null;
        }

        return score;
    }

    private static int ClampScore(int score) =>
        Math.Max(0, Math.Min(100, score));
}
