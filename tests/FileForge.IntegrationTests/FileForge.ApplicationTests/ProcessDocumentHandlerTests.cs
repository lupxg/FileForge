using FileForge.Application.Abstractions;
using FileForge.Application.Exceptions;
using FileForge.Application.Features.Documents.ProcessDocument;
using FileForge.Application.Features.Documents.Processing;
using FileForge.Application.Interfaces;
using FileForge.Domain.Entities;
using FileForge.Domain.Enums;
using FileForge.Infrastructure.FileProcessing;
using FileForge.Infrastructure.Persistence;
using FileForge.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FileForge.IntegrationTests.FileForge.ApplicationTests;

internal sealed class FakeTextExtractor(
    string? result = null,
    Exception? exception = null) : IDocumentTextExtractor
{
    public bool WasCalled { get; private set; }

    public Task<string> ExtractAsync(
        string storagePath,
        string contentType,
        CancellationToken ct)
    {
        WasCalled = true;
        ct.ThrowIfCancellationRequested();

        if (exception is not null)
            return Task.FromException<string>(exception);

        return Task.FromResult(result ?? "Test document content.");
    }
}

internal sealed class FakeTextChunker(
    IReadOnlyList<TextChunk> chunks) : ITextChunker
{
    public IReadOnlyList<TextChunk> Chunk(string text) => chunks;
}

internal sealed class FakeEmbeddingService(
    IReadOnlyList<ReadOnlyMemory<float>> embeddings)
    : IEmbeddingService
{
    private const int Dimensions = 1536;

    public Task<ReadOnlyMemory<float>> EmbedAsync(
        string text,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(CreateTestVector());
    }

    public Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedBatchAsync(
        IEnumerable<string> texts,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var inputs = texts.ToList();

        if (inputs.Count != embeddings.Count)
        {
            throw new InvalidOperationException(
                $"Expected {inputs.Count} embeddings, " +
                $"but received {embeddings.Count}.");
        }

        foreach (var embedding in embeddings)
        {
            if (embedding.Length != Dimensions)
            {
                throw new InvalidOperationException(
                    $"Expected {Dimensions} dimensions, " +
                    $"but received {embedding.Length}.");
            }
        }

        return Task.FromResult(embeddings);
    }

    private static ReadOnlyMemory<float> CreateTestVector()
    {
        var vector = new float[Dimensions];
        vector[0] = 1f;

        return new ReadOnlyMemory<float>(vector);
    }
}


internal sealed class TestEmbeddingService : IEmbeddingService
{
    private const int Dimensions = 1536;

    public Task<ReadOnlyMemory<float>> EmbedAsync(
        string text,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(CreateTestVector());
    }

    public Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedBatchAsync(
        IEnumerable<string> texts,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<ReadOnlyMemory<float>> embeddings = texts
            .Select(_ => CreateTestVector())
            .ToList();

        return Task.FromResult(embeddings);
    }

    private static ReadOnlyMemory<float> CreateTestVector()
    {
        var vector = new float[Dimensions];

        // Deterministic test data, not a meaningful semantic embedding.
        vector[0] = 1f;

        return vector;
    }
}




public sealed class ProcessDocumentHandlerTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static ReadOnlyMemory<float> CreateTestEmbedding(int activeIndex)
    {
        const int dimensions = 1536;

        var vector = new float[dimensions];
        vector[activeIndex] = 1f;

        return new ReadOnlyMemory<float>(vector);
    }

    [Fact]
    public async Task HandleAsync_ShouldMarkDocumentReady_WhenProcessingSucceeds()
    {
        var ct = TestContext.Current.CancellationToken;

        var document = Document.Create(
            $"success-{Guid.NewGuid():N}.txt",
            "text/plain",
            100,
            $"test/{Guid.NewGuid():N}.txt");

        // Arrange: persist the document.
        using (var setupScope = fixture.CreateScope())
        {
            var db = setupScope.ServiceProvider
                .GetRequiredService<FileForgeDbContext>();

            db.Documents.Add(document);

            await db.SaveChangesAsync(ct);
        }

        // Act: execute the handler with controlled processing services.
        using (var processingScope = fixture.CreateScope())
        {
            var services = processingScope.ServiceProvider;

            var handler = new ProcessDocumentHandler(
                services.GetRequiredService<IDocumentRepository>(),
                new FakeTextExtractor("This is test document content."),
                new FakeTextChunker(
                [
                    new TextChunk("This is test", 3),
                    new TextChunk("document content.", 2)
                ]),
                new FakeEmbeddingService(
                [
                    CreateTestEmbedding(0),
                    CreateTestEmbedding(1)
                ]),
                services.GetRequiredService<IChunkRepository>(),
                services.GetRequiredService<IUnitOfWork>(),
                NullLogger<ProcessDocumentHandler>.Instance);

            await handler.HandleAsync(document.Id, ct);
        }

        // Assert: verify persisted state using a fresh scope.
        using var verificationScope = fixture.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<FileForgeDbContext>();

        var savedDocument = await verificationDb.Documents
            .AsNoTracking()
            .SingleAsync(d => d.Id == document.Id, ct);

        var savedChunks = await verificationDb.DocumentChunks
            .AsNoTracking()
            .Where(c => c.DocumentId == document.Id)
            .OrderBy(c => c.Index)
            .ToListAsync(ct);

        Assert.Equal(DocumentStatus.Ready, savedDocument.Status);
        Assert.Equal(2, savedDocument.ChunkCount);
        Assert.Equal(2, savedChunks.Count);
        Assert.Equal("This is test", savedChunks[0].Content);
        Assert.Equal("document content.", savedChunks[1].Content);
    }



    [Fact]
    public async Task HandleAsync_ShouldMarkDocumentFailed_WhenExtractionFails()
    {
        var ct = TestContext.Current.CancellationToken;

        var document = Document.Create(
            $"failure-{Guid.NewGuid():N}.txt",
            "text/plain",
            100,
            $"test/{Guid.NewGuid():N}.txt");

        // Arrange: persist the document.
        using (var setupScope = fixture.CreateScope())
        {
            var db = setupScope.ServiceProvider
                .GetRequiredService<FileForgeDbContext>();

            db.Documents.Add(document);

            await db.SaveChangesAsync(ct);
        }

        // Act: the extractor throws an exception.
        using (var processingScope = fixture.CreateScope())
        {
            var services = processingScope.ServiceProvider;

            var handler = new ProcessDocumentHandler(
                services.GetRequiredService<IDocumentRepository>(),
                new FakeTextExtractor(
                    exception: new InvalidOperationException(
                        "Simulated extraction failure.")),
                new FakeTextChunker([]),
                new FakeEmbeddingService([]),
                services.GetRequiredService<IChunkRepository>(),
                services.GetRequiredService<IUnitOfWork>(),
                NullLogger<ProcessDocumentHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(document.Id, ct));
        }

        // Assert: the failure status was persisted.
        using var verificationScope = fixture.CreateScope();

        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<FileForgeDbContext>();

        var savedDocument = await verificationDb.Documents
            .AsNoTracking()
            .SingleAsync(d => d.Id == document.Id, ct);

        Assert.Equal(DocumentStatus.Failed, savedDocument.Status);
        Assert.Equal("Document processing failed.", savedDocument.FailureReason);
    }


    [Fact]
    public async Task HandleAsync_ShouldPreserveExistingChunks_WhenReplacementFails()
    {
        var ct = TestContext.Current.CancellationToken;

        var document = Document.Create(
            $"rollback-{Guid.NewGuid():N}.txt",
            "text/plain",
            100,
            $"test/{Guid.NewGuid():N}.txt");

        var originalChunk = DocumentChunk.Create(
            document.Id,
            0,
            "Original chunk",
            2,
            CreateTestEmbedding(0));

        // Arrange: persist the document and original chunk.
        using (var setupScope = fixture.CreateScope())
        {
            var db = setupScope.ServiceProvider
                .GetRequiredService<FileForgeDbContext>();

            db.Documents.Add(document);
            db.DocumentChunks.Add(originalChunk);

            await db.SaveChangesAsync(ct);
        }

        // Act: allow deletion, but fail when replacement chunks are added.
        using (var processingScope = fixture.CreateScope())
        {
            var services = processingScope.ServiceProvider;

            var realChunkRepository =
                services.GetRequiredService<IChunkRepository>();

            var failingChunkRepository = new FailingChunkRepository(realChunkRepository);

            var handler = new ProcessDocumentHandler(
                services.GetRequiredService<IDocumentRepository>(),
                new FakeTextExtractor("Replacement document content."),
                new FakeTextChunker(
                [
                    new TextChunk("Replacement document content.", 3)
                ]),
                new FakeEmbeddingService(
                [
                    CreateTestEmbedding(0)
                ]),
                failingChunkRepository,
                services.GetRequiredService<IUnitOfWork>(),
                NullLogger<ProcessDocumentHandler>.Instance);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(document.Id, ct));

            Assert.Equal(
                "Simulated failure while adding replacement chunks.",
                exception.Message);
        }

        // Assert: inspect persisted state through a fresh DbContext.
        using var verificationScope = fixture.CreateScope();

        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<FileForgeDbContext>();

        var savedDocument = await verificationDb.Documents
            .AsNoTracking()
            .SingleAsync(d => d.Id == document.Id, ct);

        var savedChunks = await verificationDb.DocumentChunks
            .AsNoTracking()
            .Where(c => c.DocumentId == document.Id)
            .ToListAsync(ct);

        Assert.Equal(DocumentStatus.Failed, savedDocument.Status);
        Assert.Single(savedChunks);
        Assert.Equal("Original chunk", savedChunks[0].Content);
    }



    [Fact]
    public async Task HandleAsync_ShouldPreserveExistingChunks_WhenEmbeddingCountDoesNotMatch()
    {
        var ct = TestContext.Current.CancellationToken;

        var document = Document.Create(
            $"embedding-mismatch-{Guid.NewGuid():N}.txt",
            "text/plain",
            100,
            $"test/{Guid.NewGuid():N}.txt");

        var originalChunk = DocumentChunk.Create(
            document.Id,
            0,
            "Original chunk",
            2,
            CreateTestEmbedding(0));

        // Arrange: persist the document and its original chunk.
        using (var setupScope = fixture.CreateScope())
        {
            var db = setupScope.ServiceProvider
                .GetRequiredService<FileForgeDbContext>();

            db.Documents.Add(document);
            db.DocumentChunks.Add(originalChunk);

            await db.SaveChangesAsync(ct);
        }

        // Act: return two text chunks but only one embedding.
        using (var processingScope = fixture.CreateScope())
        {
            var services = processingScope.ServiceProvider;

            var handler = new ProcessDocumentHandler(
                services.GetRequiredService<IDocumentRepository>(),
                new FakeTextExtractor("First chunk. Second chunk."),
                new FakeTextChunker(
                [
                    new TextChunk("First chunk.", 2),
                new TextChunk("Second chunk.", 2)
                ]),
                new FakeEmbeddingService(
                [
                    CreateTestEmbedding(0)
                ]),
                services.GetRequiredService<IChunkRepository>(),
                services.GetRequiredService<IUnitOfWork>(),
                NullLogger<ProcessDocumentHandler>.Instance);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(document.Id, ct));

            Assert.Equal(
                "Expected 2 embeddings, but received 1.",
                exception.Message);
        }

        // Assert: inspect persisted state using a fresh DbContext.
        using var verificationScope = fixture.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<FileForgeDbContext>();

        var savedDocument = await verificationDb.Documents
            .AsNoTracking()
            .SingleAsync(d => d.Id == document.Id, ct);

        var savedChunks = await verificationDb.DocumentChunks
            .AsNoTracking()
            .Where(c => c.DocumentId == document.Id)
            .ToListAsync(ct);

        Assert.Equal(DocumentStatus.Failed, savedDocument.Status);
        Assert.Equal(
            "Document processing failed.",
            savedDocument.FailureReason);

        var remainingChunk = Assert.Single(savedChunks);

        Assert.Equal(document.Id, remainingChunk.DocumentId);
        Assert.Equal(0, remainingChunk.Index);
        Assert.Equal("Original chunk", remainingChunk.Content);
    }


    [Fact]
    public async Task HandleAsync_ShouldThrowNotFound_WhenDocumentDoesNotExist()
    {
        var ct = TestContext.Current.CancellationToken;
        var documentId = Guid.NewGuid();

        var extractor = new FakeTextExtractor();

        using var processingScope = fixture.CreateScope();

        var services = processingScope.ServiceProvider;

        var handler = new ProcessDocumentHandler(
            services.GetRequiredService<IDocumentRepository>(),
            extractor,
            new FakeTextChunker([]),
            new FakeEmbeddingService([]),
            services.GetRequiredService<IChunkRepository>(),
            services.GetRequiredService<IUnitOfWork>(),
            NullLogger<ProcessDocumentHandler>.Instance);

        // Act: request a document ID that was never persisted.
        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.HandleAsync(documentId, ct));

        // Assert: processing never began.
        Assert.False(extractor.WasCalled);
    }

    [Fact]
    public async Task HandleAsync_ShouldExtractAndChunkText_WhenProcessingSucceeds()
    {
        var ct = TestContext.Current.CancellationToken;

        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.txt");

        var text = string.Concat(
            Enumerable.Repeat(
                "FileForge extracts document text and splits it into chunks. ",
                50));

        try
        {
            await File.WriteAllTextAsync(filePath, text, ct);
            var fileSize = new FileInfo(filePath).Length;

            var document = Document.Create(
                "integration-test.txt",
                "text/plain",
                fileSize,
                filePath);

            // Persist the document before processing.
            using (var setupScope = fixture.CreateScope())
            {
                var services = setupScope.ServiceProvider;

                await services
                    .GetRequiredService<IDocumentRepository>()
                    .AddAsync(document, ct);

                await services
                    .GetRequiredService<IUnitOfWork>()
                    .SaveChangesAsync(ct);
            }

            using (var processingScope = fixture.CreateScope())
            {
                var services = processingScope.ServiceProvider;

                var handler = new ProcessDocumentHandler(
                    services.GetRequiredService<IDocumentRepository>(),
                    new PlainTextDocumentExtractor(),
                    new FixedSizeTextChunker(),
                    new TestEmbeddingService(),
                    services.GetRequiredService<IChunkRepository>(),
                    services.GetRequiredService<IUnitOfWork>(),
                    NullLogger<ProcessDocumentHandler>.Instance);

                await handler.HandleAsync(document.Id, ct);
            }

            using var assertionScope = fixture.CreateScope();
            var db = assertionScope.ServiceProvider
                .GetRequiredService<FileForgeDbContext>();

            var savedDocument = await db.Documents
                .AsNoTracking()
                .SingleAsync(d => d.Id == document.Id, ct);

            var savedChunks = await db.DocumentChunks
                .AsNoTracking()
                .Where(c => c.DocumentId == document.Id)
                .OrderBy(c => c.Index)
                .ToListAsync(ct);

            Assert.Equal(DocumentStatus.Ready, savedDocument.Status);
            Assert.True(savedChunks.Count > 1);
            Assert.Equal(savedChunks.Count, savedDocument.ChunkCount);
            Assert.All(
                savedChunks,
                chunk => Assert.False(string.IsNullOrWhiteSpace(chunk.Content)));
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
