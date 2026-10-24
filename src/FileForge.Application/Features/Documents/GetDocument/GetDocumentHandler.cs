using FileForge.Application.Interfaces;

namespace FileForge.Application.Features.Documents.GetDocument;

public sealed class GetDocumentHandler(IDocumentRepository documents)
{
    private readonly IDocumentRepository _documents = documents;

    public async Task<DocumentDetailsResponse?> HandleAsync(Guid documentId, CancellationToken ct)
    {
        var document = await _documents.GetAsync(documentId, ct);

        if (document is null)
        {
            return null;
        }

        return new DocumentDetailsResponse(
            document.Id,
            document.FileName,
            document.ContentType,
            document.SizeInBytes,
            document.Status.ToString(),
            document.ChunkCount,
            document.FailureReason,
            document.CreatedAtUtc,
            document.ProcessedAtUtc
        );
    }
}
