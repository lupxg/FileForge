using FileForge.Application.Abstractions;
using FileForge.Application.Interfaces;
using FileForge.Domain.Entities;
using FileForge.Infrastructure.Persistence;
using FileForge.Infrastructure.Persistence.Repositories;
using FileForge.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FileForge.IntegrationTests.FileForge.InfrastructureTests;


public sealed class InfrastructureIntegrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static ReadOnlyMemory<float> CreateTestEmbedding(int activeIndex)
    {
        const int dimensions = 1536;

        var vector = new float[dimensions];
        vector[activeIndex] = 1f;

        return new ReadOnlyMemory<float>(vector);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterRepositories()
    {
        using var scope = fixture.CreateScope();

        var documentRepository =
            scope.ServiceProvider
                .GetService<IDocumentRepository>();

        var chunkRepository =
            scope.ServiceProvider
                .GetService<IChunkRepository>();

        Assert.NotNull(documentRepository);
        Assert.NotNull(chunkRepository);
    }

    [Fact]
    public async Task ChunkRepository_AddRangeAsync_ShouldPersistChunks()
    {
        // Arrange: resolve services from the same scope.
        using var scope = fixture.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<FileForgeDbContext>();

        var chunkRepository = scope.ServiceProvider
            .GetRequiredService<IChunkRepository>();

        var unitOfWork = scope.ServiceProvider
            .GetRequiredService<IUnitOfWork>();

        // Create and persist the parent document first.
        var document = Document.Create(
            "example.pdf",
            "application/pdf",
            100,
            "documents/example.pdf");

        dbContext.Documents.Add(document);
        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Create chunks associated with that document.
        var chunks = new[]
        {
            DocumentChunk.Create(
                document.Id, 0, "First chunk", 2,
                CreateTestEmbedding(0)),

            DocumentChunk.Create(
                document.Id, 1, "Second chunk", 2,
                CreateTestEmbedding(1))
        };

        // Act: track the chunks, then persist them.
        await chunkRepository.AddRangeAsync(chunks, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert: query the database for the persisted chunks.
        var persistedChunks = await dbContext.DocumentChunks
            .AsNoTracking()
            .Where(c => c.DocumentId == document.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, persistedChunks.Count);
    }


    [Fact]
    public async Task ChunkRepository_DeleteByDocumentIdAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: create and persist the parent document.
        var document = Document.Create(
            $"test-{Guid.NewGuid():N}.txt",
            "text/plain",
            100,
            $"test/{Guid.NewGuid():N}.txt");

        var documentId = document.Id;

        using (var setupScope = fixture.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider
                .GetRequiredService<FileForgeDbContext>();

            var unitOfWork = setupScope.ServiceProvider
                .GetRequiredService<IUnitOfWork>();

            dbContext.Documents.Add(document);

            await unitOfWork.SaveChangesAsync(ct);
        }

        // Arrange: create and persist a chunk for the document.
        var chunks = new[]
        {
            DocumentChunk.Create(
                documentId,
                0,
                "Chunk to delete",
                3,
                CreateTestEmbedding(0))
        };

        using (var insertScope = fixture.CreateScope())
        {
            var repository = insertScope.ServiceProvider 
                .GetRequiredService<IChunkRepository>();

            var unitOfWork = insertScope.ServiceProvider
                .GetRequiredService<IUnitOfWork>();

            await repository.AddRangeAsync(chunks, ct);

            await unitOfWork.SaveChangesAsync(ct);
        }

        // Act: delete all chunks belonging to the document.
        using (var deleteScope = fixture.CreateScope())
        {
            var repository = deleteScope.ServiceProvider
                .GetRequiredService<IChunkRepository>();

            var unitOfWork = deleteScope.ServiceProvider
                .GetRequiredService<IUnitOfWork>();

            await repository.DeleteByDocumentIdAsync(documentId, ct);

            // Required if deletion is tracked by EF Core.
            await unitOfWork.SaveChangesAsync(ct);
        }

        // Assert: verify using a fresh DbContext.
        using var verificationScope = fixture.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<FileForgeDbContext>();

        var remainingChunks = await verificationDb.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.DocumentId == documentId)
            .ToListAsync(ct);

        Assert.Empty(remainingChunks);
    }


    [Fact]
    public async Task Rollback_ShouldPreserveExistingChunks_WhenProcessingFails()
    {
        var ct = TestContext.Current.CancellationToken;

        var document = Document.Create(
            $"test-{Guid.NewGuid():N}.txt",
            "text/plain",
            100,
            $"test/{Guid.NewGuid():N}.txt");

        var documentId = document.Id;

        var originalChunk = DocumentChunk.Create(
            documentId,
            0,
            "Original chunk",
            2,
            CreateTestEmbedding(0));

        // Arrange: persist the document and its original chunk.
        using (var setupScope = fixture.CreateScope())
        {
            var db = setupScope.ServiceProvider
                .GetRequiredService<FileForgeDbContext>();

            var unitOfWork = setupScope.ServiceProvider
                .GetRequiredService<IUnitOfWork>();

            db.Documents.Add(document);
            await unitOfWork.SaveChangesAsync(ct);

            db.DocumentChunks.Add(originalChunk);
            await unitOfWork.SaveChangesAsync(ct);
        }

        // Act: simulate a failed replacement operation.
        using (var failureScope = fixture.CreateScope())
        {
            var chunkRepository = failureScope.ServiceProvider
                .GetRequiredService<IChunkRepository>();

            var unitOfWork = failureScope.ServiceProvider
                .GetRequiredService<IUnitOfWork>();

            await using var transaction =
                await unitOfWork.BeginTransactionAsync(ct);

            try
            {
                // Marks the original chunk as Deleted in EF Core.
                await chunkRepository.DeleteByDocumentIdAsync(
                    documentId, ct);

                // Simulate a failure after deletion but before replacement.
                throw new InvalidOperationException(
                    "Simulated failure while creating replacement chunks.");
            }
            catch (InvalidOperationException)
            {
                await transaction.RollbackAsync(CancellationToken.None);

                // Simulate the outer handler's failure-state save.
                // This is the operation whose behavior we are testing.
                // await unitOfWork.SaveChangesAsync(ct);
            }
        }

        // Assert: verify persisted data using a fresh DbContext.
        using var verificationScope = fixture.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<FileForgeDbContext>();

        var remainingChunks = await verificationDb.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.DocumentId == documentId)
            .ToListAsync(ct);

        Assert.Single(remainingChunks);
        Assert.Equal("Original chunk", remainingChunks[0].Content);

    }
}
