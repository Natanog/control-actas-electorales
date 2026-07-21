using Authorization.Policies;
using Microsoft.EntityFrameworkCore;
using Vote.Monitor.Core.Security;
using Vote.Monitor.Domain;
using Vote.Monitor.Domain.Entities.ActaAggregate;

namespace Feature.Attachments.Actas.Review;

public sealed record ResultInput(string Label, int Value, ActaResultEntryType Type, Guid? CandidateId);
public class Request
{
    public Guid ElectionRoundId { get; set; }
    public Guid Id { get; set; }
    [FromClaim(ApplicationClaimTypes.UserId)] public Guid ReviewerId { get; set; }
    public required string Action { get; set; }
    public string? Notes { get; set; }
    public List<ResultInput> Results { get; set; } = [];
}

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.ElectionRoundId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ReviewerId).NotEmpty();
        RuleFor(x => x.Action).Must(x => new[] { "save", "validate", "observe", "duplicate", "conflict" }.Contains(x));
        RuleForEach(x => x.Results).ChildRules(r =>
        {
            r.RuleFor(x => x.Label).NotEmpty().MaximumLength(250);
            r.RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
        });
    }
}

public class Endpoint(VoteMonitorContext context) : Endpoint<Request, Results<NoContent, NotFound, Conflict<string>>>
{
    public override void Configure()
    {
        Put("/api/election-rounds/{electionRoundId}/actas/{id}:review");
        Policies(PolicyNames.AdminsOnly);
        DontAutoTag();
        Options(x => x.WithTags("actas", "review"));
    }

    public override async Task<Results<NoContent, NotFound, Conflict<string>>> ExecuteAsync(Request req, CancellationToken ct)
    {
        var acta = await context.Actas.Include(x => x.Results)
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.ElectionRoundId == req.ElectionRoundId, ct);
        if (acta is null) return TypedResults.NotFound();

        var entries = req.Results.Select(x => ActaResultEntry.Create(x.Label, x.Value, x.Type,
            ActaResultSource.Manual, candidateId: x.CandidateId)).ToList();
        var validationErrors = ValidateTotals(entries);
        var now = DateTime.UtcNow;

        try
        {
            switch (req.Action)
            {
                case "save":
                    acta.SaveManualReview(entries, req.Notes, req.ReviewerId, now);
                    break;
                case "validate":
                    if (validationErrors.Count > 0) return TypedResults.Conflict(string.Join(" ", validationErrors));
                    var otherPrimary = await context.Actas.FirstOrDefaultAsync(x => x.Id != acta.Id
                        && x.ElectionRoundId == acta.ElectionRoundId && x.PollingStationId == acta.PollingStationId
                        && x.ContestCode == acta.ContestCode && x.IsPrimary, ct);
                    if (otherPrimary is not null) return TypedResults.Conflict("Another primary validated act already exists for this polling station and contest.");
                    acta.SaveManualReview(entries, req.Notes, req.ReviewerId, now);
                    acta.Validate(req.ReviewerId, req.Notes, now);
                    break;
                case "observe": acta.MarkObserved(req.ReviewerId, req.Notes ?? "Observed by reviewer.", now); break;
                case "duplicate": acta.MarkDuplicate(req.ReviewerId, req.Notes ?? "Marked as duplicate.", now); break;
                case "conflict": acta.MarkConflict(req.ReviewerId, req.Notes ?? "Conflicting values detected.", now); break;
            }
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(ex.Message);
        }

        await context.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static List<string> ValidateTotals(IReadOnlyCollection<ActaResultEntry> entries)
    {
        var errors = new List<string>();
        var totalEntries = entries.Where(x => x.Type == ActaResultEntryType.Total).ToList();
        if (totalEntries.Count != 1) errors.Add("Exactly one total entry is required.");
        if (totalEntries.Count == 1)
        {
            var calculated = entries.Where(x => x.Type is ActaResultEntryType.Candidate or ActaResultEntryType.Blank or ActaResultEntryType.Null).Sum(x => x.Value);
            if (calculated != totalEntries[0].Value) errors.Add($"Vote sum ({calculated}) does not match total ({totalEntries[0].Value}).");
        }
        return errors;
    }
}
