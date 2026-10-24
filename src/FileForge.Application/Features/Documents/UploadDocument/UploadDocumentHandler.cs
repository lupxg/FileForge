using FileForge.Application.Abstractions;
using FileForge.Application.Interfaces;
using FileForge.Domain.Entities;


namespace FileForge.Application.Features.Documents.UploadDocument;

public sealed class UploadDocumentHandler(
    IDocumentRepository documents,
    IFileStorage fileStorage,
    IUnitOfWork unitOfWork)
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private readonly IDocumentRepository _documents = documents;
    private readonly IFileStorage _fileStorage = fileStorage;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Document> HandleAsync(
        Stream content,
        string fileName,
        string contentType,
        long sizeInBytes,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("A file name is required.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("A content type is required.", nameof(contentType));

        if (sizeInBytes <= 0 || sizeInBytes > MaxFileSizeBytes)
            throw new ArgumentOutOfRangeException(
                nameof(sizeInBytes),
                $"File size must be between 1 byte and {MaxFileSizeBytes} bytes.");

        ct.ThrowIfCancellationRequested();

        var storagePath = await _fileStorage.SaveAsync(
            content,
            fileName,
            ct);

        try
        {
            var document = Document.Create(
                fileName,
                contentType,
                sizeInBytes,
                storagePath);

            await _documents.AddAsync(document, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return document;
        }
        catch
        {
            // Best-effort cleanup if creation or database persistence fails.
            try
            {
                await _fileStorage.DeleteAsync(storagePath, CancellationToken.None);
            }
            catch
            {
                // Do not replace the original exception.
                // Production code should log cleanup failures.
            }

            throw;
        }
    }
}
