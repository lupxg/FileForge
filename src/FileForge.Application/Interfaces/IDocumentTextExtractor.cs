using System;
using System.Collections.Generic;
using System.Text;

namespace FileForge.Application.Interfaces;

public interface IDocumentTextExtractor
{
    Task<string> ExtractAsync(string storagePath, string contentType, CancellationToken ct);
}
