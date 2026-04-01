using LeadManager.Application.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace LeadManager.Infrastructure.Auditing;

public sealed class MongoAuditTrailRepository : IAuditTrailRepository
{
    private readonly MongoAuditOptions _options;
    private readonly IMongoCollection<InteractionDocument>? _interactionCollection;
    private readonly IMongoCollection<BehaviorDocument>? _behaviorCollection;
    private readonly IMongoCollection<AiDecisionDocument>? _aiDecisionCollection;

    public MongoAuditTrailRepository(IOptions<MongoAuditOptions> options)
    {
        _options = options.Value;
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            return;
        }

        var client = new MongoClient(_options.ConnectionString);
        var database = client.GetDatabase(string.IsNullOrWhiteSpace(_options.Database) ? "lead_manager_audit" : _options.Database);
        _interactionCollection = database.GetCollection<InteractionDocument>("interaction_history");
        _behaviorCollection = database.GetCollection<BehaviorDocument>("behavior_events");
        _aiDecisionCollection = database.GetCollection<AiDecisionDocument>("ai_decision_logs");
    }

    public async Task WriteInteractionAsync(InteractionAuditRecord record, CancellationToken cancellationToken = default)
    {
        if (_interactionCollection is null)
        {
            return;
        }

        try
        {
            await _interactionCollection.InsertOneAsync(
                new InteractionDocument
                {
                    LeadId = record.LeadId,
                    EventType = record.EventType,
                    PayloadJson = record.PayloadJson,
                    OccurredAtUtc = record.OccurredAtUtc
                },
                cancellationToken: cancellationToken);
        }
        catch
        {
            // best-effort audit trail in local/offline environments
        }
    }

    public async Task WriteBehaviorEventAsync(BehaviorEventRecord record, CancellationToken cancellationToken = default)
    {
        if (_behaviorCollection is null)
        {
            return;
        }

        try
        {
            await _behaviorCollection.InsertOneAsync(
                new BehaviorDocument
                {
                    LeadId = record.LeadId,
                    EventName = record.EventName,
                    ScoreImpact = record.ScoreImpact,
                    PayloadJson = record.PayloadJson,
                    OccurredAtUtc = record.OccurredAtUtc
                },
                cancellationToken: cancellationToken);
        }
        catch
        {
            // best-effort audit trail in local/offline environments
        }
    }

    public async Task WriteAiDecisionAsync(AiDecisionRecord record, CancellationToken cancellationToken = default)
    {
        if (_aiDecisionCollection is null)
        {
            return;
        }

        try
        {
            await _aiDecisionCollection.InsertOneAsync(
                new AiDecisionDocument
                {
                    LeadId = record.LeadId,
                    Provider = record.Provider,
                    PromptFingerprint = record.PromptFingerprint,
                    PayloadJson = record.PayloadJson,
                    OccurredAtUtc = record.OccurredAtUtc
                },
                cancellationToken: cancellationToken);
        }
        catch
        {
            // best-effort audit trail in local/offline environments
        }
    }
}

file sealed class InteractionDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("leadId")]
    public Guid LeadId { get; set; }

    [BsonElement("eventType")]
    public string EventType { get; set; } = string.Empty;

    [BsonElement("payloadJson")]
    public string PayloadJson { get; set; } = "{}";

    [BsonElement("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; }
}

file sealed class BehaviorDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("leadId")]
    public Guid LeadId { get; set; }

    [BsonElement("eventName")]
    public string EventName { get; set; } = string.Empty;

    [BsonElement("scoreImpact")]
    public int ScoreImpact { get; set; }

    [BsonElement("payloadJson")]
    public string PayloadJson { get; set; } = "{}";

    [BsonElement("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; }
}

file sealed class AiDecisionDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("leadId")]
    public Guid LeadId { get; set; }

    [BsonElement("provider")]
    public string Provider { get; set; } = string.Empty;

    [BsonElement("promptFingerprint")]
    public string PromptFingerprint { get; set; } = string.Empty;

    [BsonElement("payloadJson")]
    public string PayloadJson { get; set; } = "{}";

    [BsonElement("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; }
}
