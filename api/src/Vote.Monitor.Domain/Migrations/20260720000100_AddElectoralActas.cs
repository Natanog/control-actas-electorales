using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vote.Monitor.Domain.Migrations;

[DbContext(typeof(VoteMonitorContext))]
[Migration("20260720000100_AddElectoralActas")]
public partial class AddElectoralActas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Actas",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ElectionRoundId = table.Column<Guid>(type: "uuid", nullable: false),
                PollingStationId = table.Column<Guid>(type: "uuid", nullable: false),
                MonitoringObserverId = table.Column<Guid>(type: "uuid", nullable: false),
                ContestCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                OriginalFileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                StoredFileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                FilePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                FileSize = table.Column<long>(type: "bigint", nullable: false),
                Sha256Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Latitude = table.Column<double>(type: "double precision", nullable: true),
                Longitude = table.Column<double>(type: "double precision", nullable: true),
                ObserverNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                ReviewerNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                OcrProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                OcrRawText = table.Column<string>(type: "text", nullable: true),
                OcrRawResponse = table.Column<string>(type: "jsonb", nullable: true),
                OcrConfidence = table.Column<decimal>(type: "numeric", nullable: true),
                OcrValidationErrors = table.Column<string[]>(type: "text[]", nullable: false, defaultValue: Array.Empty<string>()),
                ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Actas", x => x.Id);
                table.ForeignKey("FK_Actas_MonitoringObservers_MonitoringObserverId", x => x.MonitoringObserverId,
                    "MonitoringObservers", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ActaResultEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ActaId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateId = table.Column<Guid>(type: "uuid", nullable: true),
                Label = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Value = table.Column<int>(type: "integer", nullable: false),
                Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Confidence = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ActaResultEntries", x => x.Id);
                table.ForeignKey("FK_ActaResultEntries_Actas_ActaId", x => x.ActaId, "Actas", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Actas_MonitoringObserverId", "Actas", "MonitoringObserverId");
        migrationBuilder.CreateIndex("IX_Actas_Sha256Hash", "Actas", "Sha256Hash");
        migrationBuilder.CreateIndex("IX_Actas_ElectionRoundId_Status", "Actas", new[] { "ElectionRoundId", "Status" });
        migrationBuilder.CreateIndex("IX_Actas_Round_Station_Contest", "Actas", new[] { "ElectionRoundId", "PollingStationId", "ContestCode" });
        migrationBuilder.CreateIndex("IX_Actas_Primary", "Actas", new[] { "ElectionRoundId", "PollingStationId", "ContestCode", "IsPrimary" }, unique: true, filter: "\"IsPrimary\" = TRUE");
        migrationBuilder.CreateIndex("IX_ActaResultEntries_ActaId", "ActaResultEntries", "ActaId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ActaResultEntries");
        migrationBuilder.DropTable("Actas");
    }
}
