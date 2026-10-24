using FileForge.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileForge.Infrastructure.FileProcessing;

public sealed class PlainTextDocumentExtractor : IDocumentTextExtractor
{
    public async Task<string> ExtractAsync(
        string storagePath,
        string contentType,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException("A storage path is required.", nameof(storagePath));
        }

        if (!string.Equals(Path.GetExtension(storagePath), ".txt", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("Only .txt files are supported by this extractor.");
        }

        if (!File.Exists(storagePath))
        {
            throw new FileNotFoundException("The stored document was not found.", storagePath);
        }

        return await File.ReadAllTextAsync(storagePath, Encoding.UTF8, ct);
    }
}
