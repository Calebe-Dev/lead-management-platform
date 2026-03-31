namespace LeadManager.Domain.Leads;

public sealed class Lead
{
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
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Lead name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Lead email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Lead phone is required.", nameof(phone));
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Lead source is required.", nameof(source));

        return new Lead(
            Guid.NewGuid(),
            name.Trim(),
            email.Trim(),
            phone.Trim(),
            company.Trim(),
            jobTitle.Trim(),
            source.Trim(),
            0,
            LeadTemperature.Cold,
            LeadStatus.New,
            createdAtUtc ?? DateTime.UtcNow);
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
        Status = status;
    }
}
