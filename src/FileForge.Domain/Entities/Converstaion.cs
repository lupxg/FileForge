namespace FileForge.Domain.Entities;

public class Converstaion
{
    private readonly List<Guid> _documentIds = new();
    public IReadOnlyCollection<Guid> DocumentIds => _documentIds;

    public void AttachDocument(Guid documentId)
    {
        if (!_documentIds.Contains(documentId))
            _documentIds.Add(documentId);
    }
}
