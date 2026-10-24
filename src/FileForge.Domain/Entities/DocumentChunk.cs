namespace FileForge.Domain.Entities;

public class DocumentChunk
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int Index { get; private set; }            // order within the document
    public string Content { get; private set; } = default!;
    public int TokenCount { get; private set; }
    public ReadOnlyMemory<float> Embedding { get; private set; }

    // Needed by EF.
    public DocumentChunk() { }

    public static DocumentChunk Create(Guid documentId, int index, string content,
                                       int tokenCount, ReadOnlyMemory<float> embedding)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new Exceptions.DomainException("Chunk content is required.");

        return new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Index = index,
            Content = content,
            TokenCount = tokenCount,
            Embedding = embedding
        };
    }
}
