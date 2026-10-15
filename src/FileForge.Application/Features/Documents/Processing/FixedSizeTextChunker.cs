using FileForge.Application.Abstractions;

namespace FileForge.Application.Features.Documents.Processing;

public sealed class FixedSizeTextChunker : ITextChunker
{
    private const int ChunkSize = 1000;
    private const int OverlapSize = 150;

    public IReadOnlyList<TextChunk> Chunk(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var chunks = new List<TextChunk>();
        var start = 0;

        while (start < text.Length)
        {
            var end = Math.Min(start + ChunkSize, text.Length);

            if (end < text.Length)
            {
                var boundary = text.LastIndexOf(
                    ' ',
                    end - 1,
                    end - start);

                // Avoid creating a very small chunk just to find whitespace.
                if (boundary > start + ChunkSize / 2)
                {
                    end = boundary;
                }
            }

            var content = text[start..end].Trim();

            if (content.Length > 0)
            {
                var wordCount = content.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries).Length;

                chunks.Add(new TextChunk(content, wordCount));
            }

            if (end >= text.Length)
            {
                break;
            }

            var nextStart = Math.Max(start + 1, end - OverlapSize);

            while (nextStart < end && char.IsWhiteSpace(text[nextStart]))
            {
                nextStart++;
            }

            start = nextStart;
        }

        return chunks;
    }
}