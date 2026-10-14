
namespace FileForge.Application.Interfaces;

public record TextChunk(string Content, int TokenCount);

public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(string text);
}
