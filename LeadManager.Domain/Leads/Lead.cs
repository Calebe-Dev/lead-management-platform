namespace LeadManager.Domain.Leads;

public sealed class Lead
{
    private static readonly string[] DecisionMakerKeywords = ["ceo", "cto", "coo", "cfo", "founder", "owner", "director", "head"];
    private static readonly string[] InfluencerKeywords = ["manager", "coordinator", "supervisor", "specialist", "analyst"];

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string Phone { get; private set; }
    public string Company { get; private set; }
    public string JobTitle { get; private set; }
    public string Source { get; private set; }
    public int Score { get; private set; }
    public LeadTemperature Temperature { get; private set; }
    public LeadStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Lead(
        Guid id,
        string name,
        string email,
        string phone,
        string company,
        string jobTitle,
        string source,
        int score,
        LeadTemperature temperature,
        LeadStatus status,
        DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Email = email;
        Phone = phone;
        Company = company;
        JobTitle = jobTitle;
        Source = source;
        Score = score;
        Temperature = temperature;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    public static Lead Create(
        string name,
        string email,
        string phone,
        string company,
        string jobTitle,
        string source,
        DateTime? createdAtUtc = null)
    {
        var normalizedName = ValidateName(name);
        var normalizedEmail = ValidateEmail(email);
        var normalizedPhone = ValidatePhone(phone);
        var normalizedCompany = NormalizeOptional(company);
        var normalizedJobTitle = NormalizeOptional(jobTitle);
        var normalizedSource = ValidateSource(source);

        var lead = new Lead(
            Guid.NewGuid(),
            normalizedName,
            normalizedEmail,
            normalizedPhone,
            normalizedCompany,
            normalizedJobTitle,
            normalizedSource,
            0,
            LeadTemperature.Cold,
            LeadStatus.New,
            createdAtUtc ?? DateTime.UtcNow);

        lead.RecalculateScore();
        return lead;
    }

    public static Lead Rehydrate(
        Guid id,
        string name,
        string email,
        string phone,
        string company,
        string jobTitle,
        string source,
        int score,
        LeadTemperature temperature,
        LeadStatus status,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(id));
        }

        if (createdAtUtc == default)
        {
            throw new ArgumentException("Lead creation date is required.", nameof(createdAtUtc));
        }

        return new Lead(
            id,
            ValidateName(name),
            ValidateEmail(email),
            ValidatePhone(phone),
            NormalizeOptional(company),
            NormalizeOptional(jobTitle),
            ValidateSource(source),
            score < 0 ? throw new ArgumentOutOfRangeException(nameof(score), "Lead score cannot be negative.") : score,
            temperature,
            status,
            DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc));
    }

    public void RecalculateScore()
    {
        var score = GetSourceScore(Source)
                    + GetJobTitleScore(JobTitle)
                    + GetCompanyScore(Company)
                    + GetDataCompletenessScore(Company, JobTitle);

        ApplyScore(Math.Max(0, score));
    }

    public void ApplyScore(int score)
    {
        if (score < 0) throw new ArgumentOutOfRangeException(nameof(score), "Lead score cannot be negative.");

        Score = score;
        Temperature = score switch
        {
            <= 30 => LeadTemperature.Cold,
            <= 60 => LeadTemperature.Warm,
            _ => LeadTemperature.Hot
        };
    }

    public void ChangeStatus(LeadStatus status)
    {
        if (Status == status)
        {
            return;
        }

        if (Status is LeadStatus.Converted or LeadStatus.Lost)
        {
            throw new InvalidOperationException($"Lead in status '{Status}' cannot transition to '{status}'.");
        }

        var allowed = Status switch
        {
            LeadStatus.New => status is LeadStatus.InService or LeadStatus.Qualified or LeadStatus.Lost,
            LeadStatus.InService => status is LeadStatus.Qualified or LeadStatus.Lost,
            LeadStatus.Qualified => status is LeadStatus.Converted or LeadStatus.Lost,
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidOperationException($"Invalid lead status transition: '{Status}' to '{status}'.");
        }

        Status = status;
    }

    private static int GetSourceScore(string source) => source.ToLowerInvariant() switch
    {
        "organic" => 15,
        "referral" => 25,
        "event" => 20,
        "paid" => 10,
        "social" => 10,
        "outbound" => 5,
        _ => 0
    };

    private static int GetJobTitleScore(string jobTitle)
    {
        if (string.IsNullOrWhiteSpace(jobTitle))
        {
            return 0;
        }

        var normalized = jobTitle.ToLowerInvariant();

        if (DecisionMakerKeywords.Any(normalized.Contains))
        {
            return 30;
        }

        if (InfluencerKeywords.Any(normalized.Contains))
        {
            return 20;
        }

        return 10;
    }

    private static int GetCompanyScore(string company) =>
        string.IsNullOrWhiteSpace(company) ? 0 : 20;

    private static int GetDataCompletenessScore(string company, string jobTitle)
    {
        var isComplete = !string.IsNullOrWhiteSpace(company) && !string.IsNullOrWhiteSpace(jobTitle);
        return isComplete ? 10 : -10;
    }

    private static string ValidateName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Lead name is required.", nameof(name));
        }

        return normalized;
    }

    private static string ValidateEmail(string email)
    {
        var normalized = email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Lead email is required.", nameof(email));
        }

        var atIndex = normalized.IndexOf('@');
        if (atIndex <= 0 || atIndex != normalized.LastIndexOf('@') || atIndex == normalized.Length - 1)
        {
            throw new ArgumentException("Lead email is invalid.", nameof(email));
        }

        return normalized;
    }

    private static string ValidatePhone(string phone)
    {
        var normalized = phone?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Lead phone is required.", nameof(phone));
        }

        var digitCount = normalized.Count(char.IsDigit);
        if (digitCount < 10)
        {
            throw new ArgumentException("Lead phone is invalid.", nameof(phone));
        }

        return normalized;
    }

    private static string ValidateSource(string source)
    {
        var normalized = source?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Lead source is required.", nameof(source));
        }

        return normalized;
    }

    private static string NormalizeOptional(string value) => value?.Trim() ?? string.Empty;
}
