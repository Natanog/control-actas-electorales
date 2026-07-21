using System.Security.Cryptography;
using System.Text;
using Feature.Attachments.Specifications;
using Microsoft.EntityFrameworkCore;
using Vote.Monitor.Core.Security;
using Vote.Monitor.Core.Services.FileStorage.Contracts;
using Vote.Monitor.Domain;
using Vote.Monitor.Domain.Entities.ActaAggregate;

namespace Feature.Attachments.Actas.Create;

public class Request
{
    public Guid ElectionRoundId { get; set; }
    public Guid PollingStationId { get; set; }
    [FromClaim(ApplicationClaimTypes.UserId)] public Guid ObserverId { get; set; }
    public required string ContestCode { get; set; }
    public required IFormFile File { get; set; }
    public DateTime CapturedAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Notes { get; set; }
}

public class Validator : Validator<Request>
{
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];
    public Validator()
    {
        RuleFor(x => x.ElectionRoundId).NotEmpty();
        RuleFor(x => x.PollingStationId).NotEmpty();
        RuleFor(x => x.ObserverId).NotEmpty();
        RuleFor(x => x.ContestCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CapturedAt).Must(x => x.Kind == DateTimeKind.Utc).WithMessage("CapturedAt must be UTC.");
        RuleFor(x => x.File).NotNull().Must(x => x.Length is > 0 and <= 10 * 1024 * 1024)
            .WithMessage("The image must be between 1 byte and 10 MB.");
        RuleFor(x => x.File.ContentType).Must(x => AllowedTypes.Contains(x)).WithMessage("Only JPG, PNG and WEBP are allowed.");
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed record Response(Guid Id, string Code, string Status, string Hash, bool ExactDuplicate);

public class Endpoint(
    VoteMonitorContext context,
    IFileStorageService fileStorage,
    IReadRepository<Vote.Monitor.Domain.Entities.MonitoringObserverAggregate.MonitoringObserver> observerRepository)
    : Endpoint<Request, Results<Ok<Response>, NotFound, Conflict<string>, ProblemDetails>>
{
    public override void Configure()
    {
        Post("/api/election-rounds/{electionRoundId}/actas");
        AllowFileUploads();
        DontAutoTag();
        Options(x => x.WithTags("actas", "mobile"));
        Summary(x => x.Summary = "Archives an immutable electoral act image and registers its chain of custody.");
    }

    public override async Task<Results<Ok<Response>, NotFound, Conflict<string>, ProblemDetails>> ExecuteAsync(Request req, CancellationToken ct)
    {
        var stationExists = await context.PollingStations.AnyAsync(x => x.Id == req.PollingStationId && x.ElectionRoundId == req.ElectionRoundId, ct);
        if (!stationExists) return TypedResults.NotFound();

        var monitoringObserverId = await observerRepository.FirstOrDefaultAsync(
            new GetMonitoringObserverIdSpecification(req.ElectionRoundId, req.ObserverId), ct);
        if (monitoringObserverId == Guid.Empty) return TypedResults.NotFound();

        await using var source = req.File.OpenReadStream();
        var hashBytes = await SHA256.HashDataAsync(source, ct);
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        source.Position = 0;

        var exactDuplicate = await context.Actas.AnyAsync(x => x.ElectionRoundId == req.ElectionRoundId && x.Sha256Hash == hash, ct);
        if (exactDuplicate) return TypedResults.Conflict("An identical act image is already archived for this election.");

        var safeExtension = req.File.ContentType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
        var storedFileName = $"{Guid.NewGuid():N}{safeExtension}";
        var uploadPath = $"elections/{req.ElectionRoundId}/actas/{req.PollingStationId}";
        var upload = await fileStorage.UploadFileAsync(uploadPath, storedFileName, source, ct);
        if (upload is UploadFileResult.Failed failed)
            return new ProblemDetails { Status = 503, Detail = failed.ErrorMessage };

        var now = DateTime.UtcNow;
        var acta = Acta.Create(req.ElectionRoundId, req.PollingStationId, monitoringObserverId,
            req.ContestCode, Path.GetFileName(req.File.FileName), storedFileName, uploadPath,
            req.File.ContentType, req.File.Length, hash, req.CapturedAt, now,
            req.Latitude, req.Longitude, req.Notes);

        context.Actas.Add(acta);
        await context.SaveChangesAsync(ct);
        var code = $"ACTA-{acta.Id.ToString("N")[..12].ToUpperInvariant()}";
        return TypedResults.Ok(new Response(acta.Id, code, acta.Status.ToString(), hash, false));
    }
}
