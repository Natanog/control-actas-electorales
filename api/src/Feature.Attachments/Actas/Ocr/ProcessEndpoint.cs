using Authorization.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vote.Monitor.Core.Services.FileStorage.Contracts;
using Vote.Monitor.Domain;
using Vote.Monitor.Domain.Entities.ActaAggregate;

namespace Feature.Attachments.Actas.Ocr;

public class ProcessRequest
{
    public Guid ElectionRoundId { get; set; }
    public Guid Id { get; set; }
}

public class ProcessEndpoint(
    VoteMonitorContext context,
    IFileStorageService fileStorage,
    IActaOcrProvider ocrProvider,
    IOptions<ActaOcrOptions> options,
    IHttpClientFactory httpClientFactory)
    : Endpoint<ProcessRequest, Results<NoContent, NotFound, Conflict<string>>>
{
    public override void Configure()
    {
        Post("/api/election-rounds/{electionRoundId}/actas/{id}:ocr");
        Policies(PolicyNames.AdminsOnly);
        DontAutoTag();
        Options(x => x.WithTags("actas", "ocr"));
    }

    public override async Task<Results<NoContent, NotFound, Conflict<string>>> ExecuteAsync(ProcessRequest req, CancellationToken ct)
    {
        var acta = await context.Actas.Include(x => x.Results)
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.ElectionRoundId == req.ElectionRoundId, ct);
        if (acta is null) return TypedResults.NotFound();

        try
        {
            acta.StartOcr(DateTime.UtcNow);
            await context.SaveChangesAsync(ct);

            var fileResult = await fileStorage.GetPresignedUrlAsync(acta.FilePath, acta.StoredFileName);
            if (fileResult is not GetPresignedUrlResult.Ok file)
            {
                acta.MarkOcrFailed("The archived image could not be read.", DateTime.UtcNow);
                await context.SaveChangesAsync(ct);
                return TypedResults.Conflict("The archived image could not be read.");
            }

            using var client = httpClientFactory.CreateClient();
            await using var image = await client.GetStreamAsync(file.Url, ct);
            var result = await ocrProvider.ProcessAsync(image, acta.StoredFileName, acta.MimeType, ct);
            var entries = result.Entries.Select(x => ActaResultEntry.Create(
                x.Label,
                x.Value,
                Enum.TryParse<ActaResultEntryType>(x.Type, true, out var type) ? type : ActaResultEntryType.Other,
                ActaResultSource.Ocr,
                x.Confidence)).ToList();

            acta.ApplyOcr(result.Provider, result.RawText, result.RawResponse, result.Confidence,
                result.Errors, entries, options.Value.ReviewThreshold, DateTime.UtcNow);
            await context.SaveChangesAsync(ct);
            return TypedResults.NoContent();
        }
        catch (Exception ex)
        {
            acta.MarkOcrFailed(ex.Message, DateTime.UtcNow);
            await context.SaveChangesAsync(ct);
            return TypedResults.Conflict("OCR processing failed and the act was routed to manual review.");
        }
    }
}
