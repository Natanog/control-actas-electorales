using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Vote.Monitor.Core.Services.FileStorage.Contracts;

namespace Feature.Attachments.Actas.Ocr;

public sealed class ActaOcrOptions
{
    public const string SectionKey = "ActaOcr";
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "http://acta-ocr:8000";
    public decimal ReviewThreshold { get; set; } = 0.85m;
}

public sealed record OcrEntry(string Label, int Value, string Type, decimal? Confidence);
public sealed record ActaOcrResult(string Provider, string RawText, decimal Confidence,
    IReadOnlyList<OcrEntry> Entries, IReadOnlyList<string> Errors, string RawResponse);

public interface IActaOcrProvider
{
    Task<ActaOcrResult> ProcessAsync(Stream image, string fileName, string contentType, CancellationToken ct);
}

public sealed class HttpActaOcrProvider(HttpClient client) : IActaOcrProvider
{
    public async Task<ActaOcrResult> ProcessAsync(Stream image, string fileName, string contentType, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(image);
        streamContent.Headers.ContentType = new(contentType);
        content.Add(streamContent, "file", fileName);
        using var response = await client.PostAsync("/v1/ocr", content, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        var payload = JsonSerializer.Deserialize<OcrPayload>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("OCR provider returned an empty payload.");
        return new ActaOcrResult(payload.Provider ?? "paddleocr", payload.RawText ?? string.Empty,
            payload.Confidence, payload.Entries ?? [], payload.Errors ?? [], json);
    }

    private sealed class OcrPayload
    {
        public string? Provider { get; set; }
        public string? RawText { get; set; }
        public decimal Confidence { get; set; }
        public List<OcrEntry>? Entries { get; set; }
        public List<string>? Errors { get; set; }
    }
}

public sealed class DisabledActaOcrProvider : IActaOcrProvider
{
    public Task<ActaOcrResult> ProcessAsync(Stream image, string fileName, string contentType, CancellationToken ct) =>
        Task.FromResult(new ActaOcrResult("disabled", string.Empty, 0, [],
            ["OCR is disabled. The act was routed to manual review."], "{}"));
}
