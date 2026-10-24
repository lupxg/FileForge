using FileForge.Application.Features.Documents.Processing;


namespace FileForge.IntegrationTests.FileForge.ApplicationTests;

public sealed class FixedSizeTextChunkerTests
{
    private readonly FixedSizeTextChunker _chunker = new();

    [Fact]
    public void Chunk_ShouldReturnEmptyList_WhenTextIsEmpty()
    {
        var result = _chunker.Chunk("");

        Assert.Empty(result);
    }


    [Fact]
    public void Chunk_ShouldReturnEmptyList_WhenTextIsWhitespace()
    {
        var result = _chunker.Chunk("\r\n");

        Assert.Empty(result);
    }


    [Fact]
    public void Chunk_ShouldReturnSingleChunk_WhenTextIsShort()
    {
        const string text = "FileForge processes documents.";

        var result = _chunker.Chunk(text);

        var chunk = Assert.Single(result);

        Assert.Equal(text, chunk.Content);
        Assert.Equal(3, chunk.TokenCount);
    }


    [Fact]
    public void Chunk_ShouldSplitLongText_AndPreserveOverlap()
    {
        var sentence = "FileForge processes documents and stores extracted text. ";
        var text = string.Concat(
            Enumerable.Repeat(sentence, 50));

        var result = _chunker.Chunk(text);

        Assert.True(result.Count > 1);

        foreach (var chunk in result)
        {
            Assert.True(chunk.Content.Length <= 1000);
            Assert.False(string.IsNullOrWhiteSpace(chunk.Content));
        }

        // Consecutive chunks should share text because overlap is enabled.
        for (var i = 0; i < result.Count - 1; i++)
        {
            var current = result[i].Content;
            var next = result[i + 1].Content;

            Assert.True(
                current.Contains(next[..Math.Min(50, next.Length)]) ||
                next.Contains(current[^Math.Min(50, current.Length)..]),
                $"Expected overlapping content between chunks {i} and {i + 1}.");
        }
    }
}
