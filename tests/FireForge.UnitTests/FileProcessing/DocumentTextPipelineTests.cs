using FileForge.Application.Features.Documents.Processing;
using FileForge.Infrastructure.FileProcessing;

namespace FireForge.UnitTests.FileProcessing;

public sealed class DocumentTextPipelineTests
{
    [Fact]
    public async Task ExtractAndChunk_ShouldProduceChunks_FromTextFile()
    {
        // Arrange: create a temporary text file.
        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.txt");

        var sourceText = string.Concat(
            Enumerable.Repeat(
                "FileForge processes documents and extracts their text. ",
                50));

        try
        {
            await File.WriteAllTextAsync(filePath, sourceText);

            var extractor = new PlainTextDocumentExtractor();
            var chunker = new FixedSizeTextChunker();

            // Act: extract text, then split it into chunks.
            var extractedText = await extractor.ExtractAsync(
                filePath,
                "text/plain",
                CancellationToken.None);

            var chunks = chunker.Chunk(extractedText);

            // Assert: extraction preserves the original text.
            Assert.Equal(sourceText, extractedText);

            // Assert: long text produces multiple chunks.
            Assert.True(chunks.Count > 1);

            // Assert: each chunk contains usable text.
            Assert.All(
                chunks,
                chunk => Assert.False(
                    string.IsNullOrWhiteSpace(chunk.Content)));

            // Assert: each chunk respects the configured size.
            Assert.All(
                chunks,
                chunk => Assert.True(chunk.Content.Length <= 1000));

            // Assert: chunk order follows the source text.
            Assert.StartsWith(
                chunks[0].Content,
                extractedText.TrimStart());
        }
        finally
        {
            // Clean up the temporary file even if an assertion fails.
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task ExtractAndChunk_ShouldReturnEmptyChunks_WhenFileIsEmpty()
    {
        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.txt");

        try
        {
            await File.WriteAllTextAsync(filePath, string.Empty);

            var extractor = new PlainTextDocumentExtractor();
            var chunker = new FixedSizeTextChunker();

            var extractedText = await extractor.ExtractAsync(
                filePath,
                "text/plain",
                CancellationToken.None);

            var chunks = chunker.Chunk(extractedText);

            Assert.Equal(string.Empty, extractedText);
            Assert.Empty(chunks);
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
