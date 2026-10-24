using FileForge.Application.Features.Documents.GetDocument;
using FileForge.Application.Features.Documents.Search;
using FileForge.Application.Features.Documents.UploadDocument;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileForge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        services.AddScoped<UploadDocumentHandler>();
        services.AddScoped<GetDocumentHandler>();
        services.AddScoped<SearchDocumentsHandler>();

        return services;
    }
}
