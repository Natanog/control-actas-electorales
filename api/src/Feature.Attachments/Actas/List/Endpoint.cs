using Authorization.Policies;
using Microsoft.EntityFrameworkCore;
using Vote.Monitor.Domain;
using Vote.Monitor.Domain.Entities.ActaAggregate;

namespace Feature.Attachments.Actas.List;

public class Request
{
    public Guid ElectionRoundId { get; set; }
    public ActaStatus? Status { get; set; }
    public Guid? PollingStationId { get; set; }
    public string? ContestCode { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed record Item(Guid Id, string Code, Guid PollingStationId, string PollingStationNumber,
    string ContestCode, string Status, string Hash, decimal? OcrConfidence, DateTime CapturedAt,
    DateTime UploadedAt, string? ObserverNotes, string? ReviewerNotes);
public sealed record Response(int Total, IReadOnlyList<Item> Items);

public class Endpoint(VoteMonitorContext context) : Endpoint<Request, Ok<Response>>
{
    public override void Configure()
    {
        Get("/api/election-rounds/{electionRoundId}/actas");
        Policies(PolicyNames.AdminsOnly);
        DontAutoTag();
        Options(x => x.WithTags("actas", "review"));
    }

    public override async Task<Ok<Response>> ExecuteAsync(Request req, CancellationToken ct)
    {
        var page = Math.Max(req.Page, 1);
        var size = Math.Clamp(req.PageSize, 1, 200);
        var query = context.Actas.AsNoTracking()
            .Where(x => x.ElectionRoundId == req.ElectionRoundId);
        if (req.Status.HasValue) query = query.Where(x => x.Status == req.Status.Value);
        if (req.PollingStationId.HasValue) query = query.Where(x => x.PollingStationId == req.PollingStationId.Value);
        if (!string.IsNullOrWhiteSpace(req.ContestCode)) query = query.Where(x => x.ContestCode == req.ContestCode);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.UploadedAt).Skip((page - 1) * size).Take(size)
            .Join(context.PollingStations, a => a.PollingStationId, p => p.Id,
                (a, p) => new Item(a.Id, "ACTA-" + a.Id.ToString().Replace("-", "").Substring(0, 12).ToUpper(),
                    a.PollingStationId, p.Number, a.ContestCode, a.Status.ToString(), a.Sha256Hash,
                    a.OcrConfidence, a.CapturedAt, a.UploadedAt, a.ObserverNotes, a.ReviewerNotes))
            .ToListAsync(ct);
        return TypedResults.Ok(new Response(total, items));
    }
}

public sealed record DashboardResponse(int Total, int Archived, int OcrProcessing, int OcrSuccess,
    int ManualReview, int Validated, int Observed, int Duplicates, int Conflicts, decimal ValidationRate);

public class DashboardEndpoint(VoteMonitorContext context) : Endpoint<Request, Ok<DashboardResponse>>
{
    public override void Configure()
    {
        Get("/api/election-rounds/{electionRoundId}/actas:dashboard");
        Policies(PolicyNames.AdminsOnly);
        DontAutoTag();
        Options(x => x.WithTags("actas", "dashboard"));
    }

    public override async Task<Ok<DashboardResponse>> ExecuteAsync(Request req, CancellationToken ct)
    {
        var statuses = await context.Actas.AsNoTracking().Where(x => x.ElectionRoundId == req.ElectionRoundId)
            .GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.Count() }).ToListAsync(ct);
        int Count(ActaStatus status) => statuses.FirstOrDefault(x => x.Status == status)?.Count ?? 0;
        var total = statuses.Sum(x => x.Count);
        var validated = Count(ActaStatus.Validated) + Count(ActaStatus.Closed);
        var rate = total == 0 ? 0 : Math.Round((decimal)validated / total * 100, 2);
        return TypedResults.Ok(new DashboardResponse(total, Count(ActaStatus.Archived),
            Count(ActaStatus.OcrProcessing), Count(ActaStatus.OcrSuccess), Count(ActaStatus.ManualReview),
            validated, Count(ActaStatus.Observed), Count(ActaStatus.Duplicate), Count(ActaStatus.Conflict), rate));
    }
}
