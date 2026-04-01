using Microsoft.EntityFrameworkCore;

namespace LeadManager.Infrastructure.Persistence;

public static class DatabaseSchemaBootstrapper
{
    public static async Task EnsureAdvancedSchemaAsync(LeadManagerDbContext dbContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        // Keeps compatibility with existing migrations while enabling advanced features without a new migration generation step.
        var commands = new[]
        {
            """
            ALTER TABLE leads ADD COLUMN IF NOT EXISTS campaign_id uuid NULL;
            CREATE INDEX IF NOT EXISTS ix_leads_campaign_id ON leads(campaign_id);
            """,
            """
            CREATE TABLE IF NOT EXISTS campaigns (
              id uuid PRIMARY KEY,
              name varchar(200) NOT NULL,
              channel varchar(100) NOT NULL,
              utm varchar(250) NOT NULL,
              is_active boolean NOT NULL,
              created_at_utc timestamp with time zone NOT NULL,
              updated_at_utc timestamp with time zone NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS users (
              id uuid PRIMARY KEY,
              username varchar(200) NOT NULL UNIQUE,
              password_hash varchar(512) NOT NULL,
              role varchar(32) NOT NULL,
              created_at_utc timestamp with time zone NOT NULL,
              updated_at_utc timestamp with time zone NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS assignments (
              id uuid PRIMARY KEY,
              lead_id uuid NOT NULL,
              assignee varchar(200) NOT NULL,
              reason varchar(100) NOT NULL,
              assigned_at_utc timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_assignments_lead_id ON assignments(lead_id);
            """,
            """
            CREATE TABLE IF NOT EXISTS refresh_tokens (
              id uuid PRIMARY KEY,
              token varchar(200) NOT NULL UNIQUE,
              user_id uuid NOT NULL,
              username varchar(200) NOT NULL,
              role varchar(32) NOT NULL,
              expires_at_utc timestamp with time zone NOT NULL,
              created_at_utc timestamp with time zone NOT NULL,
              revoked_at_utc timestamp with time zone NULL
            );
            CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_id ON refresh_tokens(user_id);
            """,
            """
            CREATE TABLE IF NOT EXISTS outbox_messages (
              id uuid PRIMARY KEY,
              event_type varchar(120) NOT NULL,
              payload_json text NOT NULL,
              idempotency_key varchar(200) NOT NULL UNIQUE,
              retry_count integer NOT NULL,
              created_at_utc timestamp with time zone NOT NULL,
              processed_at_utc timestamp with time zone NULL,
              last_attempt_at_utc timestamp with time zone NULL,
              next_attempt_at_utc timestamp with time zone NULL,
              last_error text NOT NULL
            );
            """
        };

        foreach (var command in commands)
        {
            await dbContext.Database.ExecuteSqlRawAsync(command, cancellationToken);
        }
    }
}
