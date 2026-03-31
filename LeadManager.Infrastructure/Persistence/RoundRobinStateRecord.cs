namespace LeadManager.Infrastructure.Persistence;

internal sealed class RoundRobinStateRecord
{
    public string Key { get; set; } = string.Empty;
    public int LastIndex { get; set; }
}
