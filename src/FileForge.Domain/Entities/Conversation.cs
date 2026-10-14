namespace FileForge.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    private readonly List<Guid> _documentIds = new();
    public IReadOnlyCollection<Guid> DocumentIds => _documentIds;

    public void AttachDocument(Guid documentId)
    {
        if (!_documentIds.Contains(documentId))
            _documentIds.Add(documentId);
    }
}
