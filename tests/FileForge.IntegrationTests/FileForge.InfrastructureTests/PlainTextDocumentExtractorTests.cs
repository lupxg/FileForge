using FileForge.Infrastructure.FileProcessing;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileForge.IntegrationTests.FileForge.InfrastructureTests;

public sealed class PlainTextDocumentExtractorTests
{
    private readonly PlainTextDocumentExtractor _extractor = new();


    [Fact]
    public async Task ExtractAsync_ShouldReadTextFromFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");

        const string expected = "FileForge text extraction test.";

        try
        {
            await File.WriteAllTextAsync(path, expected, TestContext.Current.CancellationToken);

            var result = await _extractor.ExtractAsync(path, "text/plain", CancellationToken.None);

            Assert.Equal(expected, result);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task ExtractAsync_ShouldThrow_WhenExtensionIsUnsupported()
    {
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => _extractor.ExtractAsync(
                "document.pdf",
                "application/pdf",
                CancellationToken.None));

        Assert.Contains(".txt", exception.Message);
    }

    [Fact]
    public async Task ExtractAsync_ShouldThrow_WhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _extractor.ExtractAsync(
                path,
                "text/plain",
                CancellationToken.None));
    }
}
