using System;
using System.Collections.Generic;
using System.Text;

namespace FileForge.Application.Features.Documents.Search;


public sealed record SearchResultDto(
    Guid DocumentId,
    int ChunkIndex,
    string Content,
    double Score
);
