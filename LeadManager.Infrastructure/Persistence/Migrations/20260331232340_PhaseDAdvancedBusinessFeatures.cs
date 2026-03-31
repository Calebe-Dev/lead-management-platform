using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PhaseDAdvancedBusinessFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "assigned_to",
                table: "leads",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "cnpj",
                table: "leads",
                type: "character varying(14)",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "lead_type",
                table: "leads",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "product_interest",
                table: "leads",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "region",
                table: "leads",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "leads",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "lead_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    field_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    old_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    new_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lead_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "round_robin_state",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    last_index = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_round_robin_state", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "ix_leads_cnpj",
                table: "leads",
                column: "cnpj",
                unique: true,
                filter: "cnpj <> ''");

            migrationBuilder.CreateIndex(
                name: "ix_leads_phone",
                table: "leads",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lead_history_lead_id",
                table: "lead_history",
                column: "lead_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lead_history");

            migrationBuilder.DropTable(
                name: "round_robin_state");

            migrationBuilder.DropIndex(
                name: "ix_leads_cnpj",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "ix_leads_phone",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "assigned_to",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "cnpj",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "lead_type",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "product_interest",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "region",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "leads");
        }
    }
}
