using FileForge.Domain.Enums;
using FileForge.Domain.Exceptions;


namespace FileForge.Domain.Entities;

public class Document
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long SizeInBytes { get; private set; }
    public string StoragePath { get; private set; } = default!;
    public int ChunkCount { get; private set; }
    public DocumentStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }

    // Needed by EF.
    public Document() { }


    public void StartProcessing()
    {
        if (Status is not (DocumentStatus.Uploaded or DocumentStatus.Failed))
            throw new DomainException($"Cannot start processing from status {Status}.");

        Status = DocumentStatus.Processing;
        FailureReason = null;
    }


    public void MarkReady(int chunkCount)
    {
        if (Status != DocumentStatus.Processing)
            throw new DomainException("Document is not being processed.");

        ChunkCount = chunkCount;
        Status = DocumentStatus.Ready;
        ProcessedAtUtc = DateTime.UtcNow;
    }


    public void MarkFailed(string reason)
    {
        Status = DocumentStatus.Failed;
        FailureReason = reason;
        ProcessedAtUtc = DateTime.UtcNow;
    }
}
