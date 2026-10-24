using FileForge.Application.Features.Documents.GetDocument;
using FileForge.Application.Features.Documents.Search;
using FileForge.Application.Features.Documents.UploadDocument;

namespace FileForge.Web.Endpoints;

public static class DocumentEndpoints
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public static IEndpointRouteBuilder MapDocumentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {



        endpoints.MapGet("/api/health", () =>
        {
            return Results.Ok(new
            {
                Status = "Healthy",
                Service = "FileForge.Web"
            });
        });



        endpoints.MapPost("/api/documents",
            async (
                IFormFile? file,
                UploadDocumentHandler handler,
                CancellationToken ct) =>
            {
                if (file is null || file.Length == 0)
                {
                    return Results.BadRequest(new
                    {
                        Error = "A non-empty file is required."
                    });
                }

                if (file.Length > MaxFileSizeBytes)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status413PayloadTooLarge,
                        title: "File too large",
                        detail: "The maximum file size is 10 MiB.");
                }

                await using var stream = file.OpenReadStream();

                var document = await handler.HandleAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    ct);

                return Results.Created(
                    $"/api/documents/{document.Id}",
                    new
                    {
                        document.Id,
                        document.FileName,
                        Status = document.Status.ToString(),
                        document.SizeInBytes,
                        document.CreatedAtUtc
                    });
            })
            .WithName("UploadDocument")
            .WithTags("Documents")
            .DisableAntiforgery();




        endpoints.MapGet(
                "/api/documents/{id:guid}",
            async (
                Guid id,
                GetDocumentHandler handler,
                CancellationToken ct) =>
            {
                var document = await handler.HandleAsync(id, ct);

                return document is null
                    ? Results.NotFound(new
                    {
                        Error = "Document not found."
                    })
                    : Results.Ok(document);
            })
            .WithName("GetDocument")
            .WithTags("Documents");



        endpoints.MapGet(
            "/api/search",
            async (
                string? q,
                int? topK,
                SearchDocumentsHandler handler,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return Results.BadRequest(new
                    {
                        Error = "A non-empty search query is required."
                    });
                }

                var resultCount = topK ?? 5;

                if (resultCount is < 1 or > 20)
                {
                    return Results.BadRequest(new
                    {
                        Error = "topK must be between 1 and 20."
                    });
                }

                var results = await handler.HandleAsync(
                    q,
                    resultCount,
                    ct);

                return Results.Ok(new
                {
                    Query = q,
                    Count = results.Count,
                    Results = results
                });
            })
            .WithName("SearchDocuments")
            .WithTags("Search");


        return endpoints;
    }
}