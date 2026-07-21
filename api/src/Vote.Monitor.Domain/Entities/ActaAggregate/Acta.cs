using Vote.Monitor.Domain.Entities.MonitoringObserverAggregate;

namespace Vote.Monitor.Domain.Entities.ActaAggregate;

public class Acta : IAggregateRoot
{
    private readonly List<ActaResultEntry> _results = [];

#pragma warning disable CS8618
    private Acta() { }
#pragma warning restore CS8618

    private Acta(
        Guid electionRoundId,
        Guid pollingStationId,
        Guid monitoringObserverId,
        string contestCode,
        string originalFileName,
        string storedFileName,
        string filePath,
        string mimeType,
        long fileSize,
        string sha256Hash,
        DateTime capturedAt,
        DateTime uploadedAt,
        double? latitude,
        double? longitude,
        string? observerNotes)
    {
        Id = Guid.NewGuid();
        ElectionRoundId = electionRoundId;
        PollingStationId = pollingStationId;
        MonitoringObserverId = monitoringObserverId;
        ContestCode = contestCode.Trim();
        OriginalFileName = originalFileName;
        StoredFileName = storedFileName;
        FilePath = filePath;
        MimeType = mimeType;
        FileSize = fileSize;
        Sha256Hash = sha256Hash.ToLowerInvariant();
        CapturedAt = capturedAt;
        UploadedAt = uploadedAt;
        Latitude = latitude;
        Longitude = longitude;
        ObserverNotes = observerNotes?.Trim();
        Status = ActaStatus.Archived;
        Version = 1;
        CreatedAt = uploadedAt;
        UpdatedAt = uploadedAt;
    }

    public Guid Id { get; private set; }
    public Guid ElectionRoundId { get; private set; }
    public Guid PollingStationId { get; private set; }
    public Guid MonitoringObserverId { get; private set; }
    public MonitoringObserver MonitoringObserver { get; private set; }
    public string ContestCode { get; private set; }
    public string OriginalFileName { get; private set; }
    public string StoredFileName { get; private set; }
    public string FilePath { get; private set; }
    public string MimeType { get; private set; }
    public long FileSize { get; private set; }
    public string Sha256Hash { get; private set; }
    public DateTime CapturedAt { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public string? ObserverNotes { get; private set; }
    public string? ReviewerNotes { get; private set; }
    public ActaStatus Status { get; private set; }
    public bool IsPrimary { get; private set; }
    public int Version { get; private set; }
    public string? OcrProvider { get; private set; }
    public string? OcrRawText { get; private set; }
    public string? OcrRawResponse { get; private set; }
    public decimal? OcrConfidence { get; private set; }
    public string[] OcrValidationErrors { get; private set; } = [];
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyCollection<ActaResultEntry> Results => _results.AsReadOnly();

    public static Acta Create(
        Guid electionRoundId,
        Guid pollingStationId,
        Guid monitoringObserverId,
        string contestCode,
        string originalFileName,
        string storedFileName,
        string filePath,
        string mimeType,
        long fileSize,
        string sha256Hash,
        DateTime capturedAt,
        DateTime uploadedAt,
        double? latitude = null,
        double? longitude = null,
        string? observerNotes = null)
    {
        if (electionRoundId == Guid.Empty) throw new ArgumentException("Election round is required.");
        if (pollingStationId == Guid.Empty) throw new ArgumentException("Polling station is required.");
        if (monitoringObserverId == Guid.Empty) throw new ArgumentException("Monitoring observer is required.");
        if (string.IsNullOrWhiteSpace(contestCode)) throw new ArgumentException("Contest code is required.");
        if (string.IsNullOrWhiteSpace(originalFileName)) throw new ArgumentException("Original filename is required.");
        if (string.IsNullOrWhiteSpace(storedFileName)) throw new ArgumentException("Stored filename is required.");
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.");
        if (string.IsNullOrWhiteSpace(mimeType)) throw new ArgumentException("MIME type is required.");
        if (fileSize <= 0) throw new ArgumentOutOfRangeException(nameof(fileSize));
        if (sha256Hash.Length != 64) throw new ArgumentException("SHA-256 hash must contain 64 hexadecimal characters.");
        if (capturedAt.Kind != DateTimeKind.Utc || uploadedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Capture and upload timestamps must be UTC.");

        return new Acta(electionRoundId, pollingStationId, monitoringObserverId, contestCode,
            originalFileName, storedFileName, filePath, mimeType, fileSize, sha256Hash,
            capturedAt, uploadedAt, latitude, longitude, observerNotes);
    }

    public void StartOcr(DateTime now)
    {
        EnsureStatus(ActaStatus.Archived, ActaStatus.ManualReview);
        Status = ActaStatus.OcrProcessing;
        Touch(now);
    }

    public void ApplyOcr(string provider, string rawText, string rawResponse, decimal confidence,
        IEnumerable<string> validationErrors, IEnumerable<ActaResultEntry> entries, decimal reviewThreshold,
        DateTime now)
    {
        EnsureStatus(ActaStatus.OcrProcessing);
        OcrProvider = provider;
        OcrRawText = rawText;
        OcrRawResponse = rawResponse;
        OcrConfidence = confidence;
        OcrValidationErrors = validationErrors.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();
        ReplaceResults(entries);
        Status = confidence >= reviewThreshold && OcrValidationErrors.Length == 0
            ? ActaStatus.OcrSuccess
            : ActaStatus.ManualReview;
        Touch(now);
    }

    public void MarkOcrFailed(string error, DateTime now)
    {
        OcrValidationErrors = [error];
        Status = ActaStatus.ManualReview;
        Touch(now);
    }

    public void SaveManualReview(IEnumerable<ActaResultEntry> entries, string? reviewerNotes,
        Guid reviewerId, DateTime now)
    {
        EnsureStatus(ActaStatus.ManualReview, ActaStatus.OcrSuccess, ActaStatus.Observed,
            ActaStatus.Duplicate, ActaStatus.Conflict);
        ReplaceResults(entries);
        ReviewerNotes = reviewerNotes?.Trim();
        ReviewedBy = reviewerId;
        ReviewedAt = now;
        Status = ActaStatus.ManualReview;
        Touch(now);
    }

    public void Validate(Guid reviewerId, string? reviewerNotes, DateTime now)
    {
        EnsureStatus(ActaStatus.ManualReview, ActaStatus.OcrSuccess);
        ReviewedBy = reviewerId;
        ReviewedAt = now;
        ReviewerNotes = reviewerNotes?.Trim();
        Status = ActaStatus.Validated;
        IsPrimary = true;
        Touch(now);
    }

    public void MarkObserved(Guid reviewerId, string reason, DateTime now) => ReviewTransition(ActaStatus.Observed, reviewerId, reason, now);
    public void MarkDuplicate(Guid reviewerId, string reason, DateTime now) => ReviewTransition(ActaStatus.Duplicate, reviewerId, reason, now);
    public void MarkConflict(Guid reviewerId, string reason, DateTime now) => ReviewTransition(ActaStatus.Conflict, reviewerId, reason, now);

    public void Close(DateTime now)
    {
        EnsureStatus(ActaStatus.Validated, ActaStatus.Observed, ActaStatus.Duplicate, ActaStatus.Conflict);
        Status = ActaStatus.Closed;
        Touch(now);
    }

    public void DemotePrimary(DateTime now)
    {
        IsPrimary = false;
        Touch(now);
    }

    private void ReviewTransition(ActaStatus target, Guid reviewerId, string reason, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A reason is required.");
        ReviewedBy = reviewerId;
        ReviewedAt = now;
        ReviewerNotes = reason.Trim();
        Status = target;
        IsPrimary = false;
        Touch(now);
    }

    private void ReplaceResults(IEnumerable<ActaResultEntry> entries)
    {
        _results.Clear();
        _results.AddRange(entries);
    }

    private void EnsureStatus(params ActaStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new InvalidOperationException($"Transition from {Status} is not allowed.");
    }

    private void Touch(DateTime now)
    {
        if (now.Kind != DateTimeKind.Utc) throw new ArgumentException("Timestamp must be UTC.");
        UpdatedAt = now;
    }
}
