namespace Vote.Monitor.Domain.Entities.ActaAggregate;

public enum ActaResultEntryType
{
    Candidate = 0,
    Blank = 1,
    Null = 2,
    Total = 3,
    Other = 4
}

public enum ActaResultSource
{
    Ocr = 0,
    Manual = 1
}

public class ActaResultEntry
{
    private ActaResultEntry() { }

    private ActaResultEntry(Guid id, string label, int value, ActaResultEntryType type,
        ActaResultSource source, decimal? confidence, Guid? candidateId)
    {
        Id = id;
        Label = label;
        Value = value;
        Type = type;
        Source = source;
        Confidence = confidence;
        CandidateId = candidateId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid ActaId { get; private set; }
    public Guid? CandidateId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public int Value { get; private set; }
    public ActaResultEntryType Type { get; private set; }
    public ActaResultSource Source { get; private set; }
    public decimal? Confidence { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static ActaResultEntry Create(string label, int value, ActaResultEntryType type,
        ActaResultSource source, decimal? confidence = null, Guid? candidateId = null)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Label is required.", nameof(label));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Votes cannot be negative.");
        return new ActaResultEntry(Guid.NewGuid(), label.Trim(), value, type, source, confidence, candidateId);
    }

    public void UpdateValue(int value, ActaResultSource source)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Votes cannot be negative.");
        Value = value;
        Source = source;
        UpdatedAt = DateTime.UtcNow;
    }
}
