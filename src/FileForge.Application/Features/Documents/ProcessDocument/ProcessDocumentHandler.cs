using FileForge.Application.Abstractions;
using FileForge.Application.Exceptions;
using FileForge.Application.Interfaces;
using FileForge.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Data;


namespace FileForge.Application.Features.Documents.ProcessDocument;

public class ProcessDocumentHandler
{
    private readonly IDocumentRepository _documents;
    private readonly IDocumentTextExtractor _extractor;
    private readonly ITextChunker _chunker;
    private readonly IEmbeddingService _embeddings;
    private readonly IChunkRepository _chunks;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProcessDocumentHandler> _logger;

    public ProcessDocumentHandler(
        IDocumentRepository documents,
        IDocumentTextExtractor extractor,
        ITextChunker chunker,
        IEmbeddingService embeddings,
        IChunkRepository chunks,
        IUnitOfWork uow,
        ILogger<ProcessDocumentHandler> logger)
    {
        _documents = documents;
        _extractor = extractor;
        _chunker = chunker;
        _embeddings = embeddings;
        _chunks = chunks;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid documentId,
        CancellationToken ct)
    {
        var doc = await _documents.GetAsync(documentId, ct)
            ?? throw new NotFoundException(documentId);

        doc.StartProcessing();
        await _uow.SaveChangesAsync(ct);

        try
        {
            var text = await _extractor.ExtractAsync(
                doc.StoragePath,
                doc.ContentType,
                ct);

            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException(
                    "No extractable text was found.");
            }

            var pieces = _chunker.Chunk(text).ToList();

            if (pieces.Count == 0)
            {
                throw new InvalidOperationException(
                    "No chunks were produced.");
            }

            var embeddings = await _embeddings.EmbedBatchAsync(
                pieces.Select(p => p.Content).ToList(),
                ct);

            if (embeddings.Count != pieces.Count)
            {
                throw new InvalidOperationException(
                    $"Expected {pieces.Count} embeddings, " +
                    $"received {embeddings.Count}.");
            }

            ct.ThrowIfCancellationRequested();

            var newChunks = pieces.Select((piece, index) =>
                DocumentChunk.Create(
                    doc.Id,
                    index,
                    piece.Content,
                    piece.TokenCount,
                    embeddings[index]))
                .ToList();

            await using var transaction = await _uow.BeginTransactionAsync(ct);

            try
            {
                await _chunks.DeleteByDocumentIdAsync(doc.Id, ct);
                await _chunks.AddRangeAsync(newChunks, ct);

                doc.MarkReady(newChunks.Count);

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Document {DocumentId} processed with {ChunkCount} chunks.",
                    doc.Id,
                    newChunks.Count);

            }
            catch
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception rollbackException)
                {
                    _logger.LogError(
                        rollbackException,
                        "Rollback failed for document {DocumentId}.",
                        doc.Id);
                }

                throw;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("Processing canceled for document {DocumentId}.",
                documentId);

            // Cancellation must not be reported as a processing failure.
            // Use a fresh DbContext/cleanup scope if the current transaction
            // or DbContext may have been left in an uncertain state.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Processing failed for document {DocumentId}.",
                documentId);

            _uow.ClearTracking();

            var failedDocument = await _documents.GetAsync(doc.Id, ct);

            if (failedDocument is not null)
            {
                failedDocument.MarkFailed("Document processing failed.");
                await _uow.SaveChangesAsync(ct);
            }

            throw;
        }
    }
}