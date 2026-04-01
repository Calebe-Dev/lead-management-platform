namespace LeadManager.Infrastructure.Scoring;

public sealed class LeadScoringOptions
{
    public bool Enabled { get; init; }
    public string OpenAiEndpoint { get; init; } = string.Empty;
    public string OpenAiApiKey { get; init; } = string.Empty;
    public string OpenAiModel { get; init; } = "gpt-4.1-mini";
    public string AzureOpenAiEndpoint { get; init; } = string.Empty;
    public string AzureOpenAiApiKey { get; init; } = string.Empty;
    public string AzureOpenAiDeployment { get; init; } = string.Empty;
    public string AzureOpenAiApiVersion { get; init; } = "2024-10-21";
}
