namespace LeadManager.Application.Leads;

public sealed class DuplicateLeadException : InvalidOperationException
{
    public DuplicateLeadException(Guid existingLeadId, IReadOnlyCollection<string> matchedFields)
        : base($"Lead duplicate detected for fields: {string.Join(", ", matchedFields)}.")
    {
        ExistingLeadId = existingLeadId;
        MatchedFields = matchedFields;
    }

    public Guid ExistingLeadId { get; }
    public IReadOnlyCollection<string> MatchedFields { get; }
}
