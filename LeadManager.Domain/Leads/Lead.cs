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
    public string Region { get; private set; }
    public string LeadType { get; private set; }
    public string ProductInterest { get; private set; }
    public string Cnpj { get; private set; }
    public string AssignedTo { get; private set; }
    public Guid? CampaignId { get; private set; }
    public int Score { get; private set; }
    public LeadTemperature Temperature { get; private set; }
    public LeadStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private Lead(
        Guid id,
        string name,
        string email,
        string phone,
        string company,
        string jobTitle,
        string source,
        string region,
        string leadType,
        string productInterest,
        string cnpj,
        string assignedTo,
        Guid? campaignId,
        int score,
        LeadTemperature temperature,
        LeadStatus status,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        Name = name;
        Email = email;
        Phone = phone;
        Company = company;
        JobTitle = jobTitle;
        Source = source;
        Region = region;
        LeadType = leadType;
        ProductInterest = productInterest;
        Cnpj = cnpj;
        AssignedTo = assignedTo;
        CampaignId = campaignId;
        Score = score;
        Temperature = temperature;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static Lead Create(
        string name,
        string email,
        string phone,
        string company,
        string jobTitle,
        string source,
        string region,
        string leadType,
        string productInterest,
        string cnpj,
        Guid? campaignId = null,
        DateTime? createdAtUtc = null)
    {
        var normalizedName = ValidateName(name);
        var normalizedEmail = ValidateEmail(email);
        var normalizedPhone = ValidatePhone(phone);
        var normalizedCompany = NormalizeOptional(company);
        var normalizedJobTitle = NormalizeOptional(jobTitle);
        var normalizedSource = ValidateSource(source);
        var normalizedRegion = ValidateRegion(region);
        var normalizedLeadType = NormalizeOptional(leadType);
        var normalizedProductInterest = NormalizeOptional(productInterest);
        var normalizedCnpj = ValidateCnpj(cnpj);
        var now = createdAtUtc ?? DateTime.UtcNow;

        var lead = new Lead(
            Guid.NewGuid(),
            normalizedName,
            normalizedEmail,
            normalizedPhone,
            normalizedCompany,
            normalizedJobTitle,
            normalizedSource,
            normalizedRegion,
            normalizedLeadType,
            normalizedProductInterest,
            normalizedCnpj,
            string.Empty,
            campaignId,
            0,
            LeadTemperature.Cold,
            LeadStatus.New,
            now,
            now);

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
        string region,
        string leadType,
        string productInterest,
        string cnpj,
        string assignedTo,
        Guid? campaignId,
        int score,
        LeadTemperature temperature,
        LeadStatus status,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Lead id is required.", nameof(id));
        }

        if (createdAtUtc == default)
        {
            throw new ArgumentException("Lead creation date is required.", nameof(createdAtUtc));
        }

        if (updatedAtUtc == default)
        {
            throw new ArgumentException("Lead update date is required.", nameof(updatedAtUtc));
        }

        return new Lead(
            id,
            ValidateName(name),
            ValidateEmail(email),
            ValidatePhone(phone),
            NormalizeOptional(company),
            NormalizeOptional(jobTitle),
            ValidateSource(source),
            ValidateRegion(region),
            NormalizeOptional(leadType),
            NormalizeOptional(productInterest),
            ValidateCnpj(cnpj),
            NormalizeOptional(assignedTo),
            campaignId,
            score < 0 ? throw new ArgumentOutOfRangeException(nameof(score), "Lead score cannot be negative.") : score,
            temperature,
            status,
            DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(updatedAtUtc, DateTimeKind.Utc));
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

        Touch();
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
        Touch();
    }

    public void AssignTo(string assignedTo)
    {
        var normalizedAssignee = NormalizeOptional(assignedTo);
        if (string.Equals(AssignedTo, normalizedAssignee, StringComparison.Ordinal))
        {
            return;
        }

        AssignedTo = normalizedAssignee;
        Touch();
    }

    public void AssignCampaign(Guid? campaignId)
    {
        if (CampaignId == campaignId)
        {
            return;
        }

        CampaignId = campaignId;
        Touch();
    }

    public void MergeFrom(Lead source, LeadMergePrecedence precedence)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Id == Id)
        {
            throw new InvalidOperationException("A lead cannot be merged with itself.");
        }

        if (PrecedenceIsSource(precedence))
        {
            Name = source.Name;
            Email = source.Email;
            Phone = source.Phone;
            Company = source.Company;
            JobTitle = source.JobTitle;
            Source = source.Source;
            Region = source.Region;
            LeadType = source.LeadType;
            ProductInterest = source.ProductInterest;
            Cnpj = source.Cnpj;
            if (!string.IsNullOrWhiteSpace(source.AssignedTo))
            {
                AssignedTo = source.AssignedTo;
            }

            if (source.CampaignId.HasValue)
            {
                CampaignId = source.CampaignId;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Company) && !string.IsNullOrWhiteSpace(source.Company))
            {
                Company = source.Company;
            }

            if (string.IsNullOrWhiteSpace(JobTitle) && !string.IsNullOrWhiteSpace(source.JobTitle))
            {
                JobTitle = source.JobTitle;
            }

            if (string.IsNullOrWhiteSpace(LeadType) && !string.IsNullOrWhiteSpace(source.LeadType))
            {
                LeadType = source.LeadType;
            }

            if (string.IsNullOrWhiteSpace(ProductInterest) && !string.IsNullOrWhiteSpace(source.ProductInterest))
            {
                ProductInterest = source.ProductInterest;
            }

            if (string.IsNullOrWhiteSpace(Cnpj) && !string.IsNullOrWhiteSpace(source.Cnpj))
            {
                Cnpj = source.Cnpj;
            }

            if (string.IsNullOrWhiteSpace(AssignedTo) && !string.IsNullOrWhiteSpace(source.AssignedTo))
            {
                AssignedTo = source.AssignedTo;
            }

            if (!CampaignId.HasValue && source.CampaignId.HasValue)
            {
                CampaignId = source.CampaignId;
            }
        }

        ApplyScore(Math.Max(Score, source.Score));
    }

    private static bool PrecedenceIsSource(LeadMergePrecedence precedence) =>
        precedence == LeadMergePrecedence.Source;

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

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
        var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
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
        var normalized = NormalizeDigits(phone);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Lead phone is required.", nameof(phone));
        }

        if (normalized.Length < 10)
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

    private static string ValidateRegion(string region)
    {
        var normalized = region?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Lead region is required.", nameof(region));
        }

        return normalized;
    }

    private static string ValidateCnpj(string cnpj)
    {
        var normalized = NormalizeDigits(cnpj);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        if (normalized.Length != 14)
        {
            throw new ArgumentException("Lead CNPJ is invalid.", nameof(cnpj));
        }

        return normalized;
    }

    private static string NormalizeOptional(string value) => value?.Trim() ?? string.Empty;

    private static string NormalizeDigits(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsDigit).ToArray());
    }
}
