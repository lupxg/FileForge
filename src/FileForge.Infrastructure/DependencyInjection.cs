using FileForge.Application.Interfaces;
using FileForge.Infrastructure.Persistence;
using FileForge.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileForge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration) 
    {
        var connectionString = configuration.GetConnectionString("FileForgeConnection") 
            ?? throw new InvalidOperationException("Connection string not found");

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChunkRepository, ChunkRepository>();

        services.AddDbContext<FileForgeDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));


        return services;
    }
}
