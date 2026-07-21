namespace Vote.Monitor.Domain.Entities.ActaAggregate;

public enum ActaStatus
{
    Uploaded = 0,
    Archived = 1,
    OcrProcessing = 2,
    OcrSuccess = 3,
    ManualReview = 4,
    Validated = 5,
    Observed = 6,
    Duplicate = 7,
    Conflict = 8,
    Closed = 9
}
